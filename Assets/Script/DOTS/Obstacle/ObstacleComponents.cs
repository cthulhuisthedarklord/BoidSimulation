using Unity.Entities;

namespace Unity6Demo.DOTS.Obstacle
{
    /// <summary>
    /// Marks an entity as a threat the boids steer away from (the "shark").
    /// Queried by BoidSystem together with ObstacleData and LocalTransform.
    /// </summary>
    public struct ObstacleTag : IComponentData { }

    /// <summary>
    /// Read by InitialPerObstacleJob into the flat arrays MoveBoidJob iterates.
    /// The obstacle's world position comes from LocalTransform, so it can be
    /// animated by an Animator on the authoring GameObject or moved by a system.
    /// </summary>
    public struct ObstacleData : IComponentData
    {
        /// <summary>
        /// Distance at which boids start avoiding. MoveBoidJob.CalculateObstacleDirection
        /// only applies force when distance &lt; ThreatRadius, and scales it by
        /// saturate(abs(ThreatRadius - distance) / ThreatRadius) — so the push is
        /// strongest at the centre and falls to zero at the rim.
        /// </summary>
        public float ThreatRadius;

        /// <summary>
        /// Strength multiplier on the avoidance vector. Note that once any obstacle
        /// force is non-zero, MoveBoidJob switches to OBSTACLE_AVOID_SPEED (2x BaseSpeed),
        /// so this controls direction blending more than raw speed.
        /// </summary>
        public float ThreatForce;
    }
}
