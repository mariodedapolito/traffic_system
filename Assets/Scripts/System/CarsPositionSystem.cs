using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;


public class CarsPositionSystem : SystemBase
{

    public static NativeHashMap<int, char> carsPositionMap;

    public const int zMultiplier = 1000;

    private EntityQuery query;

    public static int GetPositionHashMapKey(float3 position)
    {
        int xPosition = (int)position.x;
        int zPosition = (int)position.z;
        return zPosition * zMultiplier + xPosition;
    }

    protected override void OnCreate()
    {
        carsPositionMap = new NativeHashMap<int, char>(0, Allocator.Persistent);
        Debug.Log("Created car position system");
        base.OnCreate();
    }

    protected override void OnDestroy()
    {
        carsPositionMap.Dispose();
        base.OnDestroy();
    }

    protected override void OnUpdate()
    {
        carsPositionMap.Clear();

        int numVehicles = query.CalculateEntityCount();
        if (numVehicles > carsPositionMap.Capacity)
        {
            carsPositionMap.Capacity = numVehicles;
        }

        Entities
                .WithoutBurst()
                .WithStoreEntityQueryInField(ref query)
                .ForEach((in Translation translation, in Vehicle vehicle, in LocalToWorld ltw) =>
               {
                   int hashMapKey = GetPositionHashMapKey(translation.Value);
                   carsPositionMap.TryAdd(hashMapKey, '1');
               }).Run();



    }

}
