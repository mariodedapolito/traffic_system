using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

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

        for (int i = 0; i < numCarsToSpawn; i++)
        {

            int randomSrcNode = UnityEngine.Random.Range(0, spawnWaypoints.Count);
            Node spawnNode = spawnWaypoints[randomSrcNode];
            //Node startingNode = spawnNode.nextNodes[0];

            int carRotation;
            if (spawnNode.gameObject.GetComponentInParent<Street>().numberLanes == 1)
            {
                if ((int)spawnNode.transform.position.x == (int)spawnNode.transform.position.x)
                {
                    if ((int)spawnNode.transform.position.z < (int)spawnNode.transform.position.z)
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
                    if ((int)spawnNode.transform.position.x < (int)spawnNode.transform.position.x)
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

            int randomDstNodeIndex = UnityEngine.Random.Range(0, parkingWaypoints.Count);
            Node destinationNode = parkingWaypoints[randomDstNodeIndex];

            int carIndex = UnityEngine.Random.Range(0, carPrefab.Count);
            GameObject carToSpawn = carPrefab[carIndex];
            CarComponents carData = carToSpawn.GetComponent<CarComponents>();

            carToSpawn.transform.position = spawnNode.transform.position;
            carToSpawn.transform.rotation = Quaternion.Euler(0, carRotation, 0);
            carData.startingNode = spawnNode;
           
            carData.Speed = 1.5f + Random.Range(-0.5f, 0.5f);  //driver angryness: top speed
            carData.SpeedDamping = carData.Speed / 10f;     //driver angryness: speeding/braking aggressivity

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
            possiblePaking.freeParkingSpots.RemoveAt(randomParkingSpot);
            
            carData.destinationNode = destinationNode;
             
            //Debug.Log("Car position: " + carToSpawn.transform.position + " rotation: " + carToSpawn.transform.rotation);

            //Instantiate a new car (which will then be converted to an entity)
            Instantiate(carToSpawn);
            
            spawnWaypoints.Remove(spawnNode);
        }

        Debug.Log("FINISH SPAWNING!!!");

    }

}