using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;

public struct ParkingComponent : IComponentData { }

public struct ParkingData : IComponentData
{
    public float3 parkingGatewayPosition;
    public int numParkingSpots;
    public int numFreeSpots;
}

public struct ParkingSpotsList : IBufferElementData
{
    public float3 spotPosition;
}

class ParkingComponents : MonoBehaviour, IConvertGameObjectToEntity
{
#pragma warning disable 649
    public Parking parking;
    public float3 parkingGateWay;
#pragma warning restore 649

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        Debug.Log("Converting");

        dstManager.AddComponent<ParkingComponent>(entity);

        dstManager.AddComponentData(entity, new ParkingData
        {
            parkingGatewayPosition = parkingGateWay,
            numParkingSpots = parking.numberParkingSpots,
            numFreeSpots = parking.numberFreeSpots
        });

        DynamicBuffer<ParkingSpotsList> parkingSpots = dstManager.AddBuffer<ParkingSpotsList>(entity);
        foreach (Node n in parking.freeParkingSpots)
        {
            parkingSpots.Add(new ParkingSpotsList { spotPosition = n.transform.position });
        }
    }

}