/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;

public class PathSystem : SystemBase
{
    private List<Node> waypoints;

    protected override void OnUpdate()
    {
        GameObject cityGenerator = GameObject.Find("CityGenerator");
        CityGenerator city = cityGenerator.GetComponent<CityGenerator>();
        waypoints = city.cityNodes;

        Entities
            .WithStructuralChanges()
            .ForEach((Entity e, ref PathFinding pathFinding, in NeedPath needPath) =>
            {
                Vector3 startPosition = new Vector3(pathFinding.startingNodePosition.x, pathFinding.startingNodePosition.y, pathFinding.startingNodePosition.z);
                Vector3 endPosition = new Vector3(pathFinding.destinationNodePosition.x, pathFinding.destinationNodePosition.y, pathFinding.destinationNodePosition.z);

                Node startingNode = null;
                Node destinationNode = null;

                bool startFound = false;
                bool endFound = false;

                foreach (Node w in waypoints)
                {
                    if (w.transform.position.Equals(pathFinding.startingNodePosition))
                    {
                        startingNode = w.GetComponent<Node>();
                        startFound = true;
                        if (endFound)
                        {
                            break;
                        }
                    }
                    if (w.transform.position.Equals(pathFinding.destinationNodePosition))
                    {
                        destinationNode = w.GetComponent<Node>();
                        endFound = true;
                        if (startFound)
                        {
                            break;
                        }
                    }

                }

                List<Node> carPath = findShortestPath(startingNode.transform, destinationNode.transform);

                if (carPath[0] != startingNode || carPath[carPath.Count - 1] != destinationNode)
                {
                    throw new System.Exception("NO CAR PATH FOUND");
                }

                //for (int i = 0; i < carPath.Count - 1; i++)
                //{
                //    Debug.DrawLine(carPath[i].transform.position, carPath[i + 1].transform.position, Color.white, 30f);
                //}

                DynamicBuffer<NodesPositionList> nodesPositionList = EntityManager.AddBuffer<NodesPositionList>(e);
                for (int i = 0; i < carPath.Count; i++)
                {
                    nodesPositionList.Add(new NodesPositionList { nodePosition = carPath[i].transform.position });
                }

                DynamicBuffer<NodesTypeList> nodesTypeList = EntityManager.AddBuffer<NodesTypeList>(e);
                for (int i = 0; i < carPath.Count; i++)
                {
                    if (carPath[i].isLaneChange) nodesTypeList.Add(new NodesTypeList { nodeType = 1 });
                    else if (carPath[i].isIntersection) nodesTypeList.Add(new NodesTypeList { nodeType = 4 });
                    else if (carPath[i].isLaneMergeLeft) nodesTypeList.Add(new NodesTypeList { nodeType = 5 });
                    else if (carPath[i].isLaneMergeRight) nodesTypeList.Add(new NodesTypeList { nodeType = 6 });
                    //else if(carPath[i].isTurnLeft) nodesTypeList.Add(new NodesTypeList { nodeType = TURN_LEFT });   //reserved for potential use
                    //else if (carPath[i].isTurnLeft) nodesTypeList.Add(new NodesTypeList { nodeType = TURN_RIGHT }); //reserved for potential use
                    else nodesTypeList.Add(new NodesTypeList { nodeType = 0 });
                }

                EntityManager.RemoveComponent<NeedPath>(e);

            }).Run();
    }

    public List<Node> findShortestPath(Transform start, Transform end)
    {
        List<Node> node = AStarSearch(start.GetComponent<Node>(), end.GetComponent<Node>());

        return node;
    }

    public static List<Node> AStarSearch(Node startPosition, Node endPosition)
    {
        List<Node> path = new List<Node>();

        Node start = startPosition;
        Node end = endPosition;

        List<Node> positionsTocheck = new List<Node>();
        Dictionary<Node, float> costDictionary = new Dictionary<Node, float>();
        Dictionary<Node, float> priorityDictionary = new Dictionary<Node, float>();
        Dictionary<Node, Node> parentsDictionary = new Dictionary<Node, Node>();

        positionsTocheck.Add(start);
        priorityDictionary.Add(start, 0);
        costDictionary.Add(start, 0);
        parentsDictionary.Add(start, null);

        while (positionsTocheck.Count > 0)
        {
            Node current = GetClosestNode(positionsTocheck, priorityDictionary);
            positionsTocheck.Remove(current);
            if (current.Equals(end))
            {
                path = GeneratePath(parentsDictionary, current);
                return path;
            }

            foreach (Node neighbour in current.nextNodes)
            {
                float newCost = costDictionary[current] + 1;
                if (!costDictionary.ContainsKey(neighbour) || newCost < costDictionary[neighbour])
                {
                    costDictionary[neighbour] = newCost;

                    float priority = newCost + ManhattanDiscance(end, neighbour);
                    positionsTocheck.Add(neighbour);
                    priorityDictionary[neighbour] = priority;

                    parentsDictionary[neighbour] = current;
                }
            }
        }
        return path;
    }
    public static List<Node> GeneratePath(Dictionary<Node, Node> parentMap, Node endState)
    {
        List<Node> path = new List<Node>();
        Node parent = endState;
        while (parent != null && parentMap.ContainsKey(parent))
        {
            path.Add(parent);
            parent = parentMap[parent];
        }
        path.Reverse();
        return path;
    }

    private static Node GetClosestNode(List<Node> list, Dictionary<Node, float> distanceMap)
    {
        Node candidate = list[0];
        foreach (Node vertex in list)
        {
            if (distanceMap[vertex] < distanceMap[candidate])
            {
                candidate = vertex;
            }
        }
        return candidate;
    }
    private static float ManhattanDiscance(Node endPos, Node position)
    {
        return System.Math.Abs(endPos.transform.position.x - position.transform.position.x) + System.Math.Abs(endPos.transform.position.z - position.transform.position.z);
    }
}*/
