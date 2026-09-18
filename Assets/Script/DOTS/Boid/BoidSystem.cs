using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity6Demo.DOTS.Obstacle;
using Unity6Demo.DOTS.Target;
using Unity6Demo.UI;
namespace Unity6Demo.DOTS.Boid
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct BoidSystem : ISystem
    {
        private NativeArray<float3> singleNeighborOffsets;
        private NativeArray<float3> multiNeighborOffsets;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UIDataSingleton>();
            state.RequireForUpdate<BoidDestinationSingleton>();
            state.RequireForUpdate<BoidSetting>();
            singleNeighborOffsets = InitialBoidJob.CreateNeighborOffset_Single();
            multiNeighborOffsets = InitialBoidJob.CreateNeighborOffset_Multi();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            UpdateBoid(ref state);
        }

        public void OnDestroy(ref SystemState state)
        {
            singleNeighborOffsets.Dispose();
            multiNeighborOffsets.Dispose();
        }

        [BurstCompile]
        private void UpdateBoid(ref SystemState state)
        {
            var world = state.WorldUnmanaged;
            var commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(world);
            var boidQuery = SystemAPI.QueryBuilder()
                .WithAll<BoidTag>()
                .WithAllRW<BoidData>()
                .WithAllRW<LocalTransform>()
                .Build();
            var obstacleQuery = SystemAPI.QueryBuilder()
                .WithAll<ObstacleTag>()
                .WithAllRW<ObstacleData>()
                .WithAllRW<LocalTransform>()
                .Build();
            var targetQuery = SystemAPI.QueryBuilder()
                .WithAll<TargetTag>()
                .WithAllRW<TargetData>()
                .WithAllRW<LocalTransform>()
                .Build();
            int boidCount = boidQuery.CalculateEntityCount();
            int obstacleCount = obstacleQuery.CalculateEntityCount();
            int targetCount = targetQuery.CalculateEntityCount();
            // Update UI singleton
            SystemAPI.SetSingleton(new UIDataSingleton { EntityCount = boidCount });

            // Prepare shared data
            var boidHashMap = new NativeParallelMultiHashMap<uint, int>(boidCount, world.UpdateAllocator.ToAllocator);
            var boidPositions = CollectionHelper.CreateNativeArray<float3, RewindableAllocator>(boidCount, ref world.UpdateAllocator);
            var boidVelocities = CollectionHelper.CreateNativeArray<float3, RewindableAllocator>(boidCount, ref world.UpdateAllocator);
            var obstaclePositions = CollectionHelper.CreateNativeArray<float3, RewindableAllocator>(obstacleCount, ref world.UpdateAllocator);
            var obstacleRadius = CollectionHelper.CreateNativeArray<float, RewindableAllocator>(obstacleCount, ref world.UpdateAllocator);
            var obstacleForces = CollectionHelper.CreateNativeArray<float, RewindableAllocator>(obstacleCount, ref world.UpdateAllocator);
            var targetPositions = CollectionHelper.CreateNativeArray<float3, RewindableAllocator>(targetCount, ref world.UpdateAllocator);
            var targetRadius = CollectionHelper.CreateNativeArray<float, RewindableAllocator>(obstacleCount, ref world.UpdateAllocator);
            var targetAttraction = CollectionHelper.CreateNativeArray<float, RewindableAllocator>(obstacleCount, ref world.UpdateAllocator);

            var boidSetting = SystemAPI.GetSingleton<BoidSetting>();
            var deltaTime = SystemAPI.Time.DeltaTime;
            var boidBound = SystemAPI.GetSingleton<BoidBound>();
            float inverseCellSize = 1f / boidSetting.CellRadius;
            //Start Scedule Jobs
            var initialObstacleJob = new InitialPerObstacleJob
            {
                ObstaclePositions = obstaclePositions,
                ObstacleRadius = obstacleRadius,
                ObstacleForces = obstacleForces
            };

            var initialTargetJob = new InitialPerTargetJob
            {
                TargetPosition = targetPositions,
                TargetRadius = targetRadius,
                TargetAttraction = targetAttraction,
            };

            var initialBoidJob = new InitialPerBoidJob
            {
                InverseCellSize = inverseCellSize,
                SpatialHashMap = boidHashMap.AsParallelWriter(),
                BoidPositions = boidPositions,
                BoidVelocities = boidVelocities
            };

            var moveJob = new MoveBoidJob
            {
                BoidInverseCellSize = inverseCellSize,
                SingleNeighborOffsets = singleNeighborOffsets,
                MultiNeighborOffsets = multiNeighborOffsets,
                BoidHashMap = boidHashMap.AsReadOnly(),
                DeltaTime = deltaTime,
                CommandBuffer = commandBuffer.AsParallelWriter(),
                BoidPositions = boidPositions,
                BoidVelocities = boidVelocities,
                BoidSettings = boidSetting,
                ObstacleRadius = obstacleRadius,
                ObstacleForces = obstacleForces,
                BoidBounds = boidBound,
                ObstaclePositions = obstaclePositions,
                TargetPositions = targetPositions,
                TargetForces = targetAttraction,
                TargetRadius = targetRadius
            };

            var killJob = new KillBoidJob
            {
                CommandBuffer = commandBuffer.AsParallelWriter(),
                deltaTime = deltaTime,
            };

            // Chain job dependencies
            var initialTargetHandle = initialTargetJob.ScheduleParallel(targetQuery, state.Dependency);
            var initialObstacleHandle = initialObstacleJob.ScheduleParallel(obstacleQuery, state.Dependency);
            var initialBoidHandle = initialBoidJob.ScheduleParallel(boidQuery, state.Dependency);
            var preinitialHandle = JobHandle.CombineDependencies(initialBoidHandle, initialObstacleHandle);
            var initialHandle = JobHandle.CombineDependencies(initialTargetHandle, preinitialHandle);
            var moveHandle = moveJob.ScheduleParallel(boidQuery, initialHandle);
            state.Dependency = killJob.ScheduleParallel(boidQuery, moveHandle);
        }
    }
}