using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleBusSpawner : MonoBehaviour
{

    public GameObject busPrefab;
    public CityGenerator city;
    public List<Node> spawnWaypoints;
    List<List<Street>> busLines;

    public SimpleBusSpawner(GameObject busPrefab, CityGenerator city)
    {
        this.busPrefab = busPrefab;
        this.city = city;
    }

    // Start is called before the first frame update
    public bool spawnBus()
    {
        MapTile[,] cityMap = city.cityMap;
        int cityWidth = city.cityWidth;
        int cityLength = city.cityLength;

        foreach(var w in spawnWaypoints) 
        {             
            int carRotation;
                
            Node startingNode = w.nextNodes[0];
            if (w.isOccupied)
            {
                continue;
            }
             w.isOccupied = true;

            //Bus Rotation
            if ((int)w.transform.position.x == (int)startingNode.transform.position.x)
            {
                if ((int)w.transform.position.z < (int)startingNode.transform.position.z)
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
                if ((int)w.transform.position.x < (int)startingNode.transform.position.x)
                {
                    carRotation = 90;
                }
                else
                {
                    carRotation = 270;
                }
            }

            Node dstNode = w.nextNodes[0];
            //if (dstNode.transform != startingNode.transform)
            //{
                BusAI bus = busPrefab.GetComponent<BusAI>();
                bus.startWaypoint = startingNode; //starting waypoint
                bus.endWaypoint = dstNode;        //end waypoint
                Instantiate(busPrefab, w.transform.position, Quaternion.Euler(0, carRotation, 0));
                //spawnWaypoints.Remove(w);
                return true;
            //}

        }
        return false;
    }

    public void SetWaypointsSpawnBus(int nWaypointsSpawn, List<List<Street>> busLines)
    {

        this.busLines = busLines;
        MapTile[,] cityMap = city.cityMap;
        int cityWidth = city.cityWidth;
        int cityLength = city.cityLength;
        spawnWaypoints = new List<Node>();

        for(int i=0; i< cityLength; i++)
        {
            for(int j=0; j< cityWidth; j++)
            {
                if (cityMap[i, j].prefabType == 5 || cityMap[i, j].prefabType == 6)
                {
                    Street currentStreet = cityMap[i, j].instantiatedStreet.GetComponent<Street>();
                    if (currentStreet.hasBusStop)
                    {
                        foreach (var w in currentStreet.carWaypoints)
                        {
                            if (w.isBusSpawn && !spawnWaypoints.Contains(w))
                            {
                                spawnWaypoints.Add(w);
                                break;
                            }
                        }
                    }
                }
            }
        }

        /*
        foreach (var b in busLines2)
        {
            for (int i = 0; i < b.Count; i++)
            {
                foreach(var w in b[i].carWaypoints)
                {
                    if (w.isBusSpawn)
                    {
                        spawnWaypoints.Add(w);
                    }
                }
            }
        }*/
        
    }

}
