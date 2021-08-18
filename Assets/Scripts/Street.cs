using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Street : MonoBehaviour
{
    [Header("Street data")]
    public List<Node> carWaypoints;
    public bool isSimpleIntersection;
    public bool isSemaphoreIntersection;
    public bool isTBoneIntersection;
    public bool hasBusStop;
    public bool isLaneAdapter;
    public bool isCurve;
    public bool isDeadend;
    public int numberLanes;

    //Intersection data structures
    private CarAI[] intersectionQueue1Lane;
    private CarAI[,] intersectionQueue2Lane;
    private int[,] intersectionBusyMarker;
    private int numberCarsInsideIntersection;
    private int numberCarsWaiting;

    //Semaphore data structures
    [Header("Semaphore data")]
    public Semaphore[] intersectionSemaphores;
    public int semaphoreTimerMainLane;
    public int semaphoreTimerLeftLane;
    private bool yellowLightOn;
    private float semaphoreTime;
    private int semaphoreTurn;

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
                        nearbyWaypoint.GetComponent<Node>().needIncomingConnection &&
                        !carWaypoints.Contains(nearbyWaypoint.GetComponent<Node>()))
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

            //Lane adapters always have at least 1 street with 2 lanes, so we need a queue system with 2 slots

            if (numberLanes == 1 && !isLaneAdapter)
            {
                intersectionQueue1Lane = new CarAI[4] { null, null, null, null };
                intersectionBusyMarker = new int[4, 3] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } };
                //Debug.Log("Initialized intersection data structures for: 1 lane intersection");
            }
            else if (numberLanes == 2)
            {
                intersectionQueue2Lane = new CarAI[4, 2] { { null, null }, { null, null }, { null, null }, { null, null } };
                intersectionBusyMarker = new int[4, 3] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } };
                //Debug.Log("Initialized intersection data structures for: 2 lane intersection");
            }
        }

        if (isSemaphoreIntersection)
        {
            if (numberLanes == 1 && !isLaneAdapter)
            {
                semaphoreTurn = 0;
                intersectionQueue1Lane = new CarAI[4] { null, null, null, null };
                intersectionSemaphores[0].greenLights[0].enabled = true;
                intersectionSemaphores[2].greenLights[0].enabled = true;
                intersectionSemaphores[1].redLights[0].enabled = true;
                intersectionSemaphores[3].redLights[0].enabled = true;
                yellowLightOn = false;
            }
            else
            {
                semaphoreTurn = 0;
                intersectionQueue2Lane = new CarAI[4, 2] { { null, null }, { null, null }, { null, null }, { null, null } };
                //3Way intersection init traffic lights
                if (isTBoneIntersection)
                {
                    if (!isLaneAdapter)
                    {
                        intersectionSemaphores[0].greenLights[0].enabled = true;
                        intersectionSemaphores[0].greenLights[1].enabled = true;
                        intersectionSemaphores[1].redLights[0].enabled = true;
                        intersectionSemaphores[1].redLights[1].enabled = true;
                        intersectionSemaphores[3].redLights[0].enabled = true;
                        intersectionSemaphores[3].redLights[1].enabled = true;
                        yellowLightOn = false;
                    }
                    else
                    {
                        //3Way with main street with 1 lane
                        if (numberLanes == 1)
                        {
                            intersectionSemaphores[0].greenLights[0].enabled = true;
                            intersectionSemaphores[0].greenLights[1].enabled = true;
                            intersectionSemaphores[1].redLights[0].enabled = true;
                            intersectionSemaphores[3].redLights[0].enabled = true;
                            yellowLightOn = false;
                        }
                        //3Way with main street with 2 lanes
                        else
                        {
                            intersectionSemaphores[0].greenLights[0].enabled = true;
                            intersectionSemaphores[1].redLights[0].enabled = true;
                            intersectionSemaphores[1].redLights[1].enabled = true;
                            intersectionSemaphores[3].redLights[0].enabled = true;
                            intersectionSemaphores[3].redLights[1].enabled = true;
                            yellowLightOn = false;
                        }
                    }
                }
                //4Way intersection init traffic lights
                else
                {
                    //4Way intersection with both street with 2 lanes
                    if (!isLaneAdapter)
                    {
                        intersectionSemaphores[0].greenLights[0].enabled = true;
                        intersectionSemaphores[0].redLights[1].enabled = true;
                        intersectionSemaphores[2].greenLights[0].enabled = true;
                        intersectionSemaphores[2].redLights[1].enabled = true;
                        intersectionSemaphores[1].redLights[0].enabled = true;
                        intersectionSemaphores[1].redLights[1].enabled = true;
                        intersectionSemaphores[3].redLights[0].enabled = true;
                        intersectionSemaphores[3].redLights[1].enabled = true;
                        yellowLightOn = false;
                    }
                    //4Way intersection with one street with 1 lane and the other with 2 lanes
                    else
                    {
                        intersectionSemaphores[0].greenLights[0].enabled = true;
                        intersectionSemaphores[2].greenLights[0].enabled = true;
                        intersectionSemaphores[1].redLights[0].enabled = true;
                        intersectionSemaphores[1].redLights[1].enabled = true;
                        intersectionSemaphores[3].redLights[0].enabled = true;
                        intersectionSemaphores[3].redLights[1].enabled = true;
                        yellowLightOn = false;
                    }
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isSimpleIntersection)
        {
            updateSimpleIntersection();
        }

        else if (isSemaphoreIntersection)
        {
            if (numberLanes == 1 && !isLaneAdapter)
            {
                updateSemaphoreIntersection3Way4Way1Lane();
            }
            else
            {
                //3-way intersection
                if (isTBoneIntersection)
                {
                    if (isLaneAdapter)
                    {
                        if (numberLanes == 1)
                        {
                            updateSemaphoreIntersection3Way1Lane2Lane();
                        }
                        else
                        {
                            updateSemaphoreIntersection3Way2Lane1Lane();
                        }
                    }
                    else
                    {
                        updateSemaphoreIntersection3Way2Lane();
                    }
                }
                //4-way intersection
                else
                {
                    //4Way intersection with one street with 1 lane and the other with 2 lanes
                    if (isLaneAdapter)
                    {
                        updateSemaphoreIntersection4Way1Lane2Lane();
                    }
                    //4Way intersection with both street with 2 lanes
                    else
                    {
                        updateSemaphoreIntersection4Way2Lane();
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
                    //intersectionBusyMarker[calculateRoad(car.intersectionEnterId + 2) % 4, LEFT]--;
                }
                else if (car.intersectionDirection == LEFT)
                {
                    //Block traffic from the LEFT (everyone on my left that will turn left or go straight has to give me precedence)
                    intersectionBusyMarker[calculateRoad(car.intersectionEnterId - 1) % 4, LEFT]--;
                    intersectionBusyMarker[calculateRoad(car.intersectionEnterId - 1) % 4, STRAIGHT]--;
                }
                /*intersectionBusyMarker[calculateRoad(car.intersectionEnterId - 1) % 4, LEFT]--;
                intersectionBusyMarker[calculateRoad(car.intersectionEnterId - 1) % 4, STRAIGHT]--;
                intersectionBusyMarker[calculateRoad(car.intersectionEnterId - 1) % 4, RIGHT]--;*/
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
                /*intersectionBusyMarker[calculateRoad(car.intersectionEnterId - 1) % 4, LEFT]--;
                intersectionBusyMarker[calculateRoad(car.intersectionEnterId - 1) % 4, STRAIGHT]--;
                intersectionBusyMarker[calculateRoad(car.intersectionEnterId - 1) % 4, RIGHT]--;*/
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
                if (numberLanes == 1 && !isLaneAdapter)
                {
                    semaphoreIntersectionPriority3Way4Way1Lane(car, intersectionRoadId);
                }
                else
                {
                    if (isTBoneIntersection)
                    {
                        if (isLaneAdapter)
                        {
                            if (numberLanes == 1)
                            {
                                semaphoreIntersectionPriority3Way1Lane2Lane(car, intersectionRoadId);
                            }
                            else
                            {
                                semaphoreIntersectionPriority3Way2Lane1Lane(car, intersectionRoadId);
                            }
                        }
                        else
                        {
                            semaphoreIntersectionPriority3Way2Lane(car, intersectionRoadId);
                        }
                    }
                    //4-way intersection
                    else
                    {
                        //4Way intersection with one street with 1 lane and the other with 2 lanes
                        if (isLaneAdapter)
                        {
                            semaphoreIntersectionPriority4Way1Lane2Lane(car, intersectionRoadId);
                        }
                        //4Way intersection with both street with 2 lanes
                        else
                        {
                            semaphoreIntersectionPriority4Way2Lane(car,intersectionRoadId);
                        }
                    }
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
                restoreSemaphoreIntersectionPriority(car, intersectionRoadId);
            }
        }
    }


    //CAR ENTERING INTERSECTION MANAGEMENT
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
                //intersectionBusyMarker[calculateRoad(intersectionRoadId + 2) % 4, LEFT]++;
            }
            else if (car.intersectionDirection == LEFT)
            {
                //Block traffic from the LEFT (everyone on my left that will turn left or go straight has to give me precedence)
                intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, LEFT]++;
                intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, STRAIGHT]++;
            }
            /*intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, LEFT]++;
            intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, STRAIGHT]++;
            intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, RIGHT]++;*/
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
            /*Debug.Log("CROSSING on road " + intersectionRoadId + " direction " + car.intersectionDirection);
            for (int i = 0; i < 4; i++)
            {
                Debug.Log("Direction " + i + ": " + intersectionBusyMarker[i, 0] + "," + intersectionBusyMarker[i, 1] + "," + intersectionBusyMarker[i, 2]);
            }*/
        }
        else
        {
            car.intersectionStop = true;
            car.isInsideIntersection = false;
            if (intersectionQueue2Lane[intersectionRoadId, 0] == null)
            {
                intersectionQueue2Lane[intersectionRoadId, 0] = car;
            }
            else if (intersectionQueue2Lane[intersectionRoadId, 1] == null)
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
                //intersectionBusyMarker[calculateRoad(intersectionRoadId + 2) % 4, LEFT]++;
            }
            else if (car.intersectionDirection == LEFT)
            {
                //Block traffic from the LEFT (everyone on my left that will turn left or go straight has to give me precedence)
                intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, LEFT]++;
                intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, STRAIGHT]++;
            }
            /*intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, LEFT]++;
            intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, STRAIGHT]++;
            intersectionBusyMarker[calculateRoad(intersectionRoadId - 1) % 4, RIGHT]++;*/
            numberCarsWaiting++;    //Put here so not to confuse Update 
            /*Debug.Log("QUEUE on road " + intersectionRoadId + " direction " + car.intersectionDirection);
            for (int i = 0; i < 4; i++)
            {
                Debug.Log("Direction " + i + ": " + intersectionBusyMarker[i, 0] + "," + intersectionBusyMarker[i, 1] + "," + intersectionBusyMarker[i, 2]);
            }*/
        }
    }

    private void semaphoreIntersectionPriority3Way4Way1Lane(CarAI car, int intersectionRoadId)
    {
        if ((intersectionRoadId == 0 || intersectionRoadId == 2) && semaphoreTurn % 2 == 0 && !yellowLightOn)
        {
            car.intersectionStop = false;
            car.isInsideIntersection = true;
            numberCarsInsideIntersection++;
        }
        else if ((intersectionRoadId == 0 || intersectionRoadId == 2) && (semaphoreTurn % 2 == 1 || yellowLightOn))
        {
            numberCarsWaiting++;
            car.intersectionStop = true;
            intersectionQueue1Lane[intersectionRoadId] = car;
        }
        else if ((intersectionRoadId == 1 || intersectionRoadId == 3) && semaphoreTurn % 2 == 1 && !yellowLightOn)
        {
            car.intersectionStop = false;
            car.isInsideIntersection = true;
            numberCarsInsideIntersection++;
        }
        else if ((intersectionRoadId == 1 || intersectionRoadId == 3) && (semaphoreTurn % 2 == 0 || yellowLightOn))
        {
            numberCarsWaiting++;
            car.intersectionStop = true;
            intersectionQueue1Lane[intersectionRoadId] = car;
        }
    }

    private void semaphoreIntersectionPriority4Way2Lane(CarAI car, int intersectionRoadId)
    {
        if ((intersectionRoadId == 0 || intersectionRoadId == 2) && car.intersectionDirection != LEFT && semaphoreTurn == 0 && !yellowLightOn)
        {
            car.intersectionStop = false;
            car.isInsideIntersection = true;
            numberCarsInsideIntersection++;
        }
        else if ((intersectionRoadId == 0 || intersectionRoadId == 2) && car.intersectionDirection == LEFT && semaphoreTurn == 1 && !yellowLightOn)
        {
            car.intersectionStop = false;
            car.isInsideIntersection = true;
            numberCarsInsideIntersection++;
        }
        else if ((intersectionRoadId == 1 || intersectionRoadId == 3) && car.intersectionDirection != LEFT && semaphoreTurn == 2 && !yellowLightOn)
        {
            car.intersectionStop = false;
            car.isInsideIntersection = true;
            numberCarsInsideIntersection++;
        }
        else if ((intersectionRoadId == 1 || intersectionRoadId == 3) && car.intersectionDirection == LEFT && semaphoreTurn == 3 && !yellowLightOn)
        {
            car.intersectionStop = false;
            car.isInsideIntersection = true;
            numberCarsInsideIntersection++;
        }
        else
        {
            if (!car.intersectionStop)      //only cars that are just entering the intersection must be inserted in the queue
            {
                numberCarsWaiting++;
                car.intersectionStop = true;
                if (intersectionQueue2Lane[intersectionRoadId, 0] == null)
                {
                    intersectionQueue2Lane[intersectionRoadId, 0] = car;
                }
                else
                {
                    intersectionQueue2Lane[intersectionRoadId, 1] = car;
                }
            }
        }
    }

    private void semaphoreIntersectionPriority3Way2Lane(CarAI car, int intersectionRoadId)
    {
        if (intersectionRoadId == 0 && semaphoreTurn == 0 && !yellowLightOn)
        {
            car.intersectionStop = false;
            car.isInsideIntersection = true;
            numberCarsInsideIntersection++;
        }
        else if (intersectionRoadId == 1 && car.intersectionDirection != LEFT && semaphoreTurn == 1 && !yellowLightOn)
        {
            car.intersectionStop = false;
            car.isInsideIntersection = true;
            numberCarsInsideIntersection++;
        }
        else if (intersectionRoadId == 3 && semaphoreTurn == 1 && !yellowLightOn)
        {
            car.intersectionStop = false;
            car.isInsideIntersection = true;
            numberCarsInsideIntersection++;
        }
        else if (intersectionRoadId == 1 && car.intersectionDirection == LEFT && semaphoreTurn == 2 && !yellowLightOn)
        {
            car.intersectionStop = false;
            car.isInsideIntersection = true;
            numberCarsInsideIntersection++;
        }
        else
        {
            if (!car.intersectionStop)      //only cars that are just entering the intersection must be inserted in the queue
            {
                numberCarsWaiting++;
                car.intersectionStop = true;
                if (intersectionQueue2Lane[intersectionRoadId, 0] == null)
                {
                    intersectionQueue2Lane[intersectionRoadId, 0] = car;
                }
                else
                {
                    intersectionQueue2Lane[intersectionRoadId, 1] = car;
                }
            }
        }
    }

    private void semaphoreIntersectionPriority4Way1Lane2Lane(CarAI car, int intersectionRoadId)
    {
        if ((intersectionRoadId == 0 || intersectionRoadId == 2) && semaphoreTurn == 0 && !yellowLightOn)
        {
            car.intersectionStop = false;
            car.isInsideIntersection = true;
            numberCarsInsideIntersection++;
        }
        else if ((intersectionRoadId == 1 || intersectionRoadId == 3) && car.intersectionDirection != LEFT && semaphoreTurn == 1 && !yellowLightOn)
        {
            car.intersectionStop = false;
            car.isInsideIntersection = true;
            numberCarsInsideIntersection++;
        }
        else if ((intersectionRoadId == 1 || intersectionRoadId == 3) && car.intersectionDirection == LEFT && semaphoreTurn == 2 && !yellowLightOn)
        {
            car.intersectionStop = false;
            car.isInsideIntersection = true;
            numberCarsInsideIntersection++;
        }
        else
        {
            if (!car.intersectionStop)      //only cars that are just entering the intersection must be inserted in the queue
            {
                numberCarsWaiting++;
                car.intersectionStop = true;
                if (intersectionQueue2Lane[intersectionRoadId, 0] == null)
                {
                    intersectionQueue2Lane[intersectionRoadId, 0] = car;
                }
                else
                {
                    intersectionQueue2Lane[intersectionRoadId, 1] = car;
                }
            }
        }
    }

    private void semaphoreIntersectionPriority3Way1Lane2Lane(CarAI car, int intersectionRoadId)
    {
        if (intersectionRoadId == 0 && semaphoreTurn == 0 && !yellowLightOn)
        {
            car.intersectionStop = false;
            car.isInsideIntersection = true;
            numberCarsInsideIntersection++;
        }
        else if ((intersectionRoadId == 1 || intersectionRoadId == 3)  && semaphoreTurn == 1 && !yellowLightOn)
        {
            car.intersectionStop = false;
            car.isInsideIntersection = true;
            numberCarsInsideIntersection++;
        }
        else
        {
            if (!car.intersectionStop)      //only cars that are just entering the intersection must be inserted in the queue
            {
                numberCarsWaiting++;
                car.intersectionStop = true;
                if (intersectionQueue2Lane[intersectionRoadId, 0] == null)
                {
                    intersectionQueue2Lane[intersectionRoadId, 0] = car;
                }
                else
                {
                    intersectionQueue2Lane[intersectionRoadId, 1] = car;
                }
            }
        }
    }

    private void semaphoreIntersectionPriority3Way2Lane1Lane(CarAI car, int intersectionRoadId)
    {
        if (intersectionRoadId == 0 && semaphoreTurn == 0 && !yellowLightOn)
        {
            car.intersectionStop = false;
            car.isInsideIntersection = true;
            numberCarsInsideIntersection++;
        }
        else if (intersectionRoadId == 1 && car.intersectionDirection != LEFT && semaphoreTurn == 1 && !yellowLightOn)
        {
            car.intersectionStop = false;
            car.isInsideIntersection = true;
            numberCarsInsideIntersection++;
        }
        else if (intersectionRoadId == 3 && semaphoreTurn == 1 && !yellowLightOn)
        {
            car.intersectionStop = false;
            car.isInsideIntersection = true;
            numberCarsInsideIntersection++;
        }
        else if (intersectionRoadId == 1 && car.intersectionDirection == LEFT && semaphoreTurn == 2 && !yellowLightOn)
        {
            car.intersectionStop = false;
            car.isInsideIntersection = true;
            numberCarsInsideIntersection++;
        }
        else
        {
            if (!car.intersectionStop)      //only cars that are just entering the intersection must be inserted in the queue
            {
                numberCarsWaiting++;
                car.intersectionStop = true;
                if (intersectionQueue2Lane[intersectionRoadId, 0] == null)
                {
                    intersectionQueue2Lane[intersectionRoadId, 0] = car;
                }
                else
                {
                    intersectionQueue2Lane[intersectionRoadId, 1] = car;
                }
            }
        }
    }


    //CAR EXITING INTERSECTION MANAGEMENT
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
        /*Debug.Log("LEAVING on road " + intersectionRoadId + " direction " + car.intersectionDirection);
        for (int i = 0; i < 4; i++)
        {
            Debug.Log("Direction " + i + ": " + intersectionBusyMarker[i, 0] + "," + intersectionBusyMarker[i, 1] + "," + intersectionBusyMarker[i, 2]);
        }*/
    }

    private void restoreSemaphoreIntersectionPriority(CarAI car, int intersectionRoadId)
    {
        car.isInsideIntersection = false;
    }


    //SEMAPHORE LIGHTS MANAGEMENT
    private void updateSimpleIntersection()
    {
        if (numberCarsWaiting > 0)
        {
            for (int i = 0; i < 4; i++)
            {
                if (numberLanes == 1 && !isLaneAdapter)
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
                else
                {
                    for (int j = 0; j < 2; j++)
                    {
                        if (intersectionQueue2Lane[i, j] != null)
                        {
                            CarAI car = intersectionQueue2Lane[i, j];
                            if (intersectionBusyMarker[i, car.intersectionDirection] == 0)
                            {
                                intersectionPriorityCleaner(car, car.intersectionEnterId);
                                intersectionManager(car, i);
                                intersectionQueue2Lane[i, j] = null;
                                numberCarsWaiting--;
                            }
                        }
                    }
                }

            }
        }
    }

    private void updateSemaphoreIntersection3Way4Way1Lane()
    {
        semaphoreTime += Time.deltaTime;
        //Debug.Log(semaphoreTime);
        if (semaphoreTime > semaphoreTimerMainLane)
        {
            semaphoreTurn++;
            semaphoreTime = 0;
            yellowLightOn = false;
            if (semaphoreTurn % 2 == 0)
            {
                intersectionSemaphores[1].yellowLights[0].enabled = false;
                intersectionSemaphores[3].yellowLights[0].enabled = false;
                intersectionSemaphores[1].redLights[0].enabled = true;
                intersectionSemaphores[3].redLights[0].enabled = true;

                intersectionSemaphores[0].redLights[0].enabled = false;
                intersectionSemaphores[2].redLights[0].enabled = false;
                intersectionSemaphores[0].greenLights[0].enabled = true;
                intersectionSemaphores[2].greenLights[0].enabled = true;
            }
            else
            {
                intersectionSemaphores[1].redLights[0].enabled = false;
                intersectionSemaphores[3].redLights[0].enabled = false;
                intersectionSemaphores[1].greenLights[0].enabled = true;
                intersectionSemaphores[3].greenLights[0].enabled = true;


                intersectionSemaphores[0].yellowLights[0].enabled = false;
                intersectionSemaphores[2].yellowLights[0].enabled = false;
                intersectionSemaphores[0].redLights[0].enabled = true;
                intersectionSemaphores[2].redLights[0].enabled = true;
            }
            //Move cars inside the intersection
            if (semaphoreTurn % 2 == 0)
            {
                CarAI car;
                if (intersectionQueue1Lane[0] != null)
                {
                    car = intersectionQueue1Lane[0];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue1Lane[0] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue1Lane[2] != null)
                {
                    car = intersectionQueue1Lane[2];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue1Lane[2] = null;
                    numberCarsWaiting--;
                }
            }
            else
            {
                CarAI car;
                if (intersectionQueue1Lane[1] != null)
                {
                    car = intersectionQueue1Lane[1];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue1Lane[1] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue1Lane[3] != null)
                {
                    car = intersectionQueue1Lane[3];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue1Lane[3] = null;
                    numberCarsWaiting--;
                }
            }
        }
        else if (semaphoreTime > semaphoreTimerMainLane - 3)
        {
            if (semaphoreTurn % 2 == 0)
            {
                intersectionSemaphores[0].greenLights[0].enabled = false;
                intersectionSemaphores[2].greenLights[0].enabled = false;
                intersectionSemaphores[0].yellowLights[0].enabled = true;
                intersectionSemaphores[2].yellowLights[0].enabled = true;
            }
            else
            {
                intersectionSemaphores[1].greenLights[0].enabled = false;
                intersectionSemaphores[3].greenLights[0].enabled = false;
                intersectionSemaphores[1].yellowLights[0].enabled = true;
                intersectionSemaphores[3].yellowLights[0].enabled = true;
            }
            yellowLightOn = true;
        }
    }

    private void updateSemaphoreIntersection3Way2Lane()
    {
        CarAI car;
        semaphoreTime += Time.deltaTime;
        if (semaphoreTime >= semaphoreTimerMainLane - 3 && semaphoreTime <= semaphoreTimerMainLane && !yellowLightOn)
        {
            semaphoreTurn = 0;
            yellowLightOn = true;
            intersectionSemaphores[0].greenLights[0].enabled = false;
            intersectionSemaphores[0].yellowLights[0].enabled = true;
            intersectionSemaphores[0].greenLights[1].enabled = false;
            intersectionSemaphores[0].yellowLights[1].enabled = true;
        }
        else if (semaphoreTime <= semaphoreTimerMainLane && semaphoreTurn != 0)
        {
            semaphoreTurn = 0;
            yellowLightOn = false;
            intersectionSemaphores[1].yellowLights[1].enabled = false;
            intersectionSemaphores[1].redLights[1].enabled = true;

            intersectionSemaphores[0].redLights[0].enabled = false;
            intersectionSemaphores[0].greenLights[0].enabled = true;
            intersectionSemaphores[0].redLights[1].enabled = false;
            intersectionSemaphores[0].greenLights[1].enabled = true;

            if (numberCarsWaiting > 0)
            {
                if (intersectionQueue2Lane[0, 0] != null)
                {
                    car = intersectionQueue2Lane[0, 0];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[0, 0] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[0, 1] != null)
                {
                    car = intersectionQueue2Lane[0, 1];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[0, 1] = null;
                    numberCarsWaiting--;
                }
            }
        }
        else if (semaphoreTime >= 2 * semaphoreTimerMainLane - 3 && semaphoreTime <= 2 * semaphoreTimerMainLane && !yellowLightOn)
        {
            semaphoreTurn = 1;
            yellowLightOn = true;
            intersectionSemaphores[1].greenLights[0].enabled = false;
            intersectionSemaphores[3].greenLights[0].enabled = false;
            intersectionSemaphores[3].greenLights[1].enabled = false;
            intersectionSemaphores[1].yellowLights[0].enabled = true;
            intersectionSemaphores[3].yellowLights[0].enabled = true;
            intersectionSemaphores[3].yellowLights[1].enabled = true;
        }
        else if (semaphoreTime >= semaphoreTimerMainLane && semaphoreTime <= 2 * semaphoreTimerMainLane && semaphoreTurn != 1)
        {
            semaphoreTurn = 1;
            yellowLightOn = false;
            intersectionSemaphores[0].yellowLights[0].enabled = false;
            intersectionSemaphores[0].redLights[0].enabled = true;
            intersectionSemaphores[0].yellowLights[1].enabled = false;
            intersectionSemaphores[0].redLights[1].enabled = true;

            intersectionSemaphores[1].redLights[0].enabled = false;
            intersectionSemaphores[3].redLights[0].enabled = false;
            intersectionSemaphores[3].redLights[1].enabled = false;
            intersectionSemaphores[1].greenLights[0].enabled = true;
            intersectionSemaphores[3].greenLights[0].enabled = true;
            intersectionSemaphores[3].greenLights[1].enabled = true;

            if (numberCarsWaiting > 0)
            {
                if (intersectionQueue2Lane[1, 0] != null && intersectionQueue2Lane[0, 0].intersectionDirection != LEFT)
                {
                    car = intersectionQueue2Lane[1, 0];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[1, 0] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[1, 1] != null && intersectionQueue2Lane[0, 1].intersectionDirection != LEFT)
                {
                    car = intersectionQueue2Lane[1, 1];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[1, 1] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[3, 0] != null)
                {
                    car = intersectionQueue2Lane[3, 0];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[3, 0] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[3, 1] != null)
                {
                    car = intersectionQueue2Lane[3, 1];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[3, 1] = null;
                    numberCarsWaiting--;
                }
            }
        }
        else if (semaphoreTime >= 2 * semaphoreTimerMainLane + semaphoreTimerLeftLane - 3 && semaphoreTime <= 2 * semaphoreTimerMainLane + semaphoreTimerLeftLane && !yellowLightOn)
        {
            semaphoreTurn = 2;
            yellowLightOn = true;
            intersectionSemaphores[1].greenLights[1].enabled = false;
            intersectionSemaphores[1].yellowLights[1].enabled = true;
        }
        else if (semaphoreTime >= 2 * semaphoreTimerMainLane && semaphoreTime <= 2 * semaphoreTimerMainLane + semaphoreTimerLeftLane && semaphoreTurn != 2)
        {
            semaphoreTurn = 2;
            yellowLightOn = false;
            intersectionSemaphores[1].yellowLights[0].enabled = false;
            intersectionSemaphores[3].yellowLights[0].enabled = false;
            intersectionSemaphores[3].yellowLights[1].enabled = false;
            intersectionSemaphores[1].redLights[0].enabled = true;
            intersectionSemaphores[3].redLights[0].enabled = true;
            intersectionSemaphores[3].redLights[1].enabled = true;

            intersectionSemaphores[1].redLights[1].enabled = false;
            intersectionSemaphores[1].greenLights[1].enabled = true;

            if (numberCarsWaiting > 0)
            {
                if (intersectionQueue2Lane[1, 0] != null && intersectionQueue2Lane[1, 0].intersectionDirection == LEFT)
                {
                    car = intersectionQueue2Lane[1, 0];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[1, 0] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[1, 1] != null && intersectionQueue2Lane[1, 1].intersectionDirection == LEFT)
                {
                    car = intersectionQueue2Lane[1, 1];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[1, 1] = null;
                    numberCarsWaiting--;
                }
            }
        }
        else if (semaphoreTime >= 2 * semaphoreTimerMainLane + semaphoreTimerLeftLane)
        {
            semaphoreTime = 0;
        }
    }

    private void updateSemaphoreIntersection4Way2Lane()
    {
        CarAI car;
        semaphoreTime += Time.deltaTime;
        if (semaphoreTime >= semaphoreTimerMainLane - 3 && semaphoreTime <= semaphoreTimerMainLane && !yellowLightOn)
        {
            semaphoreTurn = 0;
            yellowLightOn = true;
            intersectionSemaphores[0].greenLights[0].enabled = false;
            intersectionSemaphores[2].greenLights[0].enabled = false;
            intersectionSemaphores[0].yellowLights[0].enabled = true;
            intersectionSemaphores[2].yellowLights[0].enabled = true;
        }
        else if (semaphoreTime <= semaphoreTimerMainLane && semaphoreTurn != 0)
        {
            semaphoreTurn = 0;
            yellowLightOn = false;
            intersectionSemaphores[1].yellowLights[1].enabled = false;
            intersectionSemaphores[3].yellowLights[1].enabled = false;
            intersectionSemaphores[1].redLights[1].enabled = true;
            intersectionSemaphores[3].redLights[1].enabled = true;
            intersectionSemaphores[0].redLights[0].enabled = false;
            intersectionSemaphores[2].redLights[0].enabled = false;
            intersectionSemaphores[0].greenLights[0].enabled = true;
            intersectionSemaphores[2].greenLights[0].enabled = true;

            if (numberCarsWaiting > 0)
            {
                if (intersectionQueue2Lane[0, 0] != null && intersectionQueue2Lane[0, 0].intersectionDirection != LEFT)
                {
                    car = intersectionQueue2Lane[0, 0];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[0, 0] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[0, 1] != null && intersectionQueue2Lane[0, 1].intersectionDirection != LEFT)
                {
                    car = intersectionQueue2Lane[0, 1];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[0, 1] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[2, 0] != null && intersectionQueue2Lane[2, 0].intersectionDirection != LEFT)
                {
                    car = intersectionQueue2Lane[2, 0];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[2, 0] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[2, 1] != null && intersectionQueue2Lane[2, 1].intersectionDirection != LEFT)
                {
                    car = intersectionQueue2Lane[2, 1];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[2, 1] = null;
                    numberCarsWaiting--;
                }
            }
        }
        else if (semaphoreTime >= semaphoreTimerMainLane + semaphoreTimerLeftLane - 3 && semaphoreTime <= semaphoreTimerMainLane + semaphoreTimerLeftLane && !yellowLightOn)
        {
            semaphoreTurn = 1;
            yellowLightOn = true;
            intersectionSemaphores[0].greenLights[1].enabled = false;
            intersectionSemaphores[2].greenLights[1].enabled = false;
            intersectionSemaphores[0].yellowLights[1].enabled = true;
            intersectionSemaphores[2].yellowLights[1].enabled = true;
        }
        else if (semaphoreTime >= semaphoreTimerMainLane && semaphoreTime <= semaphoreTimerMainLane + semaphoreTimerLeftLane && semaphoreTurn != 1)
        {
            semaphoreTurn = 1;
            yellowLightOn = false;
            intersectionSemaphores[0].yellowLights[0].enabled = false;
            intersectionSemaphores[2].yellowLights[0].enabled = false;
            intersectionSemaphores[0].redLights[0].enabled = true;
            intersectionSemaphores[2].redLights[0].enabled = true;
            intersectionSemaphores[0].redLights[1].enabled = false;
            intersectionSemaphores[2].redLights[1].enabled = false;
            intersectionSemaphores[0].greenLights[1].enabled = true;
            intersectionSemaphores[2].greenLights[1].enabled = true;

            if (numberCarsWaiting > 0)
            {
                if (intersectionQueue2Lane[0, 0] != null && intersectionQueue2Lane[0, 0].intersectionDirection == LEFT)
                {
                    car = intersectionQueue2Lane[0, 0];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[0, 0] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[0, 1] != null && intersectionQueue2Lane[0, 1].intersectionDirection == LEFT)
                {
                    car = intersectionQueue2Lane[0, 1];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[0, 1] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[2, 0] != null && intersectionQueue2Lane[2, 0].intersectionDirection == LEFT)
                {
                    car = intersectionQueue2Lane[2, 0];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[2, 0] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[2, 1] != null && intersectionQueue2Lane[2, 1].intersectionDirection == LEFT)
                {
                    car = intersectionQueue2Lane[2, 1];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[2, 1] = null;
                    numberCarsWaiting--;
                }
            }
        }
        else if (semaphoreTime >= 2 * semaphoreTimerMainLane + semaphoreTimerLeftLane - 3 && semaphoreTime <= 2 * semaphoreTimerMainLane + semaphoreTimerLeftLane && !yellowLightOn)
        {
            semaphoreTurn = 2;
            yellowLightOn = true;
            intersectionSemaphores[1].greenLights[0].enabled = false;
            intersectionSemaphores[3].greenLights[0].enabled = false;
            intersectionSemaphores[1].yellowLights[0].enabled = true;
            intersectionSemaphores[3].yellowLights[0].enabled = true;
        }
        else if (semaphoreTime >= semaphoreTimerMainLane + semaphoreTimerLeftLane && semaphoreTime <= 2 * semaphoreTimerMainLane + semaphoreTimerLeftLane && semaphoreTurn != 2)
        {
            semaphoreTurn = 2;
            yellowLightOn = false;
            intersectionSemaphores[0].yellowLights[1].enabled = false;
            intersectionSemaphores[2].yellowLights[1].enabled = false;
            intersectionSemaphores[0].redLights[1].enabled = true;
            intersectionSemaphores[2].redLights[1].enabled = true;
            intersectionSemaphores[1].redLights[0].enabled = false;
            intersectionSemaphores[3].redLights[0].enabled = false;
            intersectionSemaphores[1].greenLights[0].enabled = true;
            intersectionSemaphores[3].greenLights[0].enabled = true;

            if (numberCarsWaiting > 0)
            {
                if (intersectionQueue2Lane[1, 0] != null && intersectionQueue2Lane[1, 0].intersectionDirection != LEFT)
                {
                    car = intersectionQueue2Lane[1, 0];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[1, 0] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[1, 1] != null && intersectionQueue2Lane[1, 1].intersectionDirection != LEFT)
                {
                    car = intersectionQueue2Lane[1, 1];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[1, 1] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[3, 0] != null && intersectionQueue2Lane[3, 0].intersectionDirection != LEFT)
                {
                    car = intersectionQueue2Lane[3, 0];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[3, 0] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[3, 1] != null && intersectionQueue2Lane[3, 1].intersectionDirection != LEFT)
                {
                    car = intersectionQueue2Lane[3, 1];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[3, 1] = null;
                    numberCarsWaiting--;
                }
            }
        }
        else if (semaphoreTime >= 2 * semaphoreTimerMainLane + 2 * semaphoreTimerLeftLane - 3 && semaphoreTime <= 2 * semaphoreTimerMainLane + 2 * semaphoreTimerLeftLane && !yellowLightOn)
        {
            semaphoreTurn = 3;
            yellowLightOn = true;
            intersectionSemaphores[1].greenLights[1].enabled = false;
            intersectionSemaphores[3].greenLights[1].enabled = false;
            intersectionSemaphores[1].yellowLights[1].enabled = true;
            intersectionSemaphores[3].yellowLights[1].enabled = true;
        }
        else if (semaphoreTime >= 2 * semaphoreTimerMainLane + semaphoreTimerLeftLane && semaphoreTime <= 2 * semaphoreTimerMainLane + 2 * semaphoreTimerLeftLane && semaphoreTurn != 3)
        {
            semaphoreTurn = 3;
            yellowLightOn = false;
            intersectionSemaphores[1].yellowLights[0].enabled = false;
            intersectionSemaphores[3].yellowLights[0].enabled = false;
            intersectionSemaphores[1].redLights[0].enabled = true;
            intersectionSemaphores[3].redLights[0].enabled = true;
            intersectionSemaphores[1].redLights[1].enabled = false;
            intersectionSemaphores[3].redLights[1].enabled = false;
            intersectionSemaphores[1].greenLights[1].enabled = true;
            intersectionSemaphores[3].greenLights[1].enabled = true;

            if (numberCarsWaiting > 0)
            {
                if (intersectionQueue2Lane[1, 0] != null && intersectionQueue2Lane[1, 0].intersectionDirection == LEFT)
                {
                    car = intersectionQueue2Lane[1, 0];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[1, 0] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[1, 1] != null && intersectionQueue2Lane[1, 1].intersectionDirection == LEFT)
                {
                    car = intersectionQueue2Lane[1, 1];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[1, 1] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[3, 0] != null && intersectionQueue2Lane[3, 0].intersectionDirection == LEFT)
                {
                    car = intersectionQueue2Lane[3, 0];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[3, 0] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[3, 1] != null && intersectionQueue2Lane[3, 1].intersectionDirection == LEFT)
                {
                    car = intersectionQueue2Lane[3, 1];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[3, 1] = null;
                    numberCarsWaiting--;
                }
            }
        }
        else if (semaphoreTime >= 2 * semaphoreTimerMainLane + 2 * semaphoreTimerLeftLane)
        {
            semaphoreTime = 0;
        }
    }

    private void updateSemaphoreIntersection4Way1Lane2Lane()
    {
        CarAI car;
        semaphoreTime += Time.deltaTime;
        if (semaphoreTime >= semaphoreTimerMainLane - 3 && semaphoreTime <= semaphoreTimerMainLane && !yellowLightOn)
        {
            semaphoreTurn = 0;
            yellowLightOn = true;
            intersectionSemaphores[0].greenLights[0].enabled = false;
            intersectionSemaphores[2].greenLights[0].enabled = false;
            intersectionSemaphores[0].yellowLights[0].enabled = true;
            intersectionSemaphores[2].yellowLights[0].enabled = true;
        }
        else if (semaphoreTime <= semaphoreTimerMainLane && semaphoreTurn != 0)
        {
            semaphoreTurn = 0;
            yellowLightOn = false;
            intersectionSemaphores[1].yellowLights[1].enabled = false;
            intersectionSemaphores[3].yellowLights[1].enabled = false;
            intersectionSemaphores[1].redLights[1].enabled = true;
            intersectionSemaphores[3].redLights[1].enabled = true;
            intersectionSemaphores[0].redLights[0].enabled = false;
            intersectionSemaphores[2].redLights[0].enabled = false;
            intersectionSemaphores[0].greenLights[0].enabled = true;
            intersectionSemaphores[2].greenLights[0].enabled = true;

            if (numberCarsWaiting > 0)
            {
                if (intersectionQueue2Lane[0, 0] != null)
                {
                    car = intersectionQueue2Lane[0, 0];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[0, 0] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[0, 1] != null)
                {
                    car = intersectionQueue2Lane[0, 1];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[0, 1] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[2, 0] != null)
                {
                    car = intersectionQueue2Lane[2, 0];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[2, 0] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[2, 1] != null)
                {
                    car = intersectionQueue2Lane[2, 1];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[2, 1] = null;
                    numberCarsWaiting--;
                }
            }
        }
        else if (semaphoreTime >= 2 * semaphoreTimerMainLane - 3 && semaphoreTime <= 2 * semaphoreTimerMainLane && !yellowLightOn)
        {
            semaphoreTurn = 1;
            yellowLightOn = true;
            intersectionSemaphores[1].greenLights[0].enabled = false;
            intersectionSemaphores[3].greenLights[0].enabled = false;
            intersectionSemaphores[1].yellowLights[0].enabled = true;
            intersectionSemaphores[3].yellowLights[0].enabled = true;
        }
        else if (semaphoreTime >= semaphoreTimerMainLane && semaphoreTime <= 2 * semaphoreTimerMainLane && semaphoreTurn != 1)
        {
            semaphoreTurn = 1;
            yellowLightOn = false;
            intersectionSemaphores[0].yellowLights[0].enabled = false;
            intersectionSemaphores[2].yellowLights[0].enabled = false;
            intersectionSemaphores[0].redLights[0].enabled = true;
            intersectionSemaphores[2].redLights[0].enabled = true;
            intersectionSemaphores[1].redLights[0].enabled = false;
            intersectionSemaphores[3].redLights[0].enabled = false;
            intersectionSemaphores[1].greenLights[0].enabled = true;
            intersectionSemaphores[3].greenLights[0].enabled = true;

            if (numberCarsWaiting > 0)
            {
                if (intersectionQueue2Lane[1, 0] != null && intersectionQueue2Lane[1, 0].intersectionDirection != LEFT)
                {
                    car = intersectionQueue2Lane[1, 0];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[1, 0] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[1, 1] != null && intersectionQueue2Lane[1, 1].intersectionDirection != LEFT)
                {
                    car = intersectionQueue2Lane[1, 1];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[1, 1] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[3, 0] != null && intersectionQueue2Lane[3, 0].intersectionDirection != LEFT)
                {
                    car = intersectionQueue2Lane[3, 0];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[3, 0] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[3, 1] != null && intersectionQueue2Lane[3, 1].intersectionDirection != LEFT)
                {
                    car = intersectionQueue2Lane[3, 1];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[3, 1] = null;
                    numberCarsWaiting--;
                }
            }
        }
        else if (semaphoreTime >= 2 * semaphoreTimerMainLane + semaphoreTimerLeftLane - 3 && semaphoreTime <= 2 * semaphoreTimerMainLane + semaphoreTimerLeftLane && !yellowLightOn)
        {
            semaphoreTurn = 2;
            yellowLightOn = true;
            intersectionSemaphores[1].greenLights[1].enabled = false;
            intersectionSemaphores[3].greenLights[1].enabled = false;
            intersectionSemaphores[1].yellowLights[1].enabled = true;
            intersectionSemaphores[3].yellowLights[1].enabled = true;
        }
        else if (semaphoreTime >= 2 * semaphoreTimerMainLane && semaphoreTime <= 2 * semaphoreTimerMainLane + semaphoreTimerLeftLane && semaphoreTurn != 2)
        {
            semaphoreTurn = 2;
            yellowLightOn = false;
            intersectionSemaphores[1].yellowLights[0].enabled = false;
            intersectionSemaphores[3].yellowLights[0].enabled = false;
            intersectionSemaphores[1].redLights[0].enabled = true;
            intersectionSemaphores[3].redLights[0].enabled = true;
            intersectionSemaphores[1].redLights[1].enabled = false;
            intersectionSemaphores[3].redLights[1].enabled = false;
            intersectionSemaphores[1].greenLights[1].enabled = true;
            intersectionSemaphores[3].greenLights[1].enabled = true;

            if (numberCarsWaiting > 0)
            {
                if (intersectionQueue2Lane[1, 0] != null && intersectionQueue2Lane[1, 0].intersectionDirection == LEFT)
                {
                    car = intersectionQueue2Lane[1, 0];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[1, 0] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[1, 1] != null && intersectionQueue2Lane[1, 1].intersectionDirection == LEFT)
                {
                    car = intersectionQueue2Lane[1, 1];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[1, 1] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[3, 0] != null && intersectionQueue2Lane[3, 0].intersectionDirection == LEFT)
                {
                    car = intersectionQueue2Lane[3, 0];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[3, 0] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[3, 1] != null && intersectionQueue2Lane[3, 1].intersectionDirection == LEFT)
                {
                    car = intersectionQueue2Lane[3, 1];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[3, 1] = null;
                    numberCarsWaiting--;
                }
            }
        }
        else if (semaphoreTime > 2 * semaphoreTimerMainLane + semaphoreTimerLeftLane)
        {
            semaphoreTime = 0;
        }
    }

    private void updateSemaphoreIntersection3Way1Lane2Lane()
    {
        CarAI car;
        semaphoreTime += Time.deltaTime;
        if (semaphoreTime >= semaphoreTimerMainLane - 3 && semaphoreTime <= semaphoreTimerMainLane && !yellowLightOn)
        {
            semaphoreTurn = 0;
            yellowLightOn = true;
            intersectionSemaphores[0].greenLights[0].enabled = false;
            intersectionSemaphores[0].yellowLights[0].enabled = true;
            intersectionSemaphores[0].greenLights[1].enabled = false;
            intersectionSemaphores[0].yellowLights[1].enabled = true;
        }
        else if (semaphoreTime <= semaphoreTimerMainLane && semaphoreTurn != 0)
        {
            semaphoreTurn = 0;
            yellowLightOn = false;
            intersectionSemaphores[1].yellowLights[0].enabled = false;
            intersectionSemaphores[3].yellowLights[0].enabled = false;
            intersectionSemaphores[1].redLights[0].enabled = true;
            intersectionSemaphores[3].redLights[0].enabled = true;
            intersectionSemaphores[0].redLights[0].enabled = false;
            intersectionSemaphores[0].greenLights[0].enabled = true;
            intersectionSemaphores[0].redLights[1].enabled = false;
            intersectionSemaphores[0].greenLights[1].enabled = true;

            if (numberCarsWaiting > 0)
            {
                if (intersectionQueue2Lane[0, 0] != null)
                {
                    car = intersectionQueue2Lane[0, 0];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[0, 0] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[0, 1] != null)
                {
                    car = intersectionQueue2Lane[0, 1];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[0, 1] = null;
                    numberCarsWaiting--;
                }
            }
        }
        else if (semaphoreTime >= 2 * semaphoreTimerMainLane - 3 && semaphoreTime <= 2 * semaphoreTimerMainLane && !yellowLightOn)
        {
            semaphoreTurn = 1;
            yellowLightOn = true;
            intersectionSemaphores[1].greenLights[0].enabled = false;
            intersectionSemaphores[3].greenLights[0].enabled = false;
            intersectionSemaphores[1].yellowLights[0].enabled = true;
            intersectionSemaphores[3].yellowLights[0].enabled = true;
        }
        else if (semaphoreTime >= semaphoreTimerMainLane && semaphoreTime <= 2 * semaphoreTimerMainLane && semaphoreTurn != 1)
        {
            semaphoreTurn = 1;
            yellowLightOn = false;
            intersectionSemaphores[0].yellowLights[0].enabled = false;
            intersectionSemaphores[0].redLights[0].enabled = true;
            intersectionSemaphores[0].yellowLights[1].enabled = false;
            intersectionSemaphores[0].redLights[1].enabled = true;

            intersectionSemaphores[1].redLights[0].enabled = false;
            intersectionSemaphores[3].redLights[0].enabled = false;
            intersectionSemaphores[1].greenLights[0].enabled = true;
            intersectionSemaphores[3].greenLights[0].enabled = true;

            if (numberCarsWaiting > 0)
            {
                if (intersectionQueue2Lane[1, 0] != null)
                {
                    car = intersectionQueue2Lane[1, 0];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[1, 0] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[1, 1] != null)
                {
                    car = intersectionQueue2Lane[1, 1];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[1, 1] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[3, 0] != null)
                {
                    car = intersectionQueue2Lane[3, 0];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[3, 0] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[3, 1] != null)
                {
                    car = intersectionQueue2Lane[3, 1];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[3, 1] = null;
                    numberCarsWaiting--;
                }
            }
        }
        else if (semaphoreTime >= 2 * semaphoreTimerMainLane)
        {
            semaphoreTime = 0;
        }
    }

    private void updateSemaphoreIntersection3Way2Lane1Lane()
    {
        CarAI car;
        semaphoreTime += Time.deltaTime;
        if (semaphoreTime >= semaphoreTimerMainLane - 3 && semaphoreTime <= semaphoreTimerMainLane && !yellowLightOn)
        {
            semaphoreTurn = 0;
            yellowLightOn = true;
            intersectionSemaphores[0].greenLights[0].enabled = false;
            intersectionSemaphores[0].yellowLights[0].enabled = true;
        }
        else if (semaphoreTime <= semaphoreTimerMainLane && semaphoreTurn != 0)
        {
            semaphoreTurn = 0;
            yellowLightOn = false;
            intersectionSemaphores[1].yellowLights[1].enabled = false;
            intersectionSemaphores[1].redLights[1].enabled = true;
            intersectionSemaphores[0].redLights[0].enabled = false;
            intersectionSemaphores[0].greenLights[0].enabled = true;

            if (numberCarsWaiting > 0)
            {
                if (intersectionQueue2Lane[0, 0] != null)
                {
                    car = intersectionQueue2Lane[0, 0];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[0, 0] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[0, 1] != null)
                {
                    car = intersectionQueue2Lane[0, 1];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[0, 1] = null;
                    numberCarsWaiting--;
                }
            }
        }
        else if (semaphoreTime >= 2 * semaphoreTimerMainLane - 3 && semaphoreTime <= 2 * semaphoreTimerMainLane && !yellowLightOn)
        {
            semaphoreTurn = 1;
            yellowLightOn = true;
            intersectionSemaphores[1].greenLights[0].enabled = false;
            intersectionSemaphores[3].greenLights[0].enabled = false;
            intersectionSemaphores[3].greenLights[1].enabled = false;
            intersectionSemaphores[1].yellowLights[0].enabled = true;
            intersectionSemaphores[3].yellowLights[0].enabled = true;
            intersectionSemaphores[3].yellowLights[1].enabled = true;
        }
        else if (semaphoreTime >= semaphoreTimerMainLane && semaphoreTime <= 2 * semaphoreTimerMainLane && semaphoreTurn != 1)
        {
            semaphoreTurn = 1;
            yellowLightOn = false;
            intersectionSemaphores[0].yellowLights[0].enabled = false;
            intersectionSemaphores[0].redLights[0].enabled = true;

            intersectionSemaphores[1].redLights[0].enabled = false;
            intersectionSemaphores[3].redLights[0].enabled = false;
            intersectionSemaphores[3].redLights[1].enabled = false;
            intersectionSemaphores[1].greenLights[0].enabled = true;
            intersectionSemaphores[3].greenLights[0].enabled = true;
            intersectionSemaphores[3].greenLights[1].enabled = true;

            if (numberCarsWaiting > 0)
            {
                if (intersectionQueue2Lane[1, 0] != null && intersectionQueue2Lane[0, 0].intersectionDirection != LEFT)
                {
                    car = intersectionQueue2Lane[1, 0];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[1, 0] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[1, 1] != null && intersectionQueue2Lane[0, 1].intersectionDirection != LEFT)
                {
                    car = intersectionQueue2Lane[1, 1];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[1, 1] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[3, 0] != null)
                {
                    car = intersectionQueue2Lane[3, 0];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[3, 0] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[3, 1] != null)
                {
                    car = intersectionQueue2Lane[3, 1];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[3, 1] = null;
                    numberCarsWaiting--;
                }
            }
        }
        else if (semaphoreTime >= 2 * semaphoreTimerMainLane + semaphoreTimerLeftLane - 3 && semaphoreTime <= 2 * semaphoreTimerMainLane + semaphoreTimerLeftLane && !yellowLightOn)
        {
            semaphoreTurn = 2;
            yellowLightOn = true;
            intersectionSemaphores[1].greenLights[1].enabled = false;
            intersectionSemaphores[1].yellowLights[1].enabled = true;
        }
        else if (semaphoreTime >= 2 * semaphoreTimerMainLane && semaphoreTime <= 2 * semaphoreTimerMainLane + semaphoreTimerLeftLane && semaphoreTurn != 2)
        {
            semaphoreTurn = 2;
            yellowLightOn = false;
            intersectionSemaphores[1].yellowLights[0].enabled = false;
            intersectionSemaphores[3].yellowLights[0].enabled = false;
            intersectionSemaphores[3].yellowLights[1].enabled = false;
            intersectionSemaphores[1].redLights[0].enabled = true;
            intersectionSemaphores[3].redLights[0].enabled = true;
            intersectionSemaphores[3].redLights[1].enabled = true;

            intersectionSemaphores[1].redLights[1].enabled = false;
            intersectionSemaphores[1].greenLights[1].enabled = true;

            if (numberCarsWaiting > 0)
            {
                if (intersectionQueue2Lane[1, 0] != null && intersectionQueue2Lane[1, 0].intersectionDirection == LEFT)
                {
                    car = intersectionQueue2Lane[1, 0];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[1, 0] = null;
                    numberCarsWaiting--;
                }
                if (intersectionQueue2Lane[1, 1] != null && intersectionQueue2Lane[1, 1].intersectionDirection == LEFT)
                {
                    car = intersectionQueue2Lane[1, 1];
                    intersectionManager(car, car.intersectionEnterId);
                    intersectionQueue2Lane[1, 1] = null;
                    numberCarsWaiting--;
                }
            }
        }
        else if (semaphoreTime >= 2 * semaphoreTimerMainLane + semaphoreTimerLeftLane)
        {
            semaphoreTime = 0;
        }
    }


    private int calculateRoad(int number)
    {
        if (number == -1)
        {
            return 3;
        }
        return number;
    }


}
