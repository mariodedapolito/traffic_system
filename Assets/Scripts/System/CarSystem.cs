using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;

[UpdateInGroup(typeof(InitializationSystemGroup))]
class CarSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float time = Time.DeltaTime;
        Entities
            .WithoutBurst()
            .ForEach((DynamicBuffer<ListNode> ListNode, ref CarPosition position, ref CarDestination destination, ref Translation translation, ref Rotation rotation, ref VehicleSpeed speed, in LocalToWorld ltw) =>
            {
                
                if (position.currentNode >= ListNode.Length) 
                    position.currentNode = 0;

                position.carPosition = translation.Value;

                destination.position = ListNode[position.currentNode].listNodesTransform;

                float3 direction = destination.position - position.carPosition;
                //translation.Value = Vector3.Lerp(position.carPosition, destination.position, 0.008f);

                translation.Value  += ltw.Forward * time * 2;

                if (math.distance(translation.Value, destination.position) > 2.7f)
                {
                    rotation.Value = Quaternion.LookRotation(direction);
                }

                if ((math.distance(translation.Value, destination.position) < 1f)) 
                    position.currentNode++;

            }).Run();
    }


    void Stop(PhysicsVelocity p, bool status) //to implement
    {
        float3 v = p.Linear;
        if (status)
            p.Linear = new float3(0, 0, 0);
        else
            p.Linear = v;
    }


}
