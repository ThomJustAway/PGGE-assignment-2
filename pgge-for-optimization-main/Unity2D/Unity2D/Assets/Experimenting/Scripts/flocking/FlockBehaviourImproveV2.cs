using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Mathematics;
//what does this script current contain

/*
1. Comments about each line of code and how they work
2. Some code restructure for a small performance increase 
    - reducing extern calls like line 528 , 529
3. reduce code repeation through functions for more readibility
*/
//[BurstCompile]
namespace experimenting
{
    //current the performance
    //1000 boids: 89 - 90fps
    //3000 boids: 40 - 46FPS
    //6000 boids: 20 - 29FPS
    public class FlockBehaviourImproveV2 : MonoBehaviour
    {
        List<Obstacle> mObstacles = new List<Obstacle>();

        [SerializeField]
        GameObject[] Obstacles;

        [SerializeField]
        BoxCollider2D Bounds;

        public float TickDuration = 1.0f;
        public float TickDurationSeparationEnemy = 0.1f;
        public float TickDurationRandom = 1.0f;

        public int BoidIncr = 100; //the number of boid to spawn
        public bool useFlocking = false;
        public int BatchSize = 1000;

        [SerializeField] private ComputeShader flockingCalculation;

        public List<Flock> flocks = new List<Flock>();
        void Reset()
        {
            flocks = new List<Flock>() { new Flock() };
        }
        
        void Start()
        {
            SetObstacles();

            foreach (Flock flock in flocks)
            {
                CreateFlock(flock);
            }

            //probably have to change this to something that runs
            //on the worker thread using burst compile plus job system

            StartCoroutine(Coroutine_Flocking());
            StartCoroutine(Coroutine_Random());
            StartCoroutine(Coroutine_AvoidObstacles());
            StartCoroutine(Coroutine_Random_Motion_Obstacles());

            //StartCoroutine(Coroutine_SeparationWithEnemies());
        }

        private void SetObstacles()
        {
            // Randomize obstacles placement.
            for (int i = 0; i < Obstacles.Length; ++i)
            {
                float x = UnityEngine.Random.Range(Bounds.bounds.min.x, Bounds.bounds.max.x);
                float y = UnityEngine.Random.Range(Bounds.bounds.min.y, Bounds.bounds.max.y);
                //get random position

                Obstacles[i].transform.position = new Vector3(x, y, 0.0f); //set position of the obstacles

                Obstacle obs = Obstacles[i].AddComponent<Obstacle>();
                Autonomous autono = Obstacles[i].AddComponent<Autonomous>();
                //add the the obstacle and autonomouse component to the game

                autono.MaxSpeed = 1.0f;
                obs.mCollider = Obstacles[i].GetComponent<CircleCollider2D>();

                mObstacles.Add(obs); //add collider for reference for the boid?
            }
        }

        void CreateFlock(Flock flock)
        {
            flock.boidsGameObject = new List<GameObject>();
            flock.boidsInformation = new List<Boid>();
            for (int i = 0; i < flock.numBoids; ++i)
            {
                float x = UnityEngine.Random.Range(Bounds.bounds.min.x, Bounds.bounds.max.x);
                float y = UnityEngine.Random.Range(Bounds.bounds.min.y, Bounds.bounds.max.y);

                AddBoid(x, y, flock);
            }
        }//they are the groups of boid (flock)

        void Update()
        {
            //move the boids here!
            HandleInputs();
            //Rule_CrossBorder();
            Rule_CrossBorder_Obstacles();
        }

        void HandleInputs()
        {
            if (EventSystem.current.IsPointerOverGameObject() ||
                enabled == false)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                AddBoids(BoidIncr);
            }
        }

        void AddBoids(int count)
        {
            for (int i = 0; i < count; ++i) //increase the boids by some constant
            {
                float x = UnityEngine.Random.Range(Bounds.bounds.min.x, Bounds.bounds.max.x);
                float y = UnityEngine.Random.Range(Bounds.bounds.min.y, Bounds.bounds.max.y);

                AddBoid(x, y, flocks[0]); //only select the first boid to increment
            }
            flocks[0].numBoids += count;
        }

