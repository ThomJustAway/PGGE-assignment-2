using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine.Jobs;
using System;
using Patterns;
//using Assets.Experimenting.Scripts.Jobs;
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
    public class FlockBehaviourImproveV2 : Singleton<FlockBehaviourImproveV2>
    {
        
        List<Obstacle> mObstacles = new List<Obstacle>();

        [SerializeField]
        GameObject[] Obstacles;

        [SerializeField]
        public BoxCollider2D Bounds;

        public float TickDuration = 1.0f;
        public float TickDurationSeparationEnemy = 0.1f;
        public float TickDurationRandom = 1.0f;

        public int BoidIncr = 100; //the number of boid to spawn
        public bool useFlocking = false;


        public int BatchSize = 1000;

        [SerializeField] private ComputeShader flockingCalculation;

        public List<Flock> flocks = new List<Flock>();

        #region for job system
        private JobHandle movementJob;
        private TransformAccessArray temporaryTransformStorageContainer;
        private NativeArray<Boid> boidsNativeArray;
        #endregion

        private Queue<Action> addingBoidsCallback;

        
        void Reset()
        {
            boidsNativeArray.Dispose();
            temporaryTransformStorageContainer.Dispose();
            flocks = new List<Flock>() { new Flock() };
        }
        
        void Start()
        {
            addingBoidsCallback = new Queue<Action>();

            SetObstacles();

            foreach (Flock flock in flocks)
            {
                CreateFlock(flock);
            }

            //probably have to change this to something that runs
            //on the worker thread using burst compile plus job system

            StartCoroutine(Coroutine_Random_Motion_Obstacles());

            StartCoroutine(Coroutine_Flocking());
            //StartCoroutine(Coroutine_FlockingVersion2());
            //StartCoroutine(Coroutine_AvoidObstacles());
            //StartCoroutine(Coroutine_Random());
            StartCoroutine(MoveBoids());

            //StartCoroutine(Coroutine_SeparationWithEnemies()); // change this later
        }
        void Update()
        {
            //move the boids here!
            HandleInputs();
            //Rule_CrossBorder();
            Rule_CrossBorder_Obstacles();
        }

        void HandleAddingOfBoids()
        {
            if (addingBoidsCallback.Count > 0)
            {
                var call = addingBoidsCallback.Dequeue();
                call?.Invoke();
            }
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
            flock.boidsTransform = new List<Transform>();
            flock.boidsInformation = new List<Boid>();
            for (int i = 0; i < flock.numBoids; ++i)
            {
                AddBoid(flock);
            }
        }//they are the groups of boid (flock)

        void HandleInputs()
        {
            if (EventSystem.current.IsPointerOverGameObject() ||
                enabled == false)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                addingBoidsCallback.Enqueue(AddBoids);
                //AddBoids(BoidIncr);
            }
        }

        void AddBoids()
        {
            for (int i = 0; i < BoidIncr; ++i) //increase the boids by some constant
            {
                AddBoid(flocks[0]); //only select the first boid to increment
            }
            flocks[0].numBoids += BoidIncr;
        }

        void AddBoid( Flock flock)
        {
            float x = UnityEngine.Random.Range(Bounds.bounds.min.x, Bounds.bounds.max.x);
            float y = UnityEngine.Random.Range(Bounds.bounds.min.y, Bounds.bounds.max.y);

            Vector3 RandomPosition = new Vector3(x, y, 0.0f);
            GameObject obj = Instantiate(flock.PrefabBoid);
            obj.transform.position = RandomPosition;
            obj.name = "Boid_" + flock.name + "_" + flock.boidsTransform.Count;

            var rotation = obj.transform.rotation;
            float4 currentRotation = new float4(rotation.x, rotation.y , rotation.z, rotation.w);

            Boid boid = new Boid( (uint)flock.boidsTransform.Count
                , obj.transform.position,
                GetRandomDirection(),
                currentRotation,
                GetRandomSpeed(flock.maxSpeed)
                );
            //the index of the array is what makes the correlation between the two
            flock.boidsInformation.Add(boid);
            flock.boidsTransform.Add(obj.transform);
        }

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

            return new float3(dir.x,dir.y, 1);
        }

        #region coroutine

        IEnumerator MoveBoids()
        {
            //use unity job system here!.
            while (true)
            {
                foreach(var flock in flocks)
                {
                    HandleAddingOfBoids();

                    //execute movement here
                    temporaryTransformStorageContainer = new TransformAccessArray(flock.boidsTransform.ToArray());
                    boidsNativeArray = new NativeArray<Boid>(flock.boidsInformation.ToArray(), Allocator.TempJob);
                    BoidMovementJob boidMovementJob = new BoidMovementJob()
                    {
                        deltaTime = Time.deltaTime,
                        boidsData = boidsNativeArray,
                        RotationSpeed = flock.rotationSpeed,
                        MaxSpeed = flock.maxSpeed,
                        boxBound = Bounds.bounds,
                        canBounce = flock.bounceWall
                    };

                    movementJob = boidMovementJob.Schedule(temporaryTransformStorageContainer);
                    //schedule the job for the worker threads to complete

                    yield return null;
                    //when the next frame comes
                    movementJob.Complete();

                    temporaryTransformStorageContainer.Dispose();
                    //flock.boidsInformation = boidsNativeArray.ToList();
                    for(int i = 0; i < boidsNativeArray.Length; i++)
                    {
                        //replacing new position for the boids
                        var copyBoid = flock.boidsInformation[i];
                        copyBoid.position = boidsNativeArray[i].position;
                        flock.boidsInformation[i] = copyBoid;
                    }
                    boidsNativeArray.Dispose();
                }
                //yield return new WaitForSeconds(TickDuration);

            }
        }


        //this function is big o of n^2 , n^3 if u consider the excute function
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

                        ComputeBuffer otherBoids = new ComputeBuffer(allTheBoids.Length, Boid.sizeOfData());
                        otherBoids.SetData(allTheBoids);

                        for (uint i = 0; i < allTheBoids.Length; ++i)
                        {
                            var currentBoid = allTheBoids[(int)i];
                            partitionBoidsList.Add(new Boid(
                                i,
                                currentBoid.position,
                                currentBoid.targetDirection,
                                currentBoid.rotation,
                                currentBoid.speed
                                ));

                            //partition to boids by 100s
                            if (i % BatchSize == BatchSize - 1 || 
                                i == allTheBoids.Length - 1) //partition to 100;
                            {
                                //ComputeBuffer otherBoids = new ComputeBuffer(allTheBoids.Length , Boid.sizeOfData());
                                //otherBoids.SetData(allTheBoids);

                                ComputeBuffer currentBathBoids = new ComputeBuffer(partitionBoidsList.Count, Boid.sizeOfData());
                                currentBathBoids.SetData(partitionBoidsList);

                                int kernelIndex = SettingUpComputeShader(flock, allTheBoids.Length, otherBoids, currentBathBoids);

                                //calculate the number of threads required
                                int numberOfThreads = partitionBoidsList.Count % 1000;
                                if (numberOfThreads == 0) numberOfThreads = 1000;

                                flockingCalculation.Dispatch(kernelIndex, numberOfThreads, 1, 1);

                                Boid[] outputContainer = new Boid[partitionBoidsList.Count];
                                currentBathBoids.GetData(outputContainer);

                                foreach (var outputContainerItem in outputContainer)
                                {
                                    var copyBoid = flock.boidsInformation[(int)outputContainerItem.id];
                                    copyBoid.targetDirection = outputContainerItem.targetDirection;
                                    copyBoid.targetSpeed = outputContainerItem.targetSpeed;
                                    flock.boidsInformation[(int)outputContainerItem.id] = copyBoid;

                                    print($" old boid id: {outputContainerItem.id} , " +
                                    $"boid speed {outputContainerItem.targetSpeed}, " +
                                    $"boid target direction {outputContainerItem.targetDirection}" +
                                    $"Boid position {outputContainerItem.position}");
                                    //the old new data is overwritten by the old data
                                }
                                //release the data
                                currentBathBoids.Release();

                                yield return null;
                            }
                        }
                        foreach (var info in flock.boidsInformation)
                        {
                            print($" current boid id: {info.id} , " +
                                $"boid speed {info.targetSpeed}, " +
                                $"boid target direction {info.targetDirection}" +
                                $"Boid position {info.position}");
                        }
                        otherBoids.Release();
                    }
                }
                yield return new WaitForSeconds(TickDuration);
            }
        }

        #region version 2
        IEnumerator MoveBoidsVersion2()
        {
            //use unity job system here!.
            foreach (var flock in flocks)
            {
                HandleAddingOfBoids();

                //execute movement here
                temporaryTransformStorageContainer = new TransformAccessArray(flock.boidsTransform.ToArray());
                boidsNativeArray = new NativeArray<Boid>(flock.boidsInformation.ToArray(), Allocator.TempJob);
                BoidMovementJob boidMovementJob = new BoidMovementJob()
                {
                    deltaTime = Time.deltaTime,
                    boidsData = boidsNativeArray,
                    RotationSpeed = flock.rotationSpeed,
                    MaxSpeed = flock.maxSpeed,
                    boxBound = Bounds.bounds,
                    canBounce = flock.bounceWall
                };

                movementJob = boidMovementJob.Schedule(temporaryTransformStorageContainer);
                //schedule the job for the worker threads to complete

                yield return null;
                //when the next frame comes
                movementJob.Complete();

                temporaryTransformStorageContainer.Dispose();
                //flock.boidsInformation = boidsNativeArray.ToList();
                for (int i = 0; i < boidsNativeArray.Length; i++)
                {
                    Boid copyBoid = flock.boidsInformation[i];
                    copyBoid.position = boidsNativeArray[i].position;//get the updated position
                    flock.boidsInformation[i] = copyBoid;
                }
                boidsNativeArray.Dispose();
            }
            //yield return new WaitForSeconds(TickDuration);

        }
        IEnumerator Coroutine_FlockingVersion2()
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

                        ComputeBuffer otherBoids = new ComputeBuffer(allTheBoids.Length, Boid.sizeOfData());
                        otherBoids.SetData(allTheBoids);

                        for (uint i = 0; i < allTheBoids.Length; ++i)
                        {
                            var currentBoid = allTheBoids[(int)i];
                            partitionBoidsList.Add(new Boid(
                                i,
                                currentBoid.position,
                                currentBoid.targetDirection,
                                currentBoid.rotation,
                                currentBoid.speed
                                ));

                            //partition to boids by 100s
                            if (i % BatchSize == BatchSize - 1 ||
                                i == allTheBoids.Length - 1) //partition to 100;
                            {

                                //ComputeBuffer otherBoids = new ComputeBuffer(allTheBoids.Length , Boid.sizeOfData());
                                //otherBoids.SetData(allTheBoids);

                                ComputeBuffer currentBathBoids = new ComputeBuffer(partitionBoidsList.Count, Boid.sizeOfData());
                                currentBathBoids.SetData(partitionBoidsList);

                                int kernelIndex = SettingUpComputeShader(flock, allTheBoids.Length, otherBoids, currentBathBoids);

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
                                currentBathBoids.Release();

                                yield return MoveBoidsVersion2();
                            }
                        }
                       
                        otherBoids.Release();
                    }
                }
                yield return new WaitForSeconds(TickDuration);
            }
        }
        #endregion

        private int SettingUpComputeShader(Flock flock, int length, ComputeBuffer otherBoids, ComputeBuffer currentBathBoids)
        {
            int kernelIndex = flockingCalculation.FindKernel("CalculatingFlocking");

            flockingCalculation.SetBool("useCohesionRule", flock.useCohesionRule);
            flockingCalculation.SetBool("useAlignmentRule", flock.useAlignmentRule);
            flockingCalculation.SetBool("useSeparationRule", flock.useSeparationRule);
            flockingCalculation.SetBool("useRandomRule", flock.useRandomRule);

            //setting the float
            flockingCalculation.SetFloat("WEIGHT_COHESION", flock.WEIGHT_COHESION);
            flockingCalculation.SetFloat("WEIGHT_SEPERATION", flock.WEIGHT_SEPERATION);
            flockingCalculation.SetFloat("WEIGHT_ALIGNMENT", flock.WEIGHT_ALIGNMENT);
            flockingCalculation.SetFloat("WEIGHT_RANDOM", flock.WEIGHT_RANDOM);

            flockingCalculation.SetFloat("maxSpeed", flock.maxSpeed);
            flockingCalculation.SetFloat("visibility", flock.visibility);
            flockingCalculation.SetFloat("separationDistance", flock.separationDistance);

            flockingCalculation.SetInt("sizeOfFlock", length);

            flockingCalculation.SetBuffer(kernelIndex, "otherBoids", otherBoids);
            flockingCalculation.SetBuffer(kernelIndex, "currentBatch", currentBathBoids);
            return kernelIndex;
        }

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
        //            yield return null;
        //        }
        //        yield return null;
        //    }

        //}
        //void DoAvoidObstacleBehaviour(Flock flock)
        //{
        //    Boid[] boids = flock.boidsInformation.ToArray();
        //    for (int i = 0; i < boids.Length; ++i)
        //    {
        //        for (int j = 0; j < mObstacles.Count; ++j)
        //        {
        //            float dist = (
        //            mObstacles[j].transform.position -
        //            (Vector3)boids[i].position).magnitude;

        //            if (dist < mObstacles[j].AvoidanceRadius)
        //            {
        //                Vector3 targetDirection = (
        //                    (Vector3)boids[i].position -
        //                    mObstacles[j].transform.position).normalized;

        //                boids[i].targetDirection += (float3) targetDirection * flock.WEIGHT_AVOID_OBSTICLES;
        //                boids[i].targetDirection = NormalizeFloat3(boids[i].targetDirection);
        //            }
        //        }
        //    }
        //    flock.boidsInformation = boids.ToList(); //update the list
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

        #region obstacles
        private void Rule_CrossBorder_Obstacles()
        {
            List<Autonomous> listOfObstacle = Obstacles
                .Select(obstacle => { return obstacle.GetComponent<Autonomous>(); }).ToList();

            BounceAutonomous(listOfObstacle);

            //TeleportAutonomoous(listOfObstacle);
        }

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
        #endregion

        private float3 NormalizeFloat3(float3 vector)
        {
            // Convert to Vector3, normalize, and convert back to float3
            return ((Vector3)vector).normalized;
        }
    }

}

