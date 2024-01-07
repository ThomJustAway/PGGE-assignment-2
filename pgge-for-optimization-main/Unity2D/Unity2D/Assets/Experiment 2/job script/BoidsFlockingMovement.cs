using Assets.Improve_scripts.Jobs;
using experimenting;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Collections.AllocatorManager;

namespace experimenting2
{
    public struct BoidsFlockingMovement : IJobParallelFor
    {
        [ReadOnly] public NativeList<MovementObject> AllTheBoids;
        [ReadOnly] public NativeArray<BoidsObstacle> obstacles;
        public DataRule rules;
        public NativeArray<MovementObject> output;
        public void Execute(int index) //will look index
        {
            CalculateFlocking(index);
        }

        private void CalculateFlocking(int i)
        {
            MovementObject curr = StartCalculatingFlockingRules(i);

            if (rules.useRandomRule)
            {
                curr = DoRandomMovement(curr);
            }
            if (rules.useCohesionRule)
            {
                curr = DoAvoidObstacleBehaviour(curr);
            }

            //add this together to form the final direction needed for the flock.
            output[i] = curr;
        }

        private MovementObject StartCalculatingFlockingRules(int i)
        {
            float3 alignmentDir = Vector3.zero;
            float3 separationDir = Vector3.zero;

            float speed = 0.0f;
            float separationSpeed = 0.0f;

            int count = 0;

            float3 steerPos = Vector3.zero;

            var curr = AllTheBoids[i];

            for (int j = 0; j < AllTheBoids.Length; ++j)
            {
                //in this for loop, it will go through all the 
                //boid in the game and see if there is any boids
                //that is close to the selected boids

                var other = AllTheBoids[j];

                if (curr.id == other.id) continue; //if not the same then move on

                float3 direction = curr.position - other.position;
                float dist = Magnitude(direction);

                if (dist < rules.visibility)
                { //if it is around the current boid visible range (circle)
                    speed += other.speed;
                    alignmentDir += other.targetDirection;
                    steerPos += other.position;
                    count++; //use this count to find the average 
                }

                if (dist < rules.separationDistance)
                {//if the distance is lesser than the acceptable range

                    //usually this if statement is true so you
                    //can remove this all together

                    float3 targetDirection = Normalise(direction);
                    //get direction vector from other to current

                    separationDir += targetDirection;
                    separationSpeed += dist * rules.WEIGHT_SEPARATION;
                    //how much needs to be seperated base on the distance
                    //this formula can be tweak where the shorter the distance
                    //the more speed required to seperate the boids
                }
            }

            if (count > 0)
            {
                speed = speed / count;
                alignmentDir = alignmentDir / count;
                //getting the average speed and direction the flock needs to go

                alignmentDir = Normalise(alignmentDir);

                steerPos = steerPos / count;
                //finding the average position that the flock is going
            }

            float3 flockDirection = (steerPos - curr.position) *
                (rules.useCohesionRule ? rules.WEIGHT_COHESION : 0.0f);

            /*
            get the direction of the flock intended outcome
            if the cohesion is needed, then multiply it with
            the weight, else just ignore it
            */

            float3 separationDirection = separationDir * separationSpeed *
                (rules.useSeparationRule ? rules.WEIGHT_SEPARATION : 0.0f);
            /*
             Get the seperation direction need to seperate from the boid nearby
             This direction would be ignored if there is no seperation rule is not stated
             */

            float3 alignmentDirection = alignmentDir * speed *
                (rules.useAlignmentRule ? rules.WEIGHT_ALIGNMENT : 0.0f);
            /*
             Where the boid intended wants to go. 
             */


            curr.targetDirection = alignmentDirection +
                separationDirection +
                flockDirection;
            return curr;
        }

        MovementObject DoRandomMovement(MovementObject boid)
        {
            //autonomousList[i].TargetDirection.Normalize();
            boid.targetDirection = Normalise(boid.targetDirection);
            float rand = Mathf.Lerp(-1f, 1f, boid.targetDirection.x);
            float angle = Mathf.Atan2(boid.targetDirection.y, boid.targetDirection.x);

            if (rand > 0.5f)
            {
                angle += Mathf.Deg2Rad * 45.0f;
            }
            else
            {
                angle -= Mathf.Deg2Rad * 45.0f;
            }

            float3 dir = Vector3.zero;
            dir.x = Mathf.Cos(angle);
            dir.y = Mathf.Sin(angle);

            float speed = Mathf.Lerp(1.0f, rules.maxSpeed , boid.targetDirection.y);

            boid.targetSpeed += speed * rules.WEIGHT_SEPARATION;
            boid.targetSpeed /= 2.0f;

            boid.targetDirection += dir * rules.WEIGHT_RANDOM;

            boid.targetDirection = Normalise(boid.targetDirection);

            return boid;


            //average the speed for the boid
        }

        MovementObject DoAvoidObstacleBehaviour(MovementObject boid)
        {
            for (int j = 0; j < obstacles.Length; ++j)
            {
                var currentObstacle = obstacles[j];

                float dist = Magnitude(boid.position - currentObstacle.position);

                if (dist < currentObstacle.AvoidanceRadius)
                {
                    float3 targetDirection = Normalise(boid.position - currentObstacle.position);

                    boid.targetDirection += targetDirection * rules.WEIGHT_AVOID_OBSTACLES;
                    boid.targetDirection = Normalise(boid.targetDirection);
                }
            }
            return boid;
        }


        //void DoAvoidObstacleBehaviour(Flock flock)
        //{
        //    List<Autonomous> autonomousList = flock.mAutonomous;
        //    for (int i = 0; i < autonomousList.Count; ++i)
        //    {
        //        for (int j = 0; j < mObstacles.Count; ++j)
        //        {
        //            float dist = (
        //            mObstacles[j].transform.position -
        //            autonomousList[i].transform.position).magnitude;
        //            if (dist < mObstacles[j].AvoidanceRadius)
        //            {
        //                Vector3 targetDirection = (
        //                    autonomousList[i].transform.position -
        //                    mObstacles[j].transform.position).normalized;

        //                autonomousList[i].TargetDirection += targetDirection * flock.WEIGHT_AVOID_OBSTICLES;
        //                autonomousList[i].TargetDirection.Normalize();
        //            }
        //        }
        //    }
        //}

        #region equation
        private bool IsEqual(float2 a, float2 b)
        {
            return (a.x == b.x && a.y == b.y);
        }

        private bool IsEqual(float3 a, float3 b)
        {
            return (a.x == b.x && a.y == b.y && a.z == b.z);
        }

        private float Magnitude(float3 vector)
        {
            return ((Vector3) vector).magnitude;
        }

        private float FindDistance(float3 a, float3 b)
        {
            float3 distanceVector = a - b;
            //ignore z because we doing 2d
            return ((Vector3)distanceVector).magnitude;
        }

        private float3 Normalise(float3 a)
        {
            return ((Vector3)a).normalized;
        }

        #endregion 

    }
}