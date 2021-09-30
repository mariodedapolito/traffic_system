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
        bool exitLoop = false;

        foreach(var w in spawnWaypoints) 
        {             
            int carRotation;
            int j = 0;

            Node startingNode = w.nextNodes[0];
            if (w.isOccupied)
            {
                continue;
            }
             w.isOccupied = true;

            //Bus Rotation
            if ((int)w.transform.position.x == (int)startingNode.transform.position.x) //Top-down
            {
                if ((int)w.transform.position.z < (int)startingNode.transform.position.z) 
                {
                    // Up
                    carRotation = 0;
                }
                else
                {
                    //down
                    carRotation = 180;
                }
            }
            else //right-left
            {
                if ((int)w.transform.position.x < (int)startingNode.transform.position.x)
                {
                    //right
                    carRotation = 90;
                }
                else
                {
                    //left
                    carRotation = 270;
                }
            }

            Node dstNode = w.nextNodes[0];
            //if (dstNode.transform != startingNode.transform)
            //{
            BusAI bus = busPrefab.GetComponent<BusAI>();
            bus.endWaypoint = new Node();

            if ((carRotation == 270 || carRotation == 180))
            {
                bus.direction = 1;
            }
            else
            {
                bus.direction = 0;
            }
            foreach (var b in busLines)
            {
                if (bus.direction == 1)
                {
                    for (int i = 0; i < b.Count; i++)
                    {
                        foreach (var s in b[i].carWaypoints)
                        {
                            if (s.Equals(w))
                            {
                                if (i + 1 < b.Count)
                                {
                                    j = i + 1;
                                }
                                else
                                {
                                    j = 0;
                                }

                                foreach (var f in b[j].carWaypoints)
                                {
                                    if (f.isBusStop) bus.endWaypoint = f;
                                }
                                bus.currentStreet = i;
                                bus.busLines = b;


                                exitLoop = true;
                                break;
                            }
                        }

                        if (exitLoop) break;
                    }
                }
                else
                {
                    for (int i = b.Count -1 ; i>=0; i--)
                    {
                        foreach (var s in b[i].carWaypoints)
                        {
                            if (s.Equals(w))
                            {
                                if (i==0)
                                {
                                    j = b.Count - 1;
                                }
                                else
                                {
                                    j = i - 1;
                                }

                                foreach (var f in b[j].carWaypoints)
                                {
                                    if (f.isBusStop) bus.endWaypoint = f;
                                }

                                bus.busLines = b;

                                exitLoop = true;
                                break;
                            }
                        }

                        if (exitLoop) break;
                    }
                }     
                if (exitLoop) break;
            }
           
            bus.startWaypoint = startingNode; //starting waypoint
            if (bus.endWaypoint != null)
            {
                Instantiate(busPrefab, w.transform.position, Quaternion.Euler(0, carRotation, 0));
            }
            //spawnWaypoints.Remove(w);
            return true;
        }
        return false;
    }

    public void SetWaypointsSpawnBus(List<List<Street>> busLines)
    {

        this.busLines = new List<List<Street>>();
        MapTile[,] cityMap = city.cityMap;
        int cityWidth = city.cityWidth;
        int cityLength = city.cityLength;
        spawnWaypoints = new List<Node>();
       // int k = 0;

        foreach(var v in busLines)
        {
            List<Street> s = new List<Street>();
            for(int i = v.Count- 1 ; i>=0; i--)
            {
                s.Add(v[i]);
            }

            if (s.Count != 0)
            {
                this.busLines.Add(s);
            }
        }


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
