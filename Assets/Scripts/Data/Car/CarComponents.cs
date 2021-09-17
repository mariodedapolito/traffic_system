using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;

struct Vehicle : IComponentData { }

struct CarPosition : IComponentData
{
    public float3 carPosition;
    public int currentNode;
    public bool checkNode;
}

struct CarDestination : IComponentData
{
    public float3 position;
}
struct VehicleSpeed : IComponentData
{
    public float Speed;
    public float DesiredSpeed;
}

struct VehicleSteering : IComponentData
{
    public float MaxSteeringAngle;
    public float DesiredSteeringAngle;
}

struct ListNode : IBufferElementData
{
    public float3 listNodesTransform;
}

class CarComponents : MonoBehaviour, IConvertGameObjectToEntity
{
#pragma warning disable 649

    [Header("Handling")]
    public float Speed = 10.0f;
    public float SteeringAngle = 30.0f;
    [Range(0f, 1f)] public float SteeringDamping = 0.1f;
    [Range(0f, 1f)] public float SpeedDamping = 0.01f;
    public float3 positionDest;
    public List<Node> listNodes;
#pragma warning restore 649

    void OnValidate()
    {
        Speed = math.max(0f, Speed);
        SteeringAngle = math.max(0f, SteeringAngle);
        SteeringDamping = math.clamp(SteeringDamping, 0f, 1f);
        SpeedDamping = math.clamp(SpeedDamping, 0f, 1f);
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponent<Vehicle>(entity);

        dstManager.AddComponent<CarPosition>(entity);

        dstManager.AddComponent<ListNode>(entity);

        dstManager.AddComponentData(entity, new CarDestination
        {
            position = positionDest,
        });

        dstManager.AddComponentData(entity, new VehicleSpeed
        {
            Speed = Speed,
        });

        dstManager.AddComponentData(entity, new VehicleSteering
        {
            MaxSteeringAngle = math.radians(SteeringAngle),
        });
        
        DynamicBuffer<ListNode> listNodesTransform = dstManager.AddBuffer<ListNode>(entity);
        for (var i = 0; i < listNodes.Count; i++)
            listNodesTransform.Add( new ListNode { listNodesTransform = listNodes[i].transform.position });
    }
}



