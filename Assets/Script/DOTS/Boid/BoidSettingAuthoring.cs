using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity6Demo.DOTS.Boid
{
    /// <summary>
    /// Put ONE of these in the subscene. It bakes both singletons BoidSystem needs:
    /// BoidSetting (tuning) and BoidBound (the box the school is confined to).
    /// BoidSystem does RequireForUpdate&lt;BoidSetting&gt;(), so with this missing the
    /// whole simulation silently does nothing.
    /// </summary>
    [DisallowMultipleComponent]
    public class BoidSettingAuthoring : MonoBehaviour
    {
        [Header("Spatial hashing")]
        [Tooltip("Spatial hash cell size. Keep this close to Perception Radius: much " +
                 "smaller and neighbours fall outside the 27 sampled cells, much larger " +
                 "and every cell holds too many boids to be a useful filter.")]
        public float CellRadius = 5f;

        [Tooltip("Neighbour cutoff distance for separation / alignment / cohesion.")]
        public float PerceptionRadius = 5f;

        [Tooltip("Stop accumulating neighbours after this many. Performance guard: " +
                 "dense schools otherwise make the inner loop unbounded.")]
        public int MaxNeighbor = 30;

        [Header("Flocking weights")]
        public float SeparationWeight = 1.5f;
        public float AlignmentWeight = 1.0f;
        public float CohesionWeight = 1.0f;

        [Tooltip("Local density is normalized against this count before it modulates " +
                 "separation and cohesion.")]
        public float OptimalNeighborCount = 10f;

        [Header("Steering blend")]
        [Tooltip("How strongly flocking bends the boid's current heading.")]
        public float BoidWeight = 1.0f;

        [Tooltip("How strongly a target pulls once the boid is inside " +
                 "Destination Perception Radius.")]
        public float DestinationWeight = 1.0f;

        [Tooltip("Distance at which a target starts steering the boid.")]
        public float DestinationPerceptionRadius = 30f;

        [Header("Thresholds")]
        [Tooltip("Degrees. Below this angular error the boid does not bother rotating, " +
                 "which stops visible jitter when it is already on heading.")]
        public float AngleThreshold = 1.0f;

        [Tooltip("Below this speed the heading falls back to transform.Forward() " +
                 "instead of normalizing a near-zero velocity.")]
        public float MinVelocityThreshold = 0.01f;

        [Tooltip("A steering vector shorter than this is treated as no input at all.")]
        public float MinDirectionMagnitude = 0.1f;

        [Header("Simulation bounds")]
        [Tooltip("Centre of the box the boids bounce inside, in world space.")]
        public Vector3 BoundsCenter = Vector3.zero;

        [Tooltip("Full size of the box (not half-size). Baked into BoidBound.extent " +
                 "as the max corner, which is the form BoidMath expects.")]
        public Vector3 BoundsSize = new Vector3(200f, 100f, 200f);

        private class Baker : Baker<BoidSettingAuthoring>
        {
            public override void Bake(BoidSettingAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new BoidSetting
                {
                    CellRadius = authoring.CellRadius,
                    PerceptionRadius = authoring.PerceptionRadius,
                    MaxNeighbor = authoring.MaxNeighbor,

                    SeparationWeight = authoring.SeparationWeight,
                    AlignmentWeight = authoring.AlignmentWeight,
                    CohesionWeight = authoring.CohesionWeight,

                    OptimalNeighborCount = authoring.OptimalNeighborCount,

                    BoidWeight = authoring.BoidWeight,
                    DestinationWeight = authoring.DestinationWeight,
                    DestinationPerceptionRadius = authoring.DestinationPerceptionRadius,

                    AngleThreshold = authoring.AngleThreshold,
                    MinVelocityThreshold = authoring.MinVelocityThreshold,
                    MinDirectionMagnitude = authoring.MinDirectionMagnitude,
                });

                // BoidMath.CalculateBoundaryCollision derives the half-extent as
                // (extent - center), so 'extent' must be the max corner, not a half-size.
                float3 center = authoring.BoundsCenter;
                float3 max = center + (float3)authoring.BoundsSize * 0.5f;

                AddComponent(entity, new BoidBound
                {
                    center = center,
                    extent = max,
                });
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.6f);
            Gizmos.DrawWireCube(BoundsCenter, BoundsSize);
        }
    }
}
