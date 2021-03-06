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
    public int laneNumber;          //identify lane number (lane 0 is middlemost lane)
    public int trafficDirection;    //0: right lane traffic, 1: left lane 

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        for (int i=0;i<nextNodes.Count;i++)
        {
            Gizmos.DrawLine(transform.position, nextNodes[i].transform.position);
        }

        if (trafficDirection == 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawCube(transform.position, new Vector3(0.5f, 0.5f, 0.5f));
        }
        if(trafficDirection == 1)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawCube(transform.position, new Vector3(0.5f, 0.5f, 0.5f));
        }
        if (needIncomingConnection)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.position, 0.5f);
        }
        if (needOutgoingConnection)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(transform.position, 0.5f);
        }

        Gizmos.color = Color.white;
    }
}
