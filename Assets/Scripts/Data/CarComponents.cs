using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;

public struct Vehicle : IComponentData { }

public struct Car : IComponentData { }

public struct Bus : IComponentData { }

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
    public bool busStop;
    public int timeExitBusStop;
    public bool isChangingLanes;
    public float3 parkingNode;
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

    [Header("Vehicle type")]
    public bool isCar;
    public bool isBus;

    [Header("Handling")]
    public float Speed = 1f;
    public float SteeringAngle = 30.0f;
    [Range(0f, 1f)] public float SteeringDamping = 0.1f;
    [Range(0f, 1f)] public float SpeedDamping = 0.1f;

    [Header("Navigation")]
    public int currentNode;
    public Node startingNode;
    public Node destinationNode;
    public Node parkingNode;
    public List<Node> busStops;

    private const int LANE_CHANGE = 1;
    private const int BUS_STOP = 2;
    private const int TURN_LEFT = 9999;    //reserved for potential use
    private const int TURN_RIGHT = 9999;   //reserved for potential use

#pragma warning restore 649

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponent<Vehicle>(entity);

        if (isCar)
        {
            dstManager.AddComponent<Car>(entity);
        }
        else if (isBus)
        {
            dstManager.AddComponent<Bus>(entity);
        }
        else
        {
            throw new System.Exception("Undefined vehicle type");
        }

        dstManager.AddComponent<VehicleNavigation>(entity);

        if (isCar)
        {
            dstManager.AddComponentData(entity, new VehicleNavigation
            {
                currentNode = currentNode,
                needParking = false,
                intersectionStop = false,
                intersectionCrossing = false,
                intersectionCrossed = false,
                intersectionDirection = -1,
                intersectionId = -1,
                trafficStop = false,
                isChangingLanes = false,
                parkingNode = parkingNode.transform.position
            });
        }
        else if (isBus)
        {
            dstManager.AddComponentData(entity, new VehicleNavigation
            {
                currentNode = currentNode,
                needParking = false,
                intersectionStop = false,
                intersectionCrossing = false,
                intersectionCrossed = false,
                intersectionDirection = -1,
                intersectionId = -1,
                trafficStop = false,
                busStop = true,
                timeExitBusStop = 10,
                isChangingLanes = false,
            });
        }

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

        if (isCar)
        {
            /** Create path for car **/
            Path path = new Path();
            List<Node> carPath = path.findShortestPath(startingNode.transform, destinationNode.transform);

            if (carPath[0] != startingNode || carPath[carPath.Count - 1] != destinationNode)
            {
                throw new System.Exception("NO CAR PATH FOUND");
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
                else nodesTypeList.Add(new NodesTypeList { nodeType = 0 });
            }
        }
        else if (isBus)
        {
            Path path = new Path();
            List<Node> finalBusPath = new List<Node>();
            List<Node> busPath;
            for (int i = 0; i < busStops.Count-1; i++)
            {
                busPath = path.findShortestPath(busStops[i].transform, busStops[i + 1].transform);
                if (busPath.Count == 0)
                {
                    throw new System.Exception("NO BUS PATH FOUND");
                }
                for (int j = 0; j < busPath.Count - 1; j++)
                {
                    finalBusPath.Add(busPath[j]);
                }
            }
            busPath = path.findShortestPath(busStops[busStops.Count-1].transform, busStops[0].transform);
            if (busPath.Count == 0)
            {
                throw new System.Exception("NO BUS PATH FOUND");
            }
            for (int j = 0; j < busPath.Count; j++)
            {
                finalBusPath.Add(busPath[j]);
            }

            for (int i = 0; i < finalBusPath.Count - 1; i++)
            {
                Debug.DrawLine(finalBusPath[i].transform.position, finalBusPath[i + 1].transform.position, Color.white, 30f);
            }

            DynamicBuffer<NodesPositionList> nodesPositionList = dstManager.AddBuffer<NodesPositionList>(entity);
            for (int i = 0; i < finalBusPath.Count; i++)
            {
                nodesPositionList.Add(new NodesPositionList { nodePosition = finalBusPath[i].transform.position });
            }

            DynamicBuffer<NodesTypeList> nodesTypeList = dstManager.AddBuffer<NodesTypeList>(entity);
            for (int i = 0; i < finalBusPath.Count; i++)
            {
                if (finalBusPath[i].isLaneChange) nodesTypeList.Add(new NodesTypeList { nodeType = LANE_CHANGE });
                else if(finalBusPath[i].isBusStop) nodesTypeList.Add(new NodesTypeList { nodeType = BUS_STOP });
                /*else if(carPath[i].isTurnLeft) nodesTypeList.Add(new NodesTypeList { nodeType = TURN_LEFT });   //reserved for potential use
                else if (carPath[i].isTurnLeft) nodesTypeList.Add(new NodesTypeList { nodeType = TURN_RIGHT });*/ //reserved for potential use
                else nodesTypeList.Add(new NodesTypeList { nodeType = 0 });
            }
        }
        else
        {
            throw new System.Exception("Undefined vehicle type");
        }

        Debug.Log("ENTITY CREATED");
    }

}