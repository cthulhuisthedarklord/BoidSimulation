using Unity.Entities;
using UnityEngine;

namespace Unity6Demo.DOTS.Target
{
    /// <summary>
    /// Put this on each food source / attractor GameObject in the subscene.
    /// Dynamic transform usage, because targets are moved at runtime (see
    /// DestinationTargetSystem for the player-controlled one).
    /// </summary>
    [DisallowMultipleComponent]
    public class TargetAuthoring : MonoBehaviour
    {
        [Tooltip("Boids further than this ignore the target completely.")]
        public float TargetRadius = 50f;

        [Tooltip("Pull strength. Boids rank candidate targets by distance / attraction, " +
                 "so a strong far target can beat a weak near one. Must not be 0.")]
        public float TargetAttraction = 1f;

        [Tooltip("Also make this the target that follows the player's pointer " +
                 "(the BoidDestinationSingleton written by MovementState).")]
        public bool FollowsPlayerDestination = false;

        private class Baker : Baker<TargetAuthoring>
        {
            public override void Bake(TargetAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<TargetTag>(entity);
                AddComponent(entity, new TargetData
                {
                    TargetRadius = authoring.TargetRadius,
                    TargetAttraction = Mathf.Max(authoring.TargetAttraction, 0.0001f),
                });

                if (authoring.FollowsPlayerDestination)
                {
                    AddComponent<DestinationTargetTag>(entity);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, TargetRadius);
        }
    }

    /// <summary>
    /// Marks the one target that tracks where the player is pointing.
    /// Consumed by DestinationTargetSystem.
    /// </summary>
    public struct DestinationTargetTag : IComponentData { }
}
