using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

namespace Assets.Improve_scripts.Scripts
{
    public class FlockCreator : MonoBehaviour
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
        public int numberOfBoids { get { return boids.Count; } }
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
        [Range(0.0f, 10.0f)]
        public float WEIGHT_RANDOM = 1.0f;
        [Range(0.0f, 10.0f)]
        public float WEIGHT_ALIGNMENT = 2.0f;
        [Range(0.0f, 10.0f)]
        public float WEIGHT_COHESION = 3.0f;
        [Range(0.0f, 10.0f)]
        public float WEIGHT_SEPERATION = 8.0f;
        [Range(0.0f, 50.0f)]
        public float WEIGHT_FLEE_ENEMY_ON_SIGHT = 50.0f;
        [Range(0.0f, 50.0f)]
        public float WEIGHT_AVOID_OBSTICLES = 10.0f;
        #endregion

        [Space(10)]
        [Header("Properties")]
        #region properties
        [SerializeField] private float seperationRadius;
        public float SeparationRadius { get { return seperationRadius; } }

        [SerializeField] private float alignmentRadius;
        public float AlignmentRadius { get { return alignmentRadius; } }

        [SerializeField] private float enemySeperationDistance;
        public float EnemySeparationDistance { get { return enemySeperationDistance; } }

        [SerializeField] private float visibility;
        public float Visibility { get { return visibility; } }

        [SerializeField] private bool bounceWall;
        public bool BounceWall { get { return bounceWall; } }

        public Vector3 CohesionPoint { get; private set; } = Vector3.zero;
        //to know where the flock should combine in the end
        #endregion

        private List<Boid> boids = new List<Boid>();

        //check do cohesion as well as spawning of boids

        private void Update()
        {
            FindCohesion();
            ListenToAddBoidsInput();
        }

        private void ListenToAddBoidsInput()
        {
            if(Input.GetKeyUp(KeyCode.Space))
            {
                AddBoids();
            }
        }

        //might probably need to use unity job system here
        private void FindCohesion()
        {
            if (boids.Count == 0) return;
            foreach(var boid in boids)
            {
                CohesionPoint += boid.transform.position;
            }
            CohesionPoint /= boids.Count;
            print($"cohesion point{CohesionPoint}");
        }

        private void AddBoids()
        {
            for(int i = 0 ; i < FlocksController.Instance.numberOfBoidsToSpawn; i++)
            {
                GameObject boid = Instantiate(PrefabBoid, transform);
                Boid component = boid.GetComponent<Boid>();
                boids.Add(component);
                component.Init(this);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(CohesionPoint, 2f);
        }
    }
}