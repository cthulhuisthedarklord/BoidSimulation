using Unity.Entities;

namespace Unity6Demo.DOTS.Spawn
{
    /// <summary>
    /// One spawn request. MovementState.SpawnBoid creates a throwaway entity holding
    /// this buffer; SpawnSystem drains it into SpawnBatchData batches and then removes
    /// the buffer component from the entity.
    /// </summary>
    [InternalBufferCapacity(8)]
    public struct SpawnEventBuffer : IBufferElementData
    {
        /// <summary>
        /// How many boids this request wants. NOTE: the current SpawnSystem ignores
        /// this and uses SpawnConfig.AmountToSpawn when sizing batches — kept because
        /// MovementState still fills it in.
        /// </summary>
        public int AmountToSpawn;

        /// <summary>
        /// Seed for this request, from System.Random.Next() in MovementState.
        /// SpawnSystem offsets it per batch (UniqueSeed + batchIndex) so batches of the
        /// same request do not spawn identical boids.
        /// </summary>
        public int UniqueSeed;
    }

    /// <summary>
    /// Transient marker put on a freshly instantiated boid by SpawnBoidJob.
    /// ResetBoidJob removes it and adds BoidTag on the following playback, which is
    /// what keeps MoveBoidJob from moving a boid before its spawn transform lands.
    /// Must not be present on the prefab.
    /// </summary>
    public struct SpawnedBoidTag : IComponentData { }
}
