using System.Collections;
using UnityEngine;

namespace experimenting2
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.Jobs;

    public class FlockBehaviourV3A1 : MonoBehaviour
    {
        List<CustomObstacles> mObstacles = new List<CustomObstacles>();

        [SerializeField]
        GameObject[] Obstacles;

        [SerializeField]
        BoxCollider2D Bounds;

        public float TickDuration = 1.0f;
        public float TickDurationSeparationEnemy = 0.1f;
        public float TickDurationRandom = 1.0f;

        public int BoidIncr = 100; //the number of boid to spawn
        public bool useFlocking = false;
        public int BatchSize = 100;

        public List<Flock> flocks = new List<Flock>();
        public NativeArray<BoidsObstacle> nativeContainerObstacles;
        public NativeList<MovementObject> nativeContainerPredatorBoids;
        private Queue<Action> addBoidsCallBack = new Queue<Action>();

        void Reset()
        {
            ClearMemory();
            flocks = new List<Flock>() { new Flock() };
        }

        private void OnDestroy()
        {
            ClearMemory();
            flocks = new List<Flock>() { new Flock() };

        }

        private void OnDisable()
        {
            ClearMemory();
            flocks = new List<Flock>() { new Flock() };
        }

        void Start()
        {
            SetObstacles();
            CreateFlock();
            StartCoroutine(CoroutineMovingBoids());

            //StartCoroutine(Coroutine_Random());
            //StartCoroutine(Coroutine_AvoidObstacles());
            //StartCoroutine(Coroutine_SeparationWithEnemies());
            //StartCoroutine(Coroutine_Random_Motion_Obstacles());
        }

        void Update()
        {
            HandleInputs();
            //Rule_CrossBorder();
            //Rule_CrossBorder_Obstacles();
        }

        #region starting functions
        private void SetObstacles()
        {
            // Randomize obstacles placement.
            for (int i = 0; i < Obstacles.Length; ++i)
            {
                float x = UnityEngine.Random.Range(Bounds.bounds.min.x, Bounds.bounds.max.x);
                float y = UnityEngine.Random.Range(Bounds.bounds.min.y, Bounds.bounds.max.y);
                //get random position

                Obstacles[i].transform.position = new Vector3(x, y, 0.0f); //set position of the obstacles

                CustomObstacles obs = Obstacles[i].AddComponent<CustomObstacles>();
                Autonomous autono = Obstacles[i].AddComponent<Autonomous>();
                //add the the obstacle and autonomouse component to the game

                autono.MaxSpeed = 1.0f;
                obs.mCollider = Obstacles[i].GetComponent<CircleCollider2D>();

                mObstacles.Add(obs); //add collider for reference for the boid?
            }
        }



        void CreateFlock()
        {
            foreach (Flock flock in flocks)
            {
                flock.nativeMovementObjects = new NativeList<MovementObject>(Allocator.Persistent);
                for (int i = 0; i < flock.numBoids; ++i)
                {
                    AddBoid(flock);
                }
            }

        }//they are the groups of boid (flock)

        #endregion
        #region boids related
        void AddBoids()
        {
            for (int i = 0; i < BoidIncr; ++i)
            {//ignore the predator boids and just add normal boids
                AddBoid(flocks[0]);
            }
            flocks[0].numBoids += BoidIncr;
        }

        void AddBoid(Flock flock)
        {
            float x = UnityEngine.Random.Range(Bounds.bounds.min.x, Bounds.bounds.max.x);
            float y = UnityEngine.Random.Range(Bounds.bounds.min.y, Bounds.bounds.max.y);
            GameObject obj = Instantiate(flock.PrefabBoid);
            obj.name = "Boid_" + flock.name + "_" + flock.transforms.Count;
            var newPosition = new Vector3(x, y, 0.0f);
            obj.transform.position = newPosition;

            flock.nativeMovementObjects.Add(new MovementObject(
                (uint) flock.transforms.Count,
                GetRandomDirection(),
                GetRandomSpeed(flock.rules.maxSpeed),
                (float3) newPosition
                ));
            flock.transforms.Add(obj.transform);
        }
        #endregion
        void HandleInputs()
        {
            if (EventSystem.current.IsPointerOverGameObject() ||
                enabled == false)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                //AddBoids();
                addBoidsCallBack.Enqueue(AddBoids);
            }
        }

        #region new coroutine

        IEnumerator CoroutineMovingBoids()
        {
            while (true)
            {
                if (useFlocking)
                {
                    ClearMemory();
                    AddBoidsFromCallback();
                    SettingUpPredatorBoids();
                    SettingUpObstacle();
                    foreach (Flock flock in flocks)
                    {
                        StartingJob(flock);
                    }
                }
                yield return null;
                //now just 

                foreach(var flock  in flocks)
                {
                    flock.job.Complete();
                    UpdateBoidsData(flock);
                }
            }
        }
        private void ClearMemory()
        {
            //release memory so that it can be used for the next section
            if (nativeContainerObstacles.IsCreated) nativeContainerObstacles.Dispose();
            if(nativeContainerPredatorBoids.IsCreated) nativeContainerPredatorBoids.Dispose();
            foreach(var flock in flocks)
            {
                if (flock.nativeTransformAccessArray.isCreated) flock.nativeTransformAccessArray.Dispose();
                if (flock.NativeOutputMovementObjects.IsCreated) flock.NativeOutputMovementObjects.Dispose();
            }
        }
        private void SettingUpObstacle()
        {
            nativeContainerObstacles = new NativeArray<BoidsObstacle>(Obstacles.Length, Allocator.TempJob);

            for (int i = 0; i < mObstacles.Count; i++)
            {
                nativeContainerObstacles[i] = mObstacles[i].obstacle;

                var currentObstacle = mObstacles[i].obstacle;
            }
        }
        private void UpdateBoidsData(Flock flock)
        {
            for (int i = 0; i < flock.transforms.Count; i++)
            {
                //replace the new result back to the list

                var boidResult = flock.NativeOutputMovementObjects[i];
                boidResult.position = flock.transforms[i].position;
                flock.nativeMovementObjects[i] = boidResult;
                //just update the position of the boid
            }
        }
        private void StartingJob(Flock flock)
        {
     

            //fill up the native array with the obstacles for used
            flock.NativeOutputMovementObjects = new NativeArray<MovementObject>(flock.nativeMovementObjects.Capacity, Allocator.TempJob);
            BoidsFlockingMovement calculatingFlockingMovementJob = new BoidsFlockingMovement()
            {
                AllTheBoids = flock.nativeMovementObjects,
                rules = flock.rules,
                output = flock.NativeOutputMovementObjects,
                obstacles = nativeContainerObstacles,
                boxBound = Bounds.bounds,
                predatorBoids = nativeContainerPredatorBoids
            };

            MovingMovementObject movingBoidJob = new MovingMovementObject()
            {
                boidsData = flock.NativeOutputMovementObjects,
                boxBound = Bounds.bounds,
                deltaTime = Time.deltaTime,
                rulesData = flock.rules,

            };

            flock.nativeTransformAccessArray = new TransformAccessArray(flock.transforms.ToArray());

            JobHandle CalculatingJob = calculatingFlockingMovementJob.Schedule(flock.transforms.Count, 1);
            JobHandle movingJob = movingBoidJob.Schedule(flock.nativeTransformAccessArray, CalculatingJob);

            flock.job = movingJob;
            //dont need it anymore now so just dispose it
        }

        private void SettingUpPredatorBoids()
        {
            nativeContainerPredatorBoids = new NativeList<MovementObject>(Allocator.TempJob);
            foreach(var flock in flocks)
            {
                if (flock.rules.isPredator)
                {
                    foreach(var boid in flock.nativeMovementObjects)
                    {
                        nativeContainerPredatorBoids.Add(boid);
                    }
                }
            }
        }

        private void AddBoidsFromCallback()
        {
            if(addBoidsCallBack.Count > 0)
            {
                addBoidsCallBack.Dequeue().Invoke();
            }
        }





        #endregion

        #region coroutine legacy
        //this function is big o of n^2 , n^3 if u consider the excute function
        //IEnumerator Coroutine_Flocking()
        //{
        //    while (true)
        //    {
        //        if (useFlocking)
        //        {
        //            foreach (Flock flock in flocks)
        //            {
        //                List<Autonomous> autonomousList = flock.mAutonomous;
        //                //get all the boid from the flock
        //                for (int i = 0; i < autonomousList.Count; ++i)
        //                {
        //                    Execute(flock, i);
        //                    if (i % BatchSize == 0)
        //                    {
        //                        //yield back if it done more than a 100 boids
        //                        yield return null;
        //                    }
        //                }
        //                yield return null;
        //            }
        //        }
        //        yield return new WaitForSeconds(TickDuration);
        //    }
        //}

        //void Execute(Flock flock, int i)
        //{
        //    Vector3 flockDir = Vector3.zero;
        //    Vector3 separationDir = Vector3.zero;
        //    //Vector3 cohesionDir = Vector3.zero;

        //    float speed = 0.0f;
        //    float separationSpeed = 0.0f;

        //    int count = 0;
        //    int separationCount = 0;

        //    Vector3 steerPos = Vector3.zero;

        //    Autonomous curr = flock.mAutonomous[i];

        //    for (int j = 0; j < flock.numBoids; ++j)
        //    {
        //        //in this for loop, it will go through all the 
        //        //boid in the game and see if there is any boids
        //        //that is close to the selected boids

        //        if (i != j) continue; //if not the same then move on

        //        Autonomous other = flock.mAutonomous[j];
        //        float dist = (curr.transform.position - other.transform.position).magnitude;

        //        if (dist < flock.visibility)
        //        { //if it is around the current boid visible range (circle)
        //            speed += other.Speed;
        //            flockDir += other.TargetDirection;
        //            steerPos += other.transform.position;
        //            count++; //use this count to find the average 
        //        }
        //        if (dist < flock.separationDistance)
        //        {//if the distance is lesser than the acceptable range

        //            //usually this if statement is true so you
        //            //can remove this all together

        //            Vector3 targetDirection = (
        //            curr.transform.position -
        //            other.transform.position).normalized;
        //            //get direction vector from other to current

        //            separationDir += targetDirection;
        //            separationSpeed += dist * flock.WEIGHT_SEPERATION;
        //            //how much needs to be seperated base on the distance
        //            //this formula can be tweak where the shorter the distance
        //            //the more speed required to seperate the boids
        //        }
        //    }

        //    if (count > 0)
        //    {
        //        speed = speed / count;
        //        flockDir = flockDir / count;
        //        //getting the average speed and direction the flock needs to go

        //        flockDir.Normalize();

        //        steerPos = steerPos / count;
        //        //finding the average position that the flock is going
        //    }

        //    //code below is redundant as separation count is always 0
        //    //could open it again if u want to add separation count to the game
        //    //if (separationCount > 0)
        //    //    print("hello");
        //    //    separationSpeed = separationSpeed / count;
        //    //    separationDir = separationDir / separationSpeed;
        //    //    separationDir.Normalize();
        //    //}

        //    Vector3 flockDirection = (steerPos - curr.transform.position) *
        //        (flock.useCohesionRule ? flock.WEIGHT_COHESION : 0.0f);

        //    /*
        //    get the direction of the flock intended outcome
        //    if the cohesion is needed, then multiply it with
        //    the weight, else just ignore it
        //    */

        //    Vector3 separationDirection = separationDir * separationSpeed *
        //        (flock.useSeparationRule ? flock.WEIGHT_SEPERATION : 0.0f);
        //    /*
        //     Get the seperation direction need to seperate from the boid nearby
        //     This direction would be ignored if there is no seperation rule is not stated
        //     */

        //    Vector3 alignmentDirection = flockDir * speed *
        //        (flock.useAlignmentRule ? flock.WEIGHT_ALIGNMENT : 0.0f);
        //    /*
        //     Where the boid intended wants to go. 
        //     */


        //    curr.TargetDirection = alignmentDirection +
        //        separationDirection +
        //        flockDirection;
        //    //add this together to form the final direction needed for the flock.

        //}

        //IEnumerator Coroutine_Random()
        //{
        //    while (true)
        //    {
        //        foreach (Flock flock in flocks)
        //        {
        //            if (flock.useRandomRule)
        //            {
        //                DoRandomFlockBehaviour(flock);
        //            }
        //            //yield return null;
        //        }
        //        yield return new WaitForSeconds(TickDurationRandom);
        //    }
        //}

        //void DoRandomFlockBehaviour(Flock flock)
        //{
        //    List<Autonomous> autonomousList = flock.mAutonomous;
        //    for (int i = 0; i < autonomousList.Count; ++i)
        //    {
        //        float rand = UnityEngine.Random.Range(0.0f, 1.0f);
        //        autonomousList[i].TargetDirection.Normalize();
        //        float angle = Mathf.Atan2(autonomousList[i].TargetDirection.y, autonomousList[i].TargetDirection.x);

        //        if (rand > 0.5f)
        //        {
        //            angle += Mathf.Deg2Rad * 45.0f;
        //        }
        //        else
        //        {
        //            angle -= Mathf.Deg2Rad * 45.0f;
        //        }

        //        Vector3 dir = Vector3.zero;
        //        dir.x = Mathf.Cos(angle);
        //        dir.y = Mathf.Sin(angle);

        //        autonomousList[i].TargetDirection += dir * flock.WEIGHT_RANDOM;
        //        autonomousList[i].TargetDirection.Normalize();
        //        //Debug.Log(autonomousList[i].TargetDirection);

        //        float speed = UnityEngine.Random.Range(1.0f, autonomousList[i].MaxSpeed);
        //        autonomousList[i].TargetSpeed += speed * flock.WEIGHT_SEPERATION;
        //        autonomousList[i].TargetSpeed /= 2.0f;
        //        //average the speed for the boid
        //    }
        //}

        //IEnumerator Coroutine_AvoidObstacles()
        //{
        //    while (true)
        //    {
        //        foreach (Flock flock in flocks)
        //        {
        //            if (flock.useAvoidObstaclesRule)
        //            {
        //                DoAvoidObstacleBehaviour(flock);
        //            }
        //            //yield return null;
        //        }
        //        yield return null;
        //    }

        //}
        //void DoAvoidObstacleBehaviour(Flock flock)
        //{
        //    List<Autonomous> autonomousList = flock.mAutonomous;
        //    for (int i = 0; i < autonomousList.Count; ++i)
        //    {
        //        for (int j = 0; j < mObstacles.Count; ++j)
        //        {
        //            float dist = (
        //            mObstacles[j].transform.position -
        //            autonomousList[i].transform.position).magnitude;
        //            if (dist < mObstacles[j].AvoidanceRadius)
        //            {
        //                Vector3 targetDirection = (
        //                    autonomousList[i].transform.position -
        //                    mObstacles[j].transform.position).normalized;

        //                autonomousList[i].TargetDirection += targetDirection * flock.WEIGHT_AVOID_OBSTICLES;
        //                autonomousList[i].TargetDirection.Normalize();
        //            }
        //        }
        //    }
        //}

        //IEnumerator Coroutine_SeparationWithEnemies()
        //{
        //    while (true)
        //    {
        //        foreach (Flock flock in flocks)
        //        {
        //            if (!flock.useFleeOnSightEnemyRule || flock.isPredator) continue;
        //            //ignore this if does not have the flee on sight enemy rule 
        //            //or the flock is a predator
        //            foreach (Flock enemies in flocks)
        //            {
        //                if (!enemies.isPredator) continue;

        //                SeparationWithEnemies_Internal(
        //                flock.mAutonomous,
        //                enemies.mAutonomous,
        //                flock.enemySeparationDistance,
        //                flock.WEIGHT_FLEE_ENEMY_ON_SIGHT);
        //            }
        //        }
        //        yield return null;
        //    }
        //}

        //IEnumerator Coroutine_Random_Motion_Obstacles()
        //{
        //    while (true)
        //    {
        //        for (int i = 0; i < Obstacles.Length; ++i)
        //        {
        //            AddRandomMotionToObstacles(i);
        //        }
        //        yield return new WaitForSeconds(2.0f);
        //    }
        //}

        //private void AddRandomMotionToObstacles(int i)
        //{
        //    Autonomous autono = Obstacles[i].GetComponent<Autonomous>();
        //    float rand = Random.Range(0.0f, 1.0f);
        //    autono.TargetDirection.Normalize();
        //    float angle = Mathf.Atan2(autono.TargetDirection.y, autono.TargetDirection.x);

        //    if (rand > 0.5f)
        //    {
        //        angle += Mathf.Deg2Rad * 45.0f;
        //    }
        //    else
        //    {
        //        angle -= Mathf.Deg2Rad * 45.0f;
        //    }
        //    Vector3 dir = Vector3.zero;
        //    dir.x = Mathf.Cos(angle);
        //    dir.y = Mathf.Sin(angle);

        //    autono.TargetDirection += dir * 0.1f;
        //    autono.TargetDirection.Normalize();
        //    //Debug.Log(autonomousList[i].TargetDirection);

        //    float speed = Random.Range(1.0f, autono.MaxSpeed);
        //    autono.TargetSpeed += speed;
        //    autono.TargetSpeed /= 2.0f;
        //}

        //private void SeparationWithEnemies_Internal(
        //List<Autonomous> boids,
        //List<Autonomous> enemies,
        //float sepDist,
        //float sepWeight)
        //{
        //    for (int i = 0; i < boids.Count; ++i)
        //    {
        //        for (int j = 0; j < enemies.Count; ++j)
        //        {
        //            float dist = (
        //                enemies[j].transform.position -
        //                boids[i].transform.position).magnitude;
        //            if (dist < sepDist)
        //            {
        //                Vector3 targetDirection = (
        //                    boids[i].transform.position -
        //                    enemies[j].transform.position).normalized;

        //                boids[i].TargetDirection += targetDirection;
        //                boids[i].TargetDirection.Normalize();

        //                boids[i].TargetSpeed += dist * sepWeight;
        //                boids[i].TargetSpeed /= 2.0f;
        //            }
        //        }
        //    }
        //}
        //#endregion


        //private void Rule_CrossBorder_Obstacles()
        //{
        //    List<Autonomous> listOfObstacle = Obstacles
        //        .Select(obstacle => { return obstacle.GetComponent<Autonomous>(); }).ToList();

        //    BounceAutonomous(listOfObstacle);

        //    //TeleportAutonomoous(listOfObstacle);
        //}

        //private void Rule_CrossBorder()
        //{
        //    foreach (Flock flock in flocks)
        //    {
        //        List<Autonomous> autonomousList = flock.mAutonomous;
        //        if (flock.bounceWall)
        //        {
        //            BounceAutonomous(autonomousList);
        //        }
        //        else
        //        {
        //            TeleportAutonomous(autonomousList);
        //        }
        //    }
        //}
        #endregion

        #region extra methods

        float GetRandomSpeed(float MaxSpeed)
        {
            //return UnityEngine.Random.Range(0.0f, MaxSpeed);
            return MaxSpeed;
        }

        float3 GetRandomDirection()
        {
            float angle = UnityEngine.Random.Range(-180.0f, 180.0f);
            Vector2 dir = new Vector2(Mathf.Cos(Mathf.Deg2Rad * angle), Mathf.Sin(Mathf.Deg2Rad * angle)); //make it face a certain direction
            dir.Normalize();

            return new float3(dir.x, dir.y, 1);
        }
 

        #endregion

    }


}