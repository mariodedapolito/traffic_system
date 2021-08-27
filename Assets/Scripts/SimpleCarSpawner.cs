using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleCarSpawner : MonoBehaviour
{

    public List<GameObject> carPrefab;
    public CityGenerator city;
    public List<Node> spawnWaypoints;

    public SimpleCarSpawner(List<GameObject> carPrefab, CityGenerator city)
    {
        this.carPrefab = carPrefab;
        this.city = city;
    }

    // Start is called before the first frame update
    public bool spawnCar()
    {
        MapTile[,] cityMap = city.cityMap;
        int cityWidth = city.cityWidth;
        int cityLength = city.cityLength;

        //while (1 == 1)
        //{
            
        int randomSrcRow = UnityEngine.Random.Range(0, cityLength);
        int randomSrcCol = UnityEngine.Random.Range(0, cityWidth);

        List<Node> currentStreetNodes = spawnWaypoints;
        int randomSrcNode = (int)UnityEngine.Random.Range(0, currentStreetNodes.Count);
        
        Node spawnNode = currentStreetNodes[randomSrcNode];
        
		Node startingNode = currentStreetNodes[randomSrcNode].nextNodes[0];

        
        int carRotation;
                
           
        if (spawnNode.numberCars > 1 || spawnNode.isOccupied)
        {
                /*int i;
                for (i = 0; i < spawnWaypoints.Count; i++)
                {
                    Node findWaypointSpawn = currentStreetNodes[i].nextNodes[0];
                    if (!findWaypointSpawn.isOccupied)
                    {
                        startingNode = findWaypointSpawn;
                        break;
                    }                        
                }
                if(i == spawnWaypoints.Count)
                    break;*/
            return false;
        }
        spawnNode.isOccupied = true;
        spawnNode.numberCars = 1;
        //Car Rotation
		if(startingNode.transform.parent.gameObject.GetComponentInParent<Street>().numberLanes==1){
			Debug.Log("1 lane");
			if ((int)spawnNode.transform.position.x == (int)startingNode.transform.position.x)
					{
						if ((int)spawnNode.transform.position.z < (int)startingNode.transform.position.z)
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
						if ((int)spawnNode.transform.position.x < (int)startingNode.transform.position.x)
						{
							carRotation = 90;
						}
						else
						{
							carRotation = 270;
						}
					}
		}
		else{
			if((int)spawnNode.transform.parent.transform.parent.localRotation.eulerAngles.y == 0){		//HORIZONTAL STREET
				if(spawnNode.trafficDirection == 0){
					carRotation=90;
				}
				else{
					carRotation=270;
				}
			}
			else{	//VERTICAL
				if(spawnNode.trafficDirection == 0){
					carRotation=0;
				}
				else{
					carRotation=180;
				}
			}
		}
        //Find random destination
        //while (1 == 1)
        //{

        Node endRandomNode = startingNode;
		if(endRandomNode.nextNodes.Count <= 0){
			return false;
		}
        endRandomNode = endRandomNode.nextNodes[0];
        while (endRandomNode.GetComponentInParent<Street>().isSemaphoreIntersection || endRandomNode.GetComponentInParent<Street>().isSimpleIntersection)
        {
            endRandomNode = endRandomNode.nextNodes[0];
        }

        int randomDstRow = UnityEngine.Random.Range(0, cityLength);
        int randomDstCol = UnityEngine.Random.Range(0, cityWidth);

        //if (cityMap[randomDstRow, randomDstCol].prefabType > 0)
        //{
            Street dstStreet = endRandomNode.GetComponentInParent<Street>();
            if (!dstStreet.isSemaphoreIntersection && !dstStreet.isSimpleIntersection)
            {
                //int randomDstNode = UnityEngine.Random.Range(0, currentStreetNodes.Count);
                //Node dstNode = currentStreetNodes[randomDstNode].nextNodes[0];
                if (endRandomNode.transform != startingNode.transform)
                {
                    int carIndex = UnityEngine.Random.Range(0, carPrefab.Count);
                    CarAI car = carPrefab[carIndex].GetComponent<CarAI>();
                    car.startWaypoint = startingNode; //starting waypoint
                    car.endWaypoint = endRandomNode;        //end waypoint
                    Instantiate(carPrefab[carIndex], spawnNode.transform.position, Quaternion.Euler(0, carRotation, 0));
                    return true;
                }
            }
        //}
        //}
        return false;

        //}
    }

    public void SetWaypointsSpawnCar(int nWaypointsSpawn)
    {
        MapTile[,] cityMap = city.cityMap;
        int cityWidth = city.cityWidth;
        int cityLength = city.cityLength;

        int randomSrcRow;
        int randomSrcCol;
        spawnWaypoints = new List<Node>();

        for (int i = 0; i < nWaypointsSpawn; i++)
        {
            while (true)
            {
                randomSrcRow = UnityEngine.Random.Range(0, cityLength);
                randomSrcCol = UnityEngine.Random.Range(0, cityWidth);

                if (cityMap[randomSrcRow, randomSrcCol].prefabType > 0) //changes
                {
                    Street currentStreet = cityMap[randomSrcRow, randomSrcCol].instantiatedStreet.GetComponent<Street>();
                    
                    if (!currentStreet.isSemaphoreIntersection && !currentStreet.isSimpleIntersection && !currentStreet.isTBoneIntersection && !currentStreet.hasBusStop && !currentStreet.isCurve)
                    {
                        Node possibleWaypointSpawn = currentStreet.carWaypoints[UnityEngine.Random.Range(0, currentStreet.carWaypoints.Count)];
                        if(!spawnWaypoints.Contains(possibleWaypointSpawn) && !possibleWaypointSpawn.isLaneChange && !possibleWaypointSpawn.needOutgoingConnection)
                        {
                            possibleWaypointSpawn.GetComponent<SphereCollider>().enabled = false;
                            possibleWaypointSpawn.gameObject.AddComponent<BoxCollider>();
                            possibleWaypointSpawn.GetComponent<BoxCollider>().isTrigger = true;
                            possibleWaypointSpawn.GetComponent<BoxCollider>().center = new Vector3(0, 2.11f, 0);
                            possibleWaypointSpawn.GetComponent<BoxCollider>().size = new Vector3(3.64f, 4.23f, 7.04f);

                            //possibleWaypointSpawn.GetComponent<SphereCollider>().radius = 1f;
                            possibleWaypointSpawn.isCarSpawn = true;
                            spawnWaypoints.Add(possibleWaypointSpawn);
                            break;
                        }
                    }
                }
            }
        }
        //spawnWaypoints.Sort();
    }

}