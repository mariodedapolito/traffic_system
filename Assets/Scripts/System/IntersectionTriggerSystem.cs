using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;

[UpdateBefore(typeof(CarsPositionSystem))]
public class IntersectionTriggerSystem : SystemBase
{

    EndSimulationEntityCommandBufferSystem m_EndSimulationEcbSystem;

    public static NativeHashMap<int, int> intersectionIdMap;
    public static NativeHashMap<int, int4> triggerMap;

    public const int xMultiplier = 1000000;

    private const int INTERSECTION_DIRECTION = 0;
    private const int INTERSECTION_ENTER_EXIT = 1;
    private const int INTERSECTION_TYPE = 2;
    private const int INTERSECTION_ROADS = 3;

    private const int INTERSECTION_SIMPLE = 0;
    private const int INTERSECTION_SEMAPHORE = 1;

    private const int INTERSECTION_ENTER = 0;
    private const int INTERSECTION_EXIT = 1;

    private EntityQuery query;

    public static int GetNodeHashMapKey(float3 position)
    {
        int xPosition = (int)position.x;
        int zPosition = (int)position.z;
        return xPosition * xMultiplier + zPosition;
    }

    protected override void OnCreate()
    {
        m_EndSimulationEcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        intersectionIdMap = new NativeHashMap<int, int>(0, Allocator.Persistent);
        triggerMap = new NativeHashMap<int, int4>(0, Allocator.Persistent);
        base.OnCreate();
    }

    protected override void OnDestroy()
    {
        intersectionIdMap.Dispose();
        triggerMap.Dispose();
        base.OnDestroy();
    }

    protected override void OnUpdate()
    {
        int numNodes = query.CalculateEntityCount() * 4 + intersectionIdMap.Count();
        if (numNodes > intersectionIdMap.Capacity)
        {
            intersectionIdMap.Capacity = numNodes;
            triggerMap.Capacity = numNodes;
        }


        var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().ToConcurrent();

        Entities
                .WithoutBurst()
                .WithStoreEntityQueryInField(ref query)
                .ForEach((Entity entity, int entityInQueryIndex, DynamicBuffer<IntersectionTriggerNodes> triggerNodesList, in IntersectionTriggerData intersectionData, in IntersectionTrigger intersectionComponent) =>
                {
                    for (int i = 0; i < triggerNodesList.Length; i++)
                    {
                        int keyPos = GetNodeHashMapKey(triggerNodesList[i].triggerPosition);

                        intersectionIdMap.Add(keyPos, intersectionData.intersectionId);

                        int4 triggerData = new int4(intersectionData.directionId,
                                                    intersectionData.isIntersectionEnter ? INTERSECTION_ENTER : INTERSECTION_EXIT,
                                                    intersectionData.isSimpleIntersection ? INTERSECTION_SIMPLE : INTERSECTION_SEMAPHORE,
                                                    intersectionData.intersectionNumRoads);
                        triggerMap.Add(keyPos, triggerData);
                    }

                    ecb.RemoveComponent<IntersectionTrigger>(entityInQueryIndex, entity);

                }).Schedule();

    }

}

//using Unity.Collections;
//using Unity.Entities;
//using Unity.Physics;
//using Unity.Physics.Systems;
//using Unity.Burst;
//using Unity.Jobs;
//using UnityEngine;
//using UnityEngine.EventSystems;

//[UpdateAfter(typeof(CarsPositionSystem))]
//[UpdateAfter(typeof(EndFramePhysicsSystem))]    //run script logic after all physics are updated on this frame
//public class IntersectionTriggerSystem : JobComponentSystem
//{
//    private BuildPhysicsWorld buildPhysicsWorld;    //setting up colliders and entities
//    private StepPhysicsWorld stepPhysicsWorld;      //running physics simulations

//    protected override void OnCreate()
//    {
//        buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
//        stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
//    }

//    private struct IntersectionTriggerJob : ITriggerEventsJob
//    {
//        [ReadOnly] public ComponentDataFromEntity<IntersectionTriggerData> IntersectionTriggerDataGroup;
//        public ComponentDataFromEntity<VehicleNavigation> navigationGroup;

//        public void Execute(TriggerEvent triggerEvent)
//        {
//            //Both colliders are triggers
//            if(IntersectionTriggerDataGroup.Exists(triggerEvent.EntityA) && IntersectionTriggerDataGroup.Exists(triggerEvent.EntityB))
//            {
//                return;
//            }

