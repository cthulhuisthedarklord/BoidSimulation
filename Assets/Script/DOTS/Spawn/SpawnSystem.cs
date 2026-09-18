using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity6Demo.DOTS.Boid;
using static UnityEngine.Rendering.STP;
namespace Unity6Demo.DOTS.Spawn
{
    [BurstCompile]
    public partial struct SpawnSystem : ISystem
    {
        private const int MAX_SPAWNS_PER_FRAME = 2500; // Adjust this based on performance needs

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpawnConfig>();
        }
        /*
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            SpawnConfig spawnConfig = SystemAPI.GetSingleton<SpawnConfig>();
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();

            // Process each spawn event buffer
            foreach (var (buffer, entity) in SystemAPI.Query<DynamicBuffer<SpawnEventBuffer>>().WithEntityAccess())
            {
                if (buffer.Length == 0) continue;

                var spawnEcb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

                // Process each spawn event
                for (int eventIndex = 0; eventIndex < buffer.Length; eventIndex++)
                {
                    var spawnEvent = buffer[eventIndex];
                    float remainingToSpawn = spawnEvent.AmountToSpawn;
                    int totalSpawned = 0;

                    // Process in batches until all entities are spawned
                    while (remainingToSpawn > 0)
                    {
                        int batchSize = math.min((int)remainingToSpawn, MAX_SPAWNS_PER_FRAME);

                        // Create entities for this batch
                        var entities = CollectionHelper.CreateNativeArray<Entity, RewindableAllocator>(
                            batchSize, ref state.WorldUnmanaged.UpdateAllocator);

                        state.EntityManager.Instantiate(spawnConfig.BoidPrefabEntity, entities);

                        // Schedule SetBoidDataJob
                        var setBoidDataJob = new SetBoidDataJob
                        {
                            Entities = entities,
                            SpawnConfig = spawnConfig,
                            CommandBuffer = spawnEcb.AsParallelWriter(),
                            Seed = spawnEvent.UniqueSeed,
                        };

                        var handle = setBoidDataJob.Schedule(batchSize, 64, state.Dependency);
                        handle.Complete();

                        remainingToSpawn -= batchSize;
                        totalSpawned += batchSize;
                    }
                }

                // Create a new command buffer for reset job
                var resetEcb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

                // Process spawned boids
                var spawnedBoidQuery = SystemAPI.QueryBuilder()
                    .WithAll<SpawnedBoidTag>()
                    .Build();

                var resetBoidJob = new ResetBoidJob
                {
                    CommandBuffer = resetEcb.AsParallelWriter()
                };

                var resetHandle = resetBoidJob.ScheduleParallel(spawnedBoidQuery, state.Dependency);
                resetHandle.Complete();

                // Clear the spawn event buffer
                buffer.Clear();
            }
        }
        */

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Get common resources
            var spawnConfig = SystemAPI.GetSingleton<SpawnConfig>();
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Convert spawn events to spawn batches
            foreach (var (buffer, entity) in SystemAPI.Query<DynamicBuffer<SpawnEventBuffer>>().WithEntityAccess())
            {
                if (buffer.Length == 0) continue;

                CreateSpawnBatches(buffer, ecb, spawnConfig);
                ecb.RemoveComponent<SpawnEventBuffer>(entity);
            }

            // Process spawn batches
            ProcessSpawnBatches(ref state, spawnConfig);

