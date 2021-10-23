using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Entities;


public class NewPathSystem : SystemBase
{
    // private NativeList<JobHandle> jobHandles;

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
        CityGenerator city = GameObject.FindGameObjectWithTag("CityGenerator").GetComponent<CityGenerator>();

        if (!city.useAStarInMultiThread) return;
        NativeList<JobHandle> jobHandles = new NativeList<JobHandle>(Allocator.TempJob);
        float deltaTime = Time.DeltaTime;

        List<Node> nodes = city.cityNodes;
        List<Node> parkingNodes = city.cityCarParkingNodes;
        List<Node> cityParkingNodes = city.cityParkingNodes;
        List<Node> carSpanGameObj = city.citySpawnNodes;

        Dictionary<int, Node> nodesMap = city.nodesMap;//new Dictionary<int, Node>();
        Dictionary<int, Node> nodesMapParking = city.nodesMapParking;// new Dictionary<int, Node>();

        NativeMultiHashMap<int, float3> nodesCity = new NativeMultiHashMap<int, float3>(nodes.Count + carSpanGameObj.Count, Allocator.Persistent);
        NativeMultiHashMap<float3, float3> nodesCityFloat3 = new NativeMultiHashMap<float3, float3>(nodes.Count + carSpanGameObj.Count, Allocator.Persistent);
        NativeArray<float3> waypoitnsCity = new NativeArray<float3>(nodes.Count + carSpanGameObj.Count, Allocator.Persistent);

        Dictionary<int, NativeList<float3>> sampleJobArray = new Dictionary<int, NativeList<float3>>();

        NativeList<float3> cityParkingPosition = new NativeList<float3>(cityParkingNodes.Count, Allocator.Persistent);

