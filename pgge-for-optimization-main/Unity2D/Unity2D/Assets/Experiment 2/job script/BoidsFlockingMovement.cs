using Assets.Improve_scripts.Jobs;
using experimenting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using static Unity.Collections.AllocatorManager;

namespace experimenting2
{
    [BurstCompile]
    public struct BoidsFlockingMovement : IJobParallelFor
    {
        [ReadOnly] public NativeList<MovementObject> AllTheBoids;
        [ReadOnly] public NativeList<MovementObject> predatorBoids;

        [ReadOnly] public NativeArray<BoidsObstacle> obstacles;
        public NativeArray<MovementObject> output;
        public DataRule rules;
        public Bounds boxBound;

        public void Execute(int index) //will look index
        {
            MovementObject curr = StartCalculatingFlockingRules(index);
            curr = DoRandomMovement(curr);
            curr = DoAvoidObstacleBehaviour(curr);
            curr = HandleBoundries(curr);
            curr = DoAvoidPredatorBoidsBehaviour(curr);
            //add this together to form the final direction needed for the flock.
            output[index] = curr;
        }


        //handle the main flocking behaviour
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
            //else
            //{ //if it is equal to 0 then have some changes to the values because
            //  //the boids can act abit weird
            //    float randx = Mathf.Cos(curr.position.x);
            //    float randy = Mathf.Sin(curr.position.y);

            //    separationDir = new float3(randx, randy, 0);

            //    float randomLerpValue = separationDir.x;
            //    if (randomLerpValue < 0)
            //    {
            //        randomLerpValue = -randomLerpValue; //make it positive
            //    }
            //    randx = Mathf.Lerp(boxBound.min.x, boxBound.max.x, randomLerpValue);
            //    randy = Mathf.Lerp(boxBound.min.y, boxBound.max.y, randomLerpValue);
            //    steerPos = new float3(randx, randy, 0);
            //}

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
        //for random movement
        private MovementObject DoRandomMovement(MovementObject boid)
        {
            if (!rules.useRandomRule) return boid;

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
        //for avoiding obstacles
        private MovementObject DoAvoidObstacleBehaviour(MovementObject boid)
        {
            if (!rules.useAvoidObstaclesRule) return boid;

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
        //for handling boundries

        private MovementObject DoAvoidPredatorBoidsBehaviour(MovementObject boid)
        {
            //ignore this rules if the boid dont have to flee predator or if the boid is a predator
            if(!rules.useFleeOnSightEnemyRule || rules.isPredator) return boid;
            
            //do the calculation
            foreach(var predator in predatorBoids)
            {
                var targetDirection = predator.position - boid.position;
                var distanceFromEnemy = Magnitude(targetDirection); 
                if (distanceFromEnemy < rules.enemySeparationDistance)
                { //within range of the visible enemy
                    targetDirection = Normalise(targetDirection);
                    //                boids[i].TargetDirection += targetDirection;
                    //                boids[i].TargetDirection.Normalize();

                    //                boids[i].TargetSpeed += dist * sepWeight;
                    //                boids[i].TargetSpeed /= 2.0f;
                    boid.targetDirection += targetDirection;
                    boid.targetDirection = Normalise(boid.targetDirection);

                    boid.targetSpeed += distanceFromEnemy * rules.WEIGHT_FLEE_ENEMY_ON_SIGHT;
                    boid.targetSpeed /= 2.0f;
                }
            }
            return boid;

        }

        #region handle Boundary
        private MovementObject HandleBoundries(MovementObject boid)
        {
            if (rules.bounceWall)
            {
                return BounceBoid(boid);
            }
            else
            {
                return TeleportBoid(boid);
            }
        }
        private MovementObject TeleportBoid(MovementObject boid)
        {
            //reduce extern calls
            Vector3 pos = boid.position;

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

            boid.position = pos;
            return boid;
        }

        private MovementObject BounceBoid(MovementObject curBoid)
        {
            Vector3 pos = curBoid.position;

            curBoid.targetDirection = Normalise(curBoid.targetDirection);

            //for horizontal bounds
            if (pos.x + 5.0f > boxBound.max.x)
            {
                //if near the right bound box, force it to go left
                curBoid.targetDirection.x = -1.0f;
            }
            else if (pos.x - 5.0f < boxBound.min.x)
            {
                //if near the left bound box, force it to go right
                curBoid.targetDirection.x = 1.0f;
            }
            //for vectical bounds
            if (pos.y + 5.0f > boxBound.max.y)
            {
                //if near the top bound box, force it to go down
                curBoid.targetDirection.y = -1.0f;
            }
            else if (pos.y - 5.0f < boxBound.min.y)
            {
                //if near the bottom bound box, force it to go up
                curBoid.targetDirection.y = 1.0f;
            }


            return curBoid;
        }
        #endregion

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