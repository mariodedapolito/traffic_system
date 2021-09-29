using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;


public struct IntersectionTriggerData : IComponentData
{
    public int directionId;
    public int intersectionId;
    public bool isIntersectionEnter;
    public bool isIntersectionExit;
    public bool isSimpleIntersection;
    public bool isSemaphoreIntersection;
    public int intersectionNumRoads;
}

class IntersectionTriggerComponent : MonoBehaviour, IConvertGameObjectToEntity
{
#pragma warning disable 649
    [Header("Navigation")]
    public int directionId;
    public int dynamicIntersectionId;
    public bool isIntersectionEnter;
    public bool isIntersectionExit;
    public bool isSimpleIntersection;
    public bool isSemaphoreIntersection;
    public int intersectionNumRoads;

#pragma warning restore 649

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new IntersectionTriggerData
        {
            directionId = directionId,
            intersectionId = dynamicIntersectionId,
            isIntersectionEnter = isIntersectionEnter,
            isIntersectionExit = isIntersectionExit,
            isSimpleIntersection = isSimpleIntersection,
            isSemaphoreIntersection = isSemaphoreIntersection,
            intersectionNumRoads = intersectionNumRoads
        });
    }
}

