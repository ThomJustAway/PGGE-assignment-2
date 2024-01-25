using experimenting;
using System.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace experimenting2
{

    //this is the job for moving the boid transform. use burst compile 
    //to compile it to highly optimise code.
    [BurstCompile]
    public struct MovingMovementObject : IJobParallelForTransform
    {
        //will only be reading data found from the flocking movement
        [NativeDisableParallelForRestriction]
        public NativeArray<MovementObject> boidsData;

        public float deltaTime; //delta time is pass as it time.delta time cant be used in jobs
        public DataRule rulesData; //rules data for the max speed and rotation.



        public void Execute(int index, TransformAccess transform)
        {//the index are the same after all...
            MovementObject currentBoid = boidsData[index];

            //ensure it is the current position is similar to what was found in the flocking movement
            transform.position = currentBoid.position; 

            RotateGameObjectBasedOnTargetDirection(currentBoid, transform); //will rotate the boid
            MoveObject(currentBoid, transform); //will then move the boid

            currentBoid.position = transform.position;

            boidsData[index] = currentBoid;
        }

        //this is the function used to move the game object
        private void MoveObject(MovementObject curBoid, TransformAccess transform)
        {
            //this is to find the boid speed based on the target speed found in the boidsflockingMovement job
            curBoid.speed = curBoid.speed +
                ((curBoid.targetSpeed - curBoid.speed) / 10.0f) * deltaTime;

            if (curBoid.speed > rulesData.maxSpeed) //cap the next speed
                curBoid.speed = rulesData.maxSpeed;

            //this is to find where the boid should move based on the rotation of the boid
            float3 vectorToMove = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z) * new float3(1, 0, 0);

            //find the new position of the boid
            float3 currentPosition = transform.position;
            currentPosition += (vectorToMove * curBoid.speed * deltaTime);

            //move the transform based of that current position. This will simulate movement of the boids
            transform.position = currentPosition;
        }

        //This is the function to rotate the game object
        private void RotateGameObjectBasedOnTargetDirection(MovementObject curBoid, TransformAccess transform)
        {
            //do another normalize of the target direction to find the final direction the boid need to go
            float3 targetDirection = NormalizeFloat3(curBoid.targetDirection);

            float3 rotatedVectorToTarget =
                Quaternion.Euler(0, 0, 90) *
                targetDirection;
            //find the target vector to rotate to. The Quaternion.Euler(0, 0, 90) is to offset due to the prefab which is already 90 degree

            Quaternion targetRotation = Quaternion.LookRotation(
                forward: Vector3.forward, //want to rotate the object through the z axis
                upwards: rotatedVectorToTarget);
            //then create a rotation based of the vector. 
            //from: vector3.up to: rotatedVectorToTarget

            //give out the rotation that the boid should move and add that into the current boid transform.
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rulesData.maxRotationSpeed * deltaTime); 
        }

        private float3 NormalizeFloat3(float3 vector)
        {
            // Convert to Vector3, normalize, and convert back to float3
            return ((Vector3)vector).normalized;
        }
    }
}