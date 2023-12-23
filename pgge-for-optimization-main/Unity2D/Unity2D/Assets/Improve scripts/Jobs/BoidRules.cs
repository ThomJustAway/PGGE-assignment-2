using Assets.Improve_scripts.Scripts;
using System.Collections;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Assets.Improve_scripts.Jobs
{
    public struct BoidRules : IJob
    {
        [ReadOnly]
        public DataForJobRule data;
        public NativeArray<Vector3> result;
        public BoidData thisBoidData;
        public NativeArray<BoidData> allBoids;
        public Vector2 randomVelocityPreMade;
        public void Execute()
        {
            CalculateVelocityThroughRules();
        }


        private void CalculateVelocityThroughRules()
        {
            //Vector2 FinalVelocity = Vector2.zero;
            //velocity = Vector2.zero;


            var velocity = thisBoidData.velocity;

            var seperationVelocity = Vector2.zero;
            var cohesionVelocity = Vector2.zero;
            var AlignmentVelocity = Vector2.zero;

            if (data.useSeparationRule) seperationVelocity = SeperationRule() * data.WEIGHT_SEPERATION;
            if (data.useCohesionRule) cohesionVelocity = CohesionRule() * data.WEIGHT_COHESION;
            if (data.useAlignmentRule) AlignmentVelocity = AlignmentRule() * data.WEIGHT_ALIGNMENT;

            Vector2 totalSumOfVelocity = seperationVelocity + AlignmentVelocity + cohesionVelocity;

            if (velocity == Vector2.zero) velocity = randomVelocityPreMade;
            if (totalSumOfVelocity != Vector2.zero) velocity += totalSumOfVelocity;

            result[0] = (Vector3)velocity;
            //CheckIfOutOfBound(); //if bounce then this will work
            ////the bound velocity will take priorty then the other rules

            //TargetDirection = velocity.normalized;
            //TargetSpeed = velocity.magnitude * FlockCreator.WEIGHT_SPEED;
        }

        private Vector2 SeperationRule()
        {
            Vector3 boidPosition = thisBoidData.position;

            Vector2 resultantVelocity = Vector2.zero;

            foreach(var otherBoid in allBoids)
            {
                Vector3 otherBoidPosition = otherBoid.position;
                float distance = Vector3.Distance(boidPosition, otherBoidPosition);
                if(distance <= data.SeparationRadius)
                {//within range
                    //it is fine to include (this boid position - this boid position) since opposite direction = 0;
                    Vector2 oppositeDirection = (Vector2) (boidPosition - otherBoidPosition);
                    resultantVelocity += oppositeDirection;
                }
            }

            return resultantVelocity;
        }

        private Vector2 CohesionRule()
        {
            Vector2 position = thisBoidData.position;
            Vector2 avgCohesionPoint = ((Vector2)data.cohesionPoint - position) /
                (allBoids.Length - 1);

            Vector2 velocity = avgCohesionPoint - position; //the direction needed to move to the center
            return velocity.normalized;
        }

        private Vector2 AlignmentRule()
        {
            /*
            var boidsNearby = Physics2D.CircleCastAll(transform.position,
               data.AlignmentRadius,
               Vector2.zero);

            foreach (var boid in boidsNearby)
            {
                if (boid.collider == boidCollider) continue; //make sure it does not reference the current boid
                if (boid.collider.TryGetComponent<Boid>(out var boidComponent))
                {
                    if (boidComponent.FlockCreator != FlockCreator) continue; //make sure it is the correct boid
                    resultantDirection += (Vector2)boidComponent.velocity.normalized;
                }
            }
            */
            Vector2 resultantDirection = Vector2.zero;
            int count = 0;
            foreach(var otherBoid in allBoids)
            {
                if (otherBoid.position == thisBoidData.position &&
                    otherBoid.velocity == thisBoidData.velocity
                    ) continue; //is the same boid

                float distance = Vector2.Distance(
                    thisBoidData.position,
                    otherBoid.position
                    );
                if(distance <= data.AlignmentRadius)
                {//within distance
                    resultantDirection += otherBoid.velocity.normalized;
                    count++;
                }
            }

            if (resultantDirection != Vector2.zero)
            {//that mean that there are boids nearby so the number of boid is n - 1.
                resultantDirection /= count - 1;
            }

            //because the effect can be quite powerful so divide by 10 to reduce it
            return resultantDirection.normalized;

        }

    }
}