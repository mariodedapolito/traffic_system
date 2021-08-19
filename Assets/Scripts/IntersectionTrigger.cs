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
    private List<CarAI> collisionRegistered;

    private void OnTriggerEnter(Collider other)
    {
        CarAI car = other.GetComponent<CarAI>();
        if (other.gameObject.layer==9 && !collisionRegistered.Contains(car))
        {
            collisionRegistered.Add(car);
            if (!car.isInsideIntersection)
            {
                identifyCarDirection(car);
                car.intersectionEnterId = id;
                intersection.intersectionManager(car, id);
            }
            else
            {
                intersection.intersectionManager(car, car.intersectionEnterId);
            }
            
        }
    }

    private void OnTriggerExit(Collider other)
    {
        CarAI car = other.GetComponent<CarAI>();
        collisionRegistered.Remove(car);
    }

    private void identifyCarDirection(CarAI car)
    {
        List<Node> intersectionCarPathNodes = new List<Node>();
        List<Node> carPathNodes = car.carPath;
        foreach(var pathNode in carPathNodes)
        {
            if (intersectionNodes.IndexOf(pathNode)!=-1)
            {
                intersectionCarPathNodes.Add(pathNode);
            }
        }

        foreach(var node in intersectionCarPathNodes)
        {
            if (node.isTurnLeft)
            {
                car.intersectionDirection=LEFT;
                return;
            }
            else if (node.isTurnRight)
            {
                car.intersectionDirection=RIGHT;
                return;
            }
        }
        car.intersectionDirection=STRAIGHT;
    }

    // Start is called before the first frame update
    void Start()
    {
        intersection = this.transform.parent.transform.parent.GetComponent<Street>();
        intersectionNodes = intersection.carWaypoints;
        collisionRegistered = new List<CarAI>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
