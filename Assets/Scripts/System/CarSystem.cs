using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;

[UpdateBefore(typeof(CarsPositionSystem))]
[UpdateAfter(typeof(IntersectionTriggerSystem))]
[UpdateInGroup(typeof(InitializationSystemGroup))]
class CarSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float time = Time.DeltaTime;
        Entities
            .WithoutBurst()
            .ForEach((DynamicBuffer<ListNode> ListNode, ref VehicleNavigation navigation, ref Translation translation, ref Rotation rotation, ref VehicleSpeed speed, in VehicleSteering steering, in LocalToWorld ltw) =>
            {
                NativeHashMap<int, char> carsPosition = CarsPositionSystem.carsPositionMap;
                int lookaheadLength = 2;
                for (int i = 1; i <= lookaheadLength && !navigation.intersectionStop; i++)
                {
                    int positionKey = CarsPositionSystem.GetPositionHashMapKey(translation.Value + ltw.Forward * i);
                    if (carsPosition.TryGetValue(positionKey, out _))
                    {
                        navigation.trafficStop = true;
                        break;
                    }
                    else
                    {
                        navigation.trafficStop = false;
                    }
                }

                if(navigation.currentNode == ListNode.Length-1)
                {
                    navigation.needParking = true;
                    navigation.trafficStop = true;
                }

                if (navigation.trafficStop || navigation.intersectionStop)  //STOPPING IN TRAFFIC OR INTERSECTION
                {
                    if (speed.currentSpeed > 0.25)
                    {
                        speed.currentSpeed -= speed.maxSpeed * speed.speedDamping;
                        //Debug.Log("STOPPING");
                    }
                    else
                    {
                        speed.currentSpeed = 0;
                    }
                    translation.Value += ltw.Forward * time * speed.currentSpeed;
                    float3 direction = ListNode[navigation.currentNode].listNodesTransform - translation.Value;
                    rotation.Value = Quaternion.LookRotation(direction);
                }
                else  //SPEEDING UP IF NO TRAFFIC OR MY TURN IN INTERSECTION
                {
                    if (speed.currentSpeed < speed.maxSpeed)
                    {
                        speed.currentSpeed += speed.maxSpeed * speed.speedDamping;
                        //Debug.Log("SPEEDING");
                    }
                    else
                    {
                        speed.currentSpeed = speed.maxSpeed;

                    }
                    translation.Value += ltw.Forward * time * speed.currentSpeed;
                    float3 direction = ListNode[navigation.currentNode].listNodesTransform - translation.Value;
                    rotation.Value = Quaternion.LookRotation(direction);
                }

                if ((math.distance(translation.Value, ListNode[navigation.currentNode].listNodesTransform) < 1f && !navigation.needParking))
                    navigation.currentNode++;

            }).ScheduleParallel();
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