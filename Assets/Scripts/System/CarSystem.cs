using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(InitializationSystemGroup))]
class CarSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float time = Time.DeltaTime;
        Entities
            .WithoutBurst()
            .ForEach((ref CarPosition position, ref CarDestination destination, ref Translation translation, in VehicleSpeed speed) =>
            {
                
                position.carPosition = translation.Value;

                float3 direction = destination.position - position.carPosition;

                translation.Value += direction * time;

            }).Run();
    }
}
