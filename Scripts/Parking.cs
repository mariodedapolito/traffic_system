using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parking : MonoBehaviour
{
    public int numberParkingSpots;
    public int numberFreeSpots;
    public Node parkingGateway;
    public List<Node> freeParkingSpots;     //nodes that are free parking spots (use to get location for parking & car rotation)

    // Start is called before the first frame update
    void Start()
    {
        //parkingTrigger = GetComponentInChildren<ParkingTrigger>();
    }

}
