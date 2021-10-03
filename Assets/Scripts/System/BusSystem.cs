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
class BusSystem : SystemBase
{

    private const int LANE_CHANGE = 1;
    private const int BUS_STOP = 1;

    protected override void OnUpdate()
    {
        int elapsedTime = (int)UnityEngine.Time.time;

        float time = Time.DeltaTime;

        Entities
            .WithoutBurst()
            .ForEach((DynamicBuffer<NodesPositionList> NodesPositionList, DynamicBuffer<NodesTypeList> NodesTypeList, ref VehicleNavigation navigation, ref Translation translation, ref Rotation rotation, ref VehicleSpeed speed, in Bus bus, in LocalToWorld ltw) =>
            {
                //detect bus stop
                if (navigation.busStop && elapsedTime >= navigation.timeExitBusStop)
                {
                    navigation.busStop = false;
                    navigation.timeExitBusStop = int.MaxValue;
                }

                //semaphore crossing turn
                if (navigation.intersectionStop && navigation.isSemaphoreIntersection)
                {
                    float preciseCrossingTurn = (elapsedTime / 15f) % (float)navigation.intersectionNumRoads;
                    int actualCrossingTurn = -1;
                    if (preciseCrossingTurn % 1 <= 0.8f)
                    {
                        actualCrossingTurn = (int)preciseCrossingTurn;
                    }

                    if (navigation.intersectionNumRoads == 3 && actualCrossingTurn == 2)
                    {
                        actualCrossingTurn = 3;
                    }

                    if (navigation.intersectionDirection == actualCrossingTurn)
                    {
                        navigation.intersectionStop = false;
                        navigation.intersectionCrossing = true;
                    }
                }
                //moving vehicle collision avoidance
                else if (!navigation.intersectionStop && !navigation.busStop)
                {
                    NativeHashMap<int, char> carsPosition = CarsPositionSystem.carsPositionMap;
                    int lookaheadLength = 2;
                    for (int i = 1; i <= lookaheadLength; i++)
                    {
                        //Debug.DrawLine(translation.Value, translation.Value + ltw.Forward * i, Color.white, 0.1f, false);
                        int positionKey = CarsPositionSystem.GetPositionHashMapKey(translation.Value + ltw.Forward * i);
                        if (carsPosition.ContainsKey(positionKey))
                        {
                            navigation.trafficStop = true;
                        }
                        else
                        {
                            navigation.trafficStop = false;
                        }
                    }

                    if (NodesTypeList[navigation.currentNode - 1].nodeType == LANE_CHANGE || NodesTypeList[navigation.currentNode].nodeType == LANE_CHANGE)
                    {
                        navigation.isChangingLanes = true;

                        int positionKey1, positionKey2;

                        float3 leftDirection = (-1) * ltw.Right;
                        float3 leftDiagDirection = leftDirection + ltw.Forward;
                        float3 rightDirection = ltw.Right;
                        float3 rightDiagDirection = ltw.Right + ltw.Forward;
                        float3 carRotation = ((Quaternion)rotation.Value).eulerAngles;

                        //int multSide = 1, multDiag = 1;


                        //Get angle between 0 and 360
                        float carAngle = carRotation.y - Mathf.CeilToInt(carRotation.y / 360f) * 360f;
                        if (carAngle < 0)
                        {
                            carAngle += 360f;
                        }
                        ////Debug.Log(carAngle);
                        if (carAngle >= 65 && carAngle <= 75)    //LEFT -> RIGHT lanechange (go left lane)
                        {
                            int3 curr = (int3)(translation.Value);
                            int3 left = (int3)(translation.Value + leftDirection);
                            int3 leftDiag = (int3)(translation.Value + leftDiagDirection);
                            //Debug.DrawLine(translation.Value, translation.Value + leftDirection, Color.white, 0.1f, false);
                            //Debug.DrawLine(translation.Value, translation.Value + leftDiagDirection, Color.white, 0.1f, false);
                            positionKey1 = CarsPositionSystem.GetPositionHashMapKey(translation.Value + leftDirection);
                            positionKey2 = CarsPositionSystem.GetPositionHashMapKey(translation.Value + leftDiagDirection);
                            if ((!curr.Equals(left) && carsPosition.ContainsKey(positionKey1)) || (!curr.Equals(leftDiag) && carsPosition.ContainsKey(positionKey2)))
                            {
                                navigation.trafficStop = true;
                                if (carsPosition.ContainsKey(positionKey1))
                                {
                                    //Debug.DrawLine(translation.Value, translation.Value + leftDirection, Color.red, 0.1f, false);
                                }
                                if (carsPosition.ContainsKey(positionKey1))
                                {
                                    //Debug.DrawLine(translation.Value, translation.Value + leftDiagDirection, Color.red, 0.1f, false);
                                }
                            }
                            else
                            {
                                navigation.trafficStop = false;
                            }
                        }
                        else if (carAngle >= 100 && carAngle <= 110)    //LEFT -> RIGHT lanechange (go right lane)
                        {
                            int3 curr = (int3)(translation.Value);
                            int3 right = (int3)(translation.Value + rightDirection);
                            int3 rightDiag = (int3)(translation.Value + rightDiagDirection);
                            //Debug.DrawLine(translation.Value, translation.Value + rightDirection, Color.white, 0.1f, false);
                            //Debug.DrawLine(translation.Value, translation.Value + rightDiagDirection, Color.white, 0.1f, false);
                            positionKey1 = CarsPositionSystem.GetPositionHashMapKey(translation.Value + rightDirection);
                            positionKey2 = CarsPositionSystem.GetPositionHashMapKey(translation.Value + rightDiagDirection);
                            if ((!curr.Equals(right) && carsPosition.ContainsKey(positionKey1)) || (!curr.Equals(rightDiag) && carsPosition.ContainsKey(positionKey2)))
                            {
                                navigation.trafficStop = true;
                                if (carsPosition.ContainsKey(positionKey1))
                                {
                                    //Debug.DrawLine(translation.Value, translation.Value + rightDirection, Color.red, 0.1f, false);
                                }
                                if (carsPosition.ContainsKey(positionKey1))
                                {
                                    //Debug.DrawLine(translation.Value, translation.Value + rightDiagDirection, Color.red, 0.1f, false);
                                }
                            }
                            else
                            {
                                navigation.trafficStop = false;
                            }
                        }
                        else if (carAngle >= 250 && carAngle <= 260)    //RIGHT -> LEFT lanechange (go to left lane)
                        {
                            int3 curr = (int3)(translation.Value);
                            int3 left = (int3)(translation.Value + leftDirection);
                            int3 leftDiag = (int3)(translation.Value + leftDiagDirection);
                            //Debug.DrawLine(translation.Value, translation.Value + leftDirection, Color.white, 0.1f, false);
                            //Debug.DrawLine(translation.Value, translation.Value + leftDiagDirection, Color.white, 0.1f, false);
                            positionKey1 = CarsPositionSystem.GetPositionHashMapKey(translation.Value + leftDirection);
                            positionKey2 = CarsPositionSystem.GetPositionHashMapKey(translation.Value + leftDiagDirection);
                            if ((!curr.Equals(left) && carsPosition.ContainsKey(positionKey1)) || (!curr.Equals(leftDiag) && carsPosition.ContainsKey(positionKey2)))
                            {
                                navigation.trafficStop = true;
                                if (carsPosition.ContainsKey(positionKey1))
                                {
                                    //Debug.DrawLine(translation.Value, translation.Value + leftDirection, Color.red, 0.1f, false);
                                }
                                if (carsPosition.ContainsKey(positionKey1))
                                {
                                    //Debug.DrawLine(translation.Value, translation.Value + leftDiagDirection, Color.red, 0.1f, false);
                                }
                            }
                            else
                            {
                                navigation.trafficStop = false;
                            }
                        }
                        else if (carAngle >= 280 && carAngle <= 290)    //RIGHT -> LEFT lanechange (go to right lane)
                        {
                            int3 curr = (int3)(translation.Value);
                            int3 right = (int3)(translation.Value + rightDirection);
                            int3 rightDiag = (int3)(translation.Value + rightDiagDirection);
                            //Debug.DrawLine(translation.Value, translation.Value + rightDirection, Color.white, 0.1f, false);
                            //Debug.DrawLine(translation.Value, translation.Value + rightDiagDirection, Color.white, 0.1f, false);
                            positionKey1 = CarsPositionSystem.GetPositionHashMapKey(translation.Value + rightDirection);
                            positionKey2 = CarsPositionSystem.GetPositionHashMapKey(translation.Value + rightDiagDirection);
                            if ((!curr.Equals(right) && carsPosition.ContainsKey(positionKey1)) || (!curr.Equals(rightDiag) && carsPosition.ContainsKey(positionKey2)))
                            {
                                navigation.trafficStop = true;
                                if (carsPosition.ContainsKey(positionKey1))
                                {
                                    //Debug.DrawLine(translation.Value, translation.Value + rightDirection, Color.red, 0.1f, false);
                                }
                                if (carsPosition.ContainsKey(positionKey1))
                                {
                                    //Debug.DrawLine(translation.Value, translation.Value + rightDiagDirection, Color.red, 0.1f, false);
                                }
                            }
                            else
                            {
                                navigation.trafficStop = false;
                            }
                        }
                        else if (carAngle >= 160 && carAngle <= 170)    //TOP -> BOTTOM lanechange (go to left lane)
                        {
                            int3 curr = (int3)(translation.Value);
                            int3 left = (int3)(translation.Value + leftDirection);
                            int3 leftDiag = (int3)(translation.Value + leftDiagDirection);
                            //Debug.DrawLine(translation.Value, translation.Value + leftDirection, Color.white, 0.1f, false);
                            //Debug.DrawLine(translation.Value, translation.Value + leftDiagDirection, Color.white, 0.1f, false);
                            positionKey1 = CarsPositionSystem.GetPositionHashMapKey(translation.Value + leftDirection);
                            positionKey2 = CarsPositionSystem.GetPositionHashMapKey(translation.Value + leftDiagDirection);
                            if ((!curr.Equals(left) && carsPosition.ContainsKey(positionKey1)) || (!curr.Equals(leftDiag) && carsPosition.ContainsKey(positionKey2)))
                            {
                                navigation.trafficStop = true;
                                if (carsPosition.ContainsKey(positionKey1))
                                {
                                    //Debug.DrawLine(translation.Value, translation.Value + leftDirection, Color.red, 0.1f, false);
                                }
                                if (carsPosition.ContainsKey(positionKey1))
                                {
                                    //Debug.DrawLine(translation.Value, translation.Value + leftDiagDirection, Color.red, 0.1f, false);
                                }
                            }
                            else
                            {
                                navigation.trafficStop = false;
                            }
                        }
                        else if (carAngle >= 190 && carAngle <= 200)    //TOP -> BOTTOM lanechange (go to right lane)
                        {
                            int3 curr = (int3)(translation.Value);
                            int3 right = (int3)(translation.Value + rightDirection);
                            int3 rightDiag = (int3)(translation.Value + rightDiagDirection);
                            //Debug.DrawLine(translation.Value, translation.Value + rightDirection, Color.white, 0.1f, false);
                            //Debug.DrawLine(translation.Value, translation.Value + rightDiagDirection, Color.white, 0.1f, false);
                            positionKey1 = CarsPositionSystem.GetPositionHashMapKey(translation.Value + rightDirection);
                            positionKey2 = CarsPositionSystem.GetPositionHashMapKey(translation.Value + rightDiagDirection);
                            if ((!curr.Equals(right) && carsPosition.ContainsKey(positionKey1)) || (!curr.Equals(rightDiag) && carsPosition.ContainsKey(positionKey2)))
                            {
                                navigation.trafficStop = true;
                                if (carsPosition.ContainsKey(positionKey1))
                                {
                                    //Debug.DrawLine(translation.Value, translation.Value + rightDirection, Color.red, 0.1f, false);
                                }
                                if (carsPosition.ContainsKey(positionKey1))
                                {
                                    //Debug.DrawLine(translation.Value, translation.Value + rightDiagDirection, Color.red, 0.1f, false);
                                }
                            }
                            else
                            {
                                navigation.trafficStop = false;
                            }
                        }
                        else if (carAngle >= 340 && carAngle <= 350)    //BOTTOM -> TOP lanechange (go to left lane)
                        {
                            int3 curr = (int3)(translation.Value);
                            int3 left = (int3)(translation.Value + leftDirection);
                            int3 leftDiag = (int3)(translation.Value + leftDiagDirection);
                            //Debug.DrawLine(translation.Value, translation.Value + leftDirection, Color.white, 0.1f, false);
                            //Debug.DrawLine(translation.Value, translation.Value + leftDiagDirection, Color.white, 0.1f, false);
                            positionKey1 = CarsPositionSystem.GetPositionHashMapKey(translation.Value + leftDirection);
                            positionKey2 = CarsPositionSystem.GetPositionHashMapKey(translation.Value + leftDiagDirection);
                            if ((!curr.Equals(left) && carsPosition.ContainsKey(positionKey1)) || (!curr.Equals(leftDiag) && carsPosition.ContainsKey(positionKey2)))
                            {
                                navigation.trafficStop = true;
                                if (carsPosition.ContainsKey(positionKey1))
                                {
                                    //Debug.DrawLine(translation.Value, translation.Value + leftDirection, Color.red, 0.1f, false);
                                }
                                if (carsPosition.ContainsKey(positionKey1))
                                {
                                    //Debug.DrawLine(translation.Value, translation.Value + leftDiagDirection, Color.red, 0.1f, false);
                                }
                            }
                            else
                            {
                                navigation.trafficStop = false;
                            }
                        }
                        else if (carAngle >= 10 && carAngle <= 20)    //BOTTOM -> TOP lanechange (go to right lane)
                        {
                            int3 curr = (int3)(translation.Value);
                            int3 right = (int3)(translation.Value + rightDirection);
                            int3 rightDiag = (int3)(translation.Value + rightDiagDirection);
                            //Debug.DrawLine(translation.Value, translation.Value + rightDirection, Color.white, 0.1f, false);
                            //Debug.DrawLine(translation.Value, translation.Value + rightDiagDirection, Color.white, 0.1f, false);
                            positionKey1 = CarsPositionSystem.GetPositionHashMapKey(translation.Value + rightDirection);
                            positionKey2 = CarsPositionSystem.GetPositionHashMapKey(translation.Value + rightDiagDirection);
                            if ((!curr.Equals(right) && carsPosition.ContainsKey(positionKey1)) || (!curr.Equals(rightDiag) && carsPosition.ContainsKey(positionKey2)))
                            {
                                navigation.trafficStop = true;
                                if (carsPosition.ContainsKey(positionKey1))
                                {
                                    //Debug.DrawLine(translation.Value, translation.Value + rightDirection, Color.red, 0.1f, false);
                                }
                                if (carsPosition.ContainsKey(positionKey1))
                                {
                                    //Debug.DrawLine(translation.Value, translation.Value + rightDiagDirection, Color.red, 0.1f, false);
                                }
                            }
                            else
                            {
                                navigation.trafficStop = false;
                            }
                        }
                    }
                    else
                    {
                        navigation.isChangingLanes = false;
                    }
                }

                if (navigation.trafficStop || navigation.intersectionStop || navigation.busStop)  //STOPPING IN TRAFFIC OR INTERSECTION
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
                    float3 direction = NodesPositionList[navigation.currentNode].nodePosition - translation.Value;
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
                    float3 nextNodeDirection = Unity.Mathematics.math.normalize((NodesPositionList[navigation.currentNode].nodePosition - translation.Value));
                    translation.Value += nextNodeDirection * time * speed.currentSpeed;

                    float3 direction = NodesPositionList[navigation.currentNode].nodePosition - translation.Value;
                    float3 neededRotation = Quaternion.LookRotation(direction).eulerAngles;

                    rotation.Value = Quaternion.Euler(neededRotation);
                }

                if (math.distance(translation.Value, NodesPositionList[navigation.currentNode].nodePosition) < 0.3f)
                {
                    navigation.currentNode++;
                    //make bus move on an infinite loop
                    if (navigation.currentNode == NodesPositionList.Length)
                    {
                        navigation.currentNode = 1;
                    }
                    if (NodesTypeList[navigation.currentNode].nodeType == BUS_STOP)
                    {
                        navigation.busStop = true;
                        navigation.timeExitBusStop = elapsedTime + 10;
                    }
                }

            }).ScheduleParallel();
    }
}