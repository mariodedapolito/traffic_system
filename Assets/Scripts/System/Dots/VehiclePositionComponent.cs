using Unity.Entities;

[GenerateAuthoringComponent]
public struct VehiclePositionComponent : IComponentData
{
    public float HeadSegPos;
    public float BackSegPos;
}


