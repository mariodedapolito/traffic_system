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

    [ReadOnly]
    public static NativeMultiHashMap<int, float3> parkingSpotsMap;
    [ReadOnly]
    public static NativeHashMap<int, int> parkingCapacityMap;
    [ReadOnly]
    public static NativeHashMap<int, int> parkingFreeSpotsMap;

    [ReadOnly]
    public static NativeHashMap<int, int> intersectionIdMap;
    [ReadOnly]
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
    }

    protected override void OnDestroy()
    {
        carsPositionMap.Dispose();
        carsParkingMap.Dispose();
        intersectionQueueMap.Dispose();
        intersectionCrossingMap.Dispose();
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

        parkingSpotsMap = ParkingSystem.parkingSpotsMap;
        parkingCapacityMap = ParkingSystem.parkingCapacityMap;
        parkingFreeSpotsMap = ParkingSystem.parkingFreeSpotsMap;

        intersectionIdMap = IntersectionTriggerSystem.intersectionIdMap;
        triggerMap = IntersectionTriggerSystem.triggerMap;

        var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().ToConcurrent();

        Entities
                    .WithoutBurst()
                    .WithStoreEntityQueryInField(ref query)
                    .ForEach((Entity entity, int entityInQueryIndex, ref VehicleNavigation navigation, ref Translation translation, in Vehicle vehicle, in Rotation rotation, in LocalToWorld ltw) =>
                    {
                        //just entered parking
                        if (navigation.needParking)
                        {
                            int gatewayPos = ParkingSystem.GetNodeHashMapKey(navigation.parkingGateWay);
                            if (parkingFreeSpotsMap.TryGetValue(gatewayPos, out int numFreeSpots) && numFreeSpots > 0)
                            {

                                //Find the first free spot to park into
                                if (parkingSpotsMap.TryGetFirstValue(gatewayPos, out float3 spotPosition, out var iter))
                                {
                                    do
                                    {
                                        int spotKey = GetPositionHashMapKey(spotPosition);
                                        if (!carsParkingMap.ContainsKey(spotKey))
                                        {
                                            translation.Value = spotPosition;
                                            carsParkingMap.Add(spotKey, '1');
                                            navigation.needParking = false;
                                            navigation.isParked = true;
                                            parkingFreeSpotsMap[gatewayPos]--;
                                            ecb.AddComponent<IsParkedComponent>(entityInQueryIndex, entity);
                                            return;
                                        }

                                    } while (parkingSpotsMap.TryGetNextValue(out spotPosition, ref iter));
                                }
                            }
                            navigation.needParking = false;
                            navigation.isParked = false;
                            navigation.timeExitParking = int.MaxValue;
                            return;
                        }

                        //calculate path before exiting parking
                        else if (navigation.isParked)
                        {
                            if (elapsedTime >= navigation.timeExitParking)
                            {
                                int keyPos1 = GetPositionHashMapKey(navigation.parkingGateWay);
                                int keyPos2 = GetPositionHashMapKey(navigation.parkingGateWay + ltw.Forward);
                                int keyPos3 = GetPositionHashMapKey(navigation.parkingGateWay + (-1) * ltw.Forward);
                                if (!carsPositionMap.ContainsKey(keyPos1) && !carsPositionMap.ContainsKey(keyPos2) && !carsPositionMap.ContainsKey(keyPos3))
                                {
                                    carsParkingMap.Remove(GetPositionHashMapKey(translation.Value));
                                    carsPositionMap.TryAdd(GetPositionHashMapKey(navigation.parkingGateWay), '1');

                                    translation.Value = navigation.parkingGateWay;
                                    navigation.needParking = false;
                                    navigation.isParked = false;
                                    navigation.timeExitParking = int.MaxValue;
                                    parkingFreeSpotsMap[GetPositionHashMapKey(navigation.parkingGateWay)]++;
                                    ecb.RemoveComponent<IsParkedComponent>(entityInQueryIndex, entity);
                                }
                            }
                            else
                            {
                                carsParkingMap.TryAdd(GetPositionHashMapKey(translation.Value), '1');
                            }
                            return;
                        }

                        //update car position on the map (collision avoidance)
                        int hashMapKey = GetPositionHashMapKey(translation.Value);
                        carsPositionMap.TryAdd(hashMapKey, '1');

                        //check car intersection situation
                        if (intersectionIdMap.TryGetValue(hashMapKey, out int intersectionId) && triggerMap.TryGetValue(hashMapKey, out int4 triggerData))
                        {
                            if (triggerData[INTERSECTION_ENTER_EXIT] == INTERSECTION_ENTER && !navigation.intersectionStop && !navigation.intersectionCrossing)    //car is entering the intersection
                            {
                                navigation.intersectionStop = true;
                                navigation.intersectionCrossed = false;
                                navigation.intersectionId = intersectionId;
                                navigation.intersectionDirection = triggerData[INTERSECTION_DIRECTION];
                                navigation.intersectionNumRoads = triggerData[INTERSECTION_ROADS];
                                if (triggerData[INTERSECTION_TYPE] == INTERSECTION_SIMPLE)
                                {
                                    navigation.isSemaphoreIntersection = false;
                                    navigation.isSimpleIntersection = true;
                                }
                                else
                                {
                                    navigation.isSemaphoreIntersection = true;
                                    navigation.isSimpleIntersection = false;
                                }
                            }
                            else if (triggerData[INTERSECTION_ENTER_EXIT] == INTERSECTION_EXIT && navigation.intersectionCrossing)
                            {
                                navigation.intersectionCrossed = true;
                                navigation.intersectionCrossing = false;
                                navigation.intersectionId = -1;
                                navigation.intersectionDirection = -1;
                                navigation.intersectionNumRoads = -1;
                                navigation.isSemaphoreIntersection = false;
                                navigation.isSimpleIntersection = false;
                                return;
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