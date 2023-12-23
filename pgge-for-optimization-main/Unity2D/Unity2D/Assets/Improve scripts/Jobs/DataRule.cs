using UnityEngine;

namespace Assets.Improve_scripts.Jobs
{
    public struct DataRule
    {

        #region rules
        public bool useAlignmentRule;
        public bool useCohesionRule;
        public bool useSeparationRule;
        public bool BounceWall;
        #endregion

        public float WEIGHT_ALIGNMENT;
        public float WEIGHT_COHESION;
        public float WEIGHT_SEPERATION;

        public float AlignmentRadius;
        public float SeparationRadius;

        #region for the boids
        public Vector2 minBound;
        public Vector2 maxBound;

        public float RotationSpeed;
        public float MaxSpeed;

        #endregion
    }
}