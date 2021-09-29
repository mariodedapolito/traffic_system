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
    public bool intersectionCrossing;
    public bool intersectionCrossed;
    public int intersectionId;
    public int intersectionDirection;
    public bool isSimpleIntersection;
    public bool isSemaphoreIntersection;
    public int intersectionNumRoads;
    public bool trafficStop;
    public bool isChangingLanes;
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

public struct NodesPositionList : IBufferElementData
{
    public float3 nodePosition;
}

public struct NodesTypeList : IBufferElementData
{
    public int nodeType;
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

    private const int LANE_CHANGE = 1;
    private const int TURN_LEFT = 2;    //reserved for potential use
    private const int TURN_RIGHT = 3;   //reserved for potential use

#pragma warning restore 649

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponent<Vehicle>(entity);

        dstManager.AddComponent<VehicleNavigation>(entity);

        dstManager.AddComponentData(entity, new VehicleNavigation
        {
            currentNode = 1,
            needParking = false,
            intersectionStop = false,
            intersectionCrossing = false,
            intersectionCrossed = false,
            intersectionDirection = -1,
            intersectionId = -1,
            trafficStop = false,
            isChangingLanes = false

        });

        dstManager.AddComponentData(entity, new VehicleSpeed
        {
            maxSpeed = Speed,
            currentSpeed = Speed,
            speedDamping = SpeedDamping
        });

        dstManager.AddComponentData(entity, new VehicleSteering
        {
            MaxSteeringAngle = math.radians(SteeringAngle),
            SteeringDamping = SteeringDamping
        });

        /** Create path for car **/
        Path path = new Path();
        List<Node> carPath = new List<Node>();
        carPath = path.findShortestPath(startingNode.transform, destinationNode.transform);

        if (carPath[0] != startingNode || carPath[carPath.Count - 1] != destinationNode)
        {
            throw new System.Exception("NO PATH FOUND");
        }

        DynamicBuffer<NodesPositionList> nodesPositionList = dstManager.AddBuffer<NodesPositionList>(entity);
        for (int i = 0; i < carPath.Count; i++)
        {
            nodesPositionList.Add(new NodesPositionList { nodePosition = carPath[i].transform.position });
        }

        DynamicBuffer<NodesTypeList> nodesTypeList = dstManager.AddBuffer<NodesTypeList>(entity);
        for (int i = 0; i < carPath.Count; i++)
        {
            if (carPath[i].isLaneChange) nodesTypeList.Add(new NodesTypeList { nodeType = LANE_CHANGE });
            /*else if(carPath[i].isTurnLeft) nodesTypeList.Add(new NodesTypeList { nodeType = TURN_LEFT });   //reserved for potential use
            else if (carPath[i].isTurnLeft) nodesTypeList.Add(new NodesTypeList { nodeType = TURN_RIGHT });*/ //reserved for potential use
            else nodesTypeList.Add(new NodesTypeList { nodeType = 0});
        }

        Debug.Log("ENTITY CREATED");
    }

}