using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

public class BusSpawner
{

    public GameObject busPrefab;
    public CityGenerator city;
    private int numBusesToSpawn;
    private List<Node> busStopsSpawn;
    private List<Node> busStopsDst;

    private const int LANE_CHANGE = 1;
    private const int BUS_STOP = 2;
    private const int BUS_MERGE = 3;
    private const int INTERSECTION = 4;
    private const int MERGE_LEFT = 5;
    private const int MERGE_RIGHT = 6;
    private const int PARKING_GATEWAY = 7;
    private const int SEM_INTERSECTION = 8;
    private const int CURVE = 9;

    public BusSpawner(GameObject busPrefab, CityGenerator city)
    {
        this.busPrefab = busPrefab;
        this.city = city;
        this.numBusesToSpawn = city.numberBusesToSpawn;
        this.busStopsSpawn = city.cityBusStopsSpawn;
        this.busStopsDst = city.cityBusStopsDst;
        if (numBusesToSpawn > busStopsSpawn.Count)
        {
            numBusesToSpawn = busStopsSpawn.Count;
            city.numberBusesToSpawn = numBusesToSpawn;
            city.numberBusesToSpawnLimited = true;
        }
        else
        {
            city.numberBusesToSpawnLimited = false;
        }
    }

    public void generateBuses()
    {
        GameObject busToSpawn = busPrefab;

        int numBusStopsSpawn = busStopsSpawn.Count;
        int numBusStopsDst = busStopsDst.Count;
        Path path = new Path();

        for (int i = 0; i < numBusesToSpawn; i++)
        {
            int randSpawnNodeIndex = UnityEngine.Random.Range(0, numBusStopsSpawn);
            Node busStop_1 = busStopsSpawn[randSpawnNodeIndex];
            Node busStop_2 = busStop_1;
            while (busStop_1.Equals(busStop_2))
            {
                busStop_2 = busStopsDst[UnityEngine.Random.Range(0, numBusStopsDst)];
            }

            List<Node> path_1 = path.findShortestPath(busStop_1, busStop_2);
            List<Node> path_2 = path.findShortestPath(busStop_2, busStop_1);

            path_1.RemoveAt(path_1.Count - 1);
            path_2.RemoveAt(0);
            path_2.RemoveAt(path_2.Count - 1);

            for (int j = 0; j < path_1.Count - 2; j++)  //-2 so not to include node before last bus stop
            {
                if (path_1[j].isBusBranch)
                {
                    path_1.RemoveAt(j + 1);
                    path_1.Insert(j + 1, path_1[j].nextNodes[1]);
                    path_1.Insert(j + 2, path_1[j + 1].nextNodes[0]);
                    path_1.Insert(j + 3, path_1[j + 2].nextNodes[0]);
                    path_1.Insert(j + 4, path_1[j + 3].nextNodes[0]);
                }
            }

            for (int j = 0; j < path_2.Count - 2; j++)  //-2 so not to include node before last bus stop
            {
                if (path_2[j].isBusBranch)
                {
                    path_2.RemoveAt(j + 1);
                    path_2.Insert(j + 1, path_2[j].nextNodes[1]);
                    path_2.Insert(j + 2, path_2[j + 1].nextNodes[0]);
                    path_2.Insert(j + 3, path_2[j + 2].nextNodes[0]);
                    path_2.Insert(j + 4, path_2[j + 3].nextNodes[0]);
                }
            }

            List<Node> busPath = path_1;
            busPath.AddRange(path_2);

            NativeList<float4> dotsPath = new NativeList<float4>(busPath.Count, Allocator.Persistent);

            foreach (Node n in busPath)
            {
                if (n.isLaneChange) dotsPath.Add(new float4(n.transform.position, LANE_CHANGE));
                else if (n.isIntersection) dotsPath.Add(new float4(n.transform.position, INTERSECTION));
                else if (n.isLaneMergeLeft) dotsPath.Add(new float4(n.transform.position, MERGE_LEFT));
                else if (n.isBusMerge) dotsPath.Add(new float4(n.transform.position, BUS_MERGE));
                else if (n.isBusStop) dotsPath.Add(new float4(n.transform.position, BUS_STOP));
                else if (n.isLaneMergeRight) dotsPath.Add(new float4(n.transform.position, MERGE_RIGHT));
                else dotsPath.Add(new float4(n.transform.position, 0));
            }


            BlobBuilder builder = new BlobBuilder(Allocator.Persistent);
            ref NativeList<float4> blobData = ref builder.ConstructRoot<NativeList<float4>>();
            blobData = dotsPath;
            BlobAssetReference<NativeList<float4>> blobReference = builder.CreateBlobAssetReference<NativeList<float4>>(Allocator.Persistent);

            GameObject spawnedBus = GameObject.Instantiate(busToSpawn, busPath[0].transform.position, Quaternion.Euler(0, busPath[0].GetComponentInParent<Street>().transform.rotation.eulerAngles.y - 90, 0)) as GameObject;

            CarComponents busData = spawnedBus.GetComponent<CarComponents>();

            busData.Speed = 2f;
            busData.SpeedDamping = 0.2f;
            busData.busPath = blobReference;
            busData.currentNode = 1;
            busData.isBus = true;
            busData.isParking = false;

            spawnedBus.AddComponent<ConvertToEntity>();



            busStopsSpawn.RemoveAt(randSpawnNodeIndex);
            numBusStopsSpawn--;
        }
    }

}

//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Unity.Entities;
//using Unity.Collections;
//using Unity.Mathematics;

//public class BusSpawner : MonoBehaviour
//{

//    public GameObject busPrefab;
//    public CityGenerator city;
//    private int numBusesToSpawn;
//    private List<Node> busStopsSpawn;
//    private List<Node> busStopsDst;

//    private const int LANE_CHANGE = 1;
//    private const int BUS_STOP = 2;
//    private const int BUS_MERGE = 3;
//    private const int INTERSECTION = 4;
//    private const int MERGE_LEFT = 5;
//    private const int MERGE_RIGHT = 6;
//    private const int PARKING_GATEWAY = 7;
//    private const int SEM_INTERSECTION = 8;
//    private const int CURVE = 9;

//    

//    public BusSpawner(GameObject busPrefab, CityGenerator city)
//    {
//        this.busPrefab = busPrefab;
//        this.city = city;
//        this.numBusesToSpawn = city.numberBusesToSpawn;
//        this.busStopsSpawn = city.cityBusStopsSpawn;
//        this.busStopsDst = city.cityBusStopsDst;
//        if (numBusesToSpawn > busStopsSpawn.Count)
//        {
//            numBusesToSpawn = busStopsSpawn.Count;
//        }
//        dotsPaths = new List<NativeList<float4>>();
//        ManageUI manageUI = GameObject.Find("ManageUI").GetComponent<ManageUI>();
//        manageUI.numberOfBuses = numBusesToSpawn;
//    }

//    private void OnDestroy()
//    {
//        foreach (var el in dotsPaths)
//        {
//            el.Dispose();
//        }
//    }

//    public void generateBuses()
//    {
//        if (numBusesToSpawn == 0) return;

//        int numPathsToCreate = numBusesToSpawn / 20 + 1;

//        //dotsPaths = new NativeList<NativeList<NodeData>>(numPathsToCreate, Allocator.Persistent);

//        List<List<Node>> paths = new List<List<Node>>();
//        List<int> numSpawnSections = new List<int>();
//        List<int> numBusesSpawn = new List<int>();

//        Dictionary<int, bool> spawnedBusesPosition = new Dictionary<int, bool>();
//        Path path = new Path();

//        //Creating all looped paths
//        for (int i = 0; i < numPathsToCreate; i++)
//        {
//            int randSpawnNodeIndex = UnityEngine.Random.Range(0, busStopsSpawn.Count);
//            Node busStop_1 = busStopsSpawn[randSpawnNodeIndex];
//            Node busStop_2 = busStop_1;
//            while (busStop_1.Equals(busStop_2))
//            {
//                busStop_2 = busStopsDst[UnityEngine.Random.Range(0, busStopsDst.Count)];
//            }

//            List<Node> path_1 = path.findShortestPath(busStop_1, busStop_2);
//            List<Node> path_2 = path.findShortestPath(busStop_2, busStop_1);

//            path_1.RemoveAt(path_1.Count - 1);
//            path_2.RemoveAt(0);
//            path_2.RemoveAt(path_2.Count - 1);

//            for (int j = 0; j < path_1.Count - 2; j++)  //-2 so not to include node before last bus stop
//            {
//                if (path_1[j].isBusBranch)
//                {
//                    path_1.RemoveAt(j + 1);
//                    path_1.Insert(j + 1, path_1[j].nextNodes[1]);
//                    path_1.Insert(j + 2, path_1[j + 1].nextNodes[0]);
//                    path_1.Insert(j + 3, path_1[j + 2].nextNodes[0]);
//                    path_1.Insert(j + 4, path_1[j + 3].nextNodes[0]);
//                }
//            }

//            for (int j = 0; j < path_2.Count - 2; j++)  //-2 so not to include node before last bus stop
//            {
//                if (path_2[j].isBusBranch)
//                {
//                    path_2.RemoveAt(j + 1);
//                    path_2.Insert(j + 1, path_2[j].nextNodes[1]);
//                    path_2.Insert(j + 2, path_2[j + 1].nextNodes[0]);
//                    path_2.Insert(j + 3, path_2[j + 2].nextNodes[0]);
//                    path_2.Insert(j + 4, path_2[j + 3].nextNodes[0]);
//                }
//            }

//            paths.Insert(i, path_1);
//            paths[i].AddRange(path_2);


//            if (numBusesToSpawn < 0)
//            {
//                numBusesSpawn.Insert(i, 0);
//            }
//            else if (numBusesToSpawn - 20 < 0)
//            {
//                numBusesSpawn.Insert(i, numBusesToSpawn);
//                numBusesToSpawn -= 20;
//            }
//            else
//            {
//                numBusesSpawn.Insert(i, 20);
//                numBusesToSpawn -= 20;
//            }

//            NativeList<float4> dotsPath = new NativeList<float4>(paths[i].Count, Allocator.Persistent);

//            foreach (Node n in paths[i])
//            {
//                if (n.isLaneChange) dotsPath.Add(new float4(n.transform.position, LANE_CHANGE));
//                else if (n.isIntersection) dotsPath.Add(new float4(n.transform.position, INTERSECTION));
//                else if (n.isLaneMergeLeft) dotsPath.Add(new float4(n.transform.position, MERGE_LEFT));
//                else if (n.isBusMerge) dotsPath.Add(new float4(n.transform.position, BUS_MERGE));
//                else if (n.isBusStop) dotsPath.Add(new float4(n.transform.position, BUS_STOP));
//                else if (n.isLaneMergeRight) dotsPath.Add(new float4(n.transform.position, MERGE_RIGHT));
//                else dotsPath.Add(new float4(n.transform.position, 0));
//            }

//            dotsPaths.Add(dotsPath);

//        }

//        //Analyzing paths
//        for (int i = 0; i < numPathsToCreate; i++)
//        {
//            numSpawnSections.Insert(i, 0);
//            for (int j = 0; j < paths[i].Count; j++)
//            {
//                if (paths[i][j].isBusStop)
//                {
//                    numSpawnSections[i]++;
//                }
//            }
//        }

//        //Now spawn cars
//        for (int i = 0; i < numPathsToCreate; i++)
//        {
//            if (numBusesSpawn[i] <= 0)
//            {
//                continue;
//            }

//            BlobBuilder builder = new BlobBuilder(Allocator.Persistent);
//            ref NativeList<float4> blobData = ref builder.ConstructRoot<NativeList<float4>>();
//            blobData = this.dotsPaths[i];
//            BlobAssetReference<NativeList<float4>> blobReference = builder.CreateBlobAssetReference<NativeList<float4>>(Allocator.Persistent);
//            builder.Dispose();

//            for (int j = 0; j < paths[i].Count; j++)
//            {
//                if (paths[i][j].isBusStop && !spawnedBusesPosition.ContainsKey(GetPositionHashMapKey(paths[i][j].transform.position)))
//                {
//                    Vector3 spawnPosition = paths[i][j].transform.position;

//                    Quaternion spawnRotation = Quaternion.Euler(0, ReturnRotationBus(paths[i][j]), 0);
//                    GameObject busToSpawn = busPrefab;

//                    GameObject spawnedBus = Instantiate(busToSpawn, spawnPosition, spawnRotation);

//                    CarComponents busData = spawnedBus.GetComponent<CarComponents>();

//                    busData.Speed = 2f;
//                    busData.SpeedDamping = 0.2f;
//                    busData.busPath = blobReference;
//                    busData.currentNode = (j + 1) % paths[i].Count;
//                    busData.isBus = true;
//                    busData.isParking = false;

//                    spawnedBus.AddComponent<ConvertToEntity>();

//                    spawnedBusesPosition.Add(GetPositionHashMapKey(spawnPosition), true);

//                    numBusesSpawn[i]--;
//                    if (numBusesSpawn[i] <= 0)
//                    {
//                        break;
//                    }
//                }
//            }

//            if (numBusesToSpawn <= 0)
//            {
//                break;
//            }

//            if (i < numPathsToCreate - 1)
//            {
//                numBusesSpawn[i + 1] += numBusesSpawn[i];
//            }
//        }
//    }

//    private int ReturnRotationBus(Node spawnNode)
//    {
//        int busRotation;

//        if (spawnNode.gameObject.GetComponentInParent<Street>().numberLanes == 1)
//        {
//            if ((int)spawnNode.transform.position.x == (int)spawnNode.nextNodes[0].transform.position.x)
//            {
//                if ((int)spawnNode.transform.position.z < (int)spawnNode.nextNodes[0].transform.position.z)
//                {
//                    busRotation = 0;
//                }
//                else
//                {
//                    busRotation = 180;
//                }
//            }
//            else
//            {
//                if ((int)spawnNode.transform.position.x < (int)spawnNode.nextNodes[0].transform.position.x)
//                {
//                    busRotation = 90;
//                }
//                else
//                {
//                    busRotation = 270;
//                }
//            }
//        }
//        else
//        {
//            if ((int)spawnNode.transform.parent.localRotation.eulerAngles.y == 0)
//            {       //HORIZONTAL STREET
//                if (spawnNode.trafficDirection == 0)
//                {
//                    busRotation = 90;
//                }
//                else
//                {
//                    busRotation = 270;
//                }
//            }
//            else
//            {   //VERTICAL
//                if (spawnNode.trafficDirection == 0)
//                {
//                    busRotation = 180;
//                }
//                else
//                {
//                    busRotation = 0;
//                }
//            }
//        }
//        return busRotation;
//    }

//    public static int GetPositionHashMapKey(Vector3 position)
//    {
//        int xPosition = (int)position.x;
//        int zPosition = (int)position.z;
//        return xPosition * 1000000 + zPosition;
//    }

//}