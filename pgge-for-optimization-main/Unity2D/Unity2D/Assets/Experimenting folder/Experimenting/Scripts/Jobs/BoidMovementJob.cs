using experimenting;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace experimenting
{
    public struct BoidMovementJob : IJobParallelForTransform
    {
        public float MaxSpeed;
        public NativeArray<Boid> boidsData;
        public float deltaTime;
        public float RotationSpeed;

        public Bounds boxBound;
        public bool canBounce;

        //problem: why does the boids disappear after a set frame?

        public void Execute(int index, TransformAccess transform)
        {//the index are the same after all...
            Boid currentBoid = boidsData[index];
            //check if on the edge
            if(canBounce )
            {
               currentBoid = BounceBoid(currentBoid, transform); //update the boids with new target direction
            }
            else
            {
                TeleportBoid(transform);
            }

            RotateGameObjectBasedOnTargetDirection(currentBoid , transform);
            MoveAutonomous(currentBoid, transform);
            currentBoid.position = transform.position;
            boidsData[index] = currentBoid; //update the value
        }

        private void MoveAutonomous(Boid curBoid , TransformAccess transform)
        {
            curBoid.speed = curBoid.speed + 
                (( curBoid.targetSpeed - curBoid.speed) / 10.0f) * deltaTime;

            if (curBoid.speed > MaxSpeed) //cap the next speed
                curBoid.speed = MaxSpeed;

            float3 vectorToMove = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z) * new float3(1, 0, 0);

            float3 currentPosition = transform.position;
            currentPosition += (vectorToMove * curBoid.speed * deltaTime);

            transform.position = currentPosition;
        }

        private void RotateGameObjectBasedOnTargetDirection(Boid curBoid, TransformAccess transform)
        {
            float3 targetDirection = NormalizeFloat3(curBoid.targetDirection);
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
                RotationSpeed * deltaTime); //give out the next rotation
        }

        private float3 NormalizeFloat3(float3 vector)
        {
            // Convert to Vector3, normalize, and convert back to float3
            return ((Vector3)vector).normalized;
        }

        private void TeleportBoid(TransformAccess transform)
        {
            //reduce extern calls
            Vector3 pos = transform.position;

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

        private Boid BounceBoid(Boid curBoid , TransformAccess transform)
        {

            Vector3 pos = transform.position;

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

            curBoid.targetDirection = NormalizeFloat3(curBoid.targetDirection);

            return curBoid;
        }
    }
}