        void AddBoid(float x, float y, Flock flock)
        {
            GameObject obj = Instantiate(flock.PrefabBoid);
            obj.name = "Boid_" + flock.name + "_" + flock.boidsGameObject.Count;
            obj.transform.position = new Vector3(x, y, 0.0f);
            Boid boid = new Boid( (uint)flock.boidsGameObject.Count
                , obj.transform.position,
                GetRandomDirection(),
                GetRandomSpeed(flock.maxSpeed));

            //the index of the array is what makes the correlation between the two
            flock.boidsInformation.Add(boid);
            flock.boidsGameObject.Add(obj);
            //Autonomous boid = obj.GetComponent<Autonomous>();


            //boid.RotationSpeed = flock.rotationSpeed;
        }

        float GetRandomSpeed(float MaxSpeed)
        {
            return UnityEngine.Random.Range(0.0f, MaxSpeed);
        }

        float3 GetRandomDirection()
        {
            float angle = UnityEngine.Random.Range(-180.0f, 180.0f);
            Vector2 dir = new Vector2(Mathf.Cos(Mathf.Deg2Rad * angle), Mathf.Sin(Mathf.Deg2Rad * angle)); //make it face a certain direction
            dir.Normalize();

            return new float3(dir.x,dir.y, 1);
        }

        static float Distance(Autonomous a1, Autonomous a2)
        {
            return (a1.transform.position - a2.transform.position).magnitude;
        }

        #region coroutine
        //this function is big o of n^2 , n^3 if u consider the excute function

        //IEnumerator MoveBoids()
        //{
        //    //use unity job system here!.
        //}

        IEnumerator Coroutine_Flocking()
        {
            while (true)
            {
                if (useFlocking)
                {
                    foreach (Flock flock in flocks)
                    {
                        //store the rules into data for compute shader
                        Boid[] allTheBoids = flock.boidsInformation.ToArray();

                       

                        List<Boid> partitionBoidsList = new List<Boid>();

                        for (uint i = 0; i < allTheBoids.Length; ++i)
                        {
                            var currentBoid = allTheBoids[(int)i];
                            partitionBoidsList.Add(new Boid(
                                i,
                                currentBoid.position,
                                currentBoid.targetDirection,
                                currentBoid.speed
                                ));

                            //partition to boids by 100s
                            if (i % BatchSize == BatchSize - 1 || 
                                i == allTheBoids.Length - 1) //partition to 100;
                            {
                                ComputeBuffer otherBoids = new ComputeBuffer(allTheBoids.Length , Boid.sizeOfData());
                                otherBoids.SetData(allTheBoids);

                                ComputeBuffer currentBathBoids = new ComputeBuffer(partitionBoidsList.Count, Boid.sizeOfData());
                                currentBathBoids.SetData(partitionBoidsList);

                                int kernelIndex = flockingCalculation.FindKernel("CalculatingFlocking");

                                flockingCalculation.SetBuffer(kernelIndex, "otherBoids", otherBoids);
                                flockingCalculation.SetBuffer(kernelIndex, "currentBatch", currentBathBoids);
                                //setting the float
                                flockingCalculation.SetFloat("WEIGHT_COHESION" , flock.WEIGHT_COHESION);
                                flockingCalculation.SetFloat("WEIGHT_SEPERATION", flock.WEIGHT_SEPERATION);
                                flockingCalculation.SetFloat("WEIGHT_ALIGNMENT", flock.WEIGHT_ALIGNMENT);
                                flockingCalculation.SetFloat("visibility", flock.visibility);
                                flockingCalculation.SetFloat("separationDistance", flock.separationDistance);
                                flockingCalculation.SetFloat("sizeOfFlock", allTheBoids.Length);

                                flockingCalculation.SetBool("useCohesionRule", flock.useCohesionRule);
                                flockingCalculation.SetBool("useAlignmentRule", flock.useAlignmentRule);
                                flockingCalculation.SetBool("useSeparationRule", flock.useSeparationRule);

                                //calculate the number of threads required
                                int numberOfThreads = partitionBoidsList.Count % 1000;
                                if (numberOfThreads == 0) numberOfThreads = 1000;

                                flockingCalculation.Dispatch(kernelIndex, numberOfThreads, 1, 1);

                                Boid[] outputContainer = new Boid[partitionBoidsList.Count];
                                currentBathBoids.GetData(outputContainer);

                                foreach (var outputContainerItem in outputContainer)
                                {
                                    
                                    flock.boidsInformation[(int)outputContainerItem.id] = outputContainerItem;
                                }
                                //release the data
                                otherBoids.Release();
                                currentBathBoids.Release();
                                yield return null;
                            }
                            
                        }
                        yield return null;
                    }
                }
                yield return new WaitForSeconds(TickDuration);
            }
        }
        //void Execute(Flock flock, int i)
        //{
        //    Vector3 flockDir = Vector3.zero;
        //    Vector3 separationDir = Vector3.zero;
        //    //Vector3 cohesionDir = Vector3.zero;

