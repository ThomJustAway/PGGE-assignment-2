using System;
using UnityEngine;

namespace Assets.Improve_scripts.Jobs
{
    [Serializable]
    public struct DataRule
    {
        [Header("Rules")]
        public bool useAlignmentRule;
        public bool useCohesionRule;
        public bool useSeparationRule;
        public bool BounceWall;

        [Space(5)]
        [Header("weight")]
        public float WEIGHT_ALIGNMENT;
        public float WEIGHT_COHESION;
        public float WEIGHT_SEPERATION;

        [Space(5)]
        [Header("radius")]
        public float AlignmentRadius;
        public float SeparationRadius;

        #region for the boids
        [HideInInspector] public Vector2 minBound;
        [HideInInspector] public Vector2 maxBound;

        [Space(5)]
        [Header("properties")]
        public float RotationSpeed;
        public float MaxSpeed;

        #endregion
    }
}