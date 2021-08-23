using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntersectionTrigger : MonoBehaviour
{
    public int id;

    private const int LEFT = 0;
    private const int STRAIGHT = 1;
    private const int RIGHT = 2;

    private Street intersection;
    private List<Node> intersectionNodes;
    private List<IntersectionVehicle> collisionRegistered;

    private void OnTriggerEnter(Collider other)
    {
        IntersectionVehicle collider = other.GetComponent<IntersectionVehicle>();
        if (other.gameObject.layer == 9 && !collisionRegistered.Contains(collider))
        {
            collisionRegistered.Add(collider);
            IntersectionVehicle vehicle = collider.GetComponent<IntersectionVehicle>();
            if (!vehicle.isInsideIntersection)
            {
                vehicle.intersectionEnterId = id;
                if (vehicle.GetComponentInParent<CarAI>() != null)
                {
                    identifyCarDirection(vehicle.GetComponentInParent<CarAI>());
                }
                else
                {
                    identifyBusDirection(vehicle.GetComponentInParent<BusAI>());
                }
                intersection.intersectionManager(vehicle, id);
            }
            else
            {
                intersection.intersectionManager(vehicle, vehicle.intersectionEnterId);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        IntersectionVehicle vehicle = other.GetComponent<IntersectionVehicle>();
        collisionRegistered.Remove(vehicle);
    }

    private void identifyCarDirection(CarAI car)
    {
        List<Node> intersectionCarPathNodes = new List<Node>();
        List<Node> carPathNodes = car.carPath;
        foreach (var pathNode in carPathNodes)
        {
            if (intersectionNodes.IndexOf(pathNode) != -1)
            {
                intersectionCarPathNodes.Add(pathNode);
            }
        }

        if(intersectionCarPathNodes.Count == 0)
        {
            throw new System.Exception("No path nodes");
        }

        foreach (var node in intersectionCarPathNodes)
        {
            if (node.isTurnLeft)
            {
                car.intersectionData.intersectionDirection = LEFT;
                return;
            }
            else if (node.isTurnRight)
            {
                car.intersectionData.intersectionDirection = RIGHT;
                return;
            }
        }
        car.intersectionData.intersectionDirection = STRAIGHT;
    }

    private void identifyBusDirection(BusAI bus)
    {
        List<Node> intersectionBusPathNodes = new List<Node>();
        List<Node> busPathNodes = bus.carPath;
        foreach (var pathNode in busPathNodes)
        {
            if (intersectionNodes.IndexOf(pathNode) != -1)
            {
                intersectionBusPathNodes.Add(pathNode);
            }
        }

        foreach (var node in intersectionBusPathNodes)
        {
            if (node.isTurnLeft)
            {
                bus.intersectionData.intersectionDirection = LEFT;
                return;
            }
            else if (node.isTurnRight)
            {
                bus.intersectionData.intersectionDirection = RIGHT;
                return;
            }
        }
        bus.intersectionData.intersectionDirection = STRAIGHT;
    }

    // Start is called before the first frame update
    void Start()
    {
        intersection = this.transform.parent.transform.parent.GetComponent<Street>();
        intersectionNodes = intersection.carWaypoints;
        collisionRegistered = new List<IntersectionVehicle>();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
