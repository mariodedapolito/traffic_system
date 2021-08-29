using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parking : MonoBehaviour
{
    public int numberParkingSpots;
    public int numberFreeSpots;
    public Node parkingGateway;
    public List<Node> freeParkingSpots;     //nodes that are free parking spots (use to get location for parking & car rotation)

    private ParkingTrigger parkingTrigger;
    private int MAX_NUMBER_SECONDS_CAR_WAKEUP = 120;

    // Start is called before the first frame update
    void Start()
    {
        parkingTrigger = GetComponentInChildren<ParkingTrigger>();
    }

    public void manageParkingEntrance(CarAI car)
    {
        if (numberFreeSpots == 0)
        {
            car.needParkingSpot = false;
            return;
        }

        int parkingSpotIndex = generateParkingSpotIndex();
        Node parkingSpot = freeParkingSpots[parkingSpotIndex];
        
        car.transform.position = parkingSpot.transform.position;

        int streetPrefabRotation = (int)transform.parent.transform.rotation.y;

        car.transform.rotation = Quaternion.Euler(0, parkingSpot.parkingRotation + streetPrefabRotation, 0);

        car.carPath.Clear();
        car.nodes.Clear();
        car.GetComponentInParent<IntersectionVehicle>().intersectionStop = true;

        numberFreeSpots--;
        freeParkingSpots.RemoveAt(parkingSpotIndex);

        Debug.Log("CAR PARKED");

        Debug.Log("park: " + parkingSpot.transform.position);

        StartCoroutine(manageParkingExit(parkingSpot, car));

    }

    IEnumerator manageParkingExit(Node parkingPosition, CarAI parkedCar)
    {
        int wakeupTimer = UnityEngine.Random.Range(MAX_NUMBER_SECONDS_CAR_WAKEUP / 2, MAX_NUMBER_SECONDS_CAR_WAKEUP + 1);

        yield return new WaitForSeconds(wakeupTimer);

        while (parkingTrigger.numberCarsInside > 0)
        {
            int randomWaitTime = UnityEngine.Random.Range(2, 10);
            yield return new WaitForSeconds(randomWaitTime);
        }

        parkedCar.GetComponentInParent<IntersectionVehicle>().intersectionStop = false;
        parkedCar.transform.position = parkingGateway.transform.position;
        int streetPrefabRotation = (int)transform.parent.transform.rotation.y;
        parkedCar.transform.rotation = Quaternion.Euler(0, parkingGateway.parkingExitRotation + streetPrefabRotation, 0);
        parkedCar.needParkingSpot = false;

        Debug.Log("CAR WAKEUP");

        numberFreeSpots++;
        freeParkingSpots.Add(parkingPosition);

    }

    private int generateParkingSpotIndex()
    {
        int randomParkingSpot = UnityEngine.Random.Range(0, freeParkingSpots.Count);
        return randomParkingSpot;
    }

}
