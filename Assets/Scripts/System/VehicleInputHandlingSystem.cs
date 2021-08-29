using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

struct VehicleInput : IComponentData
{
    public float2 Looking;
    public float2 Steering;
    public float Throttle;
    public int Change; // positive to change to a subsequent vehicle, negative to change to a previous one
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
class VehicleInputHandlingSystem : SystemBase
{
    protected override void OnUpdate()
    {
        

        Entities
            .WithName("ActiveVehicleInputHandlingJob")
            .WithoutBurst()
            .WithStructuralChanges()
            .ForEach((ref VehicleSpeed speed, ref VehicleSteering steering) =>
            {
                var newSpeed = speed.TopSpeed;
                speed.DriveEngaged = (byte)(newSpeed == 0f ? 0 : 1);
                speed.DesiredSpeed = math.lerp(speed.DesiredSpeed, newSpeed, speed.Damping);

                var newSteeringAngle = steering.MaxSteeringAngle;
                steering.DesiredSteeringAngle = math.lerp(steering.DesiredSteeringAngle, newSteeringAngle, steering.Damping);

               
            }).Run();
    }
}
