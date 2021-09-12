using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;


[GenerateAuthoringComponent]
public struct SegmentConfigComponent : IComponentData
{
    public Entity StartNode;
    public Entity EndNode;
    public float Length;
}

[GenerateAuthoringComponent]
public struct SegmentTrafficTypeComponent : IComponentData
{
    public ConnectionTrafficType TrafficType;
}
public enum ConnectionTrafficType
{
    Normal,
    NoEntrance,
}

[GenerateAuthoringComponent]
public struct SegmentComponent : IComponentData
{
    public float AvailableLength;
}
