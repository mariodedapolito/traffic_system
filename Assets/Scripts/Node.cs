using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Node : MonoBehaviour
{

    public List<Node> nextNodes;   //array that contains all the next waypoints that can be reached by the current waypoint
    public bool needIncomingConnection;     //identify waypoints that are at the extremities of the street prefab (need connection coming from adjacent street prefab)
    public bool needOutgoingConnection;     //identify waypoints that are at the extremities of the street prefab (need to be connected to the next street prefab)
    public bool isIntersection;     //identify waypoints that are used for intersections (can be used to avoid spawning cars here)
    public bool isLaneChange;       //identify waypoints that are used for lane changing on double lane streets
    public bool isTurnLeft;         //identify waypoints that are used for turning left in intersections (needed for intersection precedence)
    public bool isTurnRight;        //identify waypoints that are used for turning right in intersections (needed for intersection precedence)
    public bool isBusLane;          //identify waypoints that are used for bus-only lanes
    public bool isBusStop;          //identify waypoints that are used as bus stops (fermate)
    public bool isBusMerge;
    public bool isLaneMergeLeft;
    public bool isLaneMergeRight;
    public bool isParkingGateway;
    public bool isParkingSpot;      //identify waypoints that are used as car parking spots
    public int parkingExitRotation;
    public int parkingRotation;     //identify car rotation when parked
    public int laneNumber;          //identify lane number (lane 0 is middlemost lane)
    public int trafficDirection;    //0: right lane traffic, 1: left lane 

    public bool isBusSpawn;
    public bool isCarSpawn;
    public bool isOccupied = false;
    public int numberCars;

    public GameObject parkingPrefab;


    /*private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        for (int i = 0; i < nextNodes.Count; i++)
        {
            if (nextNodes[i] == null)
            {
                Debug.Log(transform.position);
            }
            Gizmos.DrawLine(transform.position, nextNodes[i].transform.position);
            float distance = Vector3.Distance(transform.position, nextNodes[i].transform.position);
            Gizmos.DrawSphere(Vector3.MoveTowards(transform.position, nextNodes[i].transform.position, distance - 0.4f), 0.15f);
        }
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, 0.25f);
        //if (isTurnLeft)
        //{
        //    Gizmos.color = Color.yellow;
        //    Gizmos.DrawCube(transform.position, new Vector3(0.25f, 0.25f, 1f));
        //}
        //if (isTurnRight)
        //{
        //    Gizmos.color = Color.cyan;
        //    Gizmos.DrawCube(transform.position, new Vector3(0.25f, 0.25f, 1f));
        //}
        if (needIncomingConnection)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.position, 0.3f);
        }
        else if (needOutgoingConnection)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(transform.position, 0.3f);
        }
        else if (trafficDirection == 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawCube(transform.position, new Vector3(0.5f, 0.5f, 0.5f));
        }
        else if (trafficDirection == 1)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawCube(transform.position, new Vector3(0.5f, 0.5f, 0.5f));
        }
        if (isParkingSpot)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position, 0.1f);
        }
        if (isLaneChange)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawCube(transform.position, new Vector3(0.7f, 0.7f, 0.7f));
        }
        if (isIntersection)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawCube(transform.position, new Vector3(0.7f, 0.7f, 0.7f));
        }
        Gizmos.color = Color.white;
    }*/

}