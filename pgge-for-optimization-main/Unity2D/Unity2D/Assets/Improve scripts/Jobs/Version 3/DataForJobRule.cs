using System.Collections;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace Assets.Improve_scripts.Jobs
{
    public struct DataForJobRule 
    {

        #region rules
        public bool useAlignmentRule;
        public bool useCohesionRule;
        public bool useSeparationRule;
        #endregion

        public float WEIGHT_ALIGNMENT;
        public float WEIGHT_COHESION;
        public float WEIGHT_SEPERATION;

        public float AlignmentRadius;
        public float SeparationRadius;
        public Vector3 cohesionPoint;
    }

    public struct BoidData
    {
        public float3 position;
        public float2 velocity;

        public BoidData(Vector3 position,Vector2 velocity )
        {
            this.position = position;
            this.velocity = velocity;
        }
    }

    public struct BoidDataTransform 
    {
        public Vector3 position;
        public Vector2 velocity;
        public Transform transform;
        public BoidDataTransform(Vector3 position, Vector2 velocity , Transform transform)
        {
            this.position = position;
            this.velocity = velocity;
            this.transform = transform;
        }
    }

}