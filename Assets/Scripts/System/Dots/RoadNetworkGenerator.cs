using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class RoadNetworkGenerator : MonoBehaviour
{
    private const float MINIMUM_PROXIMITY = 0.3f;


    public EntityManager dstManager;
    //list of nodes in road pieces
    private List<NodeGenerationInfo> nodesList = new List<NodeGenerationInfo>();

    private Dictionary<Node, NodeGenerationInfo> nodesMap = new Dictionary<Node, NodeGenerationInfo>();
    // array of roads filled in traffic spawner
    private List<RoadPiece> roadPieces;

    public RoadNetworkGenerator(EntityManager dstManager)
    {
        this.dstManager = dstManager;
    }
    public void GenerateNetwork(List<RoadPiece> roadPieces, out List<Entity> roadNodes, out List<Entity> roadSegments)
    {
        this.roadPieces = roadPieces;
        if(nodesMap==null)
            nodesMap = new Dictionary<Node, NodeGenerationInfo>();
        if (nodesList == null)
            nodesList = new List<NodeGenerationInfo>();
        FindNodesAtSamePositions();
        roadNodes = GenerateNodesEntities(out var roadNodesMap);
        roadSegments = GenerateSegmentEntities(roadNodesMap);
    }
   
    private void FindNodesAtSamePositions()
    {
        for (int i = 0; i < roadPieces.Count; i++)
        {
            var roadPiece = roadPieces[i];
            // for each node in a roadpiece add node to list
            foreach (var roadNode in roadPiece.RoadNodes)
                GenerateNodeInfo(roadNode, i);
        }
    }
    // adds road node to map 
    private NodeGenerationInfo GenerateNodeInfo(Node checkedRoadNode, int index)
    {
        if (nodesMap.ContainsKey(checkedRoadNode))
            return null;

        var nodeInfo = new NodeGenerationInfo();
        nodeInfo.position = checkedRoadNode.transform.position;
        nodesMap.Add(checkedRoadNode, nodeInfo);
        nodesList.Add(nodeInfo);
        nodeInfo.nodesAtSamePosition.Add(checkedRoadNode);

        for (int i = index + 1; i < roadPieces.Count; i++)
        {
            var roadPiece = roadPieces[i];
            foreach (var roadNode in roadPiece.RoadNodes)
            {
                if (Vector3.Distance(checkedRoadNode.transform.position, roadNode.transform.position) <
                    MINIMUM_PROXIMITY)
                {
                    if (nodesMap[roadNode] != nodeInfo)
                    {
                        nodesMap.Add(roadNode, nodeInfo);
                        nodeInfo.nodesAtSamePosition.Add(roadNode);
                    }
                }
            }
        }

        return nodeInfo;
    }

    private List<Entity> GenerateNodesEntities(out Dictionary<Node, Entity> roadNodeMap)
    {
        //The map that should be created and pass out
        roadNodeMap = new Dictionary<Node, Entity>();
        // list of nodes with roadnodecomponent position
        var roadNodes = new List<Entity>();

        foreach (var roadNode in nodesList)
        {
            var roadNodeEntity = dstManager.CreateEntity(typeof(RoadNodeComponent));
            dstManager.AddBuffer<ConnectedSegmentBufferElement>(roadNodeEntity);
            var roadNodeComponent = dstManager.GetComponentData<RoadNodeComponent>(roadNodeEntity);
            roadNodeComponent.Position = roadNode.position;
            dstManager.SetComponentData(roadNodeEntity, roadNodeComponent);
            roadNodes.Add(roadNodeEntity);
            foreach (var node in roadNode.nodesAtSamePosition)
                roadNodeMap.Add(node, roadNodeEntity);
        }

        return roadNodes;
    }

    private List<Entity> GenerateSegmentEntities(Dictionary<Node, Entity> roadNodes)
    {
        // list of segments
        List<Entity> segments = new List<Entity>();
        // list of segment entities
        Dictionary<Segment, Entity> roadSegmentsMap = new Dictionary<Segment, Entity>();

        foreach (var roadPiece in roadPieces)
        {
            foreach (var segment in roadPiece.RoadSegments)
            {
                var segmentEntity = dstManager.CreateEntity(typeof(SegmentConfigComponent), typeof(SplineComponent),
                    typeof(SegmentComponent), typeof(SegmentTrafficTypeComponent));

                var splineComponent = SplineComponent.CreateSpline(segment.StartNode.transform, segment.EndNode.transform,
                    segment.CurveIn);

                var segmentComponent = dstManager.GetComponentData<SegmentConfigComponent>(segmentEntity);
                segmentComponent.StartNode = roadNodes[segment.StartNode];
                segmentComponent.EndNode = roadNodes[segment.EndNode];
                segmentComponent.Length = splineComponent.TotalLength();

                var nodeBuffer = dstManager.GetBuffer<ConnectedSegmentBufferElement>(segmentComponent.StartNode);
                nodeBuffer.Add(new ConnectedSegmentBufferElement { segment = segmentEntity });

                dstManager.SetComponentData(segmentEntity, segmentComponent);
                dstManager.SetComponentData(segmentEntity, splineComponent);
                dstManager.SetComponentData(segmentEntity,
                    new SegmentComponent { AvailableLength = segmentComponent.Length });

                segments.Add(segmentEntity);
                roadSegmentsMap.Add(segment, segmentEntity);
            }
        }

        foreach (var roadPiece in roadPieces)
        {
            //is intersection
            if (roadPiece.intersectionGroups.Length > 0)
            {
                var intersectionEntity = dstManager.CreateEntity(typeof(IntersectionComponent), typeof(IntersectionTimerComponent));
                dstManager.AddBuffer<IntersectionSegmentsGroupBufferElement>(intersectionEntity);
                var intersectionSegmentBufferElements = dstManager.AddBuffer<IntersectionSegmentBufferElement>(intersectionEntity);
                var intersectionSegmentsGroupBufferElements =
                    dstManager.GetBuffer<IntersectionSegmentsGroupBufferElement>(intersectionEntity);
                var counter = 0;
                for (int i = 0; i < roadPiece.intersectionGroups.Length; i++)
                {
                    var group = roadPiece.intersectionGroups[i];
                    intersectionSegmentsGroupBufferElements.Add(new IntersectionSegmentsGroupBufferElement
                    {
                        StartIndex = counter,
                        EndIndex = counter + group.Segments.Length - 1,
                        Time = group.Time
                    });
                    foreach (var roadSegment in group.Segments)
                    {
                        var segmentEntity = roadSegmentsMap[roadSegment];
                        intersectionSegmentBufferElements.Add(new IntersectionSegmentBufferElement
                        {
                            Segment = segmentEntity
                        });
                        counter++;
                        dstManager.SetComponentData(segmentEntity, new SegmentTrafficTypeComponent { TrafficType = ConnectionTrafficType.NoEntrance });
                    }
                }
            }
        }

        return segments;
    }
}

public class NodeGenerationInfo
{
    public List<Node> nodesAtSamePosition = new List<Node>();
    public Vector3 position;
}
