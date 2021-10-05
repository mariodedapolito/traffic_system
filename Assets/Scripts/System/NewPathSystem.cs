using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;

public class NewPathSystem : SystemBase
{

    protected override void OnUpdate()
    {

        //public NativeList<float3> FindPath(float3 startPosition, float3 endPosition, NativeArray<float3> waypointsCity, NativeMultiHashMap<float3, float3> nodesCity
/*
        GameObject[] nodes = GameObject.FindGameObjectsWithTag("CarWaypoint");
        List<Node> nodesList = new List<Node>();

        nodes[0].GetComponent<Node>();

        //NewPathSystem pathSystem = new NewPathSystem();

        NativeMultiHashMap<float3, float3> nodesCity = new NativeMultiHashMap<float3, float3>(nodes.Length, Allocator.Temp);
        NativeArray<float3> waypointsCity = new NativeArray<float3>(nodes.Length, Allocator.Temp);

        for (int i = 0; i < nodes.Length; i++)
        {
            if (!nodes[i].GetComponent<Node>().isParkingSpot)
            {

                for (int j = 0; j < nodes[i].GetComponent<Node>().nextNodes.Count; j++)
                {
                    nodesCity.Add(nodes[i].GetComponent<Node>().transform.position, nodes[i].GetComponent<Node>().nextNodes[j].transform.position);
                }


                waypointsCity[i] = nodes[i].GetComponent<Node>().transform.position;
            }

            //nextNodes.Dispose();
        }
        int c = 0;
        
        Entities
                .ForEach((Entity e, ref PathFinding pathFinding, in NeedPath needPath) =>
                {
                    float3 startPosition = pathFinding.startingNodePosition;
                    float3 endPosition = pathFinding.destinationNodePosition;

                    int gridSize = waypointsCity.Length;

                    NativeArray<PathNode> pathNodeArray = new NativeArray<PathNode>(gridSize, Allocator.Temp); //first value is number of Waypoint

                    for (int i = 0; i < gridSize; i++)
                    {
                        PathNode pathNode = new PathNode();
                        pathNode.node = waypointsCity[i];
                        pathNode.x = waypointsCity[i].x;
                        pathNode.y = waypointsCity[i].y;
                        pathNode.z = waypointsCity[i].z;
                        pathNode.index = i;//CalculateIndex(waypointsCity[i].x, waypointsCity[i].z, gridSize);

                        pathNode.gCost = float.MaxValue;

                        float a = endPosition.x - waypointsCity[i].x;
                        float b = endPosition.z - waypointsCity[i].z;

                        if (a < 0) a = -a;
                        if (b < 0) b = -b;

                        pathNode.hCost = a + b; //CalculateDistanceCostManhattanDiscance(waypointsCity[i], endPosition);
                        pathNode.ClaculateFCost();

                        pathNode.isWalkable = true;
                        pathNode.cameFromNodeIndex = -1;

                        pathNodeArray[pathNode.index] = pathNode;
                    }

                    int endNodeIndex = -1; //GetIndex(endPosition, pathNodeArray);
                    for (int i = 0; i < pathNodeArray.Length; i++)
                    {
                        if (pathNodeArray[i].node.Equals(endPosition))
                        {
                            endNodeIndex = i;
                            break;
                        }
                    }

                    int startNodeIndex = -1; 
                    for (int i = 0; i < pathNodeArray.Length; i++)
                    {
                        if (pathNodeArray[i].node.Equals(startPosition))
                        {
                            startNodeIndex = i;
                            break;
                        }
                    }


                    PathNode startNode = pathNodeArray[startNodeIndex];//GetIndex(startPosition, pathNodeArray)];
                    startNode.gCost = 0;
                    startNode.ClaculateFCost();
                    pathNodeArray[startNode.index] = startNode;

                    // only index 
                    NativeList<int> openList = new NativeList<int>(Allocator.Temp);
                    NativeList<int> closedList = new NativeList<int>(Allocator.Temp);

                    openList.Add(startNode.index);

                    while (openList.Length > 0)
                    {

                        PathNode lowestCostPathNode = pathNodeArray[openList[0]];
                        for (int i = 1; i < openList.Length; i++)
                        {
                            PathNode testPathNode = pathNodeArray[openList[i]];
                            if (testPathNode.fCost < lowestCostPathNode.fCost)
                            {
                                lowestCostPathNode = testPathNode;
                            }
                        }                        

                        int currentNodeIndex = lowestCostPathNode.index;// GetLowestCostFNodeIndex(openList, pathNodeArray);
                        PathNode currentNode = pathNodeArray[currentNodeIndex];

                        if (currentNodeIndex == endNodeIndex)
                        {
                            //reached destination
                            break;
                        }


                        //remove Node from openList
                        for (int i = 0; i < openList.Length; i++)
                        {
                            if (openList[i] == currentNodeIndex)
                            {
                                openList.RemoveAtSwapBack(i);
                                break;
                            }
                        }

                        NativeList<float3> nextNodes = new NativeList<float3>(Allocator.Temp);
                        float3 value;

                        if (nodesCity.TryGetFirstValue(currentNode.node, out value, out var iterator))
                        {
                            do
                            {
                                nextNodes.Add(value);
                            } while (nodesCity.TryGetNextValue(out value, ref iterator));
                        }

                        closedList.Add(currentNodeIndex);

                        for (int i = 0; i < nextNodes.Length; i++)
                        {
                            //float2 neighbourOffset = neighbourOffsetArray[i];
                            float3 neigbourPosition = nextNodes[i]; //new float2(currentNode.x + neighbourOffset.x, currentNode.z + neighbourOffset.y);

                            bool flag = false; 

                            for (int j = 0; j < pathNodeArray.Length; j++)
                            {
                                if (pathNodeArray[j].node.Equals(neigbourPosition))
                                {
                                    flag = true;
                                }
                            }

                            if (!flag)
                            {
                                //Neighbour not valid
                                continue;
                            }



                            int neighbourNodeIndex = -1;//GetIndex(neigbourPosition, pathNodeArray);

                            for (int j = 0; j < pathNodeArray.Length; j++)
                            {
                                if (pathNodeArray[j].node.Equals(neigbourPosition))
                                {
                                    neighbourNodeIndex = j;
                                }
                            }


                            if (closedList.Contains(neighbourNodeIndex))
                            {
                                //already searched this node
                                continue;
                            }

                            PathNode neighbourNode = pathNodeArray[neighbourNodeIndex];
                            if (!neighbourNode.isWalkable)
                            {
                                //Not something
                                continue;
                            }

                            float3 currentNodePosition = new float3(currentNode.x, startPosition.y, currentNode.z);
                            float3 neigbourPositionFloat3 = new float3(neigbourPosition.x, startPosition.y, neigbourPosition.y);

                            float a = neigbourPositionFloat3.x - currentNodePosition.x;
                            float b = neigbourPositionFloat3.z - currentNodePosition.z;

                            if (a < 0) a = -a;
                            if (b < 0) b = -b;

                            float tentativeGCost = currentNode.gCost + a + b;//CalculateDistanceCostManhattanDiscance(currentNodePosition, neigbourPositionFloat3);

                            if (tentativeGCost < neighbourNode.gCost)
                            {
                                neighbourNode.cameFromNodeIndex = currentNodeIndex;
                                neighbourNode.gCost = tentativeGCost;
                                neighbourNode.ClaculateFCost();
                                pathNodeArray[neighbourNodeIndex] = neighbourNode;

                                if (!openList.Contains(neighbourNode.index))
                                {
                                    openList.Add(neighbourNode.index);
                                }
                            }
                        }

                        nextNodes.Dispose();
                    }

                    PathNode endNode = pathNodeArray[endNodeIndex];
                    if (endNode.cameFromNodeIndex == -1)
                    {
                        //Didn't find a path!
                        Debug.Log("Didnt find a path");
                    }
                    else
                    {
                        //Found a path
                        //NativeArray<float3> result = CalculatePath(pathNodeArray, endNode);

                        if (endNode.cameFromNodeIndex == -1)
                        {
                            // couldn't found a path
                            NativeArray<float3> result = new NativeList<float3>(Allocator.Temp);
                        }
                        else
                        {
                            //Found a path
                            NativeList<float3> path = new NativeList<float3>(Allocator.Temp);
                            path.Add(new float3(endNode.x, endNode.y, endNode.z));

                            PathNode currentNode = endNode;
                            while (currentNode.cameFromNodeIndex != -1)
                            {
                                PathNode cameFromNode = pathNodeArray[currentNode.cameFromNodeIndex];
                                path.Add(new float3(cameFromNode.x, cameFromNode.y, cameFromNode.z));
                                currentNode = cameFromNode;
                            }
                        }

                        Debug.Log("Calculated");
                        //path.Dispose();
                    }

                    pathNodeArray.Dispose();
                    //neighbourOffsetArray.Dispose();
                    openList.Dispose();
                    closedList.Dispose();

                    //return new NativeList<float3>(Allocator.Temp);
                }).Schedule();*/
    }
        private NativeList<float3> CalculatePath(NativeArray<PathNode> pathNodeArray, PathNode endNode)
        {
            if (endNode.cameFromNodeIndex == -1)
            {
                // couldn't found a path
                return new NativeList<float3>(Allocator.Temp);
            }
            else
            {
                //Found a path
                NativeList<float3> path = new NativeList<float3>(Allocator.Temp);
                path.Add(new float3(endNode.x, endNode.y, endNode.z));

                PathNode currentNode = endNode;
                while (currentNode.cameFromNodeIndex != -1)
                {
                    PathNode cameFromNode = pathNodeArray[currentNode.cameFromNodeIndex];
                    path.Add(new float3(cameFromNode.x, cameFromNode.y, cameFromNode.z));
                    currentNode = cameFromNode;
                }
                return path;
            }
        }

