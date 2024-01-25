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

        //[HideInInspector]
        //public List<Autonomous> mAutonomous = new List<Autonomous>();
        //public List<MovementObject> movementObjects = new List<MovementObject>();
        [HideInInspector]
        public List<Transform> transforms = new List<Transform>();

        //this job handle for every flock so that the main thread can later on access the native data 
        //this flocking data
        [HideInInspector] public JobHandle job;
        //[HideInInspector] public NativeArray<MovementObject> partitionBoids;

        //this is a temporary new nativeMovementObjects data used so that it can pass the data onto the movement job.
        [HideInInspector] public NativeArray<MovementObject> NativeOutputMovementObjects;
        
        //this is to store the transformaccessarray so that the job system can use it as well as remove it once it is done
        [HideInInspector] public TransformAccessArray nativeTransformAccessArray;
        //native movementObject is used to store all the information about the boids for the job system to use
        [HideInInspector] public NativeList<MovementObject> nativeMovementObjects;

        public Flock()
        {
        }
    }
}
