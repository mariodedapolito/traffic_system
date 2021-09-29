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
    public static NativeHashMap<int, int> intersectionQueueMap;
    public static NativeHashMap<int, int> intersectionCrossingMap;

    public const int zMultiplier = 100000;

    private EntityQuery query;

    public static int GetPositionHashMapKey(float3 position)
    {
        int xPosition = (int)position.x;
        int zPosition = (int)position.z;
        return zPosition * zMultiplier + xPosition;
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
        intersectionQueueMap = new NativeHashMap<int, int>(10000, Allocator.Persistent);
        intersectionCrossingMap = new NativeHashMap<int, int>(1250, Allocator.Persistent);

        base.OnCreate();
    }

    protected override void OnDestroy()
    {
        carsPositionMap.Dispose();
        intersectionQueueMap.Dispose();
        intersectionCrossingMap.Dispose();
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
        }

        Entities
                .WithoutBurst()
                .WithStoreEntityQueryInField(ref query)
                .ForEach((ref VehicleNavigation navigation, in Vehicle vehicle, in Translation translation, in Rotation rotation, in LocalToWorld ltw) =>
               {
                   //update car position on the map (collision avoidance)
                   int hashMapKey = GetPositionHashMapKey(translation.Value);
                   if (carsPositionMap.ContainsKey(hashMapKey))
                   {
                       navigation.trafficStop = true;
                   }
                   else
                   {
                       carsPositionMap.TryAdd(hashMapKey, '1');
                   }

                   //compensate for car position being on the limit between 2 adjacent coordinates
                   if (!navigation.isChangingLanes && !navigation.intersectionCrossing)
                   {
                       float3 carRotation = ((Quaternion)rotation.Value).eulerAngles;
                       float carAngle = carRotation.y - Mathf.CeilToInt(carRotation.y / 360f) * 360f;
                       if (carAngle < 0)
                       {
                           carAngle += 360f;
                       }

                       if (carAngle >= 80 && carAngle <= 100)  //LEFT -> RIGHT movement
                       {
                           //compensate for vertical movements
                           if (translation.Value.z % 1 < 0.1)
                           {
                               float3 adjacentPos = new float3(translation.Value.x, translation.Value.y, translation.Value.z - 1);
                               carsPositionMap.TryAdd(GetPositionHashMapKey(adjacentPos), '1');
                           }
                           else if (translation.Value.z % 1 > 0.9)
                           {
                               float3 adjacentPos = new float3(translation.Value.x, translation.Value.y, translation.Value.z + 1);
                               carsPositionMap.TryAdd(GetPositionHashMapKey(adjacentPos), '1');
                           }
                           //compensate for horizontal movement
                           if (translation.Value.x % 1 < 0.1)
                           {
                               float3 adjacentPos = new float3(translation.Value.x - 1, translation.Value.y, translation.Value.z);
                               carsPositionMap.TryAdd(GetPositionHashMapKey(adjacentPos), '1');
                           }
                       }
                       else if (carAngle >= 260 && carAngle <= 280)    //RIGHT -> LEFT movement
                       {
                           //compensate for vertical movements
                           if (translation.Value.z % 1 < 0.1)
                           {
                               float3 adjacentPos = new float3(translation.Value.x, translation.Value.y, translation.Value.z - 1);
                               carsPositionMap.TryAdd(GetPositionHashMapKey(adjacentPos), '1');
                           }
                           else if (translation.Value.z % 1 > 0.9)
                           {
                               float3 adjacentPos = new float3(translation.Value.x, translation.Value.y, translation.Value.z + 1);
                               carsPositionMap.TryAdd(GetPositionHashMapKey(adjacentPos), '1');
                           }
                           //compensate for horizontal movement
                           if (translation.Value.x % 1 > 0.9)
                           {
                               float3 adjacentPos = new float3(translation.Value.x + 1, translation.Value.y, translation.Value.z);
                               carsPositionMap.TryAdd(GetPositionHashMapKey(adjacentPos), '1');
                           }
                       }
                       else if (carAngle >= 170 && carAngle <= 190)    //TOP -> BOTTOM movement
                       {
                           //compensate for horizontal movements
                           if (translation.Value.x % 1 < 0.1)
                           {
                               float3 adjacentPos = new float3(translation.Value.x - 1, translation.Value.y, translation.Value.z);
                               carsPositionMap.TryAdd(GetPositionHashMapKey(adjacentPos), '1');
                           }
                           else if (translation.Value.x % 1 > 0.9)
                           {
                               float3 adjacentPos = new float3(translation.Value.x + 1, translation.Value.y, translation.Value.z);
                               carsPositionMap.TryAdd(GetPositionHashMapKey(adjacentPos), '1');
                           }
                           //compensate for vertical movements
                           if (translation.Value.z % 1 < 0.1)
                           {
                               float3 adjacentPos = new float3(translation.Value.x, translation.Value.y, translation.Value.z - 1);
                               carsPositionMap.TryAdd(GetPositionHashMapKey(adjacentPos), '1');
                           }
                       }
                       else if (carAngle >= -10 && carAngle <= 10)     //BOTTOM -> TOP movement
                       {
                           //compensate for horizontal movements
                           if (translation.Value.x % 1 < 0.1)
                           {
                               float3 adjacentPos = new float3(translation.Value.x - 1, translation.Value.y, translation.Value.z);
                               carsPositionMap.TryAdd(GetPositionHashMapKey(adjacentPos), '1');
                           }
                           else if (translation.Value.x % 1 > 0.9)
                           {
                               float3 adjacentPos = new float3(translation.Value.x + 1, translation.Value.y, translation.Value.z);
                               carsPositionMap.TryAdd(GetPositionHashMapKey(adjacentPos), '1');
                           }
                           //compensate for vertical movement
                           if (translation.Value.z % 1 > 0.9)
                           {
                               float3 adjacentPos = new float3(translation.Value.x, translation.Value.y, translation.Value.z + 1);
                               carsPositionMap.TryAdd(GetPositionHashMapKey(adjacentPos), '1');
                           }
                       }
                   }

                   //update intersection queues
                   if (navigation.intersectionStop)
                   {
                       int intersectionQueueHashMapKey = GetIntersectionQueueHashMapKey(navigation.intersectionId, navigation.intersectionDirection);
                       intersectionQueueMap.TryAdd(intersectionQueueHashMapKey, 1);
                   }
                   else if (navigation.intersectionCrossing && navigation.isSimpleIntersection)
                   {
                       int intersectionCrossingHashMapKey = GetIntersectionCrossingHashMapKey(navigation.intersectionId);
                       intersectionCrossingMap.TryAdd(intersectionCrossingHashMapKey, navigation.intersectionDirection);
                   }


               }).Schedule();

    }

}
