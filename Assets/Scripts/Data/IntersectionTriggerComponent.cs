using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;

[GenerateAuthoringComponent]
public struct IntersectionTriggerComponent : IComponentData
{
    public int directionId;
    public int intersectionId;
}


