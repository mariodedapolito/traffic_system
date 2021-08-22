using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleCarSpawner : MonoBehaviour
{

    public GameObject carPrefab;
    public CityGenerator city;
    public List<Node> spawnWaypoints;

    public SimpleCarSpawner(GameObject carPrefab, CityGenerator city)
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

        while (1 == 1)
        {
            int randomSrcRow = UnityEngine.Random.Range(0, cityLength);
            int randomSrcCol = UnityEngine.Random.Range(0, cityWidth);

            //Debug.Log(randomSrcRow + "," + randomSrcCol);

            if (cityMap[randomSrcRow, randomSrcCol].prefabType == 1 || cityMap[randomSrcRow, randomSrcCol].prefabType == 2)
            {
                int carRotation;
                
                List<Node> currentStreetNodes = spawnWaypoints;
                int randomSrcNode = (int)UnityEngine.Random.Range(0, currentStreetNodes.Count - 1);

                //Debug.Log(currentStreetNodes[randomSrcNode].transform.position);
                Node startingNode = currentStreetNodes[randomSrcNode].nextNodes[0];
                if (startingNode.isOccupied)
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
                startingNode.isOccupied = true;
                //Car Rotation
                if ((int)currentStreetNodes[randomSrcNode].transform.position.x == (int)startingNode.transform.position.x)
                {
                    if ((int)currentStreetNodes[randomSrcNode].transform.position.z < (int)startingNode.transform.position.z)
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
                    if ((int)currentStreetNodes[randomSrcNode].transform.position.x < (int)startingNode.transform.position.x)
                    {
                        carRotation = 90;
                    }
                    else
                    {
                        carRotation = 270;
                    }
                }
                //Find random destination
                while (1 == 1)
                {
                    int randomDstRow = UnityEngine.Random.Range(0, cityLength);
                    int randomDstCol = UnityEngine.Random.Range(0, cityWidth);
                    if (cityMap[randomDstRow, randomDstCol].prefabType != 0)
                    {
                        Street dstStreet = cityMap[randomDstRow, randomDstCol].instantiatedPrefab.GetComponent<Street>();
                        if (!dstStreet.isSemaphoreIntersection && !dstStreet.isSimpleIntersection)
                        {
                            List<Node> dstStreetNodes = dstStreet.carWaypoints;
                            int randomDstNode = UnityEngine.Random.Range(0, currentStreetNodes.Count);
                            Node dstNode = currentStreetNodes[randomDstNode].nextNodes[0];
                            if (dstNode.transform != startingNode.transform)
                            {
                                CarAI car = carPrefab.GetComponent<CarAI>();
                                car.startWaypoint = startingNode; //starting waypoint
                                car.endWaypoint = dstNode;        //end waypoint
                                Instantiate(carPrefab, currentStreetNodes[randomSrcNode].transform.position, Quaternion.Euler(0, carRotation, 0));
                                return true;
                            }
                        }
                    }
                }
            }

        }
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
            while (1 == 1)
            {
                randomSrcRow = UnityEngine.Random.Range(0, cityLength);
                randomSrcCol = UnityEngine.Random.Range(0, cityWidth);

                if (cityMap[randomSrcRow, randomSrcCol].prefabType == 1 || cityMap[randomSrcRow, randomSrcCol].prefabType == 2)
                {
                    Street currentStreet = cityMap[randomSrcRow, randomSrcCol].instantiatedPrefab.GetComponent<Street>();
                    
                    if (!currentStreet.isSemaphoreIntersection && !currentStreet.isSimpleIntersection && !currentStreet.isTBoneIntersection)
                    {
                        Node possibleWaypointSpawn = currentStreet.carWaypoints[UnityEngine.Random.Range(0, currentStreet.carWaypoints.Count - 1)];
                        if(!spawnWaypoints.Contains(possibleWaypointSpawn))
                        {
                            possibleWaypointSpawn.GetComponent<SphereCollider>().radius = 7.5f;
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