        //    float speed = 0.0f;
        //    float separationSpeed = 0.0f;

        //    int count = 0;

        //    Vector3 steerPos = Vector3.zero;

        //    Autonomous curr = flock.mAutonomous[i];

        //    for (int j = 0; j < flock.numBoids; ++j)
        //    {
        //        //in this for loop, it will go through all the 
        //        //boid in the game and see if there is any boids
        //        //that is close to the selected boids

        //        if (i == j) continue; //if not the same then move on

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
        IEnumerator Coroutine_Random()
        {
            while (true)
            {
                foreach (Flock flock in flocks)
                {
                    if (flock.useRandomRule)
                    {
                        DoRandomFlockBehaviour(flock);
                    }
                    //yield return null;
                }
                yield return new WaitForSeconds(TickDurationRandom);
            }
        }
        void DoRandomFlockBehaviour(Flock flock)
        {
            //List<Autonomous> autonomousList = flock.mAutonomous;
            Boid[] boids = flock.boidsInformation.ToArray();
            for (int i = 0; i < boids.Length; ++i)
            {
                Boid currentBoid = boids[i]; //make a copy
                float rand = UnityEngine.Random.Range(0.0f, 1.0f);
                currentBoid.targetDirection = NormalizeFloat3(currentBoid.targetDirection);
                float angle = Mathf.Atan2(currentBoid.targetDirection.y, currentBoid.targetDirection.x);

                if (rand > 0.5f)
                {
                    angle += Mathf.Deg2Rad * 45.0f;
                }
                else
                {
                    angle -= Mathf.Deg2Rad * 45.0f;
                }

                Vector3 dir = Vector3.zero;
                dir.x = Mathf.Cos(angle);
                dir.y = Mathf.Sin(angle);

                currentBoid.targetDirection += (float3)dir * flock.WEIGHT_RANDOM;
                currentBoid.targetDirection = NormalizeFloat3(boids[i].targetDirection);
                //Debug.Log(autonomousList[i].TargetDirection);

                float speed = UnityEngine.Random.Range(1.0f, flock.maxSpeed);
                currentBoid.targetSpeed += speed * flock.WEIGHT_SEPERATION;
                currentBoid.targetSpeed /= 2.0f;
                //average the speed for the boid

                boids[i] = currentBoid;
            }
        }

        IEnumerator Coroutine_AvoidObstacles()
        {
            while (true)
            {
                foreach (Flock flock in flocks)
                {
                    if (flock.useAvoidObstaclesRule)
                    {
                        DoAvoidObstacleBehaviour(flock);
                    }
                    //yield return null;
                }
                yield return null;
            }

        }
        void DoAvoidObstacleBehaviour(Flock flock)
        {
            Boid[] boids = flock.boidsInformation.ToArray();
            for (int i = 0; i < boids.Length; ++i)
            {
                for (int j = 0; j < mObstacles.Count; ++j)
                {
                    float dist = (
                    mObstacles[j].transform.position -
                    (Vector3)boids[i].position).magnitude;

                    if (dist < mObstacles[j].AvoidanceRadius)
                    {
                        Vector3 targetDirection = (
                            (Vector3)boids[i].position -
                            mObstacles[j].transform.position).normalized;

                        boids[i].targetDirection += (float3) targetDirection * flock.WEIGHT_AVOID_OBSTICLES;
                        boids[i].targetDirection = NormalizeFloat3(boids[i].targetDirection);
                    }
                }
            }
        }

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

        IEnumerator Coroutine_Random_Motion_Obstacles()
        {
            while (true)
            {
                for (int i = 0; i < Obstacles.Length; ++i)
                {
                    AddRandomMotionToObstacles(i);
                }
                yield return new WaitForSeconds(2.0f);
            }
        }

