using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;


[UpdateAfter(typeof(IntersectionPrecedenceSystem))]
[UpdateAfter(typeof(CarsPositionSystem))]
class CarSystem : SystemBase
{

    private const int LANE_CHANGE = 1;
    private const int BUS_STOP = 2;
    private const int BUS_MERGE = 3;
    private const int INTERSECTION = 4;
    private const int MERGE_LEFT = 5;
    private const int MERGE_RIGHT = 6;

    private EndSimulationEntityCommandBufferSystem m_EndSimulationEcbSystem;


    protected override void OnCreate()
    {
        base.OnCreate();
        // Find the ECB system once and store it for later usage
        m_EndSimulationEcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    public int GetParkingPositionHashMapKey(float3 position)
    {
        int xMultiplier = 100000;
        int xPosition = (int)position.x;
        int zPosition = (int)position.z;
        return xPosition * xMultiplier + zPosition;
    }

    protected override void OnUpdate()
    {
        var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer().ToConcurrent();

        int elapsedTime = (int)UnityEngine.Time.time;

        float time = Time.DeltaTime;
        float timeScale = GameObject.Find("TimeScale").GetComponent<TimeScale>().timeScale;


        Entities
            .WithoutBurst()
            .ForEach((Entity entity, DynamicBuffer<NodesList> NodesList, ref PathFinding pathFinding, ref VehicleNavigation navigation, ref Translation translation, ref Rotation rotation, ref VehicleSpeed speed, in LocalToWorld ltw) =>
            {

                if (navigation.isBus)
                {
                    return;
                }

                if (NodesList.Length == 0 || pathFinding.spawnParking) return;

                /* Parking System */
                if (navigation.isParked && elapsedTime < navigation.timeExitParking)
                {
                    return;
                }
                else if (navigation.isParked && elapsedTime >= navigation.timeExitParking)
                {
                    int keyPos1 = CarsPositionSystem.GetPositionHashMapKey(navigation.destinationNodePosition);
                    int keyPos2 = CarsPositionSystem.GetPositionHashMapKey(navigation.destinationNodePosition + ltw.Forward);
                    int keyPos3 = CarsPositionSystem.GetPositionHashMapKey(navigation.destinationNodePosition + (-1) * ltw.Forward);
                    if (CarsPositionSystem.carsPositionMap.ContainsKey(keyPos1) || CarsPositionSystem.carsPositionMap.ContainsKey(keyPos2) || CarsPositionSystem.carsPositionMap.ContainsKey(keyPos3))
                    {
                        return;
                    }
                    else
                    {
                        //Debug.Log("Parking exit");
                        NeedPath needPath = new NeedPath() { };
                        ecb.AddComponent(entity.Index, entity, needPath);
                        pathFinding.spawnParking = true;
                        translation.Value = pathFinding.startingNodePosition;
                        navigation.needParking = false;
                        navigation.isParked = false;
                        navigation.timeExitParking = int.MaxValue;
                        navigation.currentNode = 1;
                        return;
                    }
                }

                if (navigation.needParking)
                {
                    //Debug.Log("Parked");
                    int hashMapKey = CarsPositionSystem.GetPositionHashMapKey(navigation.startingNodePosition);

                    navigation.isParked = true;
                    navigation.needParking = false;

                    translation.Value = pathFinding.parkingNodePosition;

                    var rnd = new Unity.Mathematics.Random((uint)entity.Index*100000);

                    pathFinding.startingNodePosition = pathFinding.destinationNodePosition;

                    navigation.timeExitParking = elapsedTime + rnd.NextInt(15, 200);
                    return;
                }

                if (navigation.currentNode == NodesList.Length - 1 && math.distance(translation.Value, NodesList[navigation.currentNode].nodePosition) < 0.3f)
                {
                    navigation.needParking = true;
                    return;
                }

                if (navigation.intersectionStop && navigation.isSemaphoreIntersection)
                {
                    float preciseCrossingTurn = (elapsedTime / 25f) % (float)navigation.intersectionNumRoads;
                    int actualCrossingTurn = -1;
                    if (preciseCrossingTurn % 1 <= 0.8f)
                    {
                        actualCrossingTurn = (int)preciseCrossingTurn;
                    }

                    if (navigation.intersectionNumRoads == 3 && actualCrossingTurn == 2)
                    {
                        actualCrossingTurn = 3;
                    }

                    if (navigation.intersectionDirection == actualCrossingTurn && (!CarsPositionSystem.intersectionCrossingMap.TryGetValue(navigation.intersectionId, out int crossingDirection) || crossingDirection == navigation.intersectionDirection))
                    {
                        navigation.intersectionStop = false;
                        navigation.intersectionCrossing = true;
                    }
                }

                if (!navigation.intersectionStop)
                {
                    NativeHashMap<int, char> carsPosition = CarsPositionSystem.carsPositionMap;
                    float3 startPosition;
                    int positionKey;
                    bool trafficStoped = false;
                    float multiplier = 1f;
                    if (navigation.currentNode < NodesList.Length - 1 &&
                        NodesList[navigation.currentNode].nodeType == INTERSECTION &&
                        math.distance(translation.Value, NodesList[navigation.currentNode].nodePosition) < 1f)
                    {

                        startPosition = NodesList[navigation.currentNode].nodePosition;
                        if (!((int3)startPosition).Equals((int3)translation.Value))
                        {
                            //Debug.DrawLine(translation.Value, startPosition, Color.green, 0.1f, false);
                            positionKey = CarsPositionSystem.GetPositionHashMapKey(startPosition);
                            if (carsPosition.ContainsKey(positionKey))
                            {
                                navigation.trafficStop = true;
                                trafficStoped = true;
                            }
                            else
                            {
                                navigation.trafficStop = false;
                            }
                        }

                        float3 direction = math.normalize(NodesList[navigation.currentNode + 1].nodePosition - NodesList[navigation.currentNode].nodePosition);


                        if (((int3)startPosition).Equals((int3)(startPosition + direction)))
                        {
                            multiplier = 1.5f;
                        }

                        int lookaheadLength = 1;
                        for (int i = 1; i <= lookaheadLength && !trafficStoped; i++)
                        {
                            //Debug.DrawLine(startPosition, startPosition + direction * multiplier * i, Color.green, 0.1f, false);
                            positionKey = CarsPositionSystem.GetPositionHashMapKey(startPosition + direction * multiplier * i);
                            if (carsPosition.ContainsKey(positionKey))
                            {
                                navigation.trafficStop = true;
                                trafficStoped = true;
                                break;
                            }
                            else
                            {
                                navigation.trafficStop = false;
                            }
                        }
                    }
                    else
                    {
                        startPosition = translation.Value;

                        int lookaheadLength = 2;
                        for (int i = 1; i <= lookaheadLength; i++)
                        {
                            //Debug.DrawLine(translation.Value, translation.Value + ltw.Forward * i, Color.white, 0.1f, false);
                            int3 direction1 = (int3)(startPosition + 0.2f * ltw.Right + ltw.Forward * i);
                            int3 direction2 = (int3)(startPosition + (-0.2f) * ltw.Right + ltw.Forward * i);
                            if (!direction1.Equals((int3)startPosition))
                            {
                                int positionKey1 = CarsPositionSystem.GetPositionHashMapKey(startPosition + 0.2f * ltw.Right + ltw.Forward * i);
                                //Debug.DrawLine(startPosition + 0.2f * ltw.Right, startPosition + 0.2f * ltw.Right + ltw.Forward * i);
                                if (carsPosition.ContainsKey(positionKey1))
                                {
                                    navigation.trafficStop = true;
                                    trafficStoped = true;
                                    break;
                                }
                                else
                                {
                                    navigation.trafficStop = false;
                                }
                            }
                            if (!direction2.Equals((int3)startPosition))
                            {
                                //Debug.DrawLine(startPosition + (-0.2f) * ltw.Right, startPosition + (-0.2f) * ltw.Right + ltw.Forward * i);
                                int positionKey2 = CarsPositionSystem.GetPositionHashMapKey(startPosition + (-0.2f) * ltw.Right + ltw.Forward * i);
                                if (carsPosition.ContainsKey(positionKey2))
                                {
                                    navigation.trafficStop = true;
                                    trafficStoped = true;
                                    break;
                                }
                                else
                                {
                                    navigation.trafficStop = false;
                                }
                            }
                        }
                    }

                    if (navigation.currentNode - 1 >= 0)
                    {

                        if (NodesList[navigation.currentNode].nodeType == MERGE_LEFT &&
                            !(NodesList[navigation.currentNode].nodeType == LANE_CHANGE || NodesList[navigation.currentNode - 1].nodeType == LANE_CHANGE) &&
                            !trafficStoped)
                        {
                            float3 leftDirection = (-1) * ltw.Right;
                            float3 leftDiagDirection = leftDirection + ltw.Forward;
                            int3 curr = (int3)(translation.Value);
                            int3 leftDiag = (int3)(translation.Value + leftDiagDirection);
                            int3 left = (int3)(translation.Value + leftDirection);
                            int positionKey2 = CarsPositionSystem.GetPositionHashMapKey(translation.Value + leftDiagDirection);
                            int positionKey1 = CarsPositionSystem.GetPositionHashMapKey(translation.Value + leftDirection);

                            //Debug.DrawLine(translation.Value, translation.Value + leftDiagDirection, Color.white, 0.1f, false);
                            //Debug.DrawLine(translation.Value, translation.Value + leftDirection, Color.white, 0.1f, false);
                            if ((!curr.Equals(leftDiag) && carsPosition.ContainsKey(positionKey2)) && (!curr.Equals(left) && carsPosition.ContainsKey(positionKey1)))
                            {
                                navigation.trafficStop = true;
                                /*if (carsPosition.ContainsKey(positionKey2))
                                {
                                    //Debug.DrawLine(translation.Value, translation.Value + leftDiagDirection, Color.red, 0.1f, false);
                                }*/
                            }
                            else
                            {
                                navigation.trafficStop = false;
                            }
                        }
                        else if (NodesList[navigation.currentNode].nodeType == MERGE_RIGHT &&
                            !(NodesList[navigation.currentNode].nodeType == LANE_CHANGE || NodesList[navigation.currentNode - 1].nodeType == LANE_CHANGE) &&
                            !trafficStoped)
                        {
                            float3 rightDirection = ltw.Right;
                            float3 rightDiagDirection = rightDirection + ltw.Forward;
                            int3 curr = (int3)(translation.Value);
                            int3 rightDiag = (int3)(translation.Value + rightDiagDirection);
                            int3 right = (int3)(translation.Value + rightDirection);
                            int positionKey2 = CarsPositionSystem.GetPositionHashMapKey(translation.Value + rightDiagDirection);
                            int positionKey1 = CarsPositionSystem.GetPositionHashMapKey(translation.Value + rightDirection);
                            //Debug.DrawLine(translation.Value, translation.Value + rightDiagDirection, Color.white, 0.1f, false);
                            //Debug.DrawLine(translation.Value, translation.Value + rightDirection, Color.white, 0.1f, false);
                            if ((!curr.Equals(rightDiag) && carsPosition.ContainsKey(positionKey2)) || (!curr.Equals(right) && carsPosition.ContainsKey(positionKey1)))
                            {
                                navigation.trafficStop = true;
                                /*if (carsPosition.ContainsKey(positionKey2))
                                {
                                    //Debug.DrawLine(translation.Value, translation.Value + rightDiagDirection, Color.red, 0.1f, false);
                                }*/
                            }
                            else
                            {
                                navigation.trafficStop = false;
                            }
                        }

                    }
                    if (navigation.trafficStop || navigation.intersectionStop)  //STOPPING IN TRAFFIC OR INTERSECTION
                    {
                        if (speed.currentSpeed > 0.25)
                        {
                            speed.currentSpeed -= speed.maxSpeed * speed.speedDamping;
                            ////Debug.Log("STOPPING");
                        }
                        else
                        {
                            speed.currentSpeed = 0;
                        }
                        translation.Value += ltw.Forward * time * speed.currentSpeed;
                        float3 direction = NodesList[navigation.currentNode].nodePosition - translation.Value;
                        if (direction.Equals(new float3 (0f, 0f, 0f)))
                        {
                            //Debug.Log("parked");
                            navigation.isParked = true;
                            translation.Value = navigation.parkingNode;
                            return;
                        }
                        rotation.Value = Quaternion.LookRotation(direction);
                    }
                    else  //SPEEDING UP IF NO TRAFFIC OR MY TURN IN INTERSECTION
                    {
                        if (speed.currentSpeed < speed.maxSpeed)
                        {
                            speed.currentSpeed += speed.maxSpeed * speed.speedDamping;
                            ////Debug.Log("SPEEDING");
                        }
                        else
                        {
                            speed.currentSpeed = speed.maxSpeed;
                        }

                        /*if (navigation.currentNode < NodesList.Length - 1 && math.distance(translation.Value, NodesList[navigation.currentNode].nodePosition) < math.distance(translation.Value + 0.25f * ltw.Forward, NodesList[navigation.currentNode].nodePosition))
                        {
                            navigation.currentNode++;
                        }*/

                        if (navigation.currentNode < NodesList.Length - 1 && math.distance(translation.Value, NodesList[navigation.currentNode].nodePosition) < math.distance(translation.Value + ltw.Forward, NodesList[navigation.currentNode].nodePosition))
                        {
                            //Debug.Log(math.distance(translation.Value, NodesList[navigation.currentNode].nodePosition) + " vs " + math.distance(translation.Value + ltw.Forward, NodesList[navigation.currentNode].nodePosition));
                            navigation.currentNode++;
                        }

                        float3 nextNodeDirection = Unity.Mathematics.math.normalize((NodesList[navigation.currentNode].nodePosition - translation.Value));
                        translation.Value += nextNodeDirection * time * speed.currentSpeed;

                        float3 direction = NodesList[navigation.currentNode].nodePosition - translation.Value;

                        if (direction.Equals(0f))
                        {
                            //Debug.Log("parked");
                            navigation.isParked = true;
                            translation.Value = navigation.parkingNode;
                            return;
                        }
                        float3 neededRotation = Quaternion.LookRotation(direction).eulerAngles;

                        rotation.Value = Quaternion.Euler(neededRotation);
                    }

                    
                    if (math.distance(translation.Value, NodesList[navigation.currentNode].nodePosition) < 0.5f * timeScale && !navigation.needParking && navigation.currentNode < NodesList.Length - 1)
                    {
                        navigation.currentNode++;
                    }
                    
                }

            }).ScheduleParallel();

        //parkingNodes.Dispose();
        //cityParkings.Dispose();
        m_EndSimulationEcbSystem.AddJobHandleForProducer(this.Dependency);
    }
}