using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class BusSpawner : MonoBehaviour
{

    public GameObject busPrefab;
    public CityGenerator city;
    private List<List<Node>> busLines; 
    private int numCarsToSpawn;

    public BusSpawner(GameObject busPrefab, CityGenerator city)
    {
        this.busPrefab = busPrefab;
        this.city = city;
        this.busLines = city.busLines;
        //Debug.Log("Num bus lines: " + busLines.Count);
    }

    public void generateBuses()
    {
        GameObject busToSpawn = busPrefab;
        CarComponents busData = busToSpawn.GetComponent<CarComponents>();

        busData.currentNode = 1;
        busData.Speed = 3f;
        busData.SpeedDamping = busData.Speed / 10f;
        
        for (int i = 0; i < busLines.Count; i++)
        {
            List<Node> busLine = busLines[i];
            //Debug.Log("Bus line stops: "+busLine.Count);
            for (int j = 0; j < busLine.Count; j++)
            {
                busData.busStops.Clear();
                for(int k = 0; k < busLine.Count; k++)
                {
                    busData.busStops.Add(busLine[(k+j)%busLine.Count]);
                }

                Instantiate(busToSpawn, busLine[j].transform.position, Quaternion.Euler(0, busLine[j].GetComponentInParent<Street>().transform.rotation.eulerAngles.y - 90, 0));
            }

            
        }
        //Debug.Log("FINISH BUS SPAWNING!!!");
    }

}