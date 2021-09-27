using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.EventSystems;


[UpdateAfter(typeof(EndFramePhysicsSystem))]    //run script logic after all physics are updated on this frame
public class IntersectionTriggerSystem : JobComponentSystem
{
    private BuildPhysicsWorld buildPhysicsWorld;    //setting up colliders and entities
    private StepPhysicsWorld stepPhysicsWorld;      //running physics simulations

    protected override void OnCreate()
    {
        buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
        stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
    }

    private struct IntersectionTriggerJob : ITriggerEventsJob
    {
        [ReadOnly] public ComponentDataFromEntity<IntersectionTriggerComponent> IntersectionTriggerComponentGroup;
        public ComponentDataFromEntity<VehicleNavigation> navigationGroup;

        public void Execute(TriggerEvent triggerEvent)
        {
            //Both colliders are triggers
            if(IntersectionTriggerComponentGroup.Exists(triggerEvent.EntityA) && IntersectionTriggerComponentGroup.Exists(triggerEvent.EntityB))
            {
                return;
            }

            if (IntersectionTriggerComponentGroup.Exists(triggerEvent.EntityA) && navigationGroup.Exists(triggerEvent.EntityB))
            {
                VehicleNavigation navigation = navigationGroup[triggerEvent.EntityB];
                navigation.intersectionStop = true;
                navigationGroup[triggerEvent.EntityB] = navigation;
                //Debug.Log("INTERSECTION REACHED");
            }
            else if (IntersectionTriggerComponentGroup.Exists(triggerEvent.EntityB) && navigationGroup.Exists(triggerEvent.EntityA))
            {
                VehicleNavigation navigation = navigationGroup[triggerEvent.EntityA];
                navigation.intersectionStop = true;
                navigationGroup[triggerEvent.EntityA] = navigation;
                //Debug.Log("INTERSECTION REACHED");
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        //Debug.Log("Hello world");
        var triggerJob = new IntersectionTriggerJob();
        triggerJob.IntersectionTriggerComponentGroup = GetComponentDataFromEntity<IntersectionTriggerComponent>(true);
        triggerJob.navigationGroup = GetComponentDataFromEntity<VehicleNavigation>(false);

        JobHandle jobHandle = triggerJob.Schedule(stepPhysicsWorld.Simulation, ref buildPhysicsWorld.PhysicsWorld, inputDeps);
        jobHandle.Complete();   //flush job & return container ownership
        return jobHandle;
    }

}
