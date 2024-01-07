using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace experimenting2
{
    public struct MovementObject
    {
        public uint id;
        public float3 targetDirection;
        public float speed;
        public float targetSpeed;
        public float3 position;

        public MovementObject(uint id,
            float3 targetDirection,
            float speed,
            float3 position
            )
        {
            this.id = id;
            this.targetDirection = targetDirection;
            this.speed = speed;
            this.targetSpeed = UnityEngine.Random.Range(4f, speed);
            this.position = position;
        }
    };

    [Serializable]
    public struct DataRule
    {
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

    public struct BoidsObstacle
    {
        public float3 position;
        public float AvoidanceRadius;
        public float AvoidanceRadiusMultFactor;
        public BoidsObstacle(float3 position, float radius)
        {
            this.position = position;
            AvoidanceRadius = radius;
            AvoidanceRadiusMultFactor = 1.5f;
        }
    }
}