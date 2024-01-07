using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

namespace experimenting2
{
    [Serializable]
    public class Flock
    {
        public GameObject PrefabBoid;

        [Space(10)]
        [Header("A flock is a group of Automous objects.")]
        public string name = "Default Name";
        public int numBoids = 10;
        public Color colour = new Color(1.0f, 1.0f, 1.0f);

        public DataRule rules;

        [HideInInspector]
        public List<Autonomous> mAutonomous = new List<Autonomous>();
        //public List<MovementObject> movementObjects = new List<MovementObject>();
        [HideInInspector]
        public List<Transform> transforms = new List<Transform>();

        [HideInInspector] public JobHandle job;
        //[HideInInspector] public NativeArray<MovementObject> partitionBoids;
        [HideInInspector] public NativeList<MovementObject> nativeMovementObjects;
        [HideInInspector] public NativeArray<MovementObject> NativeOutputMovementObjects;

        [HideInInspector] public TransformAccessArray nativeTransformAccessArray;
        public Flock()
        {
        }
    }
}
