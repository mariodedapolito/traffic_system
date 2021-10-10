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

    private int carId = 0;

    private List<Entity> carEntities;
    private EntityManager entityManager;
    private BlobAssetStore blobAssetStore;

    private List<Node> sNode;
    private List<Node> dNode;
    private List<Node> pNode;


    public CarSpawner(List<GameObject> carPrefab, CityGenerator city, int numCarsToSpawn)
    {
        this.carPrefab = carPrefab;
        this.city = city;
        this.spawnWaypoints = city.citySpawnNodes;
        this.parkingWaypoints = city.cityParkingNodes;
        if (numCarsToSpawn >= spawnWaypoints.Count)
        {
            this.numCarsToSpawn = spawnWaypoints.Count - 1;
        }
        else
        {
            this.numCarsToSpawn = numCarsToSpawn;
        }
        numCarsToSpownNow = numCarsToSpawn;
    }

    public void generateTraffic(int numberCarsToSpawnOnFrame, float profondity, NativeMultiHashMap<float3, float3> nodesCity, NativeArray<float3> waypoitnsCity)
    {
        NativeList<float3> spawnNodeList = new NativeList<float3>(numCarsToSpawn, Allocator.Temp);
        NativeList<float3> destinationNodeList = new NativeList<float3>(numCarsToSpawn, Allocator.Temp);

        //init variable the first cars on frame
        sNode = new List<Node>();
        dNode = new List<Node>();
        pNode = new List<Node>();

        if (spawnWaypoints.Count < numCarsToSpownNow)
        {
            Debug.LogError("Not enough cars node spawn! Increase the number of rows.");
            return;
        }

        for (int i = 0; i < numCarsToSpownNow; i++)
        {
            int randomSrcNode = UnityEngine.Random.Range(0, spawnWaypoints.Count);
            int randomDstNodeIndex = UnityEngine.Random.Range(0, parkingWaypoints.Count);

            while(math.distance(spawnWaypoints[randomSrcNode].transform.position, parkingWaypoints[randomDstNodeIndex].transform.position) >= profondity) randomDstNodeIndex = UnityEngine.Random.Range(0, parkingWaypoints.Count);

            

            Parking possiblePaking = parkingWaypoints[randomDstNodeIndex].parkingPrefab.GetComponent<Parking>();

            int randomParkingSpot = UnityEngine.Random.Range(0, possiblePaking.freeParkingSpots.Count);

            possiblePaking.numberFreeSpots--;

            if (possiblePaking.numberFreeSpots == 0)
            {
                possiblePaking.freeParkingSpots.RemoveAt(randomParkingSpot);
            }
            
            spawnNodeList.Add(spawnWaypoints[randomSrcNode].transform.position);
            sNode.Add(spawnWaypoints[randomSrcNode]);

            spawnWaypoints.RemoveAt(randomSrcNode);

            pNode.Add(possiblePaking.freeParkingSpots[randomParkingSpot]);
            possiblePaking.freeParkingSpots[randomParkingSpot].isOccupied = true;

            destinationNodeList.Add(parkingWaypoints[randomDstNodeIndex].transform.position);
            dNode.Add(parkingWaypoints[randomDstNodeIndex]);
        }

        int k = 0;

        for (int i = 0; i < numCarsToSpawn; i++)
        {
            Node spawnNode = sNode[i];


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


            Node destinationNode = dNode[i];

            List<Vector3> keyAdd = new List<Vector3>();
            

            int carIndex = UnityEngine.Random.Range(0, carPrefab.Count);
            GameObject carToSpawn = carPrefab[carIndex];
            CarComponents carData = carToSpawn.GetComponent<CarComponents>();

            carData.startingNode = spawnNode;
            carData.currentNode = 1;

            carData.Speed = 2f;
            carData.SpeedDamping = carData.Speed / 10f;


            carData.parkingNode = pNode[i];

            carData.destinationNode = destinationNode;

            carData.pathNodeList.Clear();

            /*
            for (int j = 0; j < sampleJobArray[k].Length; j++)
            {
                carData.pathNodeList.Add(sampleJobArray[k][j]);
            }
            keyAdd.Add(sampleJobArray[k][0]);
            keyAdd.Add(dNode[i].transform.position);
            cacheCarsSpawn.Add(keyAdd, carData.pathNodeList);
            */
            Instantiate(carToSpawn, spawnNode.transform.position, Quaternion.Euler(0, carRotation, 0));

            spawnWaypoints.Remove(spawnNode);
            k++;
        }

        //Debug.Log("FINISH CAR SPAWNING!!!");

    }

}