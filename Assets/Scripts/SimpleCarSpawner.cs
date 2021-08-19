using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleCarSpawner : MonoBehaviour
{

    public GameObject carPrefab;
    public CityGenerator city;

    public SimpleCarSpawner(GameObject carPrefab, CityGenerator city)
    {
        this.carPrefab = carPrefab;
        this.city = city;
    }

    // Start is called before the first frame update
    public void spawnCar()
    {
        MapTile[,] cityMap = city.cityMap;
        int cityWidth = city.cityWidth;
        int cityLength = city.cityLength;

        while (1 == 1)
        {
            int randomSrcRow = UnityEngine.Random.Range(0, cityLength);
            int randomSrcCol = UnityEngine.Random.Range(0, cityWidth);

            Debug.Log(randomSrcRow + "," + randomSrcCol);

            if (cityMap[randomSrcRow, randomSrcCol].prefabType == 1)
            {
                int carRotation;
                Street currentStreet = cityMap[randomSrcRow, randomSrcCol].instantiatedStreet.GetComponent<Street>();
                List<Node> currentStreetNodes = currentStreet.carWaypoints;
                int randomSrcNode = (int)UnityEngine.Random.Range(0, currentStreetNodes.Count - 1);
                Debug.Log(currentStreetNodes[randomSrcNode].transform.position);
                Node startingNode = currentStreetNodes[randomSrcNode].nextNodes[0];
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
                        Street dstStreet = cityMap[randomDstRow, randomDstCol].instantiatedStreet.GetComponent<Street>();
                        if (!dstStreet.isSemaphoreIntersection && !dstStreet.isSimpleIntersection)
                        {
                            List<Node> dstStreetNodes = dstStreet.carWaypoints;
                            int randomDstNode = UnityEngine.Random.Range(0, currentStreetNodes.Count);
                            Node dstNode = currentStreetNodes[randomDstNode].nextNodes[0];
                            if (dstNode.transform != startingNode.transform)
                            {
                                CarAI car = carPrefab.GetComponent<CarAI>();
                                car.startWaypoint = startingNode;
                                car.endWaypoint = dstNode;
                                Instantiate(carPrefab, currentStreetNodes[randomSrcNode].transform.position, Quaternion.Euler(0, carRotation, 0));
                                break;
                            }
                        }
                    }
                }
                break;
            }

        }
    }

}