//            if (IntersectionTriggerDataGroup.Exists(triggerEvent.EntityA) && navigationGroup.Exists(triggerEvent.EntityB))
//            {
//                //Entity A : intersection trigger
//                //Entity B : car 
//                IntersectionTriggerData intersectionTrigger = IntersectionTriggerDataGroup[triggerEvent.EntityA];
//                VehicleNavigation navigation = navigationGroup[triggerEvent.EntityB];

//                if (intersectionTrigger.isIntersectionEnter && !navigation.intersectionStop && !navigation.intersectionCrossing)    //car is entering the intersection
//                {
//                    navigation.intersectionStop = true;
//                    navigation.intersectionCrossed = false;
//                    navigation.intersectionId = intersectionTrigger.intersectionId;
//                    navigation.intersectionDirection = intersectionTrigger.directionId;
//                    navigation.intersectionNumRoads = intersectionTrigger.intersectionNumRoads;
//                    navigation.isSemaphoreIntersection = intersectionTrigger.isSemaphoreIntersection;
//                    navigation.isSimpleIntersection = intersectionTrigger.isSimpleIntersection;
//                    navigationGroup[triggerEvent.EntityB] = navigation;

//                }
//                else if(intersectionTrigger.isIntersectionExit && navigation.intersectionCrossing)
//                {
//                    navigation.intersectionCrossed = true;
//                    navigation.intersectionCrossing = false;
//                    navigation.intersectionId = -1;
//                    navigation.intersectionDirection = -1;
//                    navigation.intersectionNumRoads = -1;
//                    navigation.isSemaphoreIntersection = false;
//                    navigation.isSimpleIntersection = false;
//                    navigationGroup[triggerEvent.EntityB] = navigation;
//                }
//                else
//                {
//                    return;
//                }
//                navigation.intersectionStop = true;
//                navigationGroup[triggerEvent.EntityB] = navigation;
//                //Debug.Log("INTERSECTION REACHED");
//            }
//            else if (IntersectionTriggerDataGroup.Exists(triggerEvent.EntityB) && navigationGroup.Exists(triggerEvent.EntityA))
//            {
//                //Entity B : intersection trigger
//                //Entity A : car 
//                IntersectionTriggerData intersectionTrigger = IntersectionTriggerDataGroup[triggerEvent.EntityB];
//                VehicleNavigation navigation = navigationGroup[triggerEvent.EntityA];
//                if (intersectionTrigger.isIntersectionEnter && !navigation.intersectionStop && !navigation.intersectionCrossing)
//                {
//                    navigation.intersectionStop = true;
//                    navigation.intersectionCrossed = false;
//                    navigation.intersectionId = intersectionTrigger.intersectionId;
//                    navigation.intersectionDirection = intersectionTrigger.directionId;
//                    navigation.intersectionNumRoads = intersectionTrigger.intersectionNumRoads;
//                    navigation.isSemaphoreIntersection = intersectionTrigger.isSemaphoreIntersection;
//                    navigation.isSimpleIntersection = intersectionTrigger.isSimpleIntersection;
//                    navigationGroup[triggerEvent.EntityA] = navigation;
//                }
//                else if (intersectionTrigger.isIntersectionExit && navigation.intersectionCrossing)
//                {
//                    navigation.intersectionCrossed = true;
//                    navigation.intersectionCrossing = false;
//                    navigation.intersectionId = -1;
//                    navigation.intersectionDirection = -1;
//                    navigation.intersectionNumRoads = -1;
//                    navigation.isSemaphoreIntersection = false;
//                    navigation.isSimpleIntersection = false;
//                    navigationGroup[triggerEvent.EntityA] = navigation;
//                }
//                else
//                {
//                    return;
//                }
//            }
//        }
//    }

//    protected override JobHandle OnUpdate(JobHandle inputDeps)
//    {
//        //Debug.Log("Hello world");
//        var triggerJob = new IntersectionTriggerJob();
//        triggerJob.IntersectionTriggerDataGroup = GetComponentDataFromEntity<IntersectionTriggerData>(true);
//        triggerJob.navigationGroup = GetComponentDataFromEntity<VehicleNavigation>(false);

//        JobHandle jobHandle = triggerJob.Schedule(stepPhysicsWorld.Simulation, ref buildPhysicsWorld.PhysicsWorld, inputDeps);
//        jobHandle.Complete();   //flush job & return container ownership
//        return jobHandle;
//    }

//}
