using experimenting;
using System.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace experimenting2
{
    [BurstCompile]
    public struct MovingMovementObject : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<MovementObject> boidsData;
        public float deltaTime;
        public DataRule rulesData;
        public Bounds boxBound;

        //problem: why does the boids disappear after a set frame?

        public void Execute(int index, TransformAccess transform)
        {//the index are the same after all...

            MovementObject currentBoid = boidsData[index];
            
            transform.position = currentBoid.position; //ensure it is the current position it was last time

            RotateGameObjectBasedOnTargetDirection(currentBoid, transform);
            MoveObject(currentBoid, transform);
            //currentBoid.position = transform.position;
            //boidsData[index] = currentBoid; //update the value
        }

        private void MoveObject(MovementObject curBoid, TransformAccess transform)
        {
            curBoid.speed = curBoid.speed +
                ((curBoid.targetSpeed - curBoid.speed) / 10.0f) * deltaTime;

            if (curBoid.speed > rulesData.maxSpeed) //cap the next speed
                curBoid.speed = rulesData.maxSpeed;

            float3 vectorToMove = Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z) * new float3(1, 0, 0);

            float3 currentPosition = transform.position;
            currentPosition += (vectorToMove * curBoid.speed * deltaTime);

            transform.position = currentPosition;
        }

        private void RotateGameObjectBasedOnTargetDirection(MovementObject curBoid, TransformAccess transform)
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
                rulesData.maxRotationSpeed * deltaTime); //give out the next rotation
        }

        private float3 NormalizeFloat3(float3 vector)
        {
            // Convert to Vector3, normalize, and convert back to float3
            return ((Vector3)vector).normalized;
        }
    }
}