            // Reset newly spawned entities
            ResetSpawnedBoids(ref state);
        }

        private void CreateSpawnBatches(
           DynamicBuffer<SpawnEventBuffer> events,
           EntityCommandBuffer ecb,
           SpawnConfig spawnConfig)
        {
            foreach (var spawnEvent in events)
            {
                int remainingBatches = (spawnConfig.AmountToSpawn + MAX_SPAWNS_PER_FRAME - 1) / MAX_SPAWNS_PER_FRAME;

                for (int i = 0; i < remainingBatches; i++)
                {
                    int batchSize = math.min(MAX_SPAWNS_PER_FRAME, spawnConfig.AmountToSpawn - (i * MAX_SPAWNS_PER_FRAME));

                    var batchEntity = ecb.CreateEntity();
                    ecb.AddComponent(batchEntity, new SpawnBatchData
                    {
                        Amount = batchSize,
                        Seed = spawnEvent.UniqueSeed + i,
                        BaseOffset = i * MAX_SPAWNS_PER_FRAME
                    });
                }
            }
        }

        private void ProcessSpawnBatches(ref SystemState state, SpawnConfig spawnConfig)
        {
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (batch, entity) in SystemAPI.Query<RefRO<SpawnBatchData>>().WithEntityAccess())
            {
                var entities = CollectionHelper.CreateNativeArray<Entity, RewindableAllocator>(
                    batch.ValueRO.Amount,
                    ref state.WorldUnmanaged.UpdateAllocator);

                state.EntityManager.Instantiate(spawnConfig.BoidPrefabEntity, entities);

                var spawnJob = new SpawnBoidJob
                {
                    Entities = entities,
                    SpawnConfig = spawnConfig,
                    CommandBuffer = ecb.AsParallelWriter(),
                    BatchData = batch.ValueRO
                };

                state.Dependency = spawnJob.Schedule(batch.ValueRO.Amount, 64, state.Dependency);
                state.Dependency.Complete();

                ecb.DestroyEntity(entity);
            }
        }

        private void ResetSpawnedBoids(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var resetJob = new ResetBoidJob
            {
                CommandBuffer = ecb.AsParallelWriter()
            };

            var query = SystemAPI.QueryBuilder().WithAll<SpawnedBoidTag>().Build();
            state.Dependency = resetJob.ScheduleParallel(query, state.Dependency);
        }


        [BurstCompile]
        struct SpawnBoidJob : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<Entity> Entities;
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            [ReadOnly] public SpawnConfig SpawnConfig;
            [ReadOnly] public SpawnBatchData BatchData;

            private const float BASE_SPAWN_RADIUS = 1.0f;
            private const float MAX_SPAWN_RADIUS = 50.0f;

            public void Execute(int index)
            {
                int globalIndex = BatchData.BaseOffset + index;
                var entity = Entities[index];
                var random = CreateRandom(globalIndex);

                var transform = CreateTransform(random, globalIndex);
                var boidData = CreateBoidData(random);

                CommandBuffer.AddComponent(index, entity, new SpawnedBoidTag());
                CommandBuffer.AddComponent(index, entity, boidData);
                CommandBuffer.SetComponent(index, entity, transform);
            }

            private readonly Random CreateRandom(int index)
            {
                uint seed = (uint)((((int)BatchData.Seed + index) & 0x7FFFFFFF));
                return Random.CreateFromIndex(seed);
            }

            private readonly LocalTransform CreateTransform(Random random, int index)
            {
                float radius = 10f;
                float3 position = SpawnConfig.Initial + GenerateSpherePosition(false, radius, random);
                quaternion rotation = quaternion.LookRotationSafe(random.NextFloat3Direction(), math.up());
                return LocalTransform.FromPositionRotationScale(position, rotation, SpawnConfig.Scale);
            }

            private static float3 GenerateSpherePosition(bool Hollow, float radius, Random random)
            {
                float theta = random.NextFloat(-math.PI, math.PI);
                float phi = math.acos(random.NextFloat(-1f, 1f));
                if (Hollow)
                {
                    // Surface of sphere

                    return new float3(
                        radius * math.sin(phi) * math.cos(theta),
                        radius * math.sin(phi) * math.sin(theta),
                        radius * math.cos(phi)
                    );
                }
                else
                {
                    float sphereRadius = radius * random.NextFloat(0f, 1f);
                    return new float3(
                        sphereRadius * math.sin(phi) * math.cos(theta),
                        sphereRadius * math.sin(phi) * math.sin(theta),
                        sphereRadius * math.cos(phi)
                    );
                }
            }

            private BoidData CreateBoidData(Random random)
            {
                return new BoidData
                {
                    BaseSpeed = SpawnConfig.Speed * random.NextFloat(1.0f, 2.0f),
                    Acceleration = float3.zero,
                    Velocity = float3.zero,
                    RotateSpeed = SpawnConfig.RotateSpeed * random.NextFloat(1.0f, 2.0f),
                    LifeTime = SpawnConfig.BallLifeTime * random.NextFloat(1.0f, 2.0f),
                    MaxAcceleration = SpawnConfig.MaxAcceleration * random.NextFloat(1.0f, 2.0f),
                    MaxSpeed = SpawnConfig.MaxSpeed * random.NextFloat(1.0f, 2.0f),
                    MaxTurnAngle = SpawnConfig.MaxTurnAngle * random.NextFloat(1.0f, 1.5f),
                    Drag = SpawnConfig.Drag * random.NextFloat(1.0f, 2.0f),
                };
            }
        }

        [BurstCompile]
        public partial struct ResetBoidJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            public EntityCommandBuffer.ParallelWriter CommandBuffer;

            public void Execute([EntityIndexInQuery] int entityIndexInQuery, in Entity entity)
            {
                CommandBuffer.RemoveComponent<SpawnedBoidTag>(entityIndexInQuery, entity);
                CommandBuffer.AddComponent<BoidTag>(entityIndexInQuery, entity);
            }
        }

        // Component to track spawn batches
        public struct SpawnBatchData : IComponentData
        {
            public int Amount;
            public float Seed;
            public int BaseOffset;
        }
    }
}