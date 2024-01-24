using System.Collections;
using UnityEngine;

namespace experimenting2
{
    public class CustomObstacles : MonoBehaviour
    {
        public float AvoidanceRadiusMultFactor = 1.5f;
        public float AvoidanceRadius
        {
            get
            {
                return mCollider.radius * 3 * AvoidanceRadiusMultFactor;
            }
        }

        public BoidsObstacle obstacle;

        public CircleCollider2D mCollider;


        private void Start()
        {
            obstacle = new BoidsObstacle(transform.position, AvoidanceRadius);
        }

        private void Update()
        {
            obstacle.position = transform.position;
        }
    }
}