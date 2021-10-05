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

    private int carId = 0;

    private List<Entity> carEntities;
    private EntityManager entityManager;
    private BlobAssetStore blobAssetStore;

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
    }

    public void generateTraffic()
    {

        GameObject[] nodes = GameObject.FindGameObjectsWithTag("CarWaypoint");
        List<Node> nodesList = new List<Node>();

        nodes[0].GetComponent<Node>();

        //NewPathSystem pathSystem = new NewPathSystem();

        NativeMultiHashMap<float3, float3> nodesCity = new NativeMultiHashMap<float3, float3>(nodes.Length, Allocator.Temp);
        NativeArray<float3> waypoitnsCity = new NativeArray<float3>(nodes.Length, Allocator.Temp);

        for (int i = 0; i < nodes.Length; i++)
        {
            if (!nodes[i].GetComponent<Node>().isParkingSpot)
            {
                
                for (int j = 0; j < nodes[i].GetComponent<Node>().nextNodes.Count; j++)
                {
                    nodesCity.Add(nodes[i].GetComponent<Node>().transform.position, nodes[i].GetComponent<Node>().nextNodes[j].transform.position);
                }


                waypoitnsCity[i] = nodes[i].GetComponent<Node>().transform.position;
            }

            //nextNodes.Dispose();
        }

        NativeList<float3> spawnNodeList = new NativeList<float3>(numCarsToSpawn, Allocator.Temp);
        NativeList<float3> destinationNodeList = new NativeList<float3>(numCarsToSpawn, Allocator.Temp);
        List<Node> sNode = new List<Node>();
        List<Node> dNode = new List<Node>();
        List<Node> pNode = new List<Node>();

        for (int i = 0; i < numCarsToSpawn; i++)
        {
            int randomSrcNode = UnityEngine.Random.Range(0, spawnWaypoints.Count);
            int randomDstNodeIndex = UnityEngine.Random.Range(0, parkingWaypoints.Count);
            spawnNodeList.Add(spawnWaypoints[randomSrcNode].transform.position);
            sNode.Add(spawnWaypoints[randomSrcNode]);

            Parking possiblePaking = parkingWaypoints[randomDstNodeIndex].parkingPrefab.GetComponent<Parking>();
            /*
            while (possiblePaking.numberFreeSpots == 0)
            {
                randomDstNodeIndex = UnityEngine.Random.Range(0, parkingWaypoints.Count);
                destinationNode = parkingWaypoints[randomDstNodeIndex];
                possiblePaking = destinationNode.parkingPrefab.GetComponent<Parking>();
            }
            */

            int randomParkingSpot = UnityEngine.Random.Range(0, possiblePaking.freeParkingSpots.Count);

            possiblePaking.numberFreeSpots--;

            if(possiblePaking.numberFreeSpots == 0)
            {
                possiblePaking.freeParkingSpots.RemoveAt(randomParkingSpot);
            }

            pNode.Add(possiblePaking.freeParkingSpots[randomParkingSpot]);
            possiblePaking.freeParkingSpots[randomParkingSpot].isOccupied = true;
           
            destinationNodeList.Add(parkingWaypoints[randomDstNodeIndex].transform.position);
            dNode.Add(spawnWaypoints[randomSrcNode]);
        }

        NewPathSystemMono newPathSystemMono = new NewPathSystemMono();
        Dictionary<int, NativeList<float3>> sampleJobArray = new Dictionary<int, NativeList<float3>>();
        sampleJobArray = newPathSystemMono.PathSystemJob(numCarsToSpawn, spawnNodeList, destinationNodeList, waypoitnsCity, nodesCity);





        for (int i = 0; i < numCarsToSpawn; i++)
        {
            //int randomSrcNode = UnityEngine.Random.Range(0, spawnWaypoints.Count);
            Node spawnNode = sNode[i];
            //Node startingNode = spawnNode.nextNodes[0];

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

            //int randomDstNodeIndex = UnityEngine.Random.Range(0, parkingWaypoints.Count);
            Node destinationNode = dNode[i];

            int carIndex = UnityEngine.Random.Range(0, carPrefab.Count);
            GameObject carToSpawn = carPrefab[carIndex];
            CarComponents carData = carToSpawn.GetComponent<CarComponents>();

            carData.startingNode = spawnNode;
            carData.currentNode = 1;
           
            carData.Speed = 2f;  
            carData.SpeedDamping = carData.Speed / 10f; 
            
            /*
            Parking possiblePaking = destinationNode.parkingPrefab.GetComponent<Parking>();
            
            while(possiblePaking.numberFreeSpots == 0)
            {
                randomDstNodeIndex = UnityEngine.Random.Range(0, parkingWaypoints.Count);
                destinationNode = parkingWaypoints[randomDstNodeIndex];
                possiblePaking = destinationNode.parkingPrefab.GetComponent<Parking>();
            }

            int randomParkingSpot = UnityEngine.Random.Range(0, possiblePaking.freeParkingSpots.Count);

            possiblePaking.numberFreeSpots--;
            carData.parkingNode = possiblePaking.freeParkingSpots[randomParkingSpot];
            possiblePaking.freeParkingSpots[randomParkingSpot].isOccupied = true;
            possiblePaking.freeParkingSpots.RemoveAt(randomParkingSpot);*/

            carData.parkingNode = pNode[i];

            carData.destinationNode = destinationNode;

            /*if(sampleJobArray.TryGetValue(i, out carData.pathNodeList))
            {
                Debug.Log("C'è stato un problema con il path!");
            }*/
            carData.pathNodeList.Clear();
            for (int j = 0; j < sampleJobArray[i].Length; j++)
            {
                carData.pathNodeList.Add(sampleJobArray[i][j]);
            }

            //Debug.Log("Car position: " + carToSpawn.transform.position + " rotation: " + carToSpawn.transform.rotation);



            //Instantiate a new car (which will then be converted to an entity)
            Instantiate(carToSpawn, spawnNode.transform.position, Quaternion.Euler(0, carRotation, 0));
            
            spawnWaypoints.Remove(spawnNode);
        }

        //pathNative.Dispose();
        for(int i = 0; i< sampleJobArray.Count; i++ )
            sampleJobArray[i].Dispose();

        waypoitnsCity.Dispose();
        nodesCity.Dispose();

        Debug.Log("FINISH CAR SPAWNING!!!");

    }

}