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

    private bool init; 

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

        init = true;
    }

    public void generateTraffic(int numberCarsToSpawnOnFrame)
    {

        GameObject[] nodes = GameObject.FindGameObjectsWithTag("CarWaypoint");

        GameObject[] carSpanGameObj = GameObject.FindGameObjectsWithTag("CarSpawn");
        

        /*foreach(GameObject n in nodes)
        {
            if (!n.GetComponentInParent<Street>().carWaypoints.Contains(n.GetComponent<Node>()))
            {
                throw new System.Exception(n.transform.parent.name);
            }
        }*/
        List<Node> nodesList = new List<Node>();

        nodes[0].GetComponent<Node>();

        //NewPathSystem pathSystem = new NewPathSystem();

        NativeMultiHashMap<float3, float3> nodesCity = new NativeMultiHashMap<float3, float3>(nodes.Length + carSpanGameObj.Length, Allocator.Temp);
        NativeArray<float3> waypoitnsCity = new NativeArray<float3>(nodes.Length + carSpanGameObj.Length, Allocator.Temp);

        for (int i = 0; i < carSpanGameObj.Length; i++)
        {

            nodesCity.Add(carSpanGameObj[i].GetComponent<Node>().transform.position, carSpanGameObj[i].GetComponent<Node>().nextNodes[0].transform.position);

            waypoitnsCity[i] = carSpanGameObj[i].GetComponent<Node>().transform.position;
            //nodesCity.Add(carSpanGameObj[i].GetComponent<Node>());
        }

        for (int i = 0; i < nodes.Length; i++)
        {
            if (!nodes[i].GetComponent<Node>().isParkingSpot)
            {

                for (int j = 0; j < nodes[i].GetComponent<Node>().nextNodes.Count; j++)
                {
                    nodesCity.Add(nodes[i].GetComponent<Node>().transform.position, nodes[i].GetComponent<Node>().nextNodes[j].transform.position);
                }


                waypoitnsCity[i + carSpanGameObj.Length] = nodes[i].GetComponent<Node>().transform.position;
            }
            else
            {
                parkingWaypoints[i] = nodes[i].GetComponent<Node>();
            }

            //nextNodes.Dispose();
        }


        NativeList<float3> spawnNodeList = new NativeList<float3>(numCarsToSpawn, Allocator.Temp);
        NativeList<float3> destinationNodeList = new NativeList<float3>(numCarsToSpawn, Allocator.Temp);

        

        if (init)
        {
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
                spawnNodeList.Add(spawnWaypoints[randomSrcNode].transform.position);
                sNode.Add(spawnWaypoints[randomSrcNode]);

                spawnWaypoints.RemoveAt(randomSrcNode);

                Parking possiblePaking = parkingWaypoints[randomDstNodeIndex].parkingPrefab.GetComponent<Parking>();

                int randomParkingSpot = UnityEngine.Random.Range(0, possiblePaking.freeParkingSpots.Count);

                possiblePaking.numberFreeSpots--;

                if (possiblePaking.numberFreeSpots == 0)
                {
                    possiblePaking.freeParkingSpots.RemoveAt(randomParkingSpot);
                }

                pNode.Add(possiblePaking.freeParkingSpots[randomParkingSpot]);
                possiblePaking.freeParkingSpots[randomParkingSpot].isOccupied = true;

                destinationNodeList.Add(parkingWaypoints[randomDstNodeIndex].transform.position);
                dNode.Add(parkingWaypoints[randomDstNodeIndex]);
            }
            this.init = false;
            this.numCarsToSpawn = numCarsToSpownNow;
        }
        else
        {
            if(numCarsToSpawn> numCarsToSpownNow)
            for (int i = numCarsToSpawn - numCarsToSpownNow; i < numCarsToSpawn - numCarsToSpownNow + numberCarsToSpawnOnFrame; i++)
            {
                    try
                    {
                        spawnNodeList.Add(sNode[i].transform.position);
                        destinationNodeList.Add(dNode[i].transform.position);
                    }
                    catch (System.Exception e)
                    {
                        Debug.Log("prova");
                    }

            }
        }


        NewPathSystemMono newPathSystemMono = new NewPathSystemMono();
        Dictionary<int, NativeList<float3>> sampleJobArray = new Dictionary<int, NativeList<float3>>();
        sampleJobArray = newPathSystemMono.PathSystemJob(numberCarsToSpawnOnFrame, spawnNodeList, destinationNodeList, waypoitnsCity, nodesCity);
        int k = 0;
        for (int i = numCarsToSpawn - numCarsToSpownNow; i < numCarsToSpawn - numCarsToSpownNow + numberCarsToSpawnOnFrame; i++)
        {
            Node spawnNode = sNode[i];

            // si può pure togliere
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
            // fino a qui

            Node destinationNode = dNode[i];

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
            
            for (int j = 0; j < sampleJobArray[k].Length; j++)
            {
                carData.pathNodeList.Add(sampleJobArray[k][j]);
            }

            Instantiate(carToSpawn, spawnNode.transform.position, Quaternion.Euler(0, carRotation, 0));

            spawnWaypoints.Remove(spawnNode);
            k++;
        }

        numCarsToSpownNow -= numberCarsToSpawnOnFrame;

        //pathNative.Dispose();
        for (int i = 0; i < sampleJobArray.Count; i++)
            sampleJobArray[i].Dispose();

        waypoitnsCity.Dispose();
        nodesCity.Dispose();

        Debug.Log("FINISH CAR SPAWNING!!!");

    }

}