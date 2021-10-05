/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;

public class ParkingSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float time = Time.DeltaTime;
        Entities
            .WithStructuralChanges()
            .ForEach((Entity e, ref VehicleNavigation navigation, ref Translation translation, in Car car) => {
                if (navigation.needParking)
                {
                    translation.Value = navigation.parkingNode;
                    navigation.isParked = true;

                    Debug.Log("parked car");
                    EntityManager.RemoveComponent<VehicleSpeed>(e);
                    EntityManager.RemoveComponent<Vehicle>(e);
                    EntityManager.RemoveComponent<VehicleNavigation>(e);
                    EntityManager.RemoveComponent<VehicleSteering>(e);
                    EntityManager.AddComponent<EndGameNeedCount>(e);
                }
            }).Run();
    }
}*/
