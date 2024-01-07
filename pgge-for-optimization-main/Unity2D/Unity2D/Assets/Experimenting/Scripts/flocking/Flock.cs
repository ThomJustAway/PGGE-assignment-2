using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace experimenting
{
    [System.Serializable]
    public class Flock 
    {
        public GameObject PrefabBoid;

        [Space(10)]
        [Header("A flock is a group of Automous objects.")]
        public string name = "Default Name";
        public int numBoids = 10;
        public bool isPredator = false;
        public Color colour = new Color(1.0f, 1.0f, 1.0f);
        public float maxSpeed = 20.0f;
        public float rotationSpeed = 200.0f;
        [Space(10)]

        [Header("Flocking Rules")]
        //help ranging the rule
        public bool useRandomRule = true;
        public bool useAlignmentRule = true;
        public bool useCohesionRule = true;
        public bool useSeparationRule = true;
        public bool useFleeOnSightEnemyRule = true;
        public bool useAvoidObstaclesRule = true;
        [Space(10)]

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

        [Space(10)]
        [Header("Properties")]
        public float separationDistance = 5.0f; //for the seperation rule
        public float enemySeparationDistance = 5.0f; // for a special rule to move away from the enemy
        public float visibility = 20.0f; //visibility is for both alignment and Cohesion rules
        public bool bounceWall = true;

        //[HideInInspector]
        //public List<Autonomous> mAutonomous;

        //list of game object for the boids
        // plus the data of each boids which are correlated with index

        [HideInInspector] public List<Transform> boidsTransform;
        [HideInInspector] public List<Boid> boidsInformation;
    }
}
