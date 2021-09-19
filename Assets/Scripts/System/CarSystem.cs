using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;

[UpdateInGroup(typeof(InitializationSystemGroup))]
class CarSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float time = Time.DeltaTime;
        Entities
            .WithoutBurst()
            .ForEach((DynamicBuffer<ListNode> ListNode, ref CarPosition position, ref CarDestination destination, ref Translation translation, ref Rotation rotation, ref VehicleSpeed speed, in LocalToWorld ltw) =>
            {

                if (position.currentNode == ListNode.Length - 1)
                {
                    Node startWaypoint = new Node(), endWaypoint = new Node();

                    GameObject[] waypointsNew = GameObject.FindGameObjectsWithTag("CarWaypoint");
                    List<Node> nodesNew = new List<Node>();
                    foreach (GameObject w in waypointsNew)
                    {
                        if (w.GetComponent<Node>() != null && !w.GetComponent<Node>().isParkingSpot)
                        {
                            if (ListNode[ListNode.Length - 1].listNodesTransform.Equals((float3)w.transform.position)) //find start waypoint
                            {
                                startWaypoint = w.GetComponent<Node>();
                            }
                            /*
                            if (ListNode[ListNode.Length - 1].listNodesTransform.Equals((float3)w.transform.position)) //find end waypoint
                            {
                                endWaypoint = w.GetComponent<Node>();
                            }*/

                            nodesNew.Add(w.GetComponent<Node>());
                        }
                    }

                    int randomSrcNode = (int)UnityEngine.Random.Range(0, nodesNew.Count - 1);

                    /*Street s = nodesNew[randomSrcNode].GetComponentInParent<Street>();

                    while (!s.hasBusStop && s.isSemaphoreIntersection && s.isSimpleIntersection)
                    {
                        randomSrcNode = (int)UnityEngine.Random.Range(0, nodesNew.Count - 1);
                        s = nodesNew[randomSrcNode].GetComponentInParent<Street>();
                    }*/

                    endWaypoint = nodesNew[randomSrcNode];

                    /** NEW PATH **/
                    Path path = new Path();
                    List<Node> carPath = new List<Node>();
                    
                    carPath = path.findShortestPath(startWaypoint.transform, endWaypoint.transform);
                    ListNode.Clear();

                    for (int i = 0; i < carPath.Count; i++)
                    {
                        ListNode.Add(new ListNode { listNodesTransform = carPath[i].transform.position});

                    }

                    position.currentNode = 0;
                }

                position.carPosition = translation.Value;

                destination.position = ListNode[position.currentNode].listNodesTransform;

                float3 direction = destination.position - position.carPosition;
                //translation.Value = Vector3.Lerp(position.carPosition, destination.position, 0.008f);

                translation.Value  += ltw.Forward * time * 2;

                if (math.distance(translation.Value, destination.position) > 2.7f)
                {
                    rotation.Value = Quaternion.LookRotation(direction);
                }

                if ((math.distance(translation.Value, destination.position) < 1f)) 
                    position.currentNode++;

            }).Run();
    }


    void Stop(PhysicsVelocity p, bool status) //to implement
    {
        float3 v = p.Linear;
        if (status)
            p.Linear = new float3(0, 0, 0);
        else
            p.Linear = v;
    }


}