        private void AddRandomMotionToObstacles(int i)
        {
            Autonomous autono = Obstacles[i].GetComponent<Autonomous>();
            float rand = UnityEngine.Random.Range(0.0f, 1.0f);
            autono.TargetDirection.Normalize();
            float angle = Mathf.Atan2(autono.TargetDirection.y, autono.TargetDirection.x);

            if (rand > 0.5f)
            {
                angle += Mathf.Deg2Rad * 45.0f;
            }
            else
            {
                angle -= Mathf.Deg2Rad * 45.0f;
            }
            Vector3 dir = Vector3.zero;
            dir.x = Mathf.Cos(angle);
            dir.y = Mathf.Sin(angle);

            autono.TargetDirection += dir * 0.1f;
            autono.TargetDirection.Normalize();
            //Debug.Log(autonomousList[i].TargetDirection);

            float speed = UnityEngine.  Random.Range(1.0f, autono.MaxSpeed);
            autono.TargetSpeed += speed;
            autono.TargetSpeed /= 2.0f;
        }
        #endregion

        private void SeparationWithEnemies_Internal(
        List<Autonomous> boids,
        List<Autonomous> enemies,
        float sepDist,
        float sepWeight)
        {
            for (int i = 0; i < boids.Count; ++i)
            {
                for (int j = 0; j < enemies.Count; ++j)
                {
                    float dist = (
                        enemies[j].transform.position -
                        boids[i].transform.position).magnitude;
                    if (dist < sepDist)
                    {
                        Vector3 targetDirection = (
                            boids[i].transform.position -
                            enemies[j].transform.position).normalized;

                        boids[i].TargetDirection += targetDirection;
                        boids[i].TargetDirection.Normalize();

                        boids[i].TargetSpeed += dist * sepWeight;
                        boids[i].TargetSpeed /= 2.0f;
                    }
                }
            }
        }

        private void Rule_CrossBorder_Obstacles()
        {
            List<Autonomous> listOfObstacle = Obstacles
                .Select(obstacle => { return obstacle.GetComponent<Autonomous>(); }).ToList();

            BounceAutonomous(listOfObstacle);

            //TeleportAutonomoous(listOfObstacle);
        }

        //add this to the job system later
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

        private void TeleportAutonomous(List<Autonomous> autonomousList)
        {
            for (int i = 0; i < autonomousList.Count; ++i)
            {
                //reduce extern calls
                Vector3 pos = autonomousList[i].transform.position;
                Bounds boxBound = Bounds.bounds;

                if (pos.x > boxBound.max.x)
                {
                    //teleport boid to the left side of the map
                    pos.x = Bounds.bounds.min.x;
                }
                else if (pos.x < boxBound.min.x)
                {
                    //teleport boid to the right side of the map
                    pos.x = Bounds.bounds.max.x;
                }

                if (pos.y > boxBound.max.y)
                {
                    //teleport boid to the bottom of the map
                    pos.y = Bounds.bounds.min.y;
                }
                else if (pos.y < boxBound.min.y)
                {
                    //teleport boid to the top of the map
                    pos.y = Bounds.bounds.max.y;
                }
                autonomousList[i].transform.position = pos;
            }
        }

        private void BounceAutonomous(List<Autonomous> autonomousList)
        {
            for (int i = 0; i < autonomousList.Count; ++i)
            {
                Vector3 pos = autonomousList[i].transform.position;
                //reduce extern call
                Transform currentTransform = autonomousList[i].transform;
                Bounds boxBound = Bounds.bounds;

                //for horizontal bounds
                if (currentTransform.position.x + 5.0f > boxBound.max.x)
                {
                    //if near the right bound box, force it to go left
                    autonomousList[i].TargetDirection.x = -1.0f;
                }
                else if (currentTransform.position.x - 5.0f < boxBound.min.x)
                {
                    //if near the left bound box, force it to go right
                    autonomousList[i].TargetDirection.x = 1.0f;
                }
                //for vectical bounds
                if (currentTransform.position.y + 5.0f > boxBound.max.y)
                {
                    //if near the top bound box, force it to go down
                    autonomousList[i].TargetDirection.y = -1.0f;
                }
                else if (currentTransform.position.y - 5.0f < boxBound.min.y)
                {
                    //if near the bottom bound box, force it to go up
                    autonomousList[i].TargetDirection.y = 1.0f;
                }
                autonomousList[i].TargetDirection.Normalize();
            }
        }

        private float3 NormalizeFloat3(float3 vector)
        {
            // Convert to Vector3, normalize, and convert back to float3
            return ((Vector3)vector).normalized;
        }
    }

}

