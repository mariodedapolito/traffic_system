using Unity.Entities;

[GenerateAuthoringComponent]
public struct VehicleMoveIntention : IComponentData
{
    public float AvailableDistance;
}