        private bool isPositionInsideGrid(float3 point, NativeArray<PathNode> pathNodeArray)
        {
            for (int i = 0; i < pathNodeArray.Length; i++)
            {
                if (pathNodeArray[i].node.Equals(point))
                {
                    return true;
                }
            }

            return false;
        }

        private int GetIndex(float3 point, NativeArray<PathNode> pathNodeArray)
        {
            for (int i = 0; i < pathNodeArray.Length; i++)
            {
                if (pathNodeArray[i].node.Equals(point))
                {
                    return i;
                }
            }

            return -1;
        }

        private float CalculateDistanceCostManhattanDiscance(float3 position, float3 endPos)
        {
            float a = endPos.x - position.x;
            float b = endPos.z - position.z;

            if (a < 0) a = -a;
            if (b < 0) b = -b;


        return a + b;
        }

        private float AbsVal(float integer)
        {
            if (integer < 0) return -integer;
            else return integer;
        }

        private int GetLowestCostFNodeIndex(NativeList<int> openList, NativeArray<PathNode> pathNodeArray)
            {
                PathNode lowestCostPathNode = pathNodeArray[openList[0]];
                for (int i = 1; i < openList.Length; i++)
                {
                    PathNode testPathNode = pathNodeArray[openList[i]];
                    if (testPathNode.fCost < lowestCostPathNode.fCost)
                    {
                        lowestCostPathNode = testPathNode;
                    }
                }

                return lowestCostPathNode.index;
        }

        private struct PathNode
        {
            public float3 node;

            //public Grid<PathNode> grid;
            public float x;
            public float y;
            public float z;

            public int index;

            public float gCost;
            public float hCost;
            public float fCost;

            public bool isWalkable;

            public int cameFromNodeIndex;

            public void ClaculateFCost()
            {
                fCost = gCost + hCost;
            }

            public void SetIsWalkable(bool isWalkable)
            {
                this.isWalkable = isWalkable;
            }

        }

    //}  
}
