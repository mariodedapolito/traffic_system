using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;

public struct NodeComponent : IComponentData { }

public struct NodeData : IComponentData
{
    public int nodeType;
}

public struct NextNodesList : IBufferElementData
{
    public float3 nextNode;
    public int nodeType;
}

class NodeComponents : MonoBehaviour, IConvertGameObjectToEntity
{
#pragma warning disable 649
    public Node node;

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
        dstManager.AddComponent<NodeComponent>(entity);

        int node_type = 0;

        if (node.isLaneChange)
        {
            node_type = LANE_CHANGE;
        }
        else if (node.isIntersection)
        {
            node_type = INTERSECTION;
        }

        dstManager.AddComponentData(entity, new NodeData
        {
            nodeType = node_type
        });

        DynamicBuffer<NodesList> nextNodes = dstManager.AddBuffer<NodesList>(entity);
        foreach(Node n in node.nextNodes)
        {
            if (n.isLaneChange) nextNodes.Add(new NodesList { nodePosition = n.transform.position, nodeType = LANE_CHANGE });
            else if (n.isIntersection) nextNodes.Add(new NodesList { nodePosition = n.transform.position, nodeType = INTERSECTION });
            else nextNodes.Add(new NodesList { nodePosition = n.transform.position, nodeType = 0 });
        }
    }

}