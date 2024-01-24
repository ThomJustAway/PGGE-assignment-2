using System.Collections;
using UnityEngine;

namespace experimenting2
{
    using Assets.Experiment_2.job_script;
    using experimenting;
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
    using UnityEngine.UIElements;

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

        //ignore this as this is for experiementing
        #region compute shader
        [Header("ignore this, it is legacy code")]
        [Range(5 , 1000)]
        [SerializeField] private bool useGPU;
        [SerializeField] private ComputeShader ComputeShaderFlocking;
        [SerializeField] private int partitionAmount = 1000; //how many 

        //compute buffers
        private ComputeBuffer ComputeBufferCurrentBoid;
        private ComputeBuffer ComputeBufferAllBoid;
        private ComputeBuffer ComputeBufferPredatorBoid;
        private ComputeBuffer ComputeBufferObstacles;
        #endregion

        void Reset()
        {
            ClearJobSystemMemory(true);
            flocks = new List<Flock>() { new Flock() };
        }

        private void OnDestroy()
        {
            ClearJobSystemMemory(true);
            flocks = new List<Flock>() { new Flock() };

        }

        private void OnDisable()
        {
            ClearJobSystemMemory(true);
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
            //handle the adding boid output 
            HandleInputs();
        }

        #region starting functions

        //this is to randomly place the obstacles in different location for the boids to avoid
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
            {//if the play press the space bar button, add the boids in a queue for callback once the job is done.
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
                //You can see what happen if I try using Compute shader for this
                //not recommended since due to the GPU bottleneck and it performant worst.
                if (useGPU)
                {
                    if(useFlocking)
                    {
                        ClearJobSystemMemory();
                        AddBoidsFromCallback();
                        CreateObstacleComputeBuffer();
                        CreatePredatorComputeBuffer();
                        foreach (Flock flock in flocks)
                        {//create all the boids for the current flock
                            CreateAllBoidComputerBuffer(flock);
                            List<MovementObject> listOfPartitionBoids = new List<MovementObject>();
                            for(int i = 0; i < flock.nativeMovementObjects.Length; i++)
                            {
                                listOfPartitionBoids.Add(flock.nativeMovementObjects[i]);

                                if(i % partitionAmount - 1 == 0 || i == flock.nativeMovementObjects.Length - 1)
                                {//start the compute shader
                                    ComputeBufferCurrentBoid = new ComputeBuffer(listOfPartitionBoids.Count, MovementObject.AmountOfData());
                                    ComputeBufferCurrentBoid.SetData(listOfPartitionBoids.ToArray());
                                    int kernel = StartComputeShader(flock);
                                    ComputeShaderFlocking.Dispatch(kernel, listOfPartitionBoids.Count, 1, 1);
                                
                                    //yield return null; //wait for next frame to gather the data
                                    MovementObject[] outputContainer = new MovementObject[listOfPartitionBoids.Count];

                                    yield return null;

                                    ComputeBufferCurrentBoid.GetData(outputContainer);
                                    foreach (var newData in outputContainer)
                                    {
                                        flock.nativeMovementObjects[(int)newData.id] = newData;
                                    }

                                    //release the currentBoid compute buffer to be used again
                                    ComputeBufferCurrentBoid.Release();
                                    listOfPartitionBoids.Clear(); //get ready for the next boids
                                }
                            }
                            //loop through all the boids
                            ComputeBufferAllBoid.Release();
                        }//calculate all the new target direction and position first
                        //now remove all the memory from the buffer
                        ClearComputeBuffer();
                    }

                    //start moving the boids
                    StartBoidMovement();
                    yield return null;
                    RecieveBoidMovement(); //finish the boid movement and move on to the next frame
                }
                else
                {
                    //do all the job system work here!
                    if (useFlocking)
                    {
                        ClearJobSystemMemory(); //make sure the memory is ready so that it can be used for job
                        AddBoidsFromCallback(); //To make sure to not affec the boid list, use a callback to add the boids
                        SettingUpPredatorBoids(); //set up the predator native array for job to use
                        SettingUpObstacle(); //set up the obstacle native array for job to use.

                        foreach (Flock flock in flocks)
                        {
                            //run indiviual jobs for each flock to calculate and move the boids
                            StartingJob(flock);
                        }
                    }
                    yield return null;
                    //now just 

                    foreach(var flock  in flocks)
                    {
                        //complete the job to retrieve the data
                        flock.job.Complete();
                        UpdateBoidsData(flock); //update the boids 
                    }
                }
            }
        }

        #region job system
        /// <summary>
        /// This is to clear all the memory for all the native array and list that exist in the project.
        /// </summary>
        private void ClearJobSystemMemory(bool clearAllBoids = false)
        {
            //release memory so that it can be used for the next section
            if (nativeContainerObstacles.IsCreated) nativeContainerObstacles.Dispose();
            if(nativeContainerPredatorBoids.IsCreated) nativeContainerPredatorBoids.Dispose();
            foreach(var flock in flocks)
            {
                //clear all the native transform access and native output for further usage
                if (flock.nativeTransformAccessArray.isCreated) flock.nativeTransformAccessArray.Dispose();
                if (flock.NativeOutputMovementObjects.IsCreated) flock.NativeOutputMovementObjects.Dispose();

                if(clearAllBoids)
                {
                    if (flock.nativeMovementObjects.IsCreated) flock.nativeMovementObjects.Dispose();

                }
            }
        }

        /// <summary>
        /// This is to set up the native array of obstacles for the job system to use
        /// </summary>
        /// <param name="SettingUpObstacle"></param>

        private void SettingUpObstacle()
        {
            //set up the native array so that it can be populated with the obstacles
            nativeContainerObstacles = new NativeArray<BoidsObstacle>(Obstacles.Length, Allocator.TempJob);

            for (int i = 0; i < mObstacles.Count; i++)
            {
                //populate the native container
                nativeContainerObstacles[i] = mObstacles[i].obstacle;
            }
        }

        /// <summary>
        /// This is to update the native list of boid in every flock so that it can 
        /// be used in the next job.
        /// </summary>
        /// <param name="UpdateBoidsData"></param>
        /// 

        private void UpdateBoidsData(Flock flock)
        {//the function is still quite slow so change this to be used in job
            for (int i = 0; i < flock.transforms.Count; i++)
            {
                //replace the new result back to the list
                var boidResult = flock.NativeOutputMovementObjects[i];
                boidResult.position = flock.transforms[i].position;
                flock.nativeMovementObjects[i] = boidResult;
                //just update the position of the boid
            }

        }

        ///// <summary>
        ///// This is to initialize the job so the boids can start moving
        ///// </summary>
        ///// <param name="StartingJob"></param>



        /// <summary>
        /// This is to start the job for calculating and moving of the boids
        /// </summary>
        /// <param name="flock"></param>
        private void StartingJob(Flock flock)
        {

            //fill up the native array with the obstacles for used
            flock.NativeOutputMovementObjects = new NativeArray<MovementObject>(flock.nativeMovementObjects.Capacity, Allocator.TempJob);

            //create the random element so that the boids can have randomness in their movement
            uint randomNumber = 5;
            var random = new Unity.Mathematics.Random(randomNumber);

            //this is to calculate the target direction and speed of the boids so that it can be used in the moving of the boids
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

            //this is to move the boids once the calculating is done.
            MovingMovementObject movingBoidJob = new MovingMovementObject()
            {
                boidsData = flock.NativeOutputMovementObjects,
                deltaTime = Time.deltaTime,
                rulesData = flock.rules,
            };

            //create the transform access array so that the boid transform can be access in the job system
            flock.nativeTransformAccessArray = new TransformAccessArray(flock.transforms.ToArray());

            JobHandle CalculatingJob = calculatingFlockingMovementJob.Schedule(flock.transforms.Count, 1);
            //schedule the calculating job once the calculating job is done.
            JobHandle movingJob = movingBoidJob.Schedule(flock.nativeTransformAccessArray, CalculatingJob); 

            flock.job = movingJob;
        }

        //this is an old starting job that tries to combine all the job together into a single function.
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
            //create a native list so that it can be used to store the predator boids
            nativeContainerPredatorBoids = new NativeList<MovementObject>(Allocator.TempJob);
            foreach(var flock in flocks)
            {
                if (flock.rules.isPredator)
                {//find all the boid that are predator all the boid from that flock
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
            //When there is no job, make sure to dequeue the callback to add the boids
            if(addBoidsCallBack.Count > 0)
            {
                addBoidsCallBack.Dequeue().Invoke();
            }
        }
        #endregion

        //wont bother commenting this since it is legacy
        #region Compute shader

        private void ClearComputeBuffer()
        {
            //ComputeBufferCurrentBoid.Dispose(); //it has already been cleared
            ComputeBufferAllBoid.Dispose();
            ComputeBufferObstacles.Dispose();
            ComputeBufferPredatorBoid.Dispose();
        }

        private void CreatePredatorComputeBuffer()
        {
            List<MovementObject> predatorList = new List<MovementObject>();
            foreach(Flock flock in flocks)
            {
                if(flock.rules.isPredator)
                {
                    foreach(var boid in flock.nativeMovementObjects)
                    {
                        predatorList.Add(boid);
                    }
                }
            }
            //now populated with all the boid that are predator
            ComputeBufferPredatorBoid = new ComputeBuffer(predatorList.Count , MovementObject.AmountOfData());
            ComputeBufferPredatorBoid.SetData(predatorList.ToArray());
        }

        private void CreateObstacleComputeBuffer()
        {
            List<BoidsObstacle> obstacleList = new List<BoidsObstacle>();
            foreach (var obstacle in mObstacles)
            {
                obstacleList.Add(obstacle.obstacle);
            }
            ComputeBufferObstacles = new ComputeBuffer(obstacleList.Count , BoidsObstacle.AmountOfData());
            ComputeBufferObstacles.SetData(obstacleList.ToArray());
        }

        private void CreateAllBoidComputerBuffer(Flock flock)
        {
            ComputeBufferAllBoid = new ComputeBuffer(flock.nativeMovementObjects.Length, MovementObject.AmountOfData());
            List<MovementObject> list = new List<MovementObject>();
            for(int i = 0; i < flock.nativeMovementObjects.Length; i++) {
                list.Add(flock.nativeMovementObjects[i]);
            }

            ComputeBufferAllBoid.SetData(list.ToArray());
        }

        private void StartBoidMovement()
        {
            foreach (Flock flock in flocks)
            {
                flock.NativeOutputMovementObjects = new NativeArray<MovementObject>(flock.nativeMovementObjects.Length, Allocator.TempJob);
                for(int i = 0; i < flock.nativeMovementObjects.Length; i++)
                {
                    flock.NativeOutputMovementObjects[i] = flock.nativeMovementObjects[i];
                }//make the 

                MovingMovementObject moveJob = new MovingMovementObject()
                {
                    boidsData = flock.NativeOutputMovementObjects,
                    deltaTime = Time.deltaTime,
                    rulesData = flock.rules,
                };

                flock.nativeTransformAccessArray = new TransformAccessArray(flock.transforms.ToArray());
                flock.job = moveJob.Schedule(flock.nativeTransformAccessArray);
            }
        }

        private void RecieveBoidMovement()
        {
            //foreach (Flock flock in flocks)
            //{
            //    flock.job.Complete();
            //    UpdateBoidsData(flock);
            //}
        }

        private int StartComputeShader(Flock flock)
        {
            int kernelIndex = ComputeShaderFlocking.FindKernel("FlockingComputeShader");

            ComputeShaderFlocking.SetBool("useCohesionRule", flock.rules.useCohesionRule);
            ComputeShaderFlocking.SetBool("useAlignmentRule", flock.rules.useAlignmentRule);
            ComputeShaderFlocking.SetBool("useSeparationRule", flock.rules.useSeparationRule);
            ComputeShaderFlocking.SetBool("useRandomRule", flock.rules.useRandomRule);
            ComputeShaderFlocking.SetBool("useAvoidObstaclesRule", flock.rules.useAvoidObstaclesRule);
            ComputeShaderFlocking.SetBool("useFleeOnSightEnemyRule", flock.rules.useFleeOnSightEnemyRule);
            ComputeShaderFlocking.SetBool("isPredator", flock.rules.isPredator);
            ComputeShaderFlocking.SetBool("canBounce", flock.rules.bounceWall);

            //setting the float
            ComputeShaderFlocking.SetFloat("WEIGHT_COHESION", flock.rules.WEIGHT_COHESION);
            ComputeShaderFlocking.SetFloat("WEIGHT_SEPERATION", flock.rules.WEIGHT_SEPARATION);
            ComputeShaderFlocking.SetFloat("WEIGHT_ALIGNMENT", flock.rules.WEIGHT_ALIGNMENT);
            ComputeShaderFlocking.SetFloat("WEIGHT_RANDOM", flock.rules.WEIGHT_RANDOM);
            ComputeShaderFlocking.SetFloat("WEIGHT_AVOID_OBSTACLES", flock.rules.WEIGHT_AVOID_OBSTACLES);
            ComputeShaderFlocking.SetFloat("WEIGHT_FLEE_ENEMY_ON_SIGHT", flock.rules.WEIGHT_FLEE_ENEMY_ON_SIGHT);


            ComputeShaderFlocking.SetFloat("maxSpeed", flock.rules.maxSpeed);
            ComputeShaderFlocking.SetFloat("visibility", flock.rules.visibility);
            ComputeShaderFlocking.SetFloat("separationDistance", flock.rules.separationDistance);
            ComputeShaderFlocking.SetFloat("enemySeparationDistance", flock.rules.enemySeparationDistance);

            ComputeShaderFlocking.SetFloat("maxBoundX", Bounds.bounds.max.x);
            ComputeShaderFlocking.SetFloat("maxBoundY", Bounds.bounds.max.y);
            ComputeShaderFlocking.SetFloat("minBoundX", Bounds.bounds.min.x);
            ComputeShaderFlocking.SetFloat("minBoundY", Bounds.bounds.min.y);
            ComputeShaderFlocking.SetFloat("padding", 0);

            ComputeShaderFlocking.SetInt("sizeOfObstacles", ComputeBufferObstacles.count);
            ComputeShaderFlocking.SetInt("sizeOfFlock", ComputeBufferAllBoid.count);
            ComputeShaderFlocking.SetInt("sizeOfPredatorBoids", ComputeBufferPredatorBoid.count);

            ComputeShaderFlocking.SetBuffer(kernelIndex, "otherBoids", ComputeBufferAllBoid);
            ComputeShaderFlocking.SetBuffer(kernelIndex, "currentBatch", ComputeBufferCurrentBoid);
            ComputeShaderFlocking.SetBuffer(kernelIndex, "predatorBoids", ComputeBufferPredatorBoid);
            ComputeShaderFlocking.SetBuffer(kernelIndex, "obstacles", ComputeBufferObstacles);

            return kernelIndex;
        }

        //private void 

        #endregion

        #endregion

        //this is legacy as well as to test the mapping
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

        //this are some calculation method because float3 / float 2 dont have method to calculate them
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