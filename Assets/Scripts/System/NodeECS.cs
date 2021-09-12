using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;

struct NodeECS : IComponentData
{

   // public List<Vector3> nextNodes;   //array that contains all the next waypoints that can be reached by the current waypoint
    public bool needIncomingConnection;     //identify waypoints that are at the extremities of the street prefab (need connection coming from adjacent street prefab)
    public bool needOutgoingConnection;     //identify waypoints that are at the extremities of the street prefab (need to be connected to the next street prefab)
    public bool isIntersection;     //identify waypoints that are used for intersections (can be used to avoid spawning cars here)
    public bool isLaneChange;       //identify waypoints that are used for lane changing on double lane streets
    public bool isTurnLeft;         //identify waypoints that are used for turning left in intersections (needed for intersection precedence)
    public bool isTurnRight;        //identify waypoints that are used for turning right in intersections (needed for intersection precedence)
    public bool isBusLane;          //identify waypoints that are used for bus-only lanes
    public bool isBusStop;          //identify waypoints that are used as bus stops (fermate)
    public bool isParkingGateway;
    public bool isParkingSpot;      //identify waypoints that are used as car parking spots
    public int parkingExitRotation;
    public int parkingRotation;     //identify car rotation when parked
    public int laneNumber;          //identify lane number (lane 0 is middlemost lane)
    public int trafficDirection;    //0: right lane traffic, 1: left lane 

    public bool isBusSpawn;
    public bool isCarSpawn;
    public bool isOccupied;
    public int numberCars;
    public Vector3 position;
 
}
struct NodeBlobAsset
{
    public BlobArray<NodeECS> nodesArray;
}
struct NodeECSArrayData:IComponentData
{
   public BlobAssetReference<NodeBlobAsset> nodesRef;
    public int waypointIndex;
}
 