        for (int i = 0; i < carSpanGameObj.Count; i++)
        {
            if (carSpanGameObj[i].Equals(null))
            {
                carSpanGameObj.RemoveAt(i); continue;
            }
            nodesCity.Add(GetPositionHashMapKey(carSpanGameObj[i].transform.position), carSpanGameObj[i].GetComponent<Node>().nextNodes[0].transform.position);
            nodesCityFloat3.Add(carSpanGameObj[i].transform.position, carSpanGameObj[i].GetComponent<Node>().nextNodes[0].transform.position);
            waypoitnsCity[i] = carSpanGameObj[i].transform.position;
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].Equals(null)) { nodes.RemoveAt(i); continue; }
            if (!nodes[i].GetComponent<Node>().isParkingSpot)
            {
                for (int j = 0; j < nodes[i].nextNodes.Count; j++)
                {
                    if (nodes[i].nextNodes[j].Equals(null)) { nodes[i].nextNodes.RemoveAt(j); continue; }
                    nodesCity.Add(GetPositionHashMapKey(nodes[i].transform.position), nodes[i].GetComponent<Node>().nextNodes[j].transform.position);
                    nodesCityFloat3.Add(nodes[i].transform.position, nodes[i].GetComponent<Node>().nextNodes[j].transform.position);
                }
                waypoitnsCity[i + carSpanGameObj.Count] = nodes[i].GetComponent<Node>().transform.position;
            }

        }
        for (int i = 0; i < cityParkingNodes.Count; i++)
        {
            cityParkingPosition.Add(cityParkingNodes[i].transform.position);
        }


        Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .ForEach((Entity e, ref PathFinding pathFinding, in NeedPath needPath) =>
            {
                NativeList<float3> result = new NativeList<float3>(Allocator.Persistent);

                var rnd = new Unity.Mathematics.Random((uint)e.Index * 100000);
                int p = rnd.NextInt(0, cityParkingPosition.Length - 1);
                float3 destinationNode = cityParkingPosition[p];

                while (destinationNode.Equals(pathFinding.startingNodePosition))
                {
                    p = rnd.NextInt(0, cityParkingPosition.Length - 1);
                    destinationNode = cityParkingPosition[p];
                }

                pathFinding.destinationNodePosition = destinationNode;
                /*
                NewPathSystemJob sampleJob = new NewPathSystemJob
                {
                    startPosition = pathFinding.startingNodePosition,
                    endPosition = pathFinding.destinationNodePosition,
                    waypointsCity = waypoitnsCity,
                    nodesCity = nodesCity,
                    result = result
                };

                sampleJobArray.Add(e.Index, sampleJob.result);
                jobHandles.Add(sampleJob.Schedule());*/

                ComplexPathSystemJob complexJob = new ComplexPathSystemJob
                {
                    startPosition = pathFinding.startingNodePosition,
                    endPosition = pathFinding.destinationNodePosition,
                    nextNodes = nodesCityFloat3,
                    numberParentNode = nodes.Count + carSpanGameObj.Count,
                    numberWaypoint = nodes.Count + carSpanGameObj.Count,
                    result = result
                };

                sampleJobArray.Add(e.Index, complexJob.result);
                jobHandles.Add(complexJob.Schedule());


            }).Run();

        JobHandle.CompleteAll(jobHandles);
        jobHandles.Dispose();

        Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .ForEach((Entity e, ref PathFinding pathFinding, in NeedPath needPath) =>
            {

                if (!sampleJobArray.ContainsKey(e.Index)) return;

                List<float3> pathNodeFinal = new List<float3>();
                pathNodeFinal.Clear();

                foreach (var n in sampleJobArray[e.Index])
                {
                    if (!nodesMap.ContainsKey(GetPositionHashMapKey(n))) continue;
                    pathNodeFinal.Add(n);
                }
                sampleJobArray[e.Index].Dispose();

                pathNodeFinal.Reverse();

                if (pathNodeFinal.Count <= 0) return;

                var nodesList = GetBufferFromEntity<NodesList>();

                nodesList[e].Clear();

                Parking possibleParking = nodesMap[GetPositionHashMapKey(pathNodeFinal[pathNodeFinal.Count - 1])].GetComponent<Parking>();

                /*if (possibleParking == null || possibleParking.numberFreeSpots.Equals(null))
                {
                    Debug.Log("");
                }*/

                if (possibleParking.numberFreeSpots == 0)
                {
                    return;
                }

                for (int i = 0; i < pathNodeFinal.Count; i++)
                {
                    Node node = nodesMap[GetPositionHashMapKey(pathNodeFinal[i])];
                    if (node.isLaneChange) nodesList[e].Add(new NodesList { nodePosition = pathNodeFinal[i], nodeType = LANE_CHANGE });
                    else if (node.isIntersection) nodesList[e].Add(new NodesList { nodePosition = pathNodeFinal[i], nodeType = INTERSECTION });
                    else if (node.isLaneMergeLeft) nodesList[e].Add(new NodesList { nodePosition = pathNodeFinal[i], nodeType = MERGE_LEFT });
                    else if (node.isLaneMergeRight) nodesList[e].Add(new NodesList { nodePosition = pathNodeFinal[i], nodeType = MERGE_RIGHT });
                    else nodesList[e].Add(new NodesList { nodePosition = pathNodeFinal[i], nodeType = 0 });

                }

                Node parkingNode = null;
                if (!pathFinding.parkingNodePosition.Equals(new float3(-1f, -1f, -1f))) //car exit
                {
                    float3 parking = pathFinding.parkingNodePosition;
                    parkingNode = nodesMapParking[GetPositionHashMapKey(parking)];
                    parkingNode.isOccupied = false;
                    Node gateWay = parkingNode.parkingPrefab.GetComponent<Node>();
                    gateWay.GetComponent<Parking>().numberFreeSpots++;
                }

                int p = 0;

                while (possibleParking.freeParkingSpots[p].isOccupied)
                {
                    p++;
                }

                pathFinding.parkingNodePosition = possibleParking.freeParkingSpots[p].transform.position;

                possibleParking.numberFreeSpots--;

                pathFinding.spawnParking = false;

                EntityManager.RemoveComponent<NeedPath>(e);
                EntityManager.AddComponentData(e, new VehicleSpeed
                {
                    maxSpeed = 2f,
                    currentSpeed = 0f,
                    speedDamping = 2f/10
                });

            }).Run();

        GameObject[] numSpawnWaypoints = GameObject.FindGameObjectsWithTag("CarSpawn");
        if (numSpawnWaypoints.Length != 0)
        {
            foreach (var s in numSpawnWaypoints) UnityEngine.Object.Destroy(s);
        }

        nodesCityFloat3.Dispose();
        cityParkingPosition.Dispose();
        waypoitnsCity.Dispose();
        nodesCity.Dispose();
    }


    [BurstCompile]
    public struct ComplexPathSystemJob : IJob
    {
        public float3 startPosition;
        public float3 endPosition;
        public NativeMultiHashMap<float3, float3> nextNodes;
        public int numberParentNode;
        public int numberWaypoint;
        public NativeList<float3> result;

        public void Execute()
        {
            //public static List<Node> AStarSearch(float3 startPosition, float3 endPosition, NativeMultiHashMap<float3, float3> nextNodes, int numberParentNode, int numberWaypoint)
            //{
            //NativeList<float3> path = new NativeList<float3>();

            float3 start = startPosition;
            float3 end = endPosition;

            NativeList<float3> positionsTocheck = new NativeList<float3>(numberWaypoint, Allocator.Temp);
            NativeHashMap<float3, float> costDictionary = new NativeHashMap<float3, float>(numberWaypoint, Allocator.Temp);
            NativeHashMap<float3, float> priorityDictionary = new NativeHashMap<float3, float>(numberWaypoint, Allocator.Temp);
            NativeHashMap<float3, float3> parentsDictionary = new NativeHashMap<float3, float3>(numberParentNode, Allocator.Temp);

            positionsTocheck.Add(start);
            priorityDictionary.Add(start, 0f);
            costDictionary.Add(start, 0f);
            parentsDictionary.Add(start, 0f);

            while (positionsTocheck.Length > 0)
            {
                float3 current = GetClosestNode(positionsTocheck, priorityDictionary);
                positionsTocheck.RemoveAt(positionsTocheck.IndexOf(current));
                if (current.Equals(end))
                {
                    NativeList<float3> path = GeneratePath(parentsDictionary, current);
                    for (int i = 0; i < path.Length; i++)
                        result.Add(path[i]);

                }

                foreach (float3 neighbour in nextNodes.GetValuesForKey(current))
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

            //path.Dispose();
            positionsTocheck.Dispose();
            costDictionary.Dispose();
            priorityDictionary.Dispose();
            parentsDictionary.Dispose();

        }
        public static NativeList<float3> GeneratePath(NativeHashMap<float3, float3> parentMap, float3 endState) //need reverse
        {
            NativeList<float3> path = new NativeList<float3>(parentMap.Capacity, Allocator.Temp);
            float3 parent = endState;
            while (!parent.Equals(0f) && parentMap.ContainsKey(parent))
            {
                path.Add(parent);
                parent = parentMap[parent];
            }
            return path;
        }

        private static float3 GetClosestNode(NativeList<float3> list, NativeHashMap<float3, float> distanceMap)
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
        }
    }


    [BurstCompile]
    public struct NewPathSystemJob : IJob
    {
        public float3 startPosition;
        public float3 endPosition;
        public NativeArray<float3> waypointsCity;
        public NativeMultiHashMap<int, float3> nodesCity;
        public NativeList<float3> result;
        public void Execute()
        {
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
                pathNode.hCost = CalculateDistanceCostManhattanDiscance(waypointsCity[i], endPosition);
                pathNode.ClaculateFCost();

                pathNode.isWalkable = true;
                pathNode.cameFromNodeIndex = -1;

                pathNodeArray[pathNode.index] = pathNode;
            }
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

                if (nodesCity.TryGetFirstValue(GetPositionHashMapKey(currentNode.node), out value, out var iterator))
                {
                    do
                    {
                        nextNodes.Add(value);
                    } while (nodesCity.TryGetNextValue(out value, ref iterator));
                }

                closedList.Add(currentNodeIndex);

                for (int i = 0; i < nextNodes.Length; i++)
                {
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
                throw new System.Exception("Didnt find a path");
            }
            else
            {
                //Found a path
                NativeList<float3> path = CalculatePath(pathNodeArray, endNode);

                for (int i = 0; i < path.Length; i++)
                    result.Add(path[i]);

                //Debug.Log("Calculated");
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
}