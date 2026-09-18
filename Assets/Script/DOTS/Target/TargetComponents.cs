using Unity.Entities;

namespace Unity6Demo.DOTS.Target
{
    /// <summary>
    /// Marks an entity as food / a destination the boids are drawn towards.
    /// Queried by BoidSystem together with TargetData and LocalTransform.
    /// </summary>
    public struct TargetTag : IComponentData { }

    /// <summary>
    /// Read by InitialPerTargetJob into the flat arrays MoveBoidJob iterates.
    /// MoveBoidJob.CalculateTargetPosition picks the single nearest target whose
    /// distance is within TargetRadius, ranked by (distance / TargetAttraction) —
    /// so a higher TargetAttraction makes a target win over closer but weaker ones.
    /// </summary>
    public struct TargetData : IComponentData
    {
        /// <summary>Boids outside this distance ignore the target entirely.</summary>
        public float TargetRadius;

        /// <summary>
        /// Pull strength. Used as a divisor when ranking targets, and passed to
        /// MoveBoidJob as TargetForces. Must be non-zero or the ranking divides by zero.
        /// </summary>
        public float TargetAttraction;
    }
}
