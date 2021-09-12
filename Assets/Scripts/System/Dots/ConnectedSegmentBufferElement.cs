using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

// pre-allocation https://forum.unity.com/threads/what-is-internalbuffercapacity.709502/
[InternalBufferCapacity(ComponentConstants.MaxSegmentsConnectedToOneNode)]
public struct ConnectedSegmentBufferElement : IBufferElementData
{
    public Entity segment;
}

public static class ComponentConstants
{
    public const int MaxIntersectionGroups = 4;
    public const int MaxIntersectionSegments = 12;
    public const int MaxSegmentsConnectedToOneNode = 4;
}