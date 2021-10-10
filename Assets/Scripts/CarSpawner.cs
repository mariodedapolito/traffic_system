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
    public int numCarsToSpawn=0;
    private int numCarsToSpownNow=0;
 
    private List<Node> sNode;
    private List<Node> dNode;
    private List<Node> pNode;
    public void init(List<GameObject> carPrefab, CityGenerator city, int numCarsToSpawn) {
        sNode = new List<Node>();
        dNode = new List<Node>();
        pNode = new List<Node>();  
        this.carPrefab = carPrefab;
        this.city = city;
        this.spawnWaypoints = city.citySpawnNodes;
        this.parkingWaypoints = city.cityParkingNodes;
        this.numCarsToSpawn=(numCarsToSpawn >= spawnWaypoints.Count)?
            spawnWaypoints.Count - 1:
            numCarsToSpawn;

        numCarsToSpownNow = numCarsToSpawn;
    }

    public void generateTraffic(int numberCarsToSpawnOnFrame, float profondity, NativeMultiHashMap<float3, float3> nodesCity, NativeArray<float3> waypoitnsCity)
    {
        NativeList<float3> spawnNodeList = new NativeList<float3>(numCarsToSpawn, Allocator.Temp);
        NativeList<float3> destinationNodeList = new NativeList<float3>(numCarsToSpawn, Allocator.Temp);

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
                    carRotation = ((int)spawnNode.transform.position.z < (int)spawnNode.nextNodes[0].transform.position.z) ? 0 : 180;
                }
                else
                {
                    carRotation = ((int)spawnNode.transform.position.x < (int)spawnNode.nextNodes[0].transform.position.x) ? 90 : 270;
                }
            }
            else
            {
                if ((int)spawnNode.transform.parent.localRotation.eulerAngles.y == 0)
                {       //HORIZONTAL STREET
                    carRotation = (spawnNode.trafficDirection == 0) ? 90 : 270;
                }
                else
                {   //VERTICAL
                    carRotation = (spawnNode.trafficDirection == 0) ? 180 : 0;
                }
            }

            Node destinationNode = dNode[i];

            List<Vector3> keyAdd = new List<Vector3>();

            int carIndex = UnityEngine.Random.Range(0, carPrefab.Count);
            GameObject carToSpawn = carPrefab[carIndex];
            carToSpawn.name = "Car" + carIndex;
            CarComponents carData = carToSpawn.GetComponent<CarComponents>();

            carData.startingNode = spawnNode;
            carData.currentNode = 1;

            carData.Speed = 2f;
            carData.SpeedDamping = carData.Speed / 10f;

            carData.parkingNode = pNode[i];

            carData.destinationNode = destinationNode;

            carData.pathNodeList.Clear();

            Instantiate(carToSpawn, spawnNode.transform.position, Quaternion.Euler(0, carRotation, 0));

            spawnWaypoints.Remove(spawnNode);
            k++;
        }

        //Debug.Log("FINISH CAR SPAWNING!!!");

    }

}