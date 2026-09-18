using Unity.Entities;
using UnityEngine;

namespace Unity6Demo.DOTS.Obstacle
{
    /// <summary>
    /// Put this on each shark / predator GameObject in the subscene.
    /// TransformUsageFlags.Dynamic is required: the obstacle's LocalTransform is read
    /// every frame by InitialPerObstacleJob, and the original project animated the
    /// shark along a looping path with an Animator.
    /// </summary>
    [DisallowMultipleComponent]
    public class ObstacleAuthoring : MonoBehaviour
    {
        [Tooltip("Boids inside this distance steer away. The force is strongest at the " +
                 "centre and falls to zero exactly at the rim.")]
        public float ThreatRadius = 20f;

        [Tooltip("Strength of the avoidance push. Any non-zero avoidance makes the boid " +
                 "switch to escape speed (2x BaseSpeed), so raising this mostly sharpens " +
                 "the escape angle rather than the speed.")]
        public float ThreatForce = 5f;

        private class Baker : Baker<ObstacleAuthoring>
        {
            public override void Bake(ObstacleAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<ObstacleTag>(entity);
                AddComponent(entity, new ObstacleData
                {
                    ThreatRadius = authoring.ThreatRadius,
                    ThreatForce = authoring.ThreatForce,
                });
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, ThreatRadius);
        }
    }
}
