using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class BusSpawner : MonoBehaviour
{

    public GameObject busPrefab;
    public CityGenerator city;
    private int numBusesToSpawn;
    private List<Node> busStopsSpawn;
    private List<Node> busStopsDst;

    public BusSpawner(GameObject busPrefab, CityGenerator city)
    {
        this.busPrefab = busPrefab;
        this.city = city;
        this.numBusesToSpawn = city.numberBusesToSpawn;
        this.busStopsSpawn = city.cityBusStopsSpawn;
        this.busStopsDst = city.cityBusStopsDst;
        if(numBusesToSpawn > busStopsSpawn.Count)
        {
            numBusesToSpawn = busStopsSpawn.Count;
        }
    }

    public void generateBuses()
    {
        GameObject busToSpawn = busPrefab;
        CarComponents busData = busToSpawn.GetComponent<CarComponents>();

        busData.currentNode = 1;
        busData.Speed = 2f;
        busData.SpeedDamping = busData.Speed / 10f;

        int numBusStopsSpawn = busStopsSpawn.Count;
        int numBusStopsDst = busStopsDst.Count;
        Path path = new Path();

        for (int i = 0; i < numBusesToSpawn; i++)
        {
            int randSpawnNodeIndex = Random.Range(0, numBusStopsSpawn);
            Node busStop_1 = busStopsSpawn[randSpawnNodeIndex];
            Node busStop_2 = busStop_1;
            while (busStop_1.Equals(busStop_2)){
                busStop_2 = busStopsDst[Random.Range(0, numBusStopsDst)];
            }

            List<Node> path_1 = path.findShortestPath(busStop_1.transform, busStop_2.transform);
            List<Node> path_2 = path.findShortestPath(busStop_2.transform, busStop_1.transform);

            path_1.RemoveAt(path_1.Count - 1);
            path_2.RemoveAt(0);
            path_2.RemoveAt(path_2.Count - 1);

            for(int j = 0; j < path_1.Count - 2; j++)  //-2 so not to include node before last bus stop
            {
                if (path_1[j].isBusBranch)
                {
                    path_1.RemoveAt(j + 1);
                    path_1.Insert(j + 1, path_1[j].nextNodes[1]);
                    path_1.Insert(j + 2, path_1[j+1].nextNodes[0]);
                    path_1.Insert(j + 3, path_1[j+2].nextNodes[0]);
                    path_1.Insert(j + 4, path_1[j+3].nextNodes[0]);
                }
            }

            for (int j = 0; j < path_2.Count - 2; j++)  //-2 so not to include node before last bus stop
            {
                if (path_2[j].isBusBranch)
                {
                    path_2.RemoveAt(j + 1);
                    path_2.Insert(j + 1, path_2[j].nextNodes[1]);
                    path_2.Insert(j + 2, path_2[j + 1].nextNodes[0]);
                    path_2.Insert(j + 3, path_2[j + 2].nextNodes[0]);
                    path_2.Insert(j + 4, path_2[j + 3].nextNodes[0]);
                }
            }

            List<Node> busPath = path_1;
            busPath.AddRange(path_2);

            busData.busPath.Clear();
            busData.busPath = busPath;

            Instantiate(busToSpawn, busPath[0].transform.position, Quaternion.Euler(0, busPath[0].GetComponentInParent<Street>().transform.rotation.eulerAngles.y - 90, 0));

            busStopsSpawn.RemoveAt(randSpawnNodeIndex);
            numBusStopsSpawn--;
        }
    }

}