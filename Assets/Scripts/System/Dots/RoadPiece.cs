using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadPiece : MonoBehaviour
{
    // mesh for showing the Cones or direction of segments
    public Mesh ConeMesh;

    // list of nods
    public  Node[] RoadNodes;
    //list of segments 
    public  Segment[] RoadSegments;

    public RoadIntersectionSegmentsGroup[] intersectionGroups;

}
