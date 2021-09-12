using Unity.Entities;


[GenerateAuthoringComponent]
public struct VehicleConfigComponent : IComponentData
{
    public float Speed;
    public float Length;
}
