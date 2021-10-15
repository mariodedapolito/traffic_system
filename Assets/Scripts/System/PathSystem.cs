using System.Collections;
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

    private const int LANE_CHANGE = 1;
    private const int BUS_STOP = 2;
    private const int BUS_MERGE = 3;
    private const int INTERSECTION = 4;
    private const int MERGE_LEFT = 5;
    private const int MERGE_RIGHT = 6;

    public static int GetPositionHashMapKey(float3 position)
    {
        int xPosition = (int)position.x;
        int zPosition = (int)position.z;
        return xPosition * 100000 + zPosition;
    }

    protected override void OnUpdate()
    {
        GameObject cityGenerator = GameObject.Find("CityGenerator");
        CityGenerator city = cityGenerator.GetComponent<CityGenerator>();

        if (!city.useAStarInMainThread) return;

        waypoints = city.cityNodes;

        Dictionary<int, Node> nodesMapParking = city.nodesMapParking;//new Dictionary<int, Node>();
        //List<Node> nodes = city.cityNodes;
        //List<Node> parkingNodes = city.cityCarParkingNodes;
        List<Node> cityParkingNodes = city.cityParkingNodes;
        /*
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].GetComponent<Node>().isParkingSpot)
            {
                //if (!nodesMapParking.TryGetValue(nodes[i].GetComponent<Node>().transform.position, out _))
                nodesMapParking.Add(GetPositionHashMapKey(nodes[i].transform.position), nodes[i].GetComponent<Node>());
            }

        }

        for (int i = 0; i < parkingNodes.Count; i++)
        {
            if (!nodesMapParking.ContainsKey(GetPositionHashMapKey(parkingNodes[i].transform.position)))
                nodesMapParking.Add(GetPositionHashMapKey(parkingNodes[i].transform.position), parkingNodes[i].GetComponent<Node>());
        }
        */
        Entities
            .WithStructuralChanges()
            .ForEach((Entity e, ref PathFinding pathFinding, in NeedPath needPath) =>
            { 
                var rnd = new Unity.Mathematics.Random((uint)e.Index * 100000);
                int p = rnd.NextInt(0, cityParkingNodes.Count - 1);
                float3 destinationNodePosition = cityParkingNodes[p].transform.position;
                Node destinationNode = cityParkingNodes[p];

                while (destinationNodePosition.Equals(pathFinding.startingNodePosition))
                {
                    p = rnd.NextInt(0, cityParkingNodes.Count - 1);
                    destinationNodePosition = cityParkingNodes[p].transform.position;
                    destinationNode = cityParkingNodes[p];
                }

                Vector3 startPosition = new Vector3(pathFinding.startingNodePosition.x, pathFinding.startingNodePosition.y, pathFinding.startingNodePosition.z);
               

                Node startingNode = null;
                
                bool endFound = false;

                foreach (Node w in waypoints)
                {
                    if (w.transform.position.Equals(pathFinding.startingNodePosition))
                    {
                        startingNode = w.GetComponent<Node>();
                        if (endFound)
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


                if (carPath.Count <= 0) return;

                var nodesList = GetBufferFromEntity<NodesList>();

                nodesList[e].Clear();

                Parking possibleParking = carPath[carPath.Count - 1].GetComponent<Parking>();

                if (possibleParking == null || possibleParking.numberFreeSpots.Equals(null))
                {
                    Debug.Log("");
                }

                if (possibleParking.numberFreeSpots == 0)
                {
                    return;
                }



                for (int i = 0; i < carPath.Count; i++)
                {
                    Node node = carPath[i];
                    if (node.isLaneChange) nodesList[e].Add(new NodesList { nodePosition = carPath[i].transform.position, nodeType = LANE_CHANGE });
                    else if (node.isIntersection) nodesList[e].Add(new NodesList { nodePosition = carPath[i].transform.position, nodeType = INTERSECTION });
                    else if (node.isLaneMergeLeft) nodesList[e].Add(new NodesList { nodePosition = carPath[i].transform.position, nodeType = MERGE_LEFT });
                    else if (node.isLaneMergeRight) nodesList[e].Add(new NodesList { nodePosition = carPath[i].transform.position, nodeType = MERGE_RIGHT });
                    else nodesList[e].Add(new NodesList { nodePosition = carPath[i].transform.position, nodeType = 0 });

                }

                Node parkingNode = null;
                if (!pathFinding.parkingNodePosition.Equals(new float3(-1f, -1f, -1f))) //car exit
                {
                    float3 parking = pathFinding.parkingNodePosition;

                    if (!nodesMapParking.ContainsKey(GetPositionHashMapKey(parking)))
                    {
                        Debug.Log("");
                    }
                        parkingNode = nodesMapParking[GetPositionHashMapKey(parking)];
                    
                    parkingNode.isOccupied = false;
                    Node gateWay = parkingNode.parkingPrefab.GetComponent<Node>();
                    gateWay.GetComponent<Parking>().numberFreeSpots++;
                }

                int k = 0;

                while (possibleParking.freeParkingSpots[k].isOccupied)
                {
                    k++;
                }

                pathFinding.parkingNodePosition = possibleParking.freeParkingSpots[k].transform.position;

                possibleParking.numberFreeSpots--;

                pathFinding.spawnParking = false;

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
}
