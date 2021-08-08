using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Street : MonoBehaviour
{

    public List<Node> carWaypoints;
    public bool isSimpleIntersection;
    public bool isSemaphoreIntersection;
    public bool hasBusStop;
    public bool isLaneAdapter;
    public bool isCurve;
    public int numberLanes;

    private CarAI[] intersectionQueue1Lane;
    private CarAI[,] intersectionQueue2Lane;
    private int[,] intersectionBusyMarker;
    private int numberCarsInsideIntersection;
    private int numberCarsWaiting;

    private const int LEFT = 0;
    private const int STRAIGHT = 1;
    private const int RIGHT = 2;



    // Start is called before the first frame update
    void Start()
    {
        //CONNECT NEIGHBORING STREET PREFABS (IN ORDER TO GENERATE A GRAPH FOR THE WHOLE CITY)
        foreach (var node in carWaypoints)
        {
            if (node.needOutgoingConnection)
            {
                Collider[] nearbyWaypoints = Physics.OverlapSphere(node.transform.position, 5f, 1 << 8);
                //Debug.Log("# of nearby waypoints:" + nearbyWaypoints.Length);
                Node targetWaypoint = null;
                float shortestDistance = 999999999;
                foreach (var nearbyWaypoint in nearbyWaypoints)
                {
                    if (nearbyWaypoint.transform.parent.position != this.transform.position &&
                        //node.trafficDirection == nearbyWaypoint.GetComponent<Node>().trafficDirection &&
                        node.laneNumber == nearbyWaypoint.GetComponent<Node>().laneNumber &&
                        nearbyWaypoint.GetComponent<Node>().needIncomingConnection)
                    {
                        float distance = Vector3.Distance(node.transform.position, nearbyWaypoint.transform.position);
                        if (distance < shortestDistance)
                        {
                            targetWaypoint = nearbyWaypoint.GetComponent<Node>();
                        }
                    }
                }
                if (targetWaypoint != null)
                {
                    node.nextNodes.Add(targetWaypoint);
                    //Debug.Log("Added connection");
                }
            }
        }

        //INITIALIZE SIMPLE INTERSECTION DATA STRUCTURES (FOR INTERSECTION PRECEDENCE)
        if ((isSimpleIntersection || isSemaphoreIntersection))
        {
            numberCarsInsideIntersection = 0;
            numberCarsWaiting = 0;
            if (numberLanes == 1)
            {
                intersectionQueue1Lane = new CarAI[4] { null, null, null, null };
                intersectionBusyMarker = new int[4, 3] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } };
                Debug.Log("Initialized intersection data structures for: 1 lane intersection");
            }
            else if (numberLanes == 2)
            {
                intersectionQueue2Lane = new CarAI[4, 2] { { null, null }, { null, null }, { null, null }, { null, null } };
                intersectionBusyMarker = new int[4, 3] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } };
                Debug.Log("Initialized intersection data structures for: 2 lane intersection");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (numberCarsWaiting > 0)
        {
            if (isSimpleIntersection)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (numberLanes == 1)
                    {
                        if (intersectionQueue1Lane[i] != null)
                        {
                            CarAI car = intersectionQueue1Lane[i];
                            if (intersectionBusyMarker[i, car.intersectionDirection] == 0)
                            {
                                intersectionPriorityCleaner(car, car.intersectionEnterId);
                                intersectionManager(car, i);
                                intersectionQueue1Lane[i] = null;
                                numberCarsWaiting--;
                            }
                        }
                    }
                    else if (numberLanes == 2)
                    {
                        for (int j = 0; j < 2; j++)
                        {
                            if (intersectionQueue2Lane[i,j] != null)
                            {
                                CarAI car = intersectionQueue2Lane[i,j];
                                if (intersectionBusyMarker[i, car.intersectionDirection] == 0)
                                {
                                    intersectionPriorityCleaner(car, car.intersectionEnterId);
                                    intersectionManager(car, i);
                                    intersectionQueue2Lane[i,j] = null;
                                    numberCarsWaiting--;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private void intersectionPriorityCleaner(CarAI car, int intersectionEnterId)
    {
        if (isSimpleIntersection)       //Identify intersection type to handle the priority correctly
        {
            if (numberLanes == 1)
            {
                if (car.intersectionDirection == RIGHT)
                {
                    intersectionBusyMarker[calculateRoad(car.intersectionEnterId - 1) % 4, STRAIGHT]--;
                }
                else if (car.intersectionDirection == STRAIGHT)
                {
                    //Block completely traffic from the LEFT (everyone on my left has to give me precedence)
                    intersectionBusyMarker[calculateRoad(car.intersectionEnterId - 1) % 4, LEFT]--;
                    intersectionBusyMarker[calculateRoad(car.intersectionEnterId - 1) % 4, STRAIGHT]--;
                    intersectionBusyMarker[calculateRoad(car.intersectionEnterId - 1) % 4, RIGHT]--;

                    //Block traffic from STRAIGHT that will turn left (everyone in front of me that will turn left has to give me precedence as I will be on their right during the turn)
                    intersectionBusyMarker[calculateRoad(car.intersectionEnterId + 2) % 4, LEFT]--;
                }
                else if (car.intersectionDirection == LEFT)
                {
                    //Block traffic from the LEFT (everyone on my left that will turn left or go straight has to give me precedence)
                    intersectionBusyMarker[calculateRoad(car.intersectionEnterId - 1) % 4, LEFT]--;
                    intersectionBusyMarker[calculateRoad(car.intersectionEnterId - 1) % 4, STRAIGHT]--;
                }
            }
            else if (numberLanes == 2)
            {
                if (car.intersectionDirection == RIGHT)
                {
                    intersectionBusyMarker[calculateRoad(car.intersectionEnterId - 1) % 4, STRAIGHT]--;
                }
                else if (car.intersectionDirection == STRAIGHT)
                {
                    //Block completely traffic from the LEFT (everyone on my left has to give me precedence)
                    intersectionBusyMarker[calculateRoad(car.intersectionEnterId - 1) % 4, LEFT]--;
                    intersectionBusyMarker[calculateRoad(car.intersectionEnterId - 1) % 4, STRAIGHT]--;
                    intersectionBusyMarker[calculateRoad(car.intersectionEnterId - 1) % 4, RIGHT]--;

                    //Block traffic from STRAIGHT that will turn left (everyone in front of me that will turn left has to give me precedence as I will be on their right during the turn)
                    intersectionBusyMarker[calculateRoad(car.intersectionEnterId + 2) % 4, LEFT]--;
                }
                else if (car.intersectionDirection == LEFT)
                {
                    //Block traffic from the LEFT (everyone on my left that will turn left or go straight has to give me precedence)
                    intersectionBusyMarker[calculateRoad(car.intersectionEnterId - 1) % 4, LEFT]--;
                    intersectionBusyMarker[calculateRoad(car.intersectionEnterId - 1) % 4, STRAIGHT]--;
                }
            }
        }
        else if (isSemaphoreIntersection)
        {
            if (numberLanes == 1)
            {

            }
            else if (numberLanes == 2)
            {

            }
        }
    }

    public void intersectionManager(CarAI car, int intersectionRoadId)
    {
        if (!car.isInsideIntersection)
        {
            if (isSimpleIntersection)       //Identify intersection type to handle the priority correctly
            {
                if (numberLanes == 1)
                {
                    simpleIntersectionPriority1Lane(car, intersectionRoadId);
                }
                else if (numberLanes == 2)
                {
                    simpleIntersectionPriority2Lane(car, intersectionRoadId);
                }
            }
            else if (isSemaphoreIntersection)
            {
                if (numberLanes == 1)
                {
                    semaphoreIntersectionPriority1Lane(car, intersectionRoadId);
                }
                else if (numberLanes == 2)
                {
                    semaphoreIntersectionPriority2Lane(car, intersectionRoadId);
                }
            }
        }
        else
        {
            if (isSimpleIntersection)       //Identify intersection type to handle the priority correctly
            {
                if (numberLanes == 1)
                {
                    restoreSimpleIntersectionPriority1Lane(car, intersectionRoadId);
                }
                else if (numberLanes == 2)
                {
                    restoreSimpleIntersectionPriority2Lane(car, intersectionRoadId);
                }
            }
            else if (isSemaphoreIntersection)
            {
                if (numberLanes == 1)
                {
                    restoreSemaphoreIntersectionPriority1Lane(car, intersectionRoadId);
                }
                else if (numberLanes == 2)
                {
                    restoreSemaphoreIntersectionPriority2Lane(car, intersectionRoadId);
                }
            }
        }
    }

    private void simpleIntersectionPriority1Lane(CarAI car, int intersectionRoadId)
    {
        if (intersectionBusyMarker[intersectionRoadId, car.intersectionDirection] == 0)
        {
            numberCarsInsideIntersection++;
            car.isInsideIntersection = true;
            car.intersectionStop = false;
            if (car.intersectionDirection == RIGHT)
            {
                intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, STRAIGHT]++;
            }
            else if (car.intersectionDirection == STRAIGHT)
            {
                //Block completely traffic from the RIGHT
                intersectionBusyMarker[calculateRoad(intersectionRoadId + 1) % 4, LEFT]++;
                intersectionBusyMarker[calculateRoad(intersectionRoadId + 1) % 4, STRAIGHT]++;
                intersectionBusyMarker[calculateRoad(intersectionRoadId + 1) % 4, RIGHT]++;

                //Block traffic from the LEFT that will turn left or go straight
                intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, LEFT]++;
                intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, STRAIGHT]++;

                //Block traffic from STRAIGHT that will turn left
                intersectionBusyMarker[calculateRoad(intersectionRoadId + 2) % 4, LEFT]++;
            }
            else if (car.intersectionDirection == LEFT)
            {
                //Block traffic from the RIGHT that will turn left or go straight
                intersectionBusyMarker[calculateRoad(intersectionRoadId + 1) % 4, LEFT]++;
                intersectionBusyMarker[calculateRoad(intersectionRoadId + 1) % 4, STRAIGHT]++;

                //Block traffic from the LEFT that will turn left or go straight
                intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, LEFT]++;
                intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, STRAIGHT]++;

                //Block completely traffic from STRAIGHT
                intersectionBusyMarker[calculateRoad(intersectionRoadId + 2) % 4, LEFT]++;
                intersectionBusyMarker[calculateRoad(intersectionRoadId + 2) % 4, STRAIGHT]++;
                intersectionBusyMarker[calculateRoad(intersectionRoadId + 2) % 4, RIGHT]++;
            }

        }
        else
        {
            car.intersectionStop = true;
            car.isInsideIntersection = false;
            intersectionQueue1Lane[intersectionRoadId] = car;
            if (car.intersectionDirection == RIGHT)
            {
                intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, STRAIGHT]++;
            }
            else if (car.intersectionDirection == STRAIGHT)
            {
                //Block completely traffic from the LEFT (everyone on my left has to give me precedence)
                intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, LEFT]++;
                intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, STRAIGHT]++;
                intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, RIGHT]++;

                //Block traffic from STRAIGHT that will turn left (everyone in front of me that will turn left has to give me precedence as I will be on their right during the turn)
                intersectionBusyMarker[calculateRoad(intersectionRoadId + 2) % 4, LEFT]++;
            }
            else if (car.intersectionDirection == LEFT)
            {
                //Block traffic from the LEFT (everyone on my left that will turn left or go straight has to give me precedence)
                intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, LEFT]++;
                intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, STRAIGHT]++;
            }
            numberCarsWaiting++;    //Put here so not to confuse 
        }
    }

    private void simpleIntersectionPriority2Lane(CarAI car, int intersectionRoadId)
    {
        if (intersectionBusyMarker[intersectionRoadId, car.intersectionDirection] == 0)
        {
            numberCarsInsideIntersection++;
            car.isInsideIntersection = true;
            car.intersectionStop = false;
            if (car.intersectionDirection == RIGHT)
            {
                intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, STRAIGHT]++;
            }
            else if (car.intersectionDirection == STRAIGHT)
            {
                //Block completely traffic from the RIGHT
                intersectionBusyMarker[calculateRoad(intersectionRoadId + 1) % 4, LEFT]++;
                intersectionBusyMarker[calculateRoad(intersectionRoadId + 1) % 4, STRAIGHT]++;
                intersectionBusyMarker[calculateRoad(intersectionRoadId + 1) % 4, RIGHT]++;

                //Block traffic from the LEFT that will turn left or go straight
                intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, LEFT]++;
                intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, STRAIGHT]++;

                //Block traffic from STRAIGHT that will turn left
                intersectionBusyMarker[calculateRoad(intersectionRoadId + 2) % 4, LEFT]++;
            }
            else if (car.intersectionDirection == LEFT)
            {
                //Block traffic from the RIGHT that will turn left or go straight
                intersectionBusyMarker[calculateRoad(intersectionRoadId + 1) % 4, LEFT]++;
                intersectionBusyMarker[calculateRoad(intersectionRoadId + 1) % 4, STRAIGHT]++;

                //Block traffic from the LEFT that will turn left or go straight
                intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, LEFT]++;
                intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, STRAIGHT]++;

                //Block completely traffic from STRAIGHT
                intersectionBusyMarker[calculateRoad(intersectionRoadId + 2) % 4, LEFT]++;
                intersectionBusyMarker[calculateRoad(intersectionRoadId + 2) % 4, STRAIGHT]++;
                intersectionBusyMarker[calculateRoad(intersectionRoadId + 2) % 4, RIGHT]++;
            }

        }
        else
        {
            car.intersectionStop = true;
            car.isInsideIntersection = false;
            if (intersectionQueue2Lane[intersectionRoadId, 0] == null)
            {
                intersectionQueue2Lane[intersectionRoadId,0] = car;
            }
            else if(intersectionQueue2Lane[intersectionRoadId, 1] == null)
            {
                intersectionQueue2Lane[intersectionRoadId, 1] = car;
            }

            if (car.intersectionDirection == RIGHT)
            {
                intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, STRAIGHT]++;
            }
            else if (car.intersectionDirection == STRAIGHT)
            {
                //Block completely traffic from the LEFT (everyone on my left has to give me precedence)
                intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, LEFT]++;
                intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, STRAIGHT]++;
                intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, RIGHT]++;

                //Block traffic from STRAIGHT that will turn left (everyone in front of me that will turn left has to give me precedence as I will be on their right during the turn)
                intersectionBusyMarker[calculateRoad(intersectionRoadId + 2) % 4, LEFT]++;
            }
            else if (car.intersectionDirection == LEFT)
            {
                //Block traffic from the LEFT (everyone on my left that will turn left or go straight has to give me precedence)
                intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, LEFT]++;
                intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, STRAIGHT]++;
            }
            numberCarsWaiting++;    //Put here so not to confuse 
        }
    }

    private void semaphoreIntersectionPriority1Lane(CarAI car, int intersectionRoadId)
    {

    }

    private void semaphoreIntersectionPriority2Lane(CarAI car, int intersectionRoadId)
    {

    }

    private void restoreSimpleIntersectionPriority1Lane(CarAI car, int intersectionRoadId)
    {
        numberCarsInsideIntersection--;
        car.isInsideIntersection = false;
        if (car.intersectionDirection == RIGHT)
        {
            intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, STRAIGHT]--;
        }
        else if (car.intersectionDirection == STRAIGHT)
        {
            //Unblock completely traffic from the RIGHT
            intersectionBusyMarker[calculateRoad(intersectionRoadId + 1) % 4, LEFT]--;
            intersectionBusyMarker[calculateRoad(intersectionRoadId + 1) % 4, STRAIGHT]--;
            intersectionBusyMarker[calculateRoad(intersectionRoadId + 1) % 4, RIGHT]--;

            //Unblock traffic from the LEFT that will turn left or go straight
            intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, LEFT]--;
            intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, STRAIGHT]--;

            //Unblock traffic from STRAIGHT that will turn left
            intersectionBusyMarker[calculateRoad(intersectionRoadId + 2) % 4, LEFT]--;
        }
        else if (car.intersectionDirection == LEFT)
        {
            //Unblock traffic from the RIGHT that will turn left or go straight
            intersectionBusyMarker[calculateRoad(intersectionRoadId + 1) % 4, LEFT]--;
            intersectionBusyMarker[calculateRoad(intersectionRoadId + 1) % 4, STRAIGHT]--;

            //Unblock traffic from the LEFT that will turn left or go straight
            intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, LEFT]--;
            intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, STRAIGHT]--;

            //Unblock completely traffic from STRAIGHT
            intersectionBusyMarker[calculateRoad(intersectionRoadId + 2) % 4, LEFT]--;
            intersectionBusyMarker[calculateRoad(intersectionRoadId + 2) % 4, STRAIGHT]--;
            intersectionBusyMarker[calculateRoad(intersectionRoadId + 2) % 4, RIGHT]--;
        }
    }

    private void restoreSimpleIntersectionPriority2Lane(CarAI car, int intersectionRoadId)
    {
        numberCarsInsideIntersection--;
        car.isInsideIntersection = false;
        if (car.intersectionDirection == RIGHT)
        {
            intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, STRAIGHT]--;
        }
        else if (car.intersectionDirection == STRAIGHT)
        {
            //Unblock completely traffic from the RIGHT
            intersectionBusyMarker[calculateRoad(intersectionRoadId + 1) % 4, LEFT]--;
            intersectionBusyMarker[calculateRoad(intersectionRoadId + 1) % 4, STRAIGHT]--;
            intersectionBusyMarker[calculateRoad(intersectionRoadId + 1) % 4, RIGHT]--;

            //Unblock traffic from the LEFT that will turn left or go straight
            intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, LEFT]--;
            intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, STRAIGHT]--;

            //Unblock traffic from STRAIGHT that will turn left
            intersectionBusyMarker[calculateRoad(intersectionRoadId + 2) % 4, LEFT]--;
        }
        else if (car.intersectionDirection == LEFT)
        {
            //Unblock traffic from the RIGHT that will turn left or go straight
            intersectionBusyMarker[calculateRoad(intersectionRoadId + 1) % 4, LEFT]--;
            intersectionBusyMarker[calculateRoad(intersectionRoadId + 1) % 4, STRAIGHT]--;

            //Unblock traffic from the LEFT that will turn left or go straight
            intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, LEFT]--;
            intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, STRAIGHT]--;

            //Unblock completely traffic from STRAIGHT
            intersectionBusyMarker[calculateRoad(intersectionRoadId + 2) % 4, LEFT]--;
            intersectionBusyMarker[calculateRoad(intersectionRoadId + 2) % 4, STRAIGHT]--;
            intersectionBusyMarker[calculateRoad(intersectionRoadId + 2) % 4, RIGHT]--;
        }
    }

    private void restoreSemaphoreIntersectionPriority1Lane(CarAI car, int intersectionRoadId)
    {

    }

    private void restoreSemaphoreIntersectionPriority2Lane(CarAI car, int intersectionRoadId)
    {

    }

    private int calculateRoad(int number)
    {
        if (number == -1)
        {
            return numberLanes * 4 - 1;
        }
        return number;
    }


}
