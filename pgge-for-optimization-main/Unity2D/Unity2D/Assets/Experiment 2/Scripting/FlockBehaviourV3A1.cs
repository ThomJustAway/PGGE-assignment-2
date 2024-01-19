using System.Collections;
using UnityEngine;

namespace experimenting2
{
    using Assets.Experiment_2.job_script;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Assertions.Comparers;
    using UnityEngine.EventSystems;
    using UnityEngine.Jobs;

    public class FlockBehaviourV3A1 : MonoBehaviour
    {
        List<CustomObstacles> mObstacles = new List<CustomObstacles>();

        [SerializeField]
        GameObject[] Obstacles;

        [SerializeField]
        BoxCollider2D Bounds;

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

                //this customObstacle component store the data of the obstacle for the job system to use
                CustomObstacles obs = Obstacles[i].AddComponent<CustomObstacles>();
                Autonomous autono = Obstacles[i].AddComponent<Autonomous>();
                //add the the obstacle and autonomouse component to the game

                //add the variables to move the obstacle
                autono.MaxSpeed = 1.0f;
                obs.mCollider = Obstacles[i].GetComponent<CircleCollider2D>();

                mObstacles.Add(obs); //add collider for reference for the boid?
            }
        }

        //this is used to initialize the flock
        void CreateFlock()
        {
            foreach (Flock flock in flocks)
            {
                //make the list persistent as it is time consuming to remove the native array/list every frame
                //if we have a large amont of boids.

                flock.nativeMovementObjects = new NativeList<MovementObject>(Allocator.Persistent);
                for (int i = 0; i < flock.numBoids; ++i)
                {
                    AddBoid(flock);
                }
            }

        }//they are the groups of boid (flock)

        #endregion
        #region boids related
        //add the basic boids into the scene
        void AddBoids()
        {
            for (int i = 0; i < BoidIncr; ++i)
            {//ignore the predator boids and just add normal boids
                AddBoid(flocks[0]);
            }
            flocks[0].numBoids += BoidIncr;
        }

        //initialize the boid with the associated boid.
        void AddBoid(Flock flock)
        {
            //find the random position for the boids to spawn
            float x = UnityEngine.Random.Range(Bounds.bounds.min.x, Bounds.bounds.max.x);
            float y = UnityEngine.Random.Range(Bounds.bounds.min.y, Bounds.bounds.max.y);

            GameObject obj = Instantiate(flock.PrefabBoid);
            obj.name = "Boid_" + flock.name + "_" + flock.transforms.Count;
            var newPosition = new Vector3(x, y, 0.0f);
            obj.transform.position = newPosition;

            //Safe it into the native list so that it can be called into the job system
            flock.nativeMovementObjects.Add(new MovementObject(
                (uint) flock.transforms.Count,
                GetRandomDirection(),
                GetRandomSpeed(flock.rules.maxSpeed),
                (float3) newPosition
                ));
            flock.transforms.Add(obj.transform);
        }
        #endregion

        //add the call back to the queue so that it can be call before the next job starts
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
        /// <summary>
        /// This is the corotine used to move the boids every frame.
        /// It follows a procedure in order to move each boid.
        /// </summary>
        /// <param name="CoroutineMovingBoids"></param>
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

        /// <summary>
        /// This is to clear all the memory for all the native array and list that exist in the project.
        /// </summary>
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

        /// <summary>
        /// This is to set up the native array of obstacles for the job system to use
        /// </summary>
        /// <param name="SettingUpObstacle"></param>

        private void SettingUpObstacle()
        {
            nativeContainerObstacles = new NativeArray<BoidsObstacle>(Obstacles.Length, Allocator.TempJob);

            for (int i = 0; i < mObstacles.Count; i++)
            {
                nativeContainerObstacles[i] = mObstacles[i].obstacle;

                var currentObstacle = mObstacles[i].obstacle;
            }
        }

        /// <summary>
        /// This is to update the native list of boid in every flock so that it can 
        /// be used in the next job.
        /// </summary>
        /// <param name="UpdateBoidsData"></param>
        /// 
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

        /// <summary>
        /// This is to initialize the job so the boids can start moving
        /// </summary>
        /// <param name="StartingJob"></param>

        

        private void StartingJob(Flock flock)
        {
            //fill up the native array with the obstacles for used
            flock.NativeOutputMovementObjects = new NativeArray<MovementObject>(flock.nativeMovementObjects.Capacity, Allocator.TempJob);

            uint randomNumber = 5;
            var random = new Unity.Mathematics.Random(randomNumber);

            BoidsFlockingMovement calculatingFlockingMovementJob = new BoidsFlockingMovement()
            {
                AllTheBoids = flock.nativeMovementObjects,
                rules = flock.rules,
                output = flock.NativeOutputMovementObjects,
                obstacles = nativeContainerObstacles,
                boxBound = Bounds.bounds,
                predatorBoids = nativeContainerPredatorBoids,
                random = random,
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

        private void StartingJob2(Flock flock)
        {
            flock.NativeOutputMovementObjects = new NativeArray<MovementObject>(flock.nativeMovementObjects.Capacity, Allocator.TempJob);
            NewerBoidsFlockingMovement movementBoids = new NewerBoidsFlockingMovement()
            {
                AllTheBoids = flock.nativeMovementObjects,
                boxBound = Bounds.bounds,
                deltaTime = Time.deltaTime,
                obstacles = nativeContainerObstacles,
                predatorBoids = nativeContainerPredatorBoids,
                output = flock.NativeOutputMovementObjects,
                rules = flock.rules,
            };

            flock.nativeTransformAccessArray = new TransformAccessArray(flock.transforms.ToArray());

            //JobHandle CalculatingJob = calculatingFlockingMovementJob.Schedule(flock.transforms.Count, 1);
            JobHandle movingJob = movementBoids.Schedule(flock.nativeTransformAccessArray);

            flock.job = movingJob;
        }

        /// <summary>
        /// This is to set up the native array of predator boids so that the 
        /// normal boids can know the location in the space and do the calculation needed to avoid them
        /// </summary>
        /// <param name="SettingUpPredatorBoids"></param>

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

        /// <summary>
        /// this function is to add the boids once the job is done.
        /// Since the native list can only be change once the job is done, 
        /// this function will take in callback function when the player hits the space bar
        /// </summary>
        /// <param name="AddBoidsFromCallback"></param>

        private void AddBoidsFromCallback()
        {
            if(addBoidsCallBack.Count > 0)
            {
                addBoidsCallBack.Dequeue().Invoke();
            }
        }
        #endregion

        #region Mapping


        ////have to hard code 16 box so that it can be used later on
        //NativeList<MovementObject> box0;
        //NativeList<MovementObject> box1;
        //NativeList<MovementObject> box2;
        //NativeList<MovementObject> box3;
        //NativeList<MovementObject> box4;
        //NativeList<MovementObject> box5;
        //NativeList<MovementObject> box6;
        //NativeList<MovementObject> box7;
        //NativeList<MovementObject> box8;
        //NativeList<MovementObject> box9;
        //NativeList<MovementObject> box10;
        //NativeList<MovementObject> box11;
        //NativeList<MovementObject> box12;
        //NativeList<MovementObject> box13;
        //NativeList<MovementObject> box14;
        //NativeList<MovementObject> box15;


        //private void MappingBoids()
        //{


        //    foreach(var flock in flocks)
        //    {
        //        foreach(var boid in flock.nativeMovementObjects)
        //        {

        //        }
        //    }
        //}


        //private void GetBoxByPosition(float3 position)
        //{
            

        //}

        //private NativeList<MovementObject> GetNativeBoxList(int i)
        //{
        //    //horrible but this is what we have to do for optimising
        //    switch(i) {
        //        case 0: return box0; 
        //        case 1: return box1; 
        //        case 2: return box2; 
        //        case 3: return box3; 
        //        case 4: return box4; 
        //        case 5: return box5; 
        //        case 6: return box6; 
        //        case 7: return box7; 
        //        case 8: return box8; 
        //        case 9: return box9; 
        //        case 10: return box10; 
        //        case 11: return box11; 
        //        case 12: return box12; 
        //        case 13: return box13; 
        //        case 14: return box14; 
        //        case 15: return box15; 
        //        default: return box0;
        //    }
        //}

        //private void ClearBox()
        //{
        //    for(int i = 0; i < 16; i++)
        //    {
        //        var grid = GetNativeBoxList(0);
        //        grid.Dispose();
        //    }
        //}

        #endregion

        #region extra methods

        //create random speed for the boids
        float GetRandomSpeed(float MaxSpeed)
        {
            return UnityEngine.Random.Range(5.0f, MaxSpeed);
        }

        //creates a random direction for the boids
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