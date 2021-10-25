using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Street : MonoBehaviour
{
    [Header("Street data")]
    public List<Node> carWaypoints;
    public List<Node> spawnWaypoints;
    public bool isSimpleIntersection;
    public bool isSemaphoreIntersection;
    public bool isTBoneIntersection;
    public bool hasBusStop;
    public Node busStopNode;
    public bool isLaneAdapter;
    public bool isCurve;
    public bool isDeadend;
    public int numberLanes;

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

    //private void OnDrawGizmos()
    //{
    //    foreach (Node n in carWaypoints)
    //    {
    //        Gizmos.color = Color.black;
    //        Gizmos.DrawSphere(n.transform.position, 0.5f);
    //    }
    //}

    void Start()
    {
        if (isSemaphoreIntersection)
        {
            if (isTBoneIntersection)
            {
                intersectionSemaphores[1].redLights[0].enabled = true;
                intersectionSemaphores[1].redLights[1].enabled = true;
                intersectionSemaphores[3].redLights[0].enabled = true;
                intersectionSemaphores[3].redLights[1].enabled = true;
            }
            else
            {
                intersectionSemaphores[1].redLights[0].enabled = true;
                intersectionSemaphores[1].redLights[1].enabled = true;
                intersectionSemaphores[2].redLights[0].enabled = true;
                intersectionSemaphores[2].redLights[1].enabled = true;
                intersectionSemaphores[3].redLights[0].enabled = true;
                intersectionSemaphores[3].redLights[1].enabled = true;
            }
        }
    }

    private void Update()
    {
        if (isSemaphoreIntersection)
        {
            if (isTBoneIntersection)
            {
                Semaphores3Way();
            }
            else
            {
                Semaphores4Way();
            }
        }
    }


    private void Semaphores4Way()
    {
        float elapsedTime = (Time.time / 25f) % 4f;

        if (elapsedTime < 0.8f)
        {
            intersectionSemaphores[3].yellowLights[0].enabled = false;
            intersectionSemaphores[3].yellowLights[1].enabled = false;
            intersectionSemaphores[3].redLights[0].enabled = true;
            intersectionSemaphores[3].redLights[1].enabled = true;
            intersectionSemaphores[0].redLights[0].enabled = false;
            intersectionSemaphores[0].redLights[1].enabled = false;
            intersectionSemaphores[0].greenLights[0].enabled = true;
            intersectionSemaphores[0].greenLights[1].enabled = true;
        }
        else if (elapsedTime < 1f)
        {
            intersectionSemaphores[0].greenLights[0].enabled = false;
            intersectionSemaphores[0].greenLights[1].enabled = false;
            intersectionSemaphores[0].yellowLights[0].enabled = true;
            intersectionSemaphores[0].yellowLights[1].enabled = true;
        }
        else if (elapsedTime < 1.8f)
        {
            intersectionSemaphores[0].yellowLights[0].enabled = false;
            intersectionSemaphores[0].yellowLights[1].enabled = false;
            intersectionSemaphores[0].redLights[0].enabled = true;
            intersectionSemaphores[0].redLights[1].enabled = true;
            intersectionSemaphores[1].redLights[0].enabled = false;
            intersectionSemaphores[1].redLights[1].enabled = false;
            intersectionSemaphores[1].greenLights[0].enabled = true;
            intersectionSemaphores[1].greenLights[1].enabled = true;
        }
        else if (elapsedTime < 2f)
        {
            intersectionSemaphores[1].greenLights[0].enabled = false;
            intersectionSemaphores[1].greenLights[1].enabled = false;
            intersectionSemaphores[1].yellowLights[0].enabled = true;
            intersectionSemaphores[1].yellowLights[1].enabled = true;
        }
        else if (elapsedTime < 2.8f)
        {
            intersectionSemaphores[1].yellowLights[0].enabled = false;
            intersectionSemaphores[1].yellowLights[1].enabled = false;
            intersectionSemaphores[1].redLights[0].enabled = true;
            intersectionSemaphores[1].redLights[1].enabled = true;
            intersectionSemaphores[2].redLights[0].enabled = false;
            intersectionSemaphores[2].redLights[1].enabled = false;
            intersectionSemaphores[2].greenLights[0].enabled = true;
            intersectionSemaphores[2].greenLights[1].enabled = true;
        }
        else if (elapsedTime < 3f)
        {
            intersectionSemaphores[2].greenLights[0].enabled = false;
            intersectionSemaphores[2].greenLights[1].enabled = false;
            intersectionSemaphores[2].yellowLights[0].enabled = true;
            intersectionSemaphores[2].yellowLights[1].enabled = true;
        }
        else if (elapsedTime < 3.8f)
        {
            intersectionSemaphores[2].yellowLights[0].enabled = false;
            intersectionSemaphores[2].yellowLights[1].enabled = false;
            intersectionSemaphores[2].redLights[0].enabled = true;
            intersectionSemaphores[2].redLights[1].enabled = true;
            intersectionSemaphores[3].redLights[0].enabled = false;
            intersectionSemaphores[3].redLights[1].enabled = false;
            intersectionSemaphores[3].greenLights[0].enabled = true;
            intersectionSemaphores[3].greenLights[1].enabled = true;
        }
        else
        {
            intersectionSemaphores[3].greenLights[0].enabled = false;
            intersectionSemaphores[3].greenLights[1].enabled = false;
            intersectionSemaphores[3].yellowLights[0].enabled = true;
            intersectionSemaphores[3].yellowLights[1].enabled = true;
        }
    }

    private void Semaphores3Way()
    {
        float elapsedTime = (Time.time / 25f) % 3f;

        if (elapsedTime < 0.8f)
        {
            intersectionSemaphores[3].yellowLights[0].enabled = false;
            intersectionSemaphores[3].yellowLights[1].enabled = false;
            intersectionSemaphores[3].redLights[0].enabled = true;
            intersectionSemaphores[3].redLights[1].enabled = true;
            intersectionSemaphores[0].redLights[0].enabled = false;
            intersectionSemaphores[0].redLights[1].enabled = false;
            intersectionSemaphores[0].greenLights[0].enabled = true;
            intersectionSemaphores[0].greenLights[1].enabled = true;
        }
        else if (elapsedTime < 1f)
        {
            intersectionSemaphores[0].greenLights[0].enabled = false;
            intersectionSemaphores[0].greenLights[1].enabled = false;
            intersectionSemaphores[0].yellowLights[0].enabled = true;
            intersectionSemaphores[0].yellowLights[1].enabled = true;
        }
        else if (elapsedTime < 1.8f)
        {
            intersectionSemaphores[0].yellowLights[0].enabled = false;
            intersectionSemaphores[0].yellowLights[1].enabled = false;
            intersectionSemaphores[0].redLights[0].enabled = true;
            intersectionSemaphores[0].redLights[1].enabled = true;
            intersectionSemaphores[1].redLights[0].enabled = false;
            intersectionSemaphores[1].redLights[1].enabled = false;
            intersectionSemaphores[1].greenLights[0].enabled = true;
            intersectionSemaphores[1].greenLights[1].enabled = true;
        }
        else if (elapsedTime < 2f)
        {
            intersectionSemaphores[1].greenLights[0].enabled = false;
            intersectionSemaphores[1].greenLights[1].enabled = false;
            intersectionSemaphores[1].yellowLights[0].enabled = true;
            intersectionSemaphores[1].yellowLights[1].enabled = true;
        }
        else if (elapsedTime < 2.8f)
        {
            intersectionSemaphores[1].yellowLights[0].enabled = false;
            intersectionSemaphores[1].yellowLights[1].enabled = false;
            intersectionSemaphores[1].redLights[0].enabled = true;
            intersectionSemaphores[1].redLights[1].enabled = true;
            intersectionSemaphores[3].redLights[0].enabled = false;
            intersectionSemaphores[3].redLights[1].enabled = false;
            intersectionSemaphores[3].greenLights[0].enabled = true;
            intersectionSemaphores[3].greenLights[1].enabled = true;
        }
        else if (elapsedTime < 3f)
        {
            intersectionSemaphores[3].greenLights[0].enabled = false;
            intersectionSemaphores[3].greenLights[1].enabled = false;
            intersectionSemaphores[3].yellowLights[0].enabled = true;
            intersectionSemaphores[3].yellowLights[1].enabled = true;
        }
    }

}

