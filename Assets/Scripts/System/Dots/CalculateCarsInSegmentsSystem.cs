using UnityEngine;
using Unity.Collections;
using Unity.Entities;


/// <summary>
/// Calculates cars inside of the segments in the beginning of the frame and adds them to NativeMultiHashMap
/// later access
/// </summary>
[UpdateInGroup(typeof(TrafficSimulationGroup))]
public class CalculateCarsInSegmentsSystem : SystemBase
{
    public static NativeMultiHashMap<Entity, VehicleSegmentData> VehiclesSegmentsHashMap;
    private SyncPointSystem syncPointSystem;

    protected override void OnCreate()
    {
        try
        {
            Debug.Log("CalculateCarsInSegmentsSystem>OnCreate");
            VehiclesSegmentsHashMap = new NativeMultiHashMap<Entity, VehicleSegmentData>(0, Allocator.Persistent);
            syncPointSystem = World.GetExistingSystem<SyncPointSystem>();
            base.OnCreate();
        }
        catch (System.Exception e)
        {

            throw e;
        }

    }

    protected override void OnDestroy()
    {
        try
        {
            Debug.Log("CalculateCarsInSegmentsSystem>OnDestroy");
            VehiclesSegmentsHashMap.Dispose();
            base.OnDestroy();
        }
        catch (System.Exception e)
        {
            throw e;
        }
   
    }

    protected override void OnUpdate()
    {
        try
        {

     
        Debug.Log("CalculateCarsInSegmentsSystem>OnUpdate");
        VehiclesSegmentsHashMap.Clear();
        EntityQuery entityQuery = GetEntityQuery(typeof(VehiclePositionComponent));
        if (entityQuery.CalculateEntityCount() > VehiclesSegmentsHashMap.Capacity)
        {
            VehiclesSegmentsHashMap.Capacity = entityQuery.CalculateEntityCount();
        }

        NativeMultiHashMap<Entity, VehicleSegmentData>.ParallelWriter multiHashMap = VehiclesSegmentsHashMap.AsParallelWriter();
        Dependency = Entities.ForEach((Entity entity, int entityInQueryIndex,
            in VehicleSegmentInfoComponent vehicleSegmentInfoComponent,
            in VehiclePositionComponent vehiclePositionComponent,
            in VehicleConfigComponent vehicleConfigComponent) =>
        {
            Entity segmentEntity = vehicleSegmentInfoComponent.IsBackInPreviousSegment
                ? vehicleSegmentInfoComponent.PreviousSegment
                : vehicleSegmentInfoComponent.HeadSegment;
            multiHashMap.Add(segmentEntity, new VehicleSegmentData
            {
                Entity = entity,
                BackSegPosition = vehiclePositionComponent.BackSegPos,
                VehicleSize = vehicleConfigComponent.Length
            });
        }).ScheduleParallel(Dependency);

        syncPointSystem.AddJobHandleForProducer(Dependency);
        }
        catch (System.Exception e)
        {

            throw e;
        }
    }
}
[UpdateInGroup(typeof(SimulationSystemGroup))]
public class TrafficSimulationGroup : ComponentSystemGroup
{
}

public struct VehicleSegmentData
{
    public Entity Entity;
    public float BackSegPosition;
    public float VehicleSize;
}

[UpdateAfter(typeof(CalculateCarsInSegmentsSystem))]
[UpdateInGroup(typeof(TrafficSimulationGroup))]
public class SyncPointSystem : EntityCommandBufferSystem
{
     
}


/// <summary>
/// Helper class to simplify readings from NativeHashMap which includes which vehicles are currently in which segment
/// </summary>
public struct VehiclesInSegmentHashMapHelper
{
    public void FindVehicleInFrontInSegment(
        NativeMultiHashMap<Entity, VehicleSegmentData> vehicleSegmentMap,
        Entity segmentEntity,
        float vehicleHeadPosition,
        ref Entity nextVehicleEntity,
        ref float nextVehicleBackPosition
    )
    {
        Debug.Log("CalculateCarsInSegmentsSystem>FindVehicleInFrontInSegment");
        NativeMultiHashMapIterator<Entity> nativeMultiHashMapIterator;
        if (vehicleSegmentMap.TryGetFirstValue(segmentEntity, out var segmentData, out nativeMultiHashMapIterator))
        {
            do
            {
                if (!(vehicleHeadPosition < segmentData.BackSegPosition))
                    continue;

                if (nextVehicleEntity == Entity.Null)
                {
                    //no next vehicle, assign
                    nextVehicleEntity = segmentData.Entity;
                    nextVehicleBackPosition = segmentData.BackSegPosition;
                }
                else
                {
                    if (segmentData.BackSegPosition < nextVehicleBackPosition)
                    {
                        nextVehicleEntity = segmentData.Entity;
                        nextVehicleBackPosition = segmentData.BackSegPosition;
                    }
                }
            } while (vehicleSegmentMap.TryGetNextValue(out segmentData, ref nativeMultiHashMapIterator));
        }
    }

    public bool IsSpaceAvailableAt(
        NativeMultiHashMap<Entity, VehicleSegmentData> vehicleSegmentMap,
        Entity segmentEntity,
        float position,
        float vehicleSize
    )
    {
        Debug.Log("CalculateCarsInSegmentsSystem>IsSpaceAvailableAt");
        NativeMultiHashMapIterator<Entity> nativeMultiHashMapIterator;
        var vehicleFrontPos = position + vehicleSize / 2;
        var vehicleBackPos = vehicleFrontPos - vehicleSize;
        var canFit = true;
        if (vehicleSegmentMap.TryGetFirstValue(segmentEntity, out var segmentData, out nativeMultiHashMapIterator))
        {
            do
            {
                if (vehicleFrontPos < segmentData.BackSegPosition)
                    continue;

                if (vehicleBackPos > segmentData.BackSegPosition + segmentData.VehicleSize)
                    continue;

                canFit = false;

            } while (vehicleSegmentMap.TryGetNextValue(out segmentData, ref nativeMultiHashMapIterator));
        }

        return canFit;
    }

    public bool HasVehicleInSegment(
        NativeMultiHashMap<Entity, VehicleSegmentData> vehicleSegmentMap,
        Entity segmentEntity
    )
    {
        Debug.Log("CalculateCarsInSegmentsSystem>HasVehicleInSegment");
        return vehicleSegmentMap.CountValuesForKey(segmentEntity) > 0;
    }
}