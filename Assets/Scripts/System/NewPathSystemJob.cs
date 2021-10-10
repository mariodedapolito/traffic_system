using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Entities;


public class NewPathSystemMono : SystemBase
{
    // private NativeList<JobHandle> jobHandles;

    protected override void OnUpdate()
    {
        NativeList<JobHandle> jobHandles = new NativeList<JobHandle>(Allocator.TempJob);
        float deltaTime = Time.DeltaTime;

        GameObject[] nodes = GameObject.FindGameObjectsWithTag("CarWaypoint");
        GameObject[] carSpanGameObj = GameObject.FindGameObjectsWithTag("CarSpawn");

        Dictionary<Vector3, Node> nodesMap = new Dictionary<Vector3, Node>();
        NativeMultiHashMap<float3, float3> nodesCity = new NativeMultiHashMap<float3, float3>(nodes.Length + carSpanGameObj.Length, Allocator.Persistent);
        NativeArray<float3> waypoitnsCity = new NativeArray<float3>(nodes.Length + carSpanGameObj.Length, Allocator.Persistent);
        Dictionary<int, NativeList<float3>> sampleJobArray = new Dictionary<int, NativeList<float3>>();

        for (int i = 0; i < carSpanGameObj.Length; i++)
        {

            nodesCity.Add(carSpanGameObj[i].GetComponent<Node>().transform.position, carSpanGameObj[i].GetComponent<Node>().nextNodes[0].transform.position);

            waypoitnsCity[i] = carSpanGameObj[i].GetComponent<Node>().transform.position;
        }

        for (int i = 0; i < nodes.Length; i++)
        {
            if (!nodes[i].GetComponent<Node>().isParkingSpot)
            {
                nodesMap.Add(nodes[i].GetComponent<Node>().transform.position, nodes[i].GetComponent<Node>());
                for (int j = 0; j < nodes[i].GetComponent<Node>().nextNodes.Count; j++)
                {
                    nodesCity.Add(nodes[i].GetComponent<Node>().transform.position, nodes[i].GetComponent<Node>().nextNodes[j].transform.position);
                }
                waypoitnsCity[i + carSpanGameObj.Length] = nodes[i].GetComponent<Node>().transform.position;
            }
        }

        Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .ForEach((Entity e, ref PathFinding pathFinding, in NeedPath needPath) =>
            {
                NativeList<float3> result = new NativeList<float3>(Allocator.Persistent);
 
                NewPathSystemJob sampleJob = new NewPathSystemJob
                {
                    startPosition = pathFinding.startingNodePosition,
                    endPosition = pathFinding.destinationNodePosition,
                    waypointsCity = waypoitnsCity,
                    nodesCity = nodesCity,
                    result = result
                };
                
                
                sampleJobArray.Add(e.Index, sampleJob.result);
                jobHandles.Add(sampleJob.Schedule());
              
 /*
                ComplexPathSystemJob complexJob = new ComplexPathSystemJob
                {
                    startPosition = pathFinding.startingNodePosition,
                    endPosition = pathFinding.destinationNodePosition,
                    nextNodes = nodesCity,
                    numberParentNode = nodes.Length + carSpanGameObj.Length,
                    numberWaypoint = nodes.Length + carSpanGameObj.Length,
                    result = result
                };
                
                sampleJobArray.Add(e.Index, complexJob.result);
                jobHandles.Add(complexJob.Schedule());
 */
                 //result.Dispose();


            }).Run();

        JobHandle.CompleteAll(jobHandles);
        jobHandles.Dispose();

        Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .ForEach((Entity e, ref PathFinding pathFinding, in NeedPath needPath) =>
            {
                
                List<float3> pathNodeFinal = new List<float3>();
                pathNodeFinal.Clear();

                foreach (var n in sampleJobArray[e.Index])
                {
                    if (!nodesMap.ContainsKey(n)) continue;
                    pathNodeFinal.Add(n);
                }
                sampleJobArray[e.Index].Dispose();

                pathNodeFinal.Reverse();

                if (pathNodeFinal.Count <= 0) return;

                var nodesPositionList = GetBufferFromEntity<NodesPositionList>();

                nodesPositionList[e].Clear();

                for (int i = 0; i< pathNodeFinal.Count; i++)
                    nodesPositionList[e].Add(new NodesPositionList { nodePosition = pathNodeFinal[i] });

                var nodesTypeList = GetBufferFromEntity<NodesTypeList>();

                nodesTypeList[e].Clear();
                for (int i = 0; i < pathNodeFinal.Count; i++)
                {
                    Node node = nodesMap[pathNodeFinal[i]]; 
                    if (node.isLaneChange) nodesTypeList[e].Add(new NodesTypeList { nodeType = 1 });
                    else if (node.isIntersection) nodesTypeList[e].Add(new NodesTypeList { nodeType = 4 });
                    else if (node.isLaneMergeLeft) nodesTypeList[e].Add(new NodesTypeList { nodeType = 5 });
                    else if (node.isLaneMergeRight) nodesTypeList[e].Add(new NodesTypeList { nodeType = 6 });
                    //else if(carPath[i].isTurnLeft) nodesTypeList.Add(new NodesTypeList { nodeType = TURN_LEFT });   //reserved for potential use
                    //else if (carPath[i].isTurnLeft) nodesTypeList.Add(new NodesTypeList { nodeType = TURN_RIGHT }); //reserved for potential use
                    else nodesTypeList[e].Add(new NodesTypeList { nodeType = 0 });
                    

                }

                EntityManager.RemoveComponent<NeedPath>(e);
                
            }).Run();


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
        public NativeMultiHashMap<float3, float3> nodesCity;
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

/*
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;

public class NewPathSystemJob : SystemBase
{

    protected override void OnUpdate()
    {

        float deltaTime = Time.DeltaTime;
        GameObject[] nodes = GameObject.FindGameObjectsWithTag("CarWaypoint");
        GameObject[] carSpanGameObj = GameObject.FindGameObjectsWithTag("CarSpawn");

        Dictionary<float3, Node> nodesMap = new Dictionary<float3, Node>();
        NativeMultiHashMap<float3, float3> nodesCity = new NativeMultiHashMap<float3, float3>(nodes.Length + carSpanGameObj.Length, Allocator.Persistent);
        NativeArray<float3> waypoitnsCity = new NativeArray<float3>(nodes.Length + carSpanGameObj.Length, Allocator.Persistent);
        

        for (int i = 0; i < carSpanGameObj.Length; i++)
        {

            nodesCity.Add(carSpanGameObj[i].GetComponent<Node>().transform.position, carSpanGameObj[i].GetComponent<Node>().nextNodes[0].transform.position);

            waypoitnsCity[i] = carSpanGameObj[i].GetComponent<Node>().transform.position;
        }

        for (int i = 0; i < nodes.Length; i++)
        {
            if (!nodes[i].GetComponent<Node>().isParkingSpot)
            {
                nodesMap.Add(nodes[i].GetComponent<Node>().transform.position, nodes[i].GetComponent<Node>());
                for (int j = 0; j < nodes[i].GetComponent<Node>().nextNodes.Count; j++)
                {
                    nodesCity.Add(nodes[i].GetComponent<Node>().transform.position, nodes[i].GetComponent<Node>().nextNodes[j].transform.position);
                }
                waypoitnsCity[i + carSpanGameObj.Length] = nodes[i].GetComponent<Node>().transform.position;
            }
        }

        //public NativeList<float3> FindPath(float3 startPosition, float3 endPosition, NativeArray<float3> waypointsCity, NativeMultiHashMap<float3, float3> nodesCity
        //NativeList<NativeList<float3>> sampleJobArray = new NativeList<NativeList<float3>>(carSpanGameObj.Length, Allocator.Persistent);

        Entities
          .WithBurst()
          .ForEach((Entity e, DynamicBuffer <NodesPositionList> nodesPositionList, ref PathFinding pathFinding, in NeedPath needPath) =>
          {
              float3 startPosition = pathFinding.startingNodePosition;
              float3 endPosition = pathFinding.destinationNodePosition;

              int gridSize = waypoitnsCity.Length;

              NativeArray<PathNode> pathNodeArray = new NativeArray<PathNode>(gridSize, Allocator.Temp); //first value is number of Waypoint

              for (int i = 0; i < gridSize; i++)
              {
                  PathNode pathNode = new PathNode();
                  pathNode.node = waypoitnsCity[i];
                  pathNode.x = waypoitnsCity[i].x;
                  pathNode.y = waypoitnsCity[i].y;
                  pathNode.z = waypoitnsCity[i].z;
                  pathNode.index = i;//CalculateIndex(waypointsCity[i].x, waypointsCity[i].z, gridSize);

                  pathNode.gCost = float.MaxValue;

                  float a = endPosition.x - waypoitnsCity[i].x;
                  float b = endPosition.z - waypoitnsCity[i].z;

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
                      nodesPositionList.Clear();
                      for (int i = 0; i<path.Length; i++)
                      {
                          nodesPositionList.Add(new NodesPositionList { nodePosition = path[i] });
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
          }).Schedule();

        int c = 0;
        Entities
            .WithStructuralChanges()
            .ForEach((Entity e, DynamicBuffer < NodesPositionList > nodesPositionList, ref PathFinding pathFinding, in NeedPath needPath) =>
            {
                //DynamicBuffer<NodesPositionList> nodesPositionList = EntityManager.Get<NodesPositionList>(e);


                DynamicBuffer<NodesTypeList> nodesTypeList = EntityManager.AddBuffer<NodesTypeList>(e);

                for (int i = 0; i < nodesPositionList.Length; i++)
                {
                    Node node = nodesMap[nodesPositionList[i].nodePosition];
                    if (node.isLaneChange) nodesTypeList.Add(new NodesTypeList { nodeType = 1 });
                    else if (node.isIntersection) nodesTypeList.Add(new NodesTypeList { nodeType = 4 });
                    else if (node.isLaneMergeLeft) nodesTypeList.Add(new NodesTypeList { nodeType = 5 });
                    else if (node.isLaneMergeRight) nodesTypeList.Add(new NodesTypeList { nodeType = 6 });
                    //else if(carPath[i].isTurnLeft) nodesTypeList.Add(new NodesTypeList { nodeType = TURN_LEFT });   //reserved for potential use
                    //else if (carPath[i].isTurnLeft) nodesTypeList.Add(new NodesTypeList { nodeType = TURN_RIGHT }); //reserved for potential use
                    else nodesTypeList.Add(new NodesTypeList { nodeType = 0 });
                }

                EntityManager.RemoveComponent<NeedPath>(e);
                c++;
            }).Run();

        waypoitnsCity.Dispose();
        nodesCity.Dispose();
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
*/
