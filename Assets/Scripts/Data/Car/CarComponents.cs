using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;

public struct Vehicle : IComponentData { }

public struct Car : IComponentData { }

public struct VehicleNavigation : IComponentData
{
    public int currentNode;
    public bool needParking;
    public bool intersectionStop;
    public bool isInsideIntersection;
    public bool trafficStop;
}

public struct VehicleSpeed : IComponentData
{
    public float currentSpeed;
    public float maxSpeed;
    public float speedDamping;
}

public struct VehicleSteering : IComponentData
{
    public float MaxSteeringAngle;
    public float DesiredSteeringAngle;
    public float SteeringDamping;
}

public struct ListNode : IBufferElementData
{
    public float3 listNodesTransform;
}

class CarComponents : MonoBehaviour, IConvertGameObjectToEntity
{
#pragma warning disable 649

    [Header("Handling")]
    public float Speed = 1f;
    public float SteeringAngle = 30.0f;
    [Range(0f, 1f)] public float SteeringDamping = 0.1f;
    [Range(0f, 1f)] public float SpeedDamping = 0.1f;

    [Header("Navigation")]
    public Node startingNode;
    public Node destinationNode;

#pragma warning restore 649

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponent<Vehicle>(entity);

        dstManager.AddComponent<VehicleNavigation>(entity);

        dstManager.AddComponentData(entity, new VehicleNavigation
        {
            currentNode = 0,
            needParking = false,
            intersectionStop = false,
            trafficStop = false,
        });

        dstManager.AddComponentData(entity, new VehicleSpeed
        {
            maxSpeed = Speed,
            currentSpeed = Speed,
            speedDamping = SpeedDamping,
        });

        dstManager.AddComponentData(entity, new VehicleSteering
        {
            MaxSteeringAngle = math.radians(SteeringAngle),
            SteeringDamping = SteeringDamping,
        }) ;

        /** Create path for car **/
        Path path = new Path();
        List<Node> carPath = new List<Node>();
        carPath = path.findShortestPath(startingNode.transform, destinationNode.transform);

        if (carPath[0]!=startingNode || carPath[carPath.Count-1]!=destinationNode)
        {
            throw new System.Exception("NO PATH FOUND");
        }

        DynamicBuffer<ListNode> listNodesTransform = dstManager.AddBuffer<ListNode>(entity);
        for (int i = 0; i < carPath.Count; i++)
        {
            listNodesTransform.Add(new ListNode { listNodesTransform = carPath[i].transform.position });
        }

        Debug.Log("ENTITY CREATED");

    }
}