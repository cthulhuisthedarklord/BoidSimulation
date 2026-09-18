using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity6Demo.DOTS.Boid;

namespace Unity6Demo.DOTS.Target
{
    /// <summary>
    /// RECONSTRUCTED GLUE — this file was inferred, not recovered.
    ///
    /// MovementState writes the player's pointer position into BoidDestinationSingleton,
    /// and BoidSystem does RequireForUpdate on that singleton, but nothing in the
    /// surviving code ever reads it. Meanwhile MoveBoidJob steers only towards entities
    /// carrying TargetTag. This system is the missing link: it drives the target marked
    /// with DestinationTargetTag to wherever the player last pointed.
    ///
    /// If the original project moved that target from a MonoBehaviour instead, delete
    /// this file — nothing else depends on it.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(BoidSystem))]
    public partial struct DestinationTargetSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BoidDestinationSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var destination = SystemAPI.GetSingleton<BoidDestinationSingleton>().Destination;

            foreach (var transform in SystemAPI
                         .Query<RefRW<LocalTransform>>()
                         .WithAll<TargetTag, DestinationTargetTag>())
            {
                transform.ValueRW.Position = destination;
            }
        }
    }
}
