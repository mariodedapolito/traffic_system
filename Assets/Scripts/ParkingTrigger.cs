using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParkingTrigger : MonoBehaviour
{

    private Parking parking;
    private List<IntersectionVehicle> collisionRegistered = new List<IntersectionVehicle>();
    public int numberCarsInside = 0;

    private void Start()
    {
        //parking = GetComponentInParent<Parking>();
    }

    private void OnTriggerEnter(Collider other)
    {
        IntersectionVehicle vehicle = other.GetComponent<IntersectionVehicle>();
        if (vehicle!=null && other.gameObject.layer == 9 && !collisionRegistered.Contains(vehicle))
        {
            collisionRegistered.Add(vehicle);
            numberCarsInside++;
            CarAI car = vehicle.GetComponentInParent<CarAI>();
            if (car != null && car.needParkingSpot)
            {
                parking.manageParkingEntrance(car);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        IntersectionVehicle vehicle = other.GetComponent<IntersectionVehicle>();
        if (vehicle!=null && other.gameObject.layer == 9 && collisionRegistered.Contains(vehicle))
        {
            collisionRegistered.Remove(vehicle);
            numberCarsInside--;
        }
    }
}
