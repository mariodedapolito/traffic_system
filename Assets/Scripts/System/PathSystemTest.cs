using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;
using Unity.Jobs;

public class PathSystemTest : SystemBase
{

    public NativeList<float3> waypoints;
    static public NativeArray<float3> generalNodeMap;
    public Dictionary<int, List<float3>> generalPathMap;

    protected override void OnUpdate()
    {
    }
        /*
        protected override void OnCreate()
        {
            waypoints = new NativeList<float3>(0, Allocator.Persistent);

            base.OnCreate();
        }

        protected override void OnDestroy()
        {
            generalNodeMap.Dispose();
            waypoints.Dispose();
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            GameObject cityGenerator = GameObject.Find("CityGenerator");
            CityGenerator city = cityGenerator.GetComponent<CityGenerator>();

            if(city.cityNodes.Count == 0)
            {
                return;
            }

            int c = city.cityNodes.Count;
            numNode = c;
            generalNodeMap = new NativeArray<float3>(city.cityNodes.Count * lenght, Allocator.Persistent);
            waypoints.Capacity = c;
            //generalNodeMap.Capacity = city.cityNodes.Count;

            int j = 0;

            for (int i = 0; i < city.cityNodes.Count; i++)
            {
                waypoints.Add(city.cityNodes[i].transform.position);
                NativeList<float3> nextNodes = new NativeList<float3>(0, Allocator.Persistent);
                nextNodes.Capacity = city.cityNodes[i].nextNodes.Count;

                foreach (Node n in city.cityNodes[i].nextNodes)
                    nextNodes.Add(n.transform.position);

                generalNodeMap[j * lenght] = waypoints[i];

                for (int k = 1; k < nextNodes.Length; k++)
                    generalNodeMap[j * lenght + k] = nextNodes[k - 1];
                j++;
                nextNodes.Dispose();
            }

            Entities
                .WithStructuralChanges()
                .ForEach((Entity e, ref PathFinding pathFinding, in NeedPath needPath) =>
                {


                    Vector3 startPosition = new Vector3(pathFinding.startingNodePosition.x, pathFinding.startingNodePosition.y, pathFinding.startingNodePosition.z);
                    Vector3 endPosition = new Vector3(pathFinding.destinationNodePosition.x, pathFinding.destinationNodePosition.y, pathFinding.destinationNodePosition.z);

                    float3 startingNode = startPosition;
                    float3 destinationNode = endPosition;

                    /*
                    bool startFound = false;
                    bool endFound = false;

                    foreach (float3 w in waypoints)
                    {
                        if (w.Equals(pathFinding.startingNodePosition))
                        {
                            startingNode = w;
                            startFound = true;
                            if (endFound)
                            {
                                break;
                            }
                        }
                        if (w.transform.position.Equals(pathFinding.destinationNodePosition))
                        {
                            destinationNode = w.GetComponent<float3>();
                            endFound = true;
                            if (startFound)
                            {
                                break;
                            }
                        }

                    }*/

        /*
                    List<float3> carPath = findShortestPath(startingNode, destinationNode);

                    if (!carPath[0].Equals(startingNode) || !carPath[carPath.Capacity - 1].Equals( destinationNode))
                    {
                        throw new System.Exception("NO CAR PATH FOUND");
                    }

                    /*
                    for (int i = 0; i < carPath.Count - 1; i++)
                    {
                        Debug.DrawLine(carPath[i].transform.position, carPath[i + 1].transform.position, Color.white, 30f);
                    }
                    */

        /*            DynamicBuffer<NodesPositionList> nodesPositionList = EntityManager.AddBuffer<NodesPositionList>(e);
                    for (int i = 0; i < carPath.Count; i++)
                    {
                        if(!carPath[i].Equals(null))
                            nodesPositionList.Add(new NodesPositionList { nodePosition = carPath[i]});
                    }

                    /*
                    DynamicBuffer<NodesTypeList> nodesTypeList = EntityManager.AddBuffer<NodesTypeList>(e);
                    for (int i = 0; i < carPath.Capacity; i++)
                    {
                        if (carPath[i].isLaneChange) nodesTypeList.Add(new NodesTypeList { nodeType = 1 });
                        else if (carPath[i].isIntersection) nodesTypeList.Add(new NodesTypeList { nodeType = 4 });
                        //else if (carPath[i].isLaneMergeLeft) nodesTypeList.Add(new NodesTypeList { nodeType = 5 });
                        //else if (carPath[i].isLaneMergeRight) nodesTypeList.Add(new NodesTypeList { nodeType = 6 });
                        //else if(carPath[i].isTurnLeft) nodesTypeList.Add(new NodesTypeList { nodeType = TURN_LEFT });   //reserved for potential use
                        //else if (carPath[i].isTurnLeft) nodesTypeList.Add(new NodesTypeList { nodeType = TURN_RIGHT }); //reserved for potential use
                        else nodesTypeList.Add(new NodesTypeList { nodeType = 0 });
                    }
                    */
        /*
                    EntityManager.RemoveComponent<NeedPath>(e);

                }).Run();

            generalNodeMap.Dispose();
            waypoints.Dispose();
        }

        public List<float3> findShortestPath(float3 start, float3 end)
        {
            List<float3> node = AStarSearch(start, end);

            return node;
        }

        public static List<float3> AStarSearch(float3 startPosition, float3 endPosition)
        {
            List<float3> path = new List<float3>();

            float3 start = startPosition;
            float3 end = endPosition;

            List<float3> positionsTocheck = new List<float3>();
            Dictionary<float3, float> costDictionary = new Dictionary<float3, float>();
            Dictionary<float3, float> priorityDictionary = new Dictionary<float3, float>();
            Dictionary<float3, float3> parentsDictionary = new Dictionary<float3, float3>();

            positionsTocheck.Add(start);
            priorityDictionary.Add(start, 0);
            costDictionary.Add(start, 0);
            parentsDictionary.Add(start, 0f);

            while (positionsTocheck.Count > 0)
            {
                float3 current = GetClosestNode(positionsTocheck, priorityDictionary);
                positionsTocheck.Remove(current);
                if (current.Equals(end))
                {
                    path = GeneratePath(parentsDictionary, current);
                    return path;
                }
                NativeList<float3> currentNextNodes = new NativeList<float3>(10, Allocator.Persistent); ;

                for(int i = 0; i< numNode; i++)
                {
                    if(generalNodeMap[i * 10].Equals(current))
                    {
                        for(int j = 1; j < 10; j++)
                        {
                            currentNextNodes.Add(generalNodeMap[i * 10 + j]);
                        }
                    }
                }

                foreach (float3 neighbour in currentNextNodes)
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

                currentNextNodes.Dispose();
            }
            return path;
        }
        public static List<float3> GeneratePath(Dictionary<float3, float3> parentMap, float3 endState)
        {
            List<float3> path = new List<float3>();

            float3 parent = endState;
            while (!parent.Equals(null) && parentMap.ContainsKey(parent))
            {
                path.Add(parent);
                parent = parentMap[parent];
            }
            path.Reverse();
            return path;
        }

        private static float3 GetClosestNode(List<float3> list, Dictionary<float3, float> distanceMap)
        {
            float3 candidate = list[0];
            foreach (float3 vertex in list)
            {
                if (distanceMap[vertex] < distanceMap[candidate])
                {
                    candidate = vertex;
                }
            }
            return candidate;
        }
        private static float ManhattanDiscance(float3 endPos, float3 position)
        {
            return System.Math.Abs(endPos.x - position.x) + System.Math.Abs(endPos.z - position.z);
        }*/
    }
