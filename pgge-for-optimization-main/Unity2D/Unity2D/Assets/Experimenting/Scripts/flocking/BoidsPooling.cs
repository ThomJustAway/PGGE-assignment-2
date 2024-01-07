using experimenting;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace experimenting
{
    public class BoidsPooling : MonoBehaviour
    {
        private Flock flock;
        [HideInInspector] public List<Transform> boidsTransform;
        [HideInInspector] public List<Boid> boidsInformation;
        public ComputeBuffer otherBoidsComputeBuffer;
        public int activatedBoids { get; private set; }
        [SerializeField] private int capacity = 5000;

        private void Init(Flock data)
        {
            flock = data;
            activatedBoids = data.numBoids;
            otherBoidsComputeBuffer = new ComputeBuffer(capacity , Boid.sizeOfData());
        }

        private void InitializedCapacity()
        {
            for(int i = 0; i < capacity; i++)
            {

            }
        }

        void AddBoid()
        {
            Bounds bound = FlockBehaviourImproveV2.Instance.Bounds.bounds;
            float x = UnityEngine.Random.Range(bound.min.x, bound.max.x);
            float y = UnityEngine.Random.Range(bound.min.y, bound.max.y);

            Vector3 RandomPosition = new Vector3(x, y, 0.0f);
            GameObject obj = Instantiate(flock.PrefabBoid);
            obj.transform.position = RandomPosition;
            obj.name = "Boid_" + flock.name + "_" + flock.boidsTransform.Count;

            var rotation = obj.transform.rotation;
            float4 currentRotation = new float4(rotation.x, rotation.y, rotation.z, rotation.w);

            Boid boid = new Boid((uint)flock.boidsTransform.Count
                , obj.transform.position,
                GetRandomDirection(),
                currentRotation,
                GetRandomSpeed(flock.maxSpeed)
                );
            //the index of the array is what makes the correlation between the two
            flock.boidsInformation.Add(boid);
            flock.boidsTransform.Add(obj.transform);
        }

        float GetRandomSpeed(float MaxSpeed)
        {
            //return UnityEngine.Random.Range(0.0f, MaxSpeed);
            return MaxSpeed;
        }

        float3 GetRandomDirection()
        {
            float angle = UnityEngine.Random.Range(-180.0f, 180.0f);
            Vector2 dir = new Vector2(Mathf.Cos(Mathf.Deg2Rad * angle), Mathf.Sin(Mathf.Deg2Rad * angle)); //make it face a certain direction
            dir.Normalize();

            return new float3(dir.x, dir.y, 1);
        }
    }
}