using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

public class NewPathSystemMono : MonoBehaviour
{
    NativeList<float3> pathNative;

    public Dictionary<int, NativeList<float3>> PathSystemJob(int numCarsToSpawn, NativeList<float3> spawnNodeList, NativeList<float3> destinationNodeList, NativeArray<float3> waypoitnsCity, NativeMultiHashMap<float3, float3> nodesCity)
    {
        NativeArray<JobHandle> jobHandleArray = new NativeArray<JobHandle>(numCarsToSpawn, Allocator.Temp);
        Dictionary<int, NativeList<float3>> sampleJobArray = new Dictionary<int, NativeList<float3>>();


        for (int i = 0; i < numCarsToSpawn; i++)
        {
            NativeList<float3> result = new NativeList<float3>(Allocator.TempJob);

            NewPathSystemJob sampleJob = new NewPathSystemJob
            {
                startPosition = spawnNodeList[i],
                endPosition = destinationNodeList[i],
                waypointsCity = waypoitnsCity,
                nodesCity = nodesCity,
                result = result
            };

            jobHandleArray[i] = sampleJob.Schedule();
            sampleJobArray.Add(i, sampleJob.result);

            //JobHandle JobHandle = sampleJob.Schedule();

            //JobHandle.Complete();

            //Debug.Log(sampleJob.result);

            //pathNative.Dispose();
        }

        JobHandle.CompleteAll(jobHandleArray);

        //sampleJobArray.Dispose();*/
        jobHandleArray.Dispose();

        return sampleJobArray;
    }
}
    [BurstCompile]
    public struct NewPathSystemJob : IJob
    {
        public float3 startPosition;
        public float3 endPosition;
        public NativeArray<float3> waypointsCity;
        public NativeMultiHashMap<float3, float3> nodesCity;
        public NativeList<float3> result;
        public void Execute()
        {

            //public NativeList<float3> FindPath(float3 startPosition, float3 endPosition, NativeArray<float3> waypointsCity, NativeMultiHashMap<float3, float3> nodesCity)

            //Grid or Map
            int gridSize = waypointsCity.Length;

            /*
            int xMax = 0;
            int zMax = 0;

            for (int i = 0; i < gridSize; i++)
            {
                if (waypointsCity[i].x > xMax) xMax = ((int) waypointsCity[i].x);
                if (waypointsCity[i].z > zMax) zMax = ((int) waypointsCity[i].z);
            }
            */

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
                pathNode.hCost = CalculateDistanceCostManhattanDiscance(waypointsCity[i], endPosition);
                pathNode.ClaculateFCost();

                pathNode.isWalkable = true;
                pathNode.cameFromNodeIndex = -1;

                pathNodeArray[pathNode.index] = pathNode;
            }
            /*
            // ???? -> nextNode
            NativeArray<int2> neighbourOffsetArray = new NativeArray<int2>(new int2[]
            {
                new int2(-1, 0),
                new int2(-1, 0),
                new int2(-1, 0),
                new int2(-1, 0),
                new int2(-1, 0),
                new int2(-1, 0),
                new int2(-1, 0)
            }, Allocator.Temp);*/

            int endNodeIndex = GetIndex(endPosition, pathNodeArray);

            PathNode startNode = pathNodeArray[GetIndex(startPosition, pathNodeArray)];
            startNode.gCost = 0;
            startNode.ClaculateFCost();
            pathNodeArray[startNode.index] = startNode;

            /* only index */
            NativeList<int> openList = new NativeList<int>(Allocator.Temp);
            NativeList<int> closedList = new NativeList<int>(Allocator.Temp);

            openList.Add(startNode.index);

            while (openList.Length > 0)
            {

                int currentNodeIndex = GetLowestCostFNodeIndex(openList, pathNodeArray);
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

                    if (!isPositionInsideGrid(neigbourPosition, pathNodeArray))
                    {
                        //Neighbour not valid
                        continue;
                    }


                    int neighbourNodeIndex = GetIndex(neigbourPosition, pathNodeArray);

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
                    float tentativeGCost = currentNode.gCost + CalculateDistanceCostManhattanDiscance(currentNodePosition, neigbourPositionFloat3);

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
                NativeList<float3> path = CalculatePath(pathNodeArray, endNode);

                for (int i = 0; i < path.Length; i++)
                    result.Add(path[i]);

                Debug.Log("Calculated");
                //path.Dispose();
            }

            pathNodeArray.Dispose();
            //neighbourOffsetArray.Dispose();
            openList.Dispose();
            closedList.Dispose();

            //return new NativeList<float3>(Allocator.Temp);

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
            return System.Math.Abs(endPos.x - position.x) + System.Math.Abs(endPos.z - position.z);
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

    }

