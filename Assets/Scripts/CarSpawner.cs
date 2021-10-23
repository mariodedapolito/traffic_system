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

    public void generateTraffic(int numberCarsToSpawn, float profondity)
    {
        Debug.Log(numCarsToSpawn);
        NativeList<float3> spawnNodeList = new NativeList<float3>(numCarsToSpawn, Allocator.Temp);
        NativeList<float3> destinationNodeList = new NativeList<float3>(numCarsToSpawn, Allocator.Temp);

        GameObject city = GameObject.FindGameObjectWithTag("CityGenerator");

        //init variable the first cars on frame
        sNode = new List<Node>();
        dNode = new List<Node>();
        pNode = new List<Node>();

        if (spawnWaypoints.Count < numCarsToSpownNow)
        {
            Debug.LogError("Not enough cars node spawn! Increase the number of rows.");
            return;
        }

        float timeExitParking = UnityEngine.Random.Range(10f, 250f);

        for (int i = 0; i < numCarsToSpownNow; i++)
        {
            int randomSrcNode = UnityEngine.Random.Range(0, spawnWaypoints.Count);
            int randomDstNodeIndex = UnityEngine.Random.Range(0, parkingWaypoints.Count);

            while (math.distance(spawnWaypoints[randomSrcNode].transform.position, parkingWaypoints[randomDstNodeIndex].transform.position) >= profondity) randomDstNodeIndex = UnityEngine.Random.Range(0, parkingWaypoints.Count);



            spawnNodeList.Add(spawnWaypoints[randomSrcNode].transform.position);
            sNode.Add(spawnWaypoints[randomSrcNode]);

            spawnWaypoints.RemoveAt(randomSrcNode);
            /*
            pNode.Add(possiblePaking.freeParkingSpots[randomParkingSpot]);
            possiblePaking.freeParkingSpots[randomParkingSpot].isOccupied = true;
            */
            destinationNodeList.Add(parkingWaypoints[randomDstNodeIndex].transform.position);
            dNode.Add(parkingWaypoints[randomDstNodeIndex]);
        }

        int k = 0;

        for (int i = 0; i < numCarsToSpawn; i++)
        {
            Node spawnNode = sNode[i];

            Node destinationNode = dNode[i];

            int carIndex = UnityEngine.Random.Range(0, carPrefab.Count);
            GameObject carToSpawn = carPrefab[carIndex];
            CarComponents carData = carToSpawn.GetComponent<CarComponents>();

            if (i < numberCarsToSpawn / 10 * 7)
            {
                int randomDstNodeIndex = UnityEngine.Random.Range(0, parkingWaypoints.Count);

                Parking possiblePaking = parkingWaypoints[randomDstNodeIndex].parkingPrefab.GetComponent<Parking>();

                int randomParkingSpot = UnityEngine.Random.Range(0, possiblePaking.freeParkingSpots.Count);

                possiblePaking.numberFreeSpots--;

                if (possiblePaking.numberFreeSpots == 0)
                {
                    possiblePaking.freeParkingSpots.RemoveAt(randomParkingSpot);
                }

                possiblePaking.freeParkingSpots[randomParkingSpot].isOccupied = true;

                carData.parkingNode = possiblePaking.freeParkingSpots[randomParkingSpot];
                carData.destinationNode = parkingWaypoints[randomDstNodeIndex];

                carData.startingNode = parkingWaypoints[randomDstNodeIndex];
                carData.isParking = true;
                carData.currentNode = 0;
                carData.timeExitParking = 15 + i * 2;

                spawnNode = possiblePaking.freeParkingSpots[randomParkingSpot];
                spawnNode.transform.rotation = Quaternion.Euler(0, ReturnRotationCar(parkingWaypoints[randomDstNodeIndex]), 0);
            }
            else
            {
                carData.isParking = false;
                carData.startingNode = spawnNode;
                carData.currentNode = 1;
                carData.destinationNode = destinationNode;
            }
            carData.Speed = 2f;
            carData.SpeedDamping = carData.Speed / 10f;


            //carData.parkingNode = pNode[i];

            //Debug.Log(spawnNode.transform.rotation);

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
            Instantiate(carToSpawn, spawnNode.transform.position, spawnNode.transform.rotation);

            spawnWaypoints.Remove(spawnNode);
            k++;
        }

        //Debug.Log("FINISH CAR SPAWNING!!!");

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

}