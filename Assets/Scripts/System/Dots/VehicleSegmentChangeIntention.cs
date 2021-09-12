using Unity.Entities;

[GenerateAuthoringComponent]
public struct VehicleSegmentChangeIntention : IComponentData
{
    public Entity NextSegment;
}
