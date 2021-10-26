using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;

public struct IntersectionTrigger : IComponentData { };

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

public struct IntersectionTriggerNodes : IBufferElementData
{
    public float3 triggerPosition;
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
        dstManager.AddComponent<IntersectionTrigger>(entity);

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

        DynamicBuffer<IntersectionTriggerNodes> triggerList = dstManager.AddBuffer<IntersectionTriggerNodes>(entity);
        foreach (TriggerNode trigNode in gameObject.GetComponentsInChildren<TriggerNode>())
        {
            triggerList.Add(new IntersectionTriggerNodes { triggerPosition = trigNode.transform.position });
        }
    }
}

