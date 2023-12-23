using Assets.Improve_scripts.Scripts;
using System.Collections;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Windows.Speech;

namespace Assets.Improve_scripts.Jobs
{
    public struct BoidJob : IJobParallelForTransform
    {
        [ReadOnly]
        public NativeArray<BoidData> InputData;
        public NativeArray<BoidData> OutputData;

        public DataRule rules;
        public float deltaTime;
        public Vector2 randomVelocityPreMade;
        public void Execute(int index, TransformAccess transform)
        {
            Vector2 velocityFound   = CalculateVelocityThroughRules(transform, index);

            Debug.Log($"index {index}, velocity: {velocityFound}");

            if (rules.BounceWall)
            {
                velocityFound = BounceBoid(transform, velocityFound);
            }
            else 
            {
                TeleportBoid(transform);
            }
            RotateGameObjectBasedOnTargetDirection(transform, velocityFound);
            MoveBoid(transform);

            var currentBoid = InputData[index];
            currentBoid.velocity = velocityFound;
            currentBoid.position = transform.position;
            OutputData[index] = currentBoid;
        }

        private Vector3 CalculateVelocityThroughRules(TransformAccess transform , int index)
        {
            Vector3 velocity = InputData[index].velocity;

            var seperationVelocity = Vector2.zero;
            var cohesionVelocity = Vector2.zero;
            var AlignmentVelocity = Vector2.zero;

            if (rules.useSeparationRule) seperationVelocity = SeperationRule(transform) * rules.WEIGHT_SEPERATION;
            //if (rules.useCohesionRule) cohesionVelocity = CohesionRule() * rules.WEIGHT_COHESION;
            if (rules.useAlignmentRule) AlignmentVelocity = AlignmentRule(transform,index) * rules.WEIGHT_ALIGNMENT;

            Debug.Log($"seperation velocity{seperationVelocity} alignment velocity {AlignmentVelocity}");

            Vector3 totalSumOfVelocity = seperationVelocity + AlignmentVelocity + cohesionVelocity;

            if (velocity == Vector3.zero) velocity = randomVelocityPreMade;
            if (totalSumOfVelocity != Vector3.zero) velocity += totalSumOfVelocity;

            return velocity;
            //CheckIfOutOfBound(); //if bounce then this will work
            ////the bound velocity will take priorty then the other rules

            //TargetDirection = velocity.normalized;
            //TargetSpeed = velocity.magnitude * FlockCreator.WEIGHT_SPEED;
        }

        private Vector2 SeperationRule(TransformAccess transform)
        {
            Vector3 boidPosition = transform.position;

            Vector2 resultantVelocity = Vector2.zero;

            for(int i = 0; i < InputData.Length; i++)
            {
                Vector3 otherBoidPosition = InputData[i].position;
                //Vector3 otherBoidPosition = otherBoid.position;
                float distance = Vector3.Distance(boidPosition, otherBoidPosition);

                if (distance <= rules.SeparationRadius) //within range
                {
                    //it is fine to include (this boid position - this boid position) since opposite direction = 0; 
                    //so no changes in the end
                    Vector2 oppositeDirection = (Vector2)(boidPosition - otherBoidPosition);
                    resultantVelocity += oppositeDirection;
                }
            }

            return resultantVelocity;
        }

        //private Vector2 CohesionRule(TransformAccess transform)
        //{
        //    Vector2 position = transform.position;
        //    Vector2 avgCohesionPoint = ((Vector2)data.cohesionPoint - position) /
        //        (allBoids.Length - 1);

        //    Vector2 velocity = avgCohesionPoint - position; //the direction needed to move to the center
        //    return velocity.normalized;
        //}

        private Vector2 AlignmentRule(TransformAccess transform , int index)
        {
            Vector2 resultantDirection = Vector2.zero;
            int count = 0;
            
            for (int i = 0; i < InputData.Length; i++)
            {
                if (i == index) continue;

                float distance = Vector2.Distance(
                    transform.position,
                    InputData[i].position);

                if (distance <= rules.AlignmentRadius)
                {//within distance
                    resultantDirection += (Vector2) InputData[i].velocity.normalized;
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

        private Vector2 BounceBoid(TransformAccess transform,Vector2 velocity)
        {
            //Vector2 newDirection = Vector2.zero;
            Bounds boxBound = FlocksController.Instance.BoxCollider2D.bounds;
            //for the x axis
            Vector2 pos = transform.position;

            float padding = 0f;
            if (pos.x > boxBound.max.x - padding)
            {
                //make sure the boids 
                velocity.x = -1f;
            }
            else if (pos.x < boxBound.min.x + padding)
            {
                //teleport boid to the right side of the map
                velocity.x = 1f;
            }
            //for the y axis
            if (pos.y > boxBound.max.y - padding)
            {
                //teleport boid to the bottom of the map
                velocity.y = -1f;
            }
            else if (pos.y < boxBound.min.y + padding)
            {
                //teleport boid to the top of the map
                velocity.y = 1f;
            }
            return velocity;
            //velocity = (Vector3)newTargetVelocity;
        }
        #endregion

        private void MoveBoid(TransformAccess transform)
        {
            //add speed is for making the speed faster or slower depending
            //on the three behaviour

            //uncomment this out later on after fixing certain parts of the boid
            //boids will move at a constant speed

            //float addSpeed = ((TargetSpeed - Speed) / 10.0f);
            //Speed = Speed + addSpeed * Time.deltaTime;

            //if (Speed > MaxSpeed) //cap the next speed
            //    Speed = MaxSpeed;
            float speed = 10;
            transform.position += (Vector3.right * speed * deltaTime);
            //the logic behind this code is because the sprite is 
            //on the vector3.right, the forward is the local rotation on the right. 
            //so it will act as if it is going forward, but dont worry about this line of code
        }

        //this function will require target direction to move (will normalize the value)
        private void RotateGameObjectBasedOnTargetDirection(TransformAccess transform, Vector3 velocity)
        {
            Vector3 targetDirection = velocity.normalized;
            //get the normalize value of the target direction
            Vector3 rotatedVectorToTarget =
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
    }
}