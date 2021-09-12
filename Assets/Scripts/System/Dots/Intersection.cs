using System;
using Unity.Entities;

public struct IntersectionComponent : IComponentData
{
    public int CurrentGroupIndex;
    public IntersectionPhaseType CurrentPhase;
}
public enum IntersectionPhaseType
{
    PassThrough, //let all vehicle pass through
    ClearingTraffic, //wait for last vehicle to exit
}
public struct IntersectionTimerComponent : IComponentData
{
    public int FramesLeft;
}

[InternalBufferCapacity(ComponentConstants.MaxIntersectionGroups)]
public struct IntersectionSegmentsGroupBufferElement : IBufferElementData
{
    public int StartIndex;
    public int EndIndex;
    public int Time;
}

[InternalBufferCapacity(ComponentConstants.MaxIntersectionSegments)]
public struct IntersectionSegmentBufferElement : IBufferElementData
{
    public Entity Segment;
}

[Serializable]
public struct RoadIntersectionSegmentsGroup
{
    public Segment[] Segments;
    public int Time;
}