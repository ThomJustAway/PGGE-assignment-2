using Assets.Improve_scripts.Jobs;
using System;
using System.Collections;
using System.Linq;
using System.Security.Cryptography;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using static Unity.Collections.AllocatorManager;

namespace Assets.Improve_scripts.Scripts
{
    [RequireComponent(typeof(Collider2D))]
    public class Boid : MonoBehaviour
    {
        public float MaxSpeed { get; private set; } = 10f;

        public float Speed { get; private set; } = 0.0f;
        //public Vector2 accel { get; private set; } = new Vector2(0.0f, 0.0f); //not too sure if need this two
        public float TargetSpeed { get; private set; } = 0.0f;
        public Vector3 TargetDirection { get; private set; } = Vector3.zero;
        /*targe direction is set by the flock behaviour 
         * used to determine where the boid/ obstacles will move
        */
        private float RotationSpeed;

        public SpriteRenderer spriteRenderer;

        public FlockCreator FlockCreator { get; private set; }

        public Vector2 velocity { get; private set; } = Vector2.zero;

        //private Vector2 seperationVelocity = Vector2.zero;
        //private Vector2 cohesionVelocity = Vector2.zero;
        //private Vector2 AlignmentVelocity = Vector2.zero;

        //to do write two function for seperation and alighnment

        //for job system
        JobHandle jobRule;
        NativeArray<Vector3> result;

        public void Init(FlockCreator creator)
        {
            name = "Boid_" + creator.name + "_" + creator.numberOfBoids;
            FlockCreator = creator;
            MaxSpeed = creator.maxSpeed;
            RotationSpeed = creator.maxRotationSpeed;

            var bounds = FlocksController.Instance.BoxCollider2D.bounds;

            //setting up the position of the boids
            float x = UnityEngine.Random.Range(bounds.min.x, bounds.max.x);
            float y = UnityEngine.Random.Range(bounds.min.y, bounds.max.y);
            transform.position = new Vector2(x, y);
        }

        void Start()
        {
            SetRandomSpeed();
            SetRandomVelocity();
        }



        
        public void Update()
        {
            //completete movement
            //CompleteBoidBehaviour();
            //MoveBoidToNewPosition();
            HandleRuleJob();
            RotateGameObjectBasedOnTargetDirection();
            MoveBoid();
            //check for collision
        }

        public void LateUpdate()
        {
            jobRule.Complete();
            //calculate velocity here!
            var foundVelocity = result[0];
            result.Dispose();
            print(foundVelocity);
        }

        private void HandleRuleJob()
        {
            var newJob = new BoidRules()
            {
                thisBoidData = new BoidData(transform.position, velocity),
                data = FlockCreator.DataForJobRule,
                result = new NativeArray<Vector3>(1, Allocator.TempJob)
            };
            jobRule = newJob.Schedule();
        }
        
        #region collision prevent
        private void CheckIfOutOfBound()
        {
            if (FlockCreator.BounceWall)
            {
                BounceBoid();
            }
            else
            {
                TeleportBoid();
            }
        }

        private void TeleportBoid()
        {
            Vector2 pos = transform.position;
            Bounds boxBound = FlocksController.Instance.BoxCollider2D.bounds;

            if (pos.x > boxBound.max.x)
            {
                //teleport boid to the left side of the map
                pos.x = boxBound.min.x;
            }
            else if (pos.x < boxBound.min.x)
            {
                //teleport boid to the right side of the map
                pos.x = boxBound.max.x;
            }

            if (pos.y > boxBound.max.y)
            {
                //teleport boid to the bottom of the map
                pos.y = boxBound.min.y;
            }
            else if (pos.y < boxBound.min.y)
            {
                //teleport boid to the top of the map
                pos.y = boxBound.max.y;
            }
            
            transform.position = pos;
        }

        private void BounceBoid() //This problem can be trace back here!
        {
            //Vector2 newDirection = Vector2.zero;
            Bounds boxBound = FlocksController.Instance.BoxCollider2D.bounds;
            //for the x axis
            Vector2 pos = transform.position;
            Vector2 newTargetVelocity = velocity;

            float padding = 0f;
            if (pos.x > boxBound.max.x - padding)
            {
                //make sure the boids 
                newTargetVelocity.x = -1f;
            }
            else if (pos.x < boxBound.min.x + padding)
            {
                //teleport boid to the right side of the map
                newTargetVelocity.x = 1f;
            }
            //for the y axis
            if (pos.y > boxBound.max.y - padding)
            {
                //teleport boid to the bottom of the map
                newTargetVelocity.y = -1f;
            }
            else if (pos.y < boxBound.min.y + padding)
            {
                //teleport boid to the top of the map
                newTargetVelocity.y = 1f;
            }
            velocity = (Vector3)newTargetVelocity;
        }
        #endregion

        #region Move base on values

        //This function will require target speed for the speed
        private void MoveBoid()
        {
            //add speed is for making the speed faster or slower depending
            //on the three behaviour

            //uncomment this out later on after fixing certain parts of the boid
            //boids will move at a constant speed

            //float addSpeed = ((TargetSpeed - Speed) / 10.0f);
            //Speed = Speed + addSpeed * Time.deltaTime;

            //if (Speed > MaxSpeed) //cap the next speed
            //    Speed = MaxSpeed;

            transform.Translate(Vector3.right * Speed * Time.deltaTime , Space.Self);
            //the logic behind this code is because the sprite is 
            //on the vector3.right, the forward is the local rotation on the right. 
            //so it will act as if it is going forward, but dont worry about this line of code
        }

        //this function will require target direction to move (will normalize the value)
        private void RotateGameObjectBasedOnTargetDirection()
        {
            Vector3 targetDirection = TargetDirection.normalized;
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
                RotationSpeed * Time.deltaTime); //give out the next rotation
        }
        #endregion

        #region setup
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 boidTransform = transform.position;
            var ObjectsNearby = Physics2D.CircleCastAll(boidTransform,
                FlockCreator.SeparationRadius,
                Vector2.zero);
            Gizmos.DrawWireSphere(boidTransform, FlockCreator.SeparationRadius);
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(boidTransform, FlockCreator.AlignmentRadius);
            Gizmos.color = Color.red;
            foreach(var boid  in ObjectsNearby)
            {
                Gizmos.DrawLine(boidTransform, boid.transform.position);
            }
            Gizmos.color = Color.white;
            Gizmos.DrawRay(boidTransform, TargetDirection);

            //Debug.DrawRay(boidTransform + new Vector3(0.5f, 0.5f,0), seperationVelocity, Color.blue);

            //Debug.DrawRay(boidTransform + new Vector3(0.5f, -0.5f, 0), AlignmentVelocity, Color.black);

            //Debug.DrawRay(boidTransform + new Vector3(-0.5f, -0.5f, 0), cohesionVelocity, Color.green);

            print( $"Velocity altogether {velocity} \n" +
                $"velocity normalise {velocity.normalized}\n" +
                $"Target direction {TargetDirection} \n" +
                $"Target speed {Speed}");

        }

        void SetRandomVelocity()
        {
            float x = UnityEngine.Random.Range(-5f,5f);
            float y = UnityEngine.Random.Range(-5f, 5f);
            velocity = new Vector2(x, y);
        }

        void SetRandomSpeed()
        {
            Speed = UnityEngine.Random.Range(5,20f);
        }
        #endregion

        /*
        #region Rules

        private void MoveBoidToNewPosition()
        {
            //Vector2 FinalVelocity = Vector2.zero;
            //velocity = Vector2.zero;
            seperationVelocity = Vector2.zero;
            cohesionVelocity = Vector2.zero;
            AlignmentVelocity = Vector2.zero;

            if(FlockCreator.useSeparationRule) seperationVelocity = SeperationRule() * FlockCreator.WEIGHT_SEPERATION;
            if(FlockCreator.useCohesionRule) cohesionVelocity = CohesionRule() * FlockCreator.WEIGHT_COHESION;
            if(FlockCreator.useAlignmentRule) AlignmentVelocity  = AlignmentRule() * FlockCreator.WEIGHT_ALIGNMENT;

            Vector2 totalSumOfVelocity = seperationVelocity + AlignmentVelocity + cohesionVelocity;

            if (velocity == Vector2.zero) SetRandomVelocity();
            if (totalSumOfVelocity != Vector2.zero) velocity += totalSumOfVelocity; 

            CheckIfOutOfBound(); //if bounce then this will work
            //the bound velocity will take priorty then the other rules

            TargetDirection = velocity.normalized;
            TargetSpeed = velocity.magnitude * FlockCreator.WEIGHT_SPEED;
        }

        private Vector2 SeperationRule()
        {
            var ObjectsNearby = Physics2D.CircleCastAll(transform.position,
                FlockCreator.SeparationRadius,
                Vector2.zero); //find the objects near the boid

            Vector2 boidPosition = transform.position;
            Vector2 resultantVelocity = Vector2.zero;

            foreach (var hitPoint in ObjectsNearby)
            {
                if (hitPoint.collider == boidCollider) continue;
                Vector2 oppositeDirection = boidPosition - hitPoint.point;
                //the further away from boid to object, the lesser magnitude of repulsion
                resultantVelocity += oppositeDirection;
            }

            return resultantVelocity;
        }

        private Vector2 CohesionRule()
        {
            Vector2 position = transform.position;
            Vector2 avgCohesionPoint = ( (Vector2)FlockCreator.TotalCohesionPoint - position) / 
                (FlockCreator.numberOfBoids - 1);
            Vector2 velocity = avgCohesionPoint - position;
            return velocity.normalized; 
        }

        private Vector2 AlignmentRule()
        {
            var boidsNearby = Physics2D.CircleCastAll(transform.position,
               FlockCreator.AlignmentRadius,
               Vector2.zero);

            Vector2 resultantDirection = Vector2.zero;
            foreach (var boid in boidsNearby)
            {
                if (boid.collider == boidCollider) continue; //make sure it does not reference the current boid
                if (boid.collider.TryGetComponent<Boid>(out var boidComponent))
                {
                    if (boidComponent.FlockCreator != FlockCreator) continue; //make sure it is the correct boid
                    resultantDirection += (Vector2)boidComponent.velocity.normalized;
                }
            }
            if( resultantDirection != Vector2.zero )
            {//that mean that there are boids nearby so the number of boid is n - 1.
                resultantDirection /= boidsNearby.Length - 1;
            }
            //because the effect can be quite powerful so divide by 10 to reduce it
            return resultantDirection ;
            
        }

        #endregion
        */
    }
}