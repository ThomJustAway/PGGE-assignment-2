using System.Collections;
using System.Linq;
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
        private float RotationSpeed = 0.0f;

        public SpriteRenderer spriteRenderer;

        public FlockCreator FlockCreator { get; private set; }

        private Collider2D boidCollider;

        //to do write two function for seperation and alighnment

        public void Init(FlockCreator creator)
        {
            name = "Boid_" + creator.name + "_" + creator.numberOfBoids;
            FlockCreator = creator;
            MaxSpeed = creator.maxSpeed;
            RotationSpeed = creator.maxRotationSpeed;

            var bounds = FlocksController.Instance.BoxCollider2D.bounds;

            //setting up the position of the boids
            float x = Random.Range(bounds.min.x, bounds.max.x);
            float y = Random.Range(bounds.min.y, bounds.max.y);
            transform.position = new Vector2(x, y);
        }

        void Start()
        {
            boidCollider = GetComponent<Collider2D>();
            SetRandomSpeed();
            SetRandomDirection();
        }

        public void Update()
        {
            //completete movement
            CompleteBoidBehaviour();
            RotateGameObjectBasedOnTargetDirection();
            MoveBoid();
            CheckIfOutOfBound();
            //check for collision
        }

        #region behaviour
        //will return a vector2 that has the magnitude and the directional vector.
        private void SeperationBehaviour()
        {
            var ObjectsNearby = Physics2D.CircleCastAll(transform.position,
                FlockCreator.SeparationRadius,
                Vector2.zero); //find the objects near the boid

            Vector2 resultantDirection = Vector2.zero;
            float speedOfRepulsion = 0f;
            Vector2 boidPosition = transform.position;

            int count = 0; //use this to find the average
            foreach(var hitPoint  in ObjectsNearby)
            {
                if (hitPoint.collider == boidCollider) continue;

                Vector2 oppositeDirection = boidPosition - hitPoint.point ; 
                //the further away from boid to object, the lesser magnitude of repulsion
                float magnitudeOfReplusion = 1 / oppositeDirection.magnitude;

                resultantDirection += oppositeDirection.normalized;

                speedOfRepulsion += magnitudeOfReplusion * FlockCreator.WEIGHT_SEPERATION;
                count++;
                //the resulting seperation will be added to the resultant vector of the boid
                //the added vector is the direction of the vector that is scaled by the magnitude and the weight of seperation
            }
            //find the average
            if( count > 0 )
            {
                //find the average
                speedOfRepulsion /= count;
                resultantDirection.Normalize();
            }

            TargetDirection += (Vector3)resultantDirection * FlockCreator.WEIGHT_SEPERATION;
            TargetSpeed = Speed + speedOfRepulsion * Speed;
        }

        private void AlignmentBehaviour()
        {
            var boidsNearby = Physics2D.CircleCastAll(transform.position,
                FlockCreator.AlignmentRadius,
                Vector2.zero);
            
            Vector2 resultantDirection = Vector2.zero;
            foreach(var boid  in boidsNearby)
            {
                if (boid.collider == boidCollider) continue; //make sure it does not reference the current boid
                if (boid.collider.TryGetComponent<Boid>(out var boidComponent))
                {
                    if(boidComponent.FlockCreator != FlockCreator) continue; //make sure it is the correct boid
                    resultantDirection += (Vector2)boidComponent.TargetDirection;
                }
            }
            TargetDirection += (Vector3) (resultantDirection.normalized * FlockCreator.WEIGHT_ALIGNMENT);
            //afterwards (use the magnitude of the target direction as speed)
        }

        private void CohesionBehaviour()
        {
            Vector3 resultantVector =  FlockCreator.CohesionPoint - transform.position;
            TargetDirection += resultantVector * FlockCreator.WEIGHT_COHESION;
        }

        private void CompleteBoidBehaviour()
        {
            if (FlockCreator.useSeparationRule)
            {
                SeperationBehaviour();
            }
            if (FlockCreator.useCohesionRule)
            {
                CohesionBehaviour();
            }
            if(FlockCreator.useAlignmentRule)
            {
                AlignmentBehaviour();
            }
        }
        #endregion

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

        private void BounceBoid()
        {
            //Vector2 newDirection = Vector2.zero;
            Bounds boxBound = FlocksController.Instance.BoxCollider2D.bounds;
            //for the x axis
            Vector2 pos = transform.position;
            Vector2 newTargetPosition = TargetDirection;

            float padding = 5f;
            if (pos.x > boxBound.max.x - padding)
            {
                //make sure the boids 
                newTargetPosition.x = -1f;
            }
            else if (pos.x < boxBound.min.x + padding)
            {
                //teleport boid to the right side of the map
                newTargetPosition.x = 1f;
            }
            //for the y axis
            if (pos.y > boxBound.max.y - padding)
            {
                //teleport boid to the bottom of the map
                newTargetPosition.y = -1f;
            }
            else if (pos.y < boxBound.min.y + padding)
            {
                //teleport boid to the top of the map
                newTargetPosition.y = 1f;
            }
            TargetDirection = (Vector3)newTargetPosition;
        }
        #endregion

        //private void BounceBoid()
        //{

        //}

        /*
         what to do
        1. cant use the find vector then move according to the vector
        2. find the target direction for rotation
        3. add the speed accordingly
        
        problem so far:
        1. how to know which direction would be better for the boid?
        2. how to calculate the speed of the boid based on the seperation and alignment of the boid
         */

        #region Move base on values

        //This function will require target speed for the speed
        private void MoveBoid()
        {
            //add speed is for making the speed faster or slower depending
            //on the three behaviour
            float addSpeed = ((TargetSpeed - Speed) / 10.0f);
            Speed = Speed + addSpeed * Time.deltaTime;

            if (Speed > MaxSpeed) //cap the next speed
                Speed = MaxSpeed;

            transform.Translate(Vector3.right * Speed * Time.deltaTime, Space.Self);
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
            var ObjectsNearby = Physics2D.CircleCastAll(transform.position,
                FlockCreator.SeparationRadius,
                Vector2.zero);
            Gizmos.DrawWireSphere(transform.position, FlockCreator.SeparationRadius);
            Gizmos.color = Color.red;
            foreach(var boid  in ObjectsNearby)
            {
                Gizmos.DrawLine(transform.position, boid.transform.position);
            }
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, transform.position + TargetDirection.normalized);
            //Gizmos.DrawSphere(transform.position, FlockCreator.SeparationRadius);
            //Gizmos.DrawRay(transform.position, TargetDirection.normalized);
        }
        //static public Vector3 GetRandom(Vector3 min, Vector3 max)
        //{
        //    return new Vector3(
        //        Random.Range(min.x, max.x),
        //        Random.Range(min.y, max.y),
        //        Random.Range(min.z, max.z));
        //}
        void SetRandomSpeed()
        {
            //Speed = Random.Range(0.0f, MaxSpeed);
            Speed = 10f;
        }

        void SetRandomDirection()
        {
            float angle = Random.Range(-180.0f, 180.0f);
            //making sure the boid are facing at any angle
            Vector2 dir = new Vector2(Mathf.Cos(Mathf.Deg2Rad * angle), Mathf.Sin(Mathf.Deg2Rad * angle));
            dir.Normalize();
            TargetDirection = dir;
        }

        public void SetColor(Color c)
        {
            spriteRenderer.color = c;
        }
        #endregion
    }
}