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
public class ParkingSystem : SystemBase
{

    EndSimulationEntityCommandBufferSystem m_EndSimulationEcbSystem;

    public static NativeMultiHashMap<int, float3> parkingSpotsMap;
    public static NativeHashMap<int, int> parkingCapacityMap;
    public static NativeHashMap<int, int> parkingFreeSpotsMap;

    public const int xMultiplier = 1000000;

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
        parkingSpotsMap = new NativeMultiHashMap<int, float3>(0, Allocator.Persistent);
        parkingCapacityMap = new NativeHashMap<int, int>(0, Allocator.Persistent);
        parkingFreeSpotsMap = new NativeHashMap<int, int>(0, Allocator.Persistent);
        base.OnCreate();
    }

    protected override void OnDestroy()
    {
        parkingCapacityMap.Dispose();
        parkingFreeSpotsMap.Dispose();
        parkingSpotsMap.Dispose();
        base.OnDestroy();
    }

    protected override void OnUpdate()
    {
        int numNodes = query.CalculateEntityCount() + parkingCapacityMap.Count();
        if (numNodes > parkingCapacityMap.Capacity)
        {
            parkingCapacityMap.Capacity = numNodes;
            parkingFreeSpotsMap.Capacity = numNodes;
            parkingSpotsMap.Capacity = numNodes * 120;
        }


        var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().ToConcurrent();

        Entities
                .WithoutBurst()
                .WithStoreEntityQueryInField(ref query)
                .ForEach((Entity entity, int entityInQueryIndex, DynamicBuffer<ParkingSpotsList> parkingSpotsList, in ParkingData parkingData, in ParkingComponent parkingComponent) =>
                {
                    int keyPosGateway = GetNodeHashMapKey(parkingData.parkingGatewayPosition);
                    parkingCapacityMap.Add(keyPosGateway, parkingData.numParkingSpots);
                    parkingFreeSpotsMap.Add(keyPosGateway, parkingData.numFreeSpots);

                    for(int i=0; i<parkingSpotsList.Length; i++)
                    {
                        parkingSpotsMap.Add(keyPosGateway, parkingSpotsList[i].spotPosition);
                    }

                    ecb.RemoveComponent<ParkingComponent>(entityInQueryIndex, entity);

                }).Schedule();

    }

}