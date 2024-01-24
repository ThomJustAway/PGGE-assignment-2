using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace experimenting2
{
    //this is the custom data sturcture for the boid. this is used for the job system where the job system
    //can do something to the data provided
    public struct MovementObject
    {
        public uint id; //unique identifier for each boids
        public float3 targetDirection; //target direction to know where the boids should look at next frame
        public float speed; 
        public float targetSpeed; //targe speed to know how much it needs to speed up or slow down
        public float3 position; //position for the jobs to know every location of the boids

        public MovementObject(uint id,
            float3 targetDirection,
            float speed,
            float3 position
            ) //initializer
        {
            this.id = id;
            this.targetDirection = targetDirection;
            this.speed = speed;
            this.targetSpeed = UnityEngine.Random.Range(4f, speed);
            this.position = position;
        }

        //ignore this as this is for compute shader
        public static int AmountOfData()
        {
            return (sizeof(uint) + sizeof(float) * 8);
        }
    };

    //this is the rule that the job system must abide.
    [Serializable]
    public struct DataRule
    {
        //this is the struct to contain all the rules so that it can be pass to the job system.
        public float maxSpeed;
        public float maxRotationSpeed;

        [Space(10)]
        [Header("Flocking Rules")]
        public bool useRandomRule;
        public bool useAlignmentRule;
        public bool useCohesionRule;
        public bool useSeparationRule;
        public bool useFleeOnSightEnemyRule;
        public bool useAvoidObstaclesRule;

        [Space(10)]
        [Header("Rule Weights")]
        [Range(0.0f, 10.0f)]
        public float WEIGHT_RANDOM;
        [Range(0.0f, 10.0f)]
        public float WEIGHT_ALIGNMENT;
        [Range(0.0f, 10.0f)]
        public float WEIGHT_COHESION;
        [Range(0.0f, 10.0f)]
        public float WEIGHT_SEPARATION;
        [Range(0.0f, 50.0f)]
        public float WEIGHT_FLEE_ENEMY_ON_SIGHT;
        [Range(0.0f, 50.0f)]
        public float WEIGHT_AVOID_OBSTACLES;

        [Space(10)]
        [Header("Properties")]
        public float separationDistance;
        public float enemySeparationDistance;
        public float visibility;
        public bool bounceWall;
        public bool isPredator ;
    }

    //This is the obstacle struct so that the boids can avoid them.
    public struct BoidsObstacle //special struct for the obstacles so that the boids know where to avoid them
    {
        public float3 position; //the position of the obstacle 
        public float AvoidanceRadius;
        public float AvoidanceRadiusMultFactor;
        public BoidsObstacle(float3 position, float radius) //initialize the struct
        {
            this.position = position;
            AvoidanceRadius = radius;
            AvoidanceRadiusMultFactor = 1.5f;
        }

        public static int AmountOfData()
        {
            return (sizeof(float) * 5);
        }
    }

}