using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


public class IntersectionPrecedenceSystem : SystemBase
{

    public static NativeHashMap<int, bool> processedIntersections;

    protected override void OnCreate()
    {
        processedIntersections = new NativeHashMap<int, bool>(1250, Allocator.Persistent);
        base.OnCreate();
    }

    protected override void OnDestroy()
    {
        processedIntersections.Dispose();
        base.OnDestroy();
    }

    protected override void OnUpdate()
    {

        processedIntersections.Clear();
        NativeHashMap<int, int> intersectionCrossingMap = CarsPositionSystem.intersectionCrossingMap;
        NativeHashMap<int, int> intersectionQueueMap = CarsPositionSystem.intersectionQueueMap;

        Entities
            .WithoutBurst()
            .ForEach((ref VehicleNavigation navigation) =>
            {
                //iterate only cars that are waiting in intersection
                if (navigation.intersectionStop && navigation.isSimpleIntersection && !processedIntersections.ContainsKey(navigation.intersectionId))
                {
                    //3way simple intersections
                    if (navigation.intersectionNumRoads == 3)
                    {
                        //cars that have no right side traffic
                        if (navigation.intersectionDirection == 1)
                        {
                            int crossingCarsDirection = 1;
                            int actualCrossingTurn;
                            if (intersectionCrossingMap.TryGetValue(navigation.intersectionId, out actualCrossingTurn))
                            {
                                crossingCarsDirection = actualCrossingTurn;
                            }
                            if (crossingCarsDirection == 1)
                            {
                                navigation.intersectionStop = false;
                                navigation.intersectionCrossing = true;
                                processedIntersections.Add(navigation.intersectionId, true);
                                //Debug.Log("Crossing " + navigation.intersectionDirection);
                            }
                            //Debug.Log(navigation.intersectionDirection + " stopped by " + crossingCarsDirection);
                        }
                        //cars with right side traffic (but their paths will never cross)
                        else
                        {
                            int rightSideDirection = (navigation.intersectionDirection + 1) % 4;
                            int rightSideQueueKey = CarsPositionSystem.GetIntersectionQueueHashMapKey(navigation.intersectionId, rightSideDirection);

                            //right side is free
                            if (!intersectionQueueMap.ContainsKey(rightSideQueueKey))
                            {
                                int crossingCarsDirection = navigation.intersectionDirection;
                                int actualCrossingTurn;
                                if (intersectionCrossingMap.TryGetValue(navigation.intersectionId, out actualCrossingTurn))
                                {
                                    crossingCarsDirection = actualCrossingTurn;
                                }
                                //there are no cars crossing now the intersection
                                if (navigation.intersectionDirection == crossingCarsDirection)
                                {
                                    navigation.intersectionStop = false;
                                    navigation.intersectionCrossing = true;
                                    processedIntersections.Add(navigation.intersectionId, true);
                                    //Debug.Log("Crossing " + navigation.intersectionDirection);
                                }
                                //Debug.Log(navigation.intersectionDirection + " Stopped by " + crossingCarsDirection);
                            }
                        }
                    }
                    //4way simple intersections
                    else
                    {
                        //int currentQueueKey = CarsPositionSystem.GetIntersectionQueueHashMapKey(navigation.intersectionId, navigation.intersectionDirection);
                        int rightSideDirection = (navigation.intersectionDirection + 1) % 4;
                        int rightSideQueueKey = CarsPositionSystem.GetIntersectionQueueHashMapKey(navigation.intersectionId, rightSideDirection);

                        //right side is free
                        if (!intersectionQueueMap.ContainsKey(rightSideQueueKey))
                        {
                            int crossingCarsDirection = navigation.intersectionDirection;
                            int actualCrossingTurn;
                            if (intersectionCrossingMap.TryGetValue(navigation.intersectionId, out actualCrossingTurn))
                            {
                                crossingCarsDirection = actualCrossingTurn;
                            }
                            //there are no cars crossing now the intersection
                            if (navigation.intersectionDirection == crossingCarsDirection)
                            {
                                navigation.intersectionStop = false;
                                navigation.intersectionCrossing = true;
                                processedIntersections.Add(navigation.intersectionId, true);
                                //Debug.Log("Crossing " + navigation.intersectionId + " with empty intersection from " + navigation.intersectionDirection);
                            }
                            //Debug.Log(navigation.intersectionDirection + " Stopped by " + crossingCarsDirection);
                        }

                        //infinite waiting avoidance
                        if (navigation.intersectionDirection == 0)
                        {
                            int rightSideKey = CarsPositionSystem.GetIntersectionQueueHashMapKey(navigation.intersectionId, 1);
                            int frontSideKey = CarsPositionSystem.GetIntersectionQueueHashMapKey(navigation.intersectionId, 2);
                            int leftSideKey = CarsPositionSystem.GetIntersectionQueueHashMapKey(navigation.intersectionId, 3);
                            int crossingCarsDirection = 0;
                            int actualCrossingTurn;
                            if (intersectionCrossingMap.TryGetValue(navigation.intersectionId, out actualCrossingTurn))
                            {
                                crossingCarsDirection = actualCrossingTurn;
                            }
                            if (intersectionQueueMap.ContainsKey(rightSideKey)
                                && intersectionQueueMap.ContainsKey(frontSideKey)
                                && intersectionQueueMap.ContainsKey(leftSideKey)
                                && crossingCarsDirection == 0)
                            {
                                navigation.intersectionStop = false;
                                navigation.intersectionCrossing = true;
                                processedIntersections.Add(navigation.intersectionId, true);
                                //Debug.Log("AVOIDING infinite waiting");
                            }
                        }
                    }
                }
            }).Schedule();
    }
}
