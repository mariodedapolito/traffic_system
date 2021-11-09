using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;


public class CarSpawner
{

    public List<GameObject> carPrefab;
    public CityGenerator city;

    //public readonly struct NodeData
    //{
    //    public readonly float3 nodePosition;
    //    public readonly int nodeType;

    //    public NodeData(float3 nodePosition, int nodeType)
    //    {
    //        this.nodePosition = nodePosition;
    //        this.nodeType = nodeType;
    //    }
    //}

    private List<Node> spawnWaypoints;
    private List<Node> parkingWaypoints;
    private int numCarsToSpownNow;

    private const int LANE_CHANGE = 1;
    private const int BUS_STOP = 2;
    private const int BUS_MERGE = 3;
    private const int INTERSECTION = 4;
    private const int MERGE_LEFT = 5;
    private const int MERGE_RIGHT = 6;
    private const int PARKING_GATEWAY = 7;
    private const int SEM_INTERSECTION = 8;
    private const int CURVE = 9;

    private CameraFollow followCar;

    public List<NativeList<float4>> dotsPaths;

    public List<BlobAssetReference<NativeList<float4>>> blobReferences;

    private void OnDestroy()
    {
        Debug.Log("DESTROYING");
        foreach (var el in dotsPaths)
        {
            el.Dispose();
        }
    }

    public CarSpawner(List<GameObject> carPrefab, CityGenerator city, int numCarsToSpawn)
    {
        this.carPrefab = carPrefab;
        this.city = city;
        this.spawnWaypoints = city.citySpawnNodes;
        this.parkingWaypoints = city.cityParkingNodes;
        //if (numCarsToSpawn >= spawnWaypoints.Count)
        //{
        //    this.numCarsToSpawn = spawnWaypoints.Count - 1;
        //}
        //else
        //{
        this.numCarsToSpownNow = numCarsToSpawn;
        dotsPaths = new List<NativeList<float4>>();
        blobReferences = new List<BlobAssetReference<NativeList<float4>>>();

        followCar = GameObject.Find("CameraManager").GetComponent<CameraFollow>();
    }

    public void generateTraffic()
    {
        if (numCarsToSpownNow == 0) return;

        int numberCarsToSpawnValue = numCarsToSpownNow;

        int numCarPrefabs = carPrefab.Count;

        int numPathsToCreate = numCarsToSpownNow / 500 + 1;

        int numCarsToCreate = numCarsToSpownNow;

        //dotsPaths = new NativeList<NativeList<NodeData>>(numPathsToCreate, Allocator.Persistent);

        List<List<Node>> paths = new List<List<Node>>();
        //List<List<CarPathNodeContainer>> dotsPaths = new List<List<CarPathNodeContainer>>();
        List<int> numParkings = new List<int>();
        List<int> numCarsSpawnParked = new List<int>();
        List<int> numCarsSpawnStreet = new List<int>();
        List<int> numSpawnSections = new List<int>();
        List<Node> tmpDstNodes = new List<Node>();  //list used to restore used destination waypoints to be used for creating DOTS parking system

        Dictionary<int, bool> spawnedCarsPosition = new Dictionary<int, bool>();
        Path path = new Path();

        //Creating all looped paths
        for (int i = 0; i < numPathsToCreate; i++)
        {
            int randomSpawnIndex = (int)UnityEngine.Random.Range(0f, spawnWaypoints.Count);
            Node randomSpawnNode = spawnWaypoints[randomSpawnIndex];
            spawnWaypoints.RemoveAt(randomSpawnIndex);
            int randomDstIndex = (int)UnityEngine.Random.Range(0f, parkingWaypoints.Count);
            Node randomDstNode = parkingWaypoints[randomDstIndex];
            tmpDstNodes.Add(randomDstNode);     //list used to restore used destination waypoints to be used for creating DOTS parking system
            parkingWaypoints.RemoveAt(randomDstIndex);

            List<Node> pathA = path.findShortestPath(randomSpawnNode, randomDstNode);
            if (pathA.Count == 0)
            {
                throw new System.Exception("NO PATH FOUND for A");
            }
            pathA.RemoveAt(pathA.Count - 1);

            List<Node> pathB = path.findShortestPath(randomDstNode, randomSpawnNode);
            if (pathB.Count == 0)
            {
                throw new System.Exception("NO PATH FOUND for B");
            }
            pathB.RemoveAt(pathB.Count - 1);

            paths.Insert(i, new List<Node>());
            paths[i].AddRange(pathA);
            paths[i].AddRange(pathB);

            NativeList<float4> dotsPath = new NativeList<float4>(paths[i].Count, Allocator.Persistent);

            foreach (Node n in paths[i])
            {
                if (n.isParkingGateway) dotsPath.Add(new float4(n.transform.position, PARKING_GATEWAY));
                else if (n.isLaneChange) dotsPath.Add(new float4(n.transform.position, LANE_CHANGE));
                else if (n.isIntersection) dotsPath.Add(new float4(n.transform.position, INTERSECTION));
                else if (n.isLaneMergeLeft) dotsPath.Add(new float4(n.transform.position, MERGE_LEFT));
                else if (n.isLaneMergeRight) dotsPath.Add(new float4(n.transform.position, MERGE_RIGHT));
                else dotsPath.Add(new float4(n.transform.position, 0));
            }

            //Debug.Log(dotsPath.Length);

            dotsPaths.Add(dotsPath);

            //Debug.Log(dotsPaths[i].Count);

        }

        //Analyzing paths
        for (int i = 0; i < numPathsToCreate; i++)
        {
            numParkings.Insert(i, 0);
            numSpawnSections.Insert(i, 0);
            for (int j = 0; j < paths[i].Count; j++)
            {
                if (paths[i][j].isParkingGateway)
                {
                    numParkings[i]++;
                }
                if ((!paths[i][j].isIntersection && !paths[i][j].isSemaphoreIntersection && !paths[i][j].isCurve && !paths[i][j].isLaneChange) &&
                    (!paths[i][(j + 1) % paths[i].Count].isIntersection && !paths[i][(j + 1) % paths[i].Count].isSemaphoreIntersection && !paths[i][(j + 1) % paths[i].Count].isCurve && !paths[i][(j + 1) % paths[i].Count].isLaneChange))
                {
                    numSpawnSections[i]++;
                }
            }
        }

        //Decide how many cars to spawn on street and on parkings
        int parkingMaxPositions = 50;
        for (int i = 0; i < numPathsToCreate; i++)
        {
            if (numCarsToSpownNow < 0)
            {
                numCarsSpawnParked.Insert(i, 0);
            }
            else if (numCarsToSpownNow - 500 < 0)
            {
                numCarsSpawnParked.Insert(i, (int)((float)numCarsToSpownNow * 0.7f));
                numCarsSpawnStreet.Insert(i, (int)((float)numCarsToSpownNow * 0.3f));
                numCarsToSpownNow -= 500;
            }
            else
            {
                numCarsToSpownNow -= 500;
                if (parkingMaxPositions * numParkings[i] > 350)
                {
                    numCarsSpawnParked.Insert(i, 350);
                    numCarsSpawnStreet.Insert(i, 150);
                }
                else
                {
                    numCarsSpawnParked.Insert(i, parkingMaxPositions * numParkings[i]);
                    numCarsSpawnStreet.Insert(i, 500 - (parkingMaxPositions * numParkings[i]));
                }
            }
        }

        int numCarsSpawned = 0;

        //Now spawn cars
        for (int i = 0; i < numPathsToCreate; i++)
        {

            BlobBuilder builder = new BlobBuilder(Allocator.Persistent);
            ref NativeList<float4> blobData = ref builder.ConstructRoot<NativeList<float4>>();
            blobData = this.dotsPaths[i];
            BlobAssetReference<NativeList<float4>> blobReference = builder.CreateBlobAssetReference<NativeList<float4>>(Allocator.Persistent);
            builder.Dispose();

            for (int j = 0; j < paths[i].Count; j++)
            {
                if (numCarsSpawnParked[i] > 0 && paths[i][j].isParkingGateway)
                {
                    Parking parking = paths[i][j].parkingPrefab.GetComponent<Parking>();
                    int maxSpawnableCars = parking.numberFreeSpots;
                    int targetCarsToSpawnParked = numCarsSpawnParked[i] / numParkings[i] + 1;
                    int startingSpawnPosition = parking.numberParkingSpots - parking.numberFreeSpots;
                    int actualCarsSpawnedInParking = 0;


                    for (int k = startingSpawnPosition; k < startingSpawnPosition + maxSpawnableCars; k++)
                    {
                        GameObject carToSpawn = carPrefab[UnityEngine.Random.Range(0, numCarPrefabs)];

                        Vector3 spawnPosition = parking.freeParkingSpots[k].transform.position;
                        Quaternion spawnRotation = Quaternion.Euler(0, ReturnRotationCar(paths[i][j]), 0);

                        GameObject spawnedCar = GameObject.Instantiate(carToSpawn, spawnPosition, spawnRotation) as GameObject;

                        CarComponents carData = spawnedCar.GetComponent<CarComponents>();

                        carData.Speed = 2f;
                        carData.SpeedDamping = 0.2f;
                        carData.carPath = blobReference;
                        carData.currentNode = (j + 1) % paths[i].Count;
                        carData.isCar = true;
                        carData.isParking = true;
                        carData.parkingGateWay = paths[i][j].transform.position;
                        carData.timeExitParking = (int)UnityEngine.Random.Range(15f, numCarsToCreate / 10 < 15 ? 150f : numCarsToCreate / 10);

                        carData.followCar = followCar;

                        spawnedCar.AddComponent<ConvertToEntity>();

                        parking.numberFreeSpots--;
                        numCarsSpawnParked[i]--;
                        actualCarsSpawnedInParking++;
                        numCarsSpawned++;
                        if (actualCarsSpawnedInParking >= targetCarsToSpawnParked)
                        {
                            break;
                        }
                    }
                    numParkings[i]--;
                }
                if (numCarsSpawnStreet[i] > 0 &&
                    (!paths[i][j].isIntersection && !paths[i][j].isSemaphoreIntersection && !paths[i][j].isCurve && !paths[i][j].isLaneChange) &&
                    (!paths[i][(j + 1) % paths[i].Count].isIntersection && !paths[i][(j + 1) % paths[i].Count].isSemaphoreIntersection && !paths[i][(j + 1) % paths[i].Count].isCurve && !paths[i][(j + 1) % paths[i].Count].isLaneChange))
                {
                    int sectionLength = (int)math.distance(paths[i][j].transform.position, paths[i][(j + 1) % paths[i].Count].transform.position);
                    int maxSpawnableCars = sectionLength / 3;
                    int targetCarsToSpawnInSection = (numCarsSpawnStreet[i] / numSpawnSections[i]) + 1;
                    int actualCarsSpawnedInSection = 0;
                    Vector3 spawnSection = math.normalize(paths[i][(j + 1) % paths[i].Count].transform.position - paths[i][j].transform.position);
                    for (int k = 0; k < maxSpawnableCars; k++)
                    {
                        Vector3 spawnPosition = paths[i][j].transform.position + (spawnSection * (k * 3));
                        if (!spawnedCarsPosition.ContainsKey(GetPositionHashMapKey(spawnPosition)))
                        {
                            Quaternion spawnRotation = Quaternion.Euler(0, ReturnRotationCar(paths[i][j]), 0);
                            GameObject carToSpawn = carPrefab[UnityEngine.Random.Range(0, numCarPrefabs)];

                            GameObject spawnedCar = GameObject.Instantiate(carToSpawn, spawnPosition, spawnRotation) as GameObject;

                            CarComponents carData = spawnedCar.GetComponent<CarComponents>();

                            carData.Speed = 2f;
                            carData.SpeedDamping = 0.2f;
                            carData.carPath = blobReference;
                            carData.currentNode = (j + 1) % paths[i].Count;
                            carData.isCar = true;
                            carData.isParking = false;

                            carData.followCar = followCar;

                            spawnedCar.AddComponent<ConvertToEntity>();

                            numCarsSpawnStreet[i]--;
                            spawnedCarsPosition.Add(GetPositionHashMapKey(spawnPosition), true);
                            actualCarsSpawnedInSection++;
                            numCarsSpawned++;
                            if (actualCarsSpawnedInSection >= targetCarsToSpawnInSection)
                            {
                                break;
                            }
                        }
                    }
                    numSpawnSections[i]--;
                }
            }
            if (i < numPathsToCreate - 1)
            {
                numCarsSpawnParked[i + 1] += numCarsSpawnParked[i];
                numCarsSpawnStreet[i + 1] += numCarsSpawnStreet[i];
            }
        }

        city.cityParkingNodes.AddRange(tmpDstNodes);

        if(numCarsSpawned< numberCarsToSpawnValue - 50)
        {
            city.numberCarsToSpawn = numCarsSpawned;
            city.numberCarsToSpawnLimited = true;
        }
        else
        {
            city.numberCarsToSpawnLimited = false;
        }

    }

    private int ReturnRotationCar(Node spawnNode)
    {
        int carRotation;

        if (spawnNode.gameObject.GetComponentInParent<Street>().numberLanes == 1)
        {
            if ((int)spawnNode.transform.position.x == (int)spawnNode.nextNodes[0].transform.position.x)
            {
                if ((int)spawnNode.transform.position.z < (int)spawnNode.nextNodes[0].transform.position.z)
                {
                    carRotation = 0;
                }
                else
                {
                    carRotation = 180;
                }
            }
            else
            {
                if ((int)spawnNode.transform.position.x < (int)spawnNode.nextNodes[0].transform.position.x)
                {
                    carRotation = 90;
                }
                else
                {
                    carRotation = 270;
                }
            }
        }
        else
        {
            if ((int)spawnNode.transform.parent.localRotation.eulerAngles.y == 0)
            {       //HORIZONTAL STREET
                if (spawnNode.trafficDirection == 0)
                {
                    carRotation = 90;
                }
                else
                {
                    carRotation = 270;
                }
            }
            else
            {   //VERTICAL
                if (spawnNode.trafficDirection == 0)
                {
                    carRotation = 180;
                }
                else
                {
                    carRotation = 0;
                }
            }
        }
        return carRotation;
    }

    public static int GetPositionHashMapKey(Vector3 position)
    {
        int xPosition = (int)position.x;
        int zPosition = (int)position.z;
        return xPosition * 1000000 + zPosition;
    }
}