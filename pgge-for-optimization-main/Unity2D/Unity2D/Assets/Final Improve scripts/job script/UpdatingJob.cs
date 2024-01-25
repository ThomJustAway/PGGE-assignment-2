//using experimenting2;
//using System.Collections;
//using Unity.Burst;
//using Unity.Collections;
//using Unity.Entities.UniversalDelegates;
//using Unity.Jobs;
//using UnityEngine;
//using UnityEngine.Jobs;


//old legacy code for experimenting
//namespace Assets.Experiment_2.job_script
//{
//    [BurstCompile]
//    public struct UpdatingJob : IJobParallelFor
//    {
//        [ReadOnly] public NativeArray<MovementObject> newData;
//        [ReadOnly] public TransformAccessArray transforms;
//        public NativeList<MovementObject> currentData;

//        public void Execute(int index)
//        {
//            var boidResult = newData[index];
//            boidResult.position = transforms[index].position;
//            currentData[index] = boidResult;
//        }
//    }
//}