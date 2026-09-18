using Unity.Entities;
using Unity.Mathematics;

namespace Unity6Demo.DOTS.Boid
{
    /// <summary>
    /// Marks an entity as an active, simulated boid.
    /// IMPORTANT: this must NOT be present on the boid prefab. SpawnSystem adds
    /// SpawnedBoidTag first, and ResetBoidJob swaps it for BoidTag once the
    /// spawn transform has been written. That ordering is what stops MoveBoidJob
    /// from moving a boid on the same frame its position is set.
    /// </summary>
    public struct BoidTag : IComponentData { }

    /// <summary>
    /// Per-boid mutable state plus the per-boid randomized limits set at spawn time.
    /// Written by SpawnSystem.SpawnBoidJob, read/written by MoveBoidJob and KillBoidJob.
    /// </summary>
    public struct BoidData : IComponentData
    {
        // Mutable state
        public float3 Velocity;
        public float3 Acceleration;

        // Per-boid limits (randomized from SpawnConfig when the boid spawns)
        public float BaseSpeed;        // cruising speed the steering aims for
        public float MaxSpeed;         // hard clamp on velocity magnitude
        public float MaxAcceleration;  // hard clamp on acceleration magnitude
        public float MaxTurnAngle;     // max turn per update, in DEGREES (BoidMath.CalculateTurn converts)
        public float RotateSpeed;      // visual yaw/pitch slerp rate used by BoidMath.LookAt
        public float Drag;             // per-second velocity damping, 0..1
        public float LifeTime;         // seconds remaining; KillBoidJob destroys the entity at <= 0
    }

    /// <summary>
    /// Singleton. Global tuning for the flocking simulation. Baked by BoidSettingAuthoring.
    /// BoidSystem does RequireForUpdate on this, so the simulation will not run without it.
    /// </summary>
    public struct BoidSetting : IComponentData
    {
        // Spatial hashing
        public float CellRadius;        // spatial hash cell size; BoidSystem uses 1f / CellRadius
        public float PerceptionRadius;  // neighbour cutoff distance
        public int MaxNeighbor;         // stop gathering after this many neighbours (perf guard)

        // Flocking weights
        public float SeparationWeight;
        public float AlignmentWeight;
        public float CohesionWeight;

        // Density response
        public float OptimalNeighborCount; // density is normalized against this

        // Steering blend
        public float BoidWeight;                  // how much flocking bends the current heading
        public float DestinationWeight;           // how much the target pulls, once in range
        public float DestinationPerceptionRadius; // distance at which a target starts pulling

        // Thresholds
        public float AngleThreshold;         // degrees; below this LookAt does not rotate
        public float MinVelocityThreshold;   // below this speed, heading falls back to transform.Forward()
        public float MinDirectionMagnitude;  // below this, a steering vector is treated as zero
    }

    /// <summary>
    /// Singleton. Axis-aligned box the boids are confined to.
    /// BoidMath.CalculateBoundaryCollision computes the half-extent as
    /// (extent - center), so 'extent' is the MAX CORNER of the box in world space,
    /// not a half-size. BoidSettingAuthoring bakes it that way.
    /// Field names are lowercase to match the existing call sites in BoidMath.
    /// </summary>
    public struct BoidBound : IComponentData
    {
        public float3 center;
        public float3 extent;
    }

    /// <summary>
    /// Singleton created at runtime by MovementState (EntityManager.CreateSingleton).
    /// Holds the world position the player last pointed at.
    /// BoidSystem requires it to exist; see DestinationTargetSystem for the optional
    /// glue that turns this into actual boid movement.
    /// </summary>
    public struct BoidDestinationSingleton : IComponentData
    {
        public float3 Destination;
    }
}
