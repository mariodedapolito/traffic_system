﻿using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;

public struct Vehicle : IComponentData { }

public struct VehicleNavigation : IComponentData
{
    public int currentNode;
    public bool needParking;
    public bool isParked;
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
    public int timeExitParking;
    public bool isChangingLanes;
    public bool isCar;
    public bool isBus;
    public float3 startingNodePosition;
    public float3 destinationNodePosition;
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

public struct NodesList : IBufferElementData
{
    public float3 nodePosition;
    public int nodeType;
}

public struct PathFinding : IComponentData
{
    public float3 startingNodePosition;
    public float3 destinationNodePosition;
    public float3 parkingNodePosition;
    public bool spawnParking;
}

public struct NeedPath : IComponentData { }

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
    public List<Node> busPath;

    public List<float3> pathNodeList;

    private const int LANE_CHANGE = 1;
    private const int BUS_STOP = 2;
    private const int BUS_MERGE = 3;
    private const int INTERSECTION = 4;
    private const int MERGE_LEFT = 5;
    private const int MERGE_RIGHT = 6;
    private const int TURN_LEFT = 9999;    //reserved for potential use
    private const int TURN_RIGHT = 9999;   //reserved for potential use

    public bool isParking;

#pragma warning restore 649

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        
        dstManager.AddComponent<Vehicle>(entity);

        if (isParking)
        {
            dstManager.AddComponentData(entity, new VehicleNavigation
            {
                needParking = true
            });
        }
        else
        {
            dstManager.AddComponentData(entity, new VehicleNavigation
            {
                currentNode = 1
            });
        }
        if (isCar)
        {
            if (isParking)
            {
                dstManager.AddComponentData(entity, new VehicleNavigation
                {
                    currentNode = 1,
                    needParking = true,
                    intersectionStop = false,
                    intersectionCrossing = false,
                    intersectionCrossed = false,
                    intersectionDirection = -1,
                    intersectionId = -1,
                    trafficStop = false,
                    isChangingLanes = false,

                    timeExitParking = int.MaxValue,
                    isCar = true,
                    startingNodePosition = startingNode.transform.position,
                    destinationNodePosition = destinationNode.transform.position
                });
            }
            else
            {
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
                    isChangingLanes = false,

                    timeExitParking = int.MaxValue,
                    isCar = true,
                    startingNodePosition = startingNode.transform.position,
                    destinationNodePosition = destinationNode.transform.position
                });
            }
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
                isBus = true
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
            DynamicBuffer<NodesList> nodeListParking = dstManager.AddBuffer<NodesList>(entity);

            if (isParking)
            {
                nodeListParking.Add(new NodesList { nodePosition = destinationNode.transform.position });

                dstManager.AddComponentData(entity, new PathFinding
                {
                    startingNodePosition = startingNode.transform.position,
                    destinationNodePosition = destinationNode.transform.position,
                    parkingNodePosition = parkingNode.transform.position
                });


            }
            else
            {
                dstManager.AddComponentData(entity, new PathFinding
                {
                    startingNodePosition = startingNode.transform.position,
                    destinationNodePosition = destinationNode.transform.position,
                    parkingNodePosition = new float3(-1f, -1f, -1f),
                    spawnParking = true
                });

                dstManager.AddComponent<NeedPath>(entity);
            }


        }
        else if (isBus)
        {
            if (busPath.Count == 0)
            {
                throw new System.Exception("NO BUS PATH FOUND");
            }

            DynamicBuffer<NodesList> nodesList = dstManager.AddBuffer<NodesList>(entity);
            for (int i = 0; i < busPath.Count; i++)
            {
                if (busPath[i].isLaneChange) nodesList.Add(new NodesList { nodePosition = busPath[i].transform.position, nodeType = LANE_CHANGE });
                else if (busPath[i].isBusStop) nodesList.Add(new NodesList { nodePosition = busPath[i].transform.position, nodeType = BUS_STOP });
                else if (busPath[i].isBusMerge) nodesList.Add(new NodesList { nodePosition = busPath[i].transform.position, nodeType = BUS_MERGE });
                else if (busPath[i].isIntersection) nodesList.Add(new NodesList { nodePosition = busPath[i].transform.position, nodeType = INTERSECTION });
                else if (busPath[i].isLaneMergeLeft) nodesList.Add(new NodesList { nodePosition = busPath[i].transform.position, nodeType = MERGE_LEFT });
                else if (busPath[i].isLaneMergeRight) nodesList.Add(new NodesList { nodePosition = busPath[i].transform.position, nodeType = MERGE_RIGHT });
                else nodesList.Add(new NodesList { nodePosition = busPath[i].transform.position, nodeType = 0 });
            }
        }
        else
        {
            throw new System.Exception("Undefined vehicle type");
        }

        //Debug.Log("ENTITY CREATED");
    }

}