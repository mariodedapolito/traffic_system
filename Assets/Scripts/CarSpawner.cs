using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;


public class CarSpawner : MonoBehaviour
{

    public List<GameObject> carPrefab;
    public CityGenerator city;
    private List<Node> spawnWaypoints;
    private List<Node> parkingWaypoints;
    private int numCarsToSpawn;
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
        this.numCarsToSpawn = numCarsToSpawn;
        //}
        numCarsToSpownNow = numCarsToSpawn;
    }

    public void generateTraffic(int numberCarsToSpawn)
    {
        if (numCarsToSpownNow == 0) return;

        int numPathsToCreate = numberCarsToSpawn / 500 + 1;

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
            if (parkingMaxPositions * numParkings[i] > 350)
            {
                numCarsSpawnParked.Insert(i, 350);
                numCarsSpawnStreet.Insert(i, 150);
            }
            else
            {
                numCarsSpawnParked.Insert(i, parkingMaxPositions * numParkings[i]);
                numCarsSpawnStreet.Insert(i, 500 - parkingMaxPositions * numParkings[i]);
            }
        }
        
        //Now spawn cars
        for (int i = 0; i < numPathsToCreate; i++)
        {
            for (int j = 0; j < paths[i].Count; j++)
            {
                if (paths[i][j].isParkingGateway)
                {
                    Parking parking = paths[i][j].parkingPrefab.GetComponent<Parking>();
                    int maxSpawnableCars = parking.numberFreeSpots;
                    int targetCarsToSpawnParked = numCarsSpawnParked[i] / numParkings[i] + 1;
                    int startingSpawnPosition = parking.numberParkingSpots - parking.numberFreeSpots;
                    int actualCarsSpawnedInParking = 0;
                    for (int k = startingSpawnPosition; k < startingSpawnPosition + maxSpawnableCars; k++)
                    {
                        GameObject carToSpawn = carPrefab[k % carPrefab.Count];
                        CarComponents carData = carToSpawn.GetComponent<CarComponents>();

                        carData.Speed = 2f;
                        carData.SpeedDamping = 0.2f;
                        carData.carPath = paths[i];
                        carData.currentNode = (j + 1) % paths[i].Count;
                        carData.isCar = true;
                        carData.isParking = true;
                        carData.parkingGateWay = paths[i][j].transform.position;
                        carData.timeExitParking = (int)UnityEngine.Random.Range(15f, 500f);

                        Vector3 spawnPosition = parking.freeParkingSpots[k].transform.position;
                        Quaternion spawnRotation = Quaternion.Euler(0, ReturnRotationCar(paths[i][j]), 0);

                        Instantiate(carToSpawn, spawnPosition, spawnRotation);

                        parking.numberFreeSpots--;
                        numCarsSpawnParked[i]--;
                        actualCarsSpawnedInParking++;
                        if (actualCarsSpawnedInParking >= targetCarsToSpawnParked)
                        {
                            break;
                        }
                    }
                    numParkings[i]--;
                }
                if ((!paths[i][j].isIntersection && !paths[i][j].isSemaphoreIntersection && !paths[i][j].isCurve && !paths[i][j].isLaneChange) &&
                    (!paths[i][(j + 1) % paths[i].Count].isIntersection && !paths[i][(j + 1) % paths[i].Count].isSemaphoreIntersection && !paths[i][(j + 1) % paths[i].Count].isCurve && !paths[i][(j + 1) % paths[i].Count].isLaneChange))
                {
                    int sectionLength = (int)math.distance(paths[i][j].transform.position, paths[i][(j + 1) % paths[i].Count].transform.position);
                    int maxSpawnableCars = sectionLength / 2;
                    int targetCarsToSpawnInSection = numCarsSpawnStreet[i] / numSpawnSections[i];
                    int actualCarsSpawnedInSection = 0;
                    Vector3 spawnSection = paths[i][(j + 1) % paths[i].Count].transform.position - paths[i][j].transform.position;
                    for (int k = 0; k < maxSpawnableCars; k++)
                    {
                        Vector3 spawnPosition = paths[i][j].transform.position + spawnSection * (k / maxSpawnableCars);
                        if (spawnedCarsPosition.ContainsKey(GetPositionHashMapKey(spawnPosition)))
                        {
                            continue;
                        }

                        GameObject carToSpawn = carPrefab[k % carPrefab.Count];
                        CarComponents carData = carToSpawn.GetComponent<CarComponents>();

                        carData.Speed = 2f;
                        carData.SpeedDamping = 0.2f;
                        carData.carPath = paths[i];
                        carData.currentNode = (j + 1) % paths[i].Count;
                        carData.isCar = true;
                        carData.isParking = false;


                        Quaternion spawnRotation = Quaternion.Euler(0, ReturnRotationCar(paths[i][j]), 0);

                        Instantiate(carToSpawn, spawnPosition, spawnRotation);

                        numCarsSpawnStreet[i]--;
                        spawnedCarsPosition.Add(GetPositionHashMapKey(spawnPosition), true);
                        if (++actualCarsSpawnedInSection >= targetCarsToSpawnInSection)
                        {
                            break;
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