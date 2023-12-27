using Assets.Improve_scripts.Jobs;
using Assets.Improve_scripts.Jobs.Version_4;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace Assets.Improve_scripts.Scripts
{
    public class FlockBehaviourV3 : MonoBehaviour
    {
        public GameObject PrefabBoid;

        #region attributes
        [Space(10)]
        [Header("A flock is a group of Automous objects.")]
        public string name = "Default Name";
        public bool isPredator = false;
        public Color colour = new Color(1.0f, 1.0f, 1.0f);
        public float maxSpeed = 20.0f;
        public float maxRotationSpeed = 200.0f;
        [Space(10)]
        #endregion

        #region rule
        [Header("Flocking Rules")]
        //help ranging the rule
        public bool useRandomRule = true;
        public bool useAlignmentRule = true;
        public bool useCohesionRule = true;
        public bool useSeparationRule = true;
        public bool useFleeOnSightEnemyRule = true;
        public bool useAvoidObstaclesRule = true;
        [Space(10)]
        #endregion

        #region weight
        [Header("Rule Weights")]
        //use cap case to know that this is a constant
        [Range(0.0f, 1.0f)]
        public float WEIGHT_RANDOM = 1.0f;
        [Range(0.0f, 1.0f)]
        public float WEIGHT_ALIGNMENT;
        [Range(0.0f, 1.0f)]
        public float WEIGHT_COHESION;
        [Range(0.0f, 1.0f)]
        public float WEIGHT_SEPERATION;
        [Range(0.0f, 50.0f)]
        public float WEIGHT_FLEE_ENEMY_ON_SIGHT = 50.0f;
        [Range(0.0f, 50.0f)]
        public float WEIGHT_AVOID_OBSTICLES = 10.0f;
        public float WEIGHT_SPEED = 1.0f;
        #endregion

        #region properties
        [Space(10)]
        [Header("Properties")]
        [SerializeField] private float seperationRadius;
        public float SeparationRadius { get { return seperationRadius; } }

        [SerializeField] private float alignmentRadius;
        public float AlignmentRadius { get { return alignmentRadius; } }

        [SerializeField] private float enemySeperationDistance;
        public float EnemySeparationDistance { get { return enemySeperationDistance; } }

        [SerializeField] private bool bounceWall;
        public bool BounceWall { get { return bounceWall; } }

        public Vector3 TotalCohesionPoint { get; private set; } = Vector3.zero;

        //public Vector2 TotalSumBoidsVelocity { get; private set; } = Vector2.zero;
        //to know where the flock should combine in the end
        #endregion
        private Bounds bounds;

        private List<BoidDataTransform> boids = new List<BoidDataTransform>();
        private TransformAccessArray boidsTransformAccessArray;

        private NativeArray<BoidData> boidsNativeDataInput;
        private NativeArray<BoidData> boidsNativeDataOutput;

        private DataRule dataForJobRule;
        private JobHandle boidJob;

        [Range(1,10)]
        [SerializeField] private int jobSplit = 1;

        private void Start()
        {
            ScheduleJob2();
            bounds = FlocksController.Instance.BoxCollider2D.bounds;
        }

        private void Update()
        {
            
            if(boidJob.IsCompleted) ScheduleNextBoidMovement();
            ListenToAddBoidsInput();
            UpdateJobRule();
        }


        private void OnDestroy()
        {
            boidsTransformAccessArray.Dispose();
            boidsNativeDataInput.Dispose();
            boidsNativeDataOutput.Dispose();
        }

        private void ScheduleNextBoidMovement()
        {
            //probably have to create multiple job handles to do this shit https://www.youtube.com/watch?v=C56bbgtPr_w&t=508s
            RecieveJob();
            ScheduleJob2();
        }

        //private void ScheduleJob()
        //{
        //    boidsTransformAccessArray = new TransformAccessArray(boids.Count);
        //    boidsNativeDataInput = new NativeArray<BoidData>(boids.Count, Allocator.TempJob);
        //    boidsNativeDataOutput = new NativeArray<BoidData>(boids.Count, Allocator.TempJob);

        //    for (int i = 0; i < boids.Count; i++)
        //    {
        //        var boid = boids[i];
        //        boidsTransformAccessArray.Add(boid.transform);
        //        boidsNativeDataInput[i] = new BoidData(boid.position, boid.velocity);
        //    }

        //    BoidJob job = new BoidJob()
        //    {
        //        rules = dataForJobRule,
        //        deltaTime = Time.deltaTime,
        //        randomVelocityPreMade = SetRandomVelocityFloat2(),
        //        InputData = boidsNativeDataInput,
        //        OutputData = boidsNativeDataOutput,
        //    };
        //    boidJob = job.Schedule(boidsTransformAccessArray);
        //}

        private void ScheduleJob2()
        {
            boidsTransformAccessArray = new TransformAccessArray(boids.Count);
            boidsNativeDataInput = new NativeArray<BoidData>(boids.Count, Allocator.TempJob);
            boidsNativeDataOutput = new NativeArray<BoidData>(boids.Count, Allocator.TempJob);
;
            for (int i = 0; i < boids.Count; i++)
            {
                var boid = boids[i];
                boidsTransformAccessArray.Add(boid.transform);
                boidsNativeDataInput[i] = new BoidData(boid.position, boid.velocity);
            }

            CalculateBoidsVelocity calculatingVelocityJob = new CalculateBoidsVelocity()
            {
                boids = boidsNativeDataInput,
                rules = dataForJobRule,
                randomVelocityPreMade = SetRandomVelocityFloat2(),
                outputBoid = boidsNativeDataOutput,
                
            };

            MoveBoidsUsingVelocity moveBoidJob = new MoveBoidsUsingVelocity()
            {
                Boids = boidsNativeDataOutput,
                deltaTime = Time.deltaTime,
                rules = dataForJobRule,
            };

            JobHandle handleCalculatingJob = calculatingVelocityJob.Schedule(boids.Count,jobSplit);
            boidJob = moveBoidJob.Schedule(boidsTransformAccessArray, handleCalculatingJob);
        }

        private void RecieveJob()
        {
            boidJob.Complete();
            var output = boidsNativeDataOutput.ToArray(); //update the position in the struct container

            for(int i = 0; i< output.Length; i++)
            {
                var newBoid = new BoidDataTransform(output[i].position, output[i].velocity, boids[i].transform);
                boids[i] = newBoid;
            }
            //free up memory
            boidsTransformAccessArray.Dispose();
            boidsNativeDataInput.Dispose();
            boidsNativeDataOutput.Dispose();
        }

        private void UpdateJobRule()
        {
            //for alignment
            dataForJobRule.SeparationRadius = seperationRadius;
            dataForJobRule.AlignmentRadius = alignmentRadius;

            //for rules
            dataForJobRule.useCohesionRule = useCohesionRule;
            dataForJobRule.useAlignmentRule = useAlignmentRule;
            dataForJobRule.useSeparationRule = useSeparationRule;

            //for weights
            dataForJobRule.WEIGHT_ALIGNMENT = WEIGHT_ALIGNMENT;
            dataForJobRule.WEIGHT_COHESION = WEIGHT_COHESION;
            dataForJobRule.WEIGHT_SEPERATION = WEIGHT_SEPERATION;

            dataForJobRule.maxBound = FlocksController.Instance.BoxCollider2D.bounds.max;
            dataForJobRule.minBound = FlocksController.Instance.BoxCollider2D.bounds.min;

            dataForJobRule.RotationSpeed = maxRotationSpeed;
            dataForJobRule.MaxSpeed = maxSpeed;
        }

        private void ListenToAddBoidsInput()
        {
            if (Input.GetKeyUp(KeyCode.Space))
            {
                AddBoids();
            }
        }

        #region boids methods
        private void AddBoids()
        {
            for (int i = 0; i < FlocksController.Instance.numberOfBoidsToSpawn; i++)
            {
                InitBoid();
            }
            //for debugging purpose
        }

        private void InitBoid()
        {
            GameObject boid = Instantiate(PrefabBoid);

            //randomly set the 
            float x = UnityEngine.Random.Range(bounds.min.x, bounds.max.x);
            float y = UnityEngine.Random.Range(bounds.min.y, bounds.max.y);

            boid.transform.position = new Vector2(x, y);
            boid.name = "Boid_" + name + "_" + boids.Count;

            //set a random velocity to the boid
            boids.Add(new BoidDataTransform(boid.transform.position,
                SetRandomVelocityVector3(),
                boid.transform
                ));
        }

        private Vector3 SetRandomVelocityVector3()
        {
            float x = UnityEngine.Random.Range(-5f, 5f);
            float y = UnityEngine.Random.Range(-5f, 5f);
            return new Vector2(x, y);
        }

        private float2 SetRandomVelocityFloat2()
        {
            float x = UnityEngine.Random.Range(-5f, 5f);
            float y = UnityEngine.Random.Range(-5f, 5f);
            return new float2(x, y);
        }
        #endregion


    }

}