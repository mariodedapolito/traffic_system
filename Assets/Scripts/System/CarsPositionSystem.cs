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

    EndSimulationEntityCommandBufferSystem m_EndSimulationEcbSystem;
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
        m_EndSimulationEcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
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
        int elapsedTime = (int)UnityEngine.Time.time;

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

        var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().ToConcurrent();

        Entities
                .WithoutBurst()
                .WithStoreEntityQueryInField(ref query)
                .ForEach((Entity entity, int entityInQueryIndex, ref VehicleNavigation navigation, ref Translation translation, in PathFinding pathFinding, in Vehicle vehicle, in Rotation rotation, in LocalToWorld ltw) =>
                {
                    //just entered parking
                    if (navigation.needParking)
                    {
                        ecb.RemoveComponent<VehicleSpeed>(entityInQueryIndex, entity);
                        navigation.needParking = false;
                        navigation.isParked = true;
                        return;
                    }

                    //calculate path before exiting parking
                    else if (navigation.isParked && elapsedTime >= navigation.timeExitParking)
                    {
                        Debug.Log("PARKING EXIT");
                        int keyPos1 = GetPositionHashMapKey(navigation.destinationNodePosition);
                        int keyPos2 = GetPositionHashMapKey(navigation.destinationNodePosition + ltw.Forward);
                        int keyPos3 = GetPositionHashMapKey(navigation.destinationNodePosition + (-1) * ltw.Forward);
                        if (!carsPositionMap.ContainsKey(keyPos1) && !carsPositionMap.ContainsKey(keyPos2) && !carsPositionMap.ContainsKey(keyPos3))
                        {
                            ecb.AddComponent<NeedPath>(entityInQueryIndex, entity);
                            translation.Value = pathFinding.startingNodePosition;
                            navigation.needParking = false;
                            navigation.isParked = false;
                            navigation.timeExitParking = int.MaxValue;
                            navigation.currentNode = 1;
                        }
                    }

                    //update car position on the map (collision avoidance)
                    if (!navigation.isParked)
                    {
                        int hashMapKey = GetPositionHashMapKey(translation.Value);
                        carsPositionMap.TryAdd(hashMapKey, '1');
                        if (navigation.isCar)
                        {
                            numActiveCars[0]++;
                        }
                    }

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