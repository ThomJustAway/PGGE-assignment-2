using experimenting2;
using System.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;



//Job to update the data from the new boid to the current boids
namespace Assets.Experiment_2.job_script
{
    //job to handle the updating of the current list
    [BurstCompile]
    public struct UpdatingJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<MovementObject> newData; //the new data
        public NativeArray<MovementObject> currentData; //the old data (from list to array

        public void Execute(int index)
        {
            //Replace the old boid with the new boid data.
            currentData[index] = newData[index];
        }
    }
}