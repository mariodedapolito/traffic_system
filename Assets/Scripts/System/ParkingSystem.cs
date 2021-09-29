using System.Collections;
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
            .ForEach((Entity e, ref VehicleNavigation navigation, ref Translation translation, ref Rotation rotation, ref VehicleSpeed speed, in VehicleSteering steering, in LocalToWorld ltw) => {
                if (navigation.needParking)
                {
                    float3 newTranslation = navigation.parkingNode;

                    translation.Value = navigation.parkingNode;

                    Debug.Log("parked:" + navigation.parkingNode);
                    EntityManager.RemoveComponent<VehicleSpeed>(e);
                    EntityManager.RemoveComponent<Car>(e);
                    EntityManager.RemoveComponent<Vehicle>(e);
                    EntityManager.RemoveComponent<VehicleNavigation>(e);
                    EntityManager.RemoveComponent<VehicleSteering>(e);

                }
            }).Run();
    }
}
