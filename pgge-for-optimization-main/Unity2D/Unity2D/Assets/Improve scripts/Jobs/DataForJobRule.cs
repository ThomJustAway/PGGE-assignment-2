using System.Collections;
using Unity.Collections;
using UnityEngine;

namespace Assets.Improve_scripts.Jobs
{
    public struct DataForJobRule 
    {
        public NativeArray<BoidData> otherBoids;

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
        public Vector3 position;
        public Vector2 velocity;

        public BoidData(Vector3 position,Vector2 velocity )
        {
            this.position = position;
            this.velocity = velocity;
        }
    }
}