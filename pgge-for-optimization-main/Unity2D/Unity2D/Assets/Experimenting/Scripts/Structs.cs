using System.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace experimenting
{
    public struct Boid
    {
        public uint id;
        public float3 position;
        public float3 targetDirection;
        public float speed;
        public float targetSpeed;

        public Boid(uint id,
            float3 position,
            float3 targetDirection,
            float speed)
        {
            this.id = id;
            this.position = position;
            this.targetDirection = targetDirection;
            this.speed = speed; 
            this.targetSpeed = speed;
        }

        public static int sizeOfData()
        {
            return (sizeof(float)* 3 * 2) + sizeof(float) * 2 + sizeof(uint);
        }
    };

    public struct rulesData
    {
        public bool useCohesionRule;
        public bool useAlignmentRule;
        public bool useSeparationRule;
        public float WEIGHT_COHESION;
        public float WEIGHT_SEPERATION;
        public float WEIGHT_ALIGNMENT;
        public float visibility;
        public float separationDistance;
        public uint sizeOfFlock;
         
        public rulesData(bool useCohesionRule,
            bool useAlignmentRule,
            bool useSeparationRule,
            float WEIGHT_COHESION,
            float WEIGHT_SEPERATION,
            float WEIGHT_ALIGNMENT,
            float visibility,
            float separationDistance,
            uint sizeOfFlock
            )
        {
            this.useCohesionRule = useCohesionRule;
            this.useAlignmentRule = useAlignmentRule;
            this.useSeparationRule = useSeparationRule;
            this.WEIGHT_COHESION = WEIGHT_COHESION;
            this.WEIGHT_SEPERATION = WEIGHT_SEPERATION;
            this.WEIGHT_ALIGNMENT = WEIGHT_ALIGNMENT;

            this.visibility = visibility;
            this.separationDistance = separationDistance;
            this.sizeOfFlock = sizeOfFlock;
        }
    };
}