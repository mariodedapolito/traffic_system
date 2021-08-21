using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parking : MonoBehaviour
{
    public int numberParkingSpots;
    public int numberFreeSpots;
    public int numberBusySpots;
    public Node[] parkingSpots; //nodes that are parking spots (use to get location for parking & car rotation)
    public CarAI[] parkedCars;  //parked cars (access by the index that the node is found in the parkingSpots)

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
