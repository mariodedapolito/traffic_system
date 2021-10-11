using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;

[UpdateBeforeAttribute(typeof(CarSystem))]
public class CarsPositionSystem : SystemBase
{

    public static NativeHashMap<int, char> carsPositionMap;
    public static NativeHashMap<int, char> carsParkingMap;
    public static NativeHashMap<int, int> intersectionQueueMap;
    public static NativeHashMap<int, int> intersectionCrossingMap;
    public static NativeArray<int> numActiveCars;

    public const int xMultiplier = 100000;

    private EntityQuery query;

    public static int GetPositionHashMapKey(float3 position)
    {
        int xPosition = (int)position.x;
        int zPosition = (int)position.z;
        return xPosition * xMultiplier + zPosition;
    }
    public static int GetIntersectionQueueHashMapKey(int intersectionId, int directionId)
    {
        return intersectionId * 10 + directionId;
    }

    public static int GetIntersectionCrossingHashMapKey(int intersectionId)
    {
        return intersectionId;
    }

    protected override void OnCreate()
    {
        carsPositionMap = new NativeHashMap<int, char>(0, Allocator.Persistent);
        carsParkingMap = new NativeHashMap<int, char>(0, Allocator.Persistent);
        intersectionQueueMap = new NativeHashMap<int, int>(10000, Allocator.Persistent);
        intersectionCrossingMap = new NativeHashMap<int, int>(1250, Allocator.Persistent);
        numActiveCars = new NativeArray<int>(1, Allocator.Persistent);
        numActiveCars[0] = 1;
        base.OnCreate();
    }

    protected override void OnDestroy()
    {
        carsPositionMap.Dispose();
        carsParkingMap.Dispose();
        intersectionQueueMap.Dispose();
        intersectionCrossingMap.Dispose();
        numActiveCars.Dispose();
        base.OnDestroy();
    }

    protected override void OnUpdate()
    {
        carsPositionMap.Clear();
        intersectionQueueMap.Clear();
        intersectionCrossingMap.Clear();

        int numVehicles = query.CalculateEntityCount();
        if (numVehicles > carsPositionMap.Capacity)
        {
            carsPositionMap.Capacity = numVehicles;
            carsParkingMap.Capacity = numVehicles;
        }

        if (numActiveCars[0] == 0)
        {
            Debug.Log("End game");
        }
        else
        {
            numActiveCars[0] = 0;
        }

        Entities
                .WithoutBurst()
                .WithStoreEntityQueryInField(ref query)
                .ForEach((ref VehicleNavigation navigation, ref Translation translation, in Vehicle vehicle, in Rotation rotation, in LocalToWorld ltw) =>
                {
                    //update car position on the map (collision avoidance)
                    if (!navigation.isParked && !navigation.needParking)
                    {
                        int hashMapKey = GetPositionHashMapKey(translation.Value);
                        carsPositionMap.TryAdd(hashMapKey, '1');
                        if (navigation.isCar)
                        {
                            numActiveCars[0]++;
                        }
                    }
                    /*
                    if(navigation.isParked)
                    {
                        int hashMapKey = GetPositionHashMapKey(translation.Value);
                        carsParkingMap.TryAdd(hashMapKey, '1');
                    }
                    */

                    //update intersection queues
                    if (navigation.intersectionStop)
                    {
                        int intersectionQueueHashMapKey = GetIntersectionQueueHashMapKey(navigation.intersectionId, navigation.intersectionDirection);
                        intersectionQueueMap.TryAdd(intersectionQueueHashMapKey, 1);
                    }
                    else if (navigation.intersectionCrossing)
                    {
                        int intersectionCrossingHashMapKey = GetIntersectionCrossingHashMapKey(navigation.intersectionId);
                        intersectionCrossingMap.TryAdd(intersectionCrossingHashMapKey, navigation.intersectionDirection);
                    }


                }).Schedule();

    }

}