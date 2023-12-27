using System.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace Assets.Improve_scripts.Jobs.Version_4
{
    public struct CalculateBoidsVelocity : IJobParallelFor
    {
        [ReadOnly]public NativeArray<BoidData> boids;
        public NativeArray<BoidData> outputBoid;
        [ReadOnly] public DataRule rules;
        public float2 randomVelocityPreMade;
        public void Execute(int index) //will look index
        {
            var data = boids[index];
            data.velocity = CalculateVelocityThroughRules(index);
            outputBoid[index] = data;
        }

        private float2 CalculateVelocityThroughRules(int index)
        {
            float2 velocity = boids[index].velocity;

            var seperationVelocity = float2.zero;
            var cohesionVelocity = float2.zero;
            var AlignmentVelocity = float2.zero;

            if (rules.useSeparationRule) seperationVelocity = SeperationRule(index) * rules.WEIGHT_SEPERATION;
            //if (rules.useCohesionRule) cohesionVelocity = CohesionRule() * rules.WEIGHT_COHESION;
            if (rules.useAlignmentRule) AlignmentVelocity = AlignmentRule( index) * rules.WEIGHT_ALIGNMENT;

            Debug.Log($"index{index} seperation velocity{seperationVelocity} alignment velocity {AlignmentVelocity}");

            float2 totalSumOfVelocity = seperationVelocity + AlignmentVelocity + cohesionVelocity;

            if (IsEqual(velocity, float2.zero)) velocity = randomVelocityPreMade;

            //totalSumOfVelocity != Vector3.zero
            if (!IsEqual(totalSumOfVelocity, float2.zero)) velocity += totalSumOfVelocity;

            return velocity;
            //CheckIfOutOfBound(); //if bounce then this will work
            ////the bound velocity will take priorty then the other rules

            //TargetDirection = velocity.normalized;
            //TargetSpeed = velocity.magnitude * FlockCreator.WEIGHT_SPEED;
        }

        private float2 SeperationRule(int index)
        {
            float3 boidPosition = boids[index].position;

            float3 resultantVelocity = float3.zero;

            for (int i = 0; i < boids.Length; i++)
            {
                if (index == i) continue; //ignore the same boid
                float3 otherBoidPosition = boids[i].position;
                //Vector3 otherBoidPosition = otherBoid.position;
                float distance = Vector3.Distance(boidPosition, otherBoidPosition);

                if (distance <= rules.SeparationRadius) //within range
                {
                    float3 oppositeDirection = (boidPosition - otherBoidPosition);
                    resultantVelocity += oppositeDirection;
                }
            }

            return new float2(resultantVelocity.x, resultantVelocity.y);
        }

        //private Vector2 CohesionRule(TransformAccess transform)
        //{
        //    Vector2 position = transform.position;
        //    Vector2 avgCohesionPoint = ((Vector2)data.cohesionPoint - position) /
        //        (allBoids.Length - 1);

        //    Vector2 velocity = avgCohesionPoint - position; //the direction needed to move to the center
        //    return velocity.normalized;
        //}

        private float2 AlignmentRule(int index)
        {
            var boid = boids[index];
            float2 resultantDirection = float2.zero;
            int count = 0;

            for (int i = 0; i < boids.Length; i++)
            {
                if (i == index) continue;

                float distance = FindDistance(
                    boid.position,
                    boids[i].position);

                if (distance <= rules.AlignmentRadius)
                {//within distance
                    resultantDirection += Normalise(boids[i].velocity);
                    count++;
                }
            }

            ////resultantDirection != float2.zero
            //if ( !IsEqual(resultantDirection , float2.zero) && count != 0)
            //{//that mean that there are boids nearby so the number of boid is n - 1.
            //    resultantDirection /= count ;
            //}

            //because the effect can be quite powerful so divide by 10 to reduce it
            return Normalise(resultantDirection);

        }

        #region equation
        private bool IsEqual(float2 a, float2 b)
        {
            return (a.x == b.x && a.y == b.y);
        }

        private bool IsEqual(float3 a, float3 b)
        {
            return (a.x == b.x && a.y == b.y && a.z == b.z);
        }


        private float FindDistance(float3 a, float3 b)
        {
            float3 distanceVector = a - b;
            //ignore z because we doing 2d
            return math.sqrt((distanceVector.x * distanceVector.x) + (distanceVector.y * distanceVector.y));
        }

        private float3 Normalise(float3 a)
        {
            float distance = math.sqrt((a.x * a.x) + (a.y * a.y));
            return a / distance;
        }

        private float2 Normalise(float2 a)
        {
            float distance = math.sqrt((a.x * a.x) + (a.y * a.y));
            return a / distance;
        }
        #endregion 
    }

    [BurstCompile]
    public struct MoveBoidsUsingVelocity : IJobParallelForTransform
    {
        public NativeArray<BoidData> Boids;
        [ReadOnly] public float deltaTime;
        [ReadOnly] public DataRule rules;
        public void Execute(int index, TransformAccess transform)
        {
            var velocityFound = Boids[index].velocity;

            if (rules.BounceWall)
            {
                velocityFound = BounceBoid(transform, velocityFound);
            }
            else
            {
                TeleportBoid(transform);
            }
            RotateGameObjectBasedOnTargetDirection(transform, velocityFound);
            MoveBoid(transform, index);

            var currentBoid = Boids[index];
            currentBoid.position = transform.position;
            Boids[index] = currentBoid;
        }

        #region bounds
        //make sure to rip this part out!
        private void TeleportBoid(TransformAccess transform)
        {
            Vector2 pos = transform.position;

            if (pos.x > rules.maxBound.x)
            {
                //teleport boid to the left side of the map
                pos.x = rules.minBound.x;
            }
            else if (pos.x < rules.minBound.x)
            {
                //teleport boid to the right side of the map
                pos.x = rules.maxBound.x;
            }

            if (pos.y > rules.maxBound.y)
            {
                //teleport boid to the bottom of the map
                pos.y = rules.minBound.y;
            }
            else if (pos.y < rules.minBound.y)
            {
                //teleport boid to the top of the map
                pos.y = rules.minBound.y;
            }

            transform.position = pos;
        }

        private Vector2 BounceBoid(TransformAccess transform, Vector2 velocity)
        {
            //Vector2 newDirection = Vector2.zero;
            //Bounds boxBound = FlocksController.Instance.BoxCollider2D.bounds;
            //for the x axis
            Vector2 pos = transform.position;

            float padding = 0f;
            if (pos.x > rules.maxBound.x - padding)
            {
                //make sure the boids 
                velocity.x = -1f;
            }
            else if (pos.x < rules.minBound.x + padding)
            {
                //teleport boid to the right side of the map
                velocity.x = 1f;
            }
            //for the y axis
            if (pos.y > rules.maxBound.y - padding)
            {
                //teleport boid to the bottom of the map
                velocity.y = -1f;
            }
            else if (pos.y < rules.minBound.y + padding)
            {
                //teleport boid to the top of the map
                velocity.y = 1f;
            }
            return velocity;
            //velocity = (Vector3)newTargetVelocity;
        }
        #endregion

        private void MoveBoid(TransformAccess transform, int index)
        {

            float speed = 10;

            //using vector3 right for forward then finding the 
            Vector3 vectorToMove = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z) * new float3(1, 0, 0);
            transform.position += (vectorToMove * speed * deltaTime);
            //the logic behind this code is because the sprite is 
            //on the vector3.right, the forward is the local rotation on the right. 
            //so it will act as if it is going forward, but dont worry about this line of code
        }

        //this function will require target direction to move (will normalize the value)
        private void RotateGameObjectBasedOnTargetDirection(TransformAccess transform, float2 velocity)
        {
            var float3Dir = new float3(velocity, 0);
            float3 targetDirection = Normalise(float3Dir);
            //get the normalize value of the target direction
            float3 rotatedVectorToTarget =
                Quaternion.Euler(0, 0, 90) *
                targetDirection;
            //not too sure why they rotate the target direction by 90 degree for this...

            Quaternion targetRotation = Quaternion.LookRotation(
                forward: Vector3.forward, //want to rotate the object through the z axis
                upwards: rotatedVectorToTarget);
            //then create a rotation based of the vector. 
            //from: vector3.up to: rotatedVectorToTarget

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rules.RotationSpeed * deltaTime); //give out the next rotation
        }


        #region equation
        private bool IsEqual(float2 a, float2 b)
        {
            return (a.x == b.x && a.y == b.y);
        }

        private bool IsEqual(float3 a, float3 b)
        {
            return (a.x == b.x && a.y == b.y && a.z == b.z);
        }


        private float FindDistance(float3 a, float3 b)
        {
            float3 distanceVector = a - b;
            //ignore z because we doing 2d
            return math.sqrt((distanceVector.x * distanceVector.x) + (distanceVector.y * distanceVector.y));
        }

        private float3 Normalise(float3 a)
        {
            float distance = math.sqrt((a.x * a.x) + (a.y * a.y));
            return a / distance;
        }

        private float2 Normalise(float2 a)
        {
            float distance = math.sqrt((a.x * a.x) + (a.y * a.y));
            return a / distance;
        }
        #endregion 

    }
}