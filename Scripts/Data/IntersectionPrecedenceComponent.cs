using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;

[GenerateAuthoringComponent]
public struct IntersectionComponent : IComponentData 
{
    public int numberIntersections;
}
