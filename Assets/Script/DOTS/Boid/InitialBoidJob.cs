using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity6Demo.DOTS.Boid;
using Unity6Demo.DOTS.Obstacle;
using Unity6Demo.DOTS.Target;

namespace Unity6Demo.DOTS.Boid
{
    [BurstCompile]
    public static class InitialBoidJob
    {
        public static NativeArray<float3> CreateNeighborOffset_Single()
        {
            NativeArray<float3> singleNeighborOffsets = new NativeArray<float3>(1, Allocator.Persistent);
            singleNeighborOffsets[0] = float3.zero;
            return singleNeighborOffsets;
        }

        public static NativeArray<float3> CreateNeighborOffset_Multi()
        {
            NativeArray<float3> multiNeighborOffsets = new NativeArray<float3>(27, Allocator.Persistent);
            int index = 0;
            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                    for (int z = -1; z <= 1; z++)
                        multiNeighborOffsets[index++] = new float3(x, y, z);
            return multiNeighborOffsets;
        }
    }

    [BurstCompile]
    public partial struct InitialPerObstacleJob : IJobEntity
    {
        [WriteOnly] public NativeArray<float3> ObstaclePositions;
        [WriteOnly] public NativeArray<float> ObstacleRadius;
        [WriteOnly] public NativeArray<float> ObstacleForces;
        public void Execute([EntityIndexInQuery] int entityIndexInQuery, [ReadOnly] in LocalTransform transform, in ObstacleData obstacleData)
        {
            ObstaclePositions[entityIndexInQuery] = transform.Position;
            ObstacleRadius[entityIndexInQuery] = obstacleData.ThreatRadius;
            ObstacleForces[entityIndexInQuery] = obstacleData.ThreatForce;
        }
    }

    [BurstCompile]
    public partial struct InitialPerTargetJob : IJobEntity
    {
        [WriteOnly] public NativeArray<float3> TargetPosition;
        [WriteOnly] public NativeArray<float> TargetRadius;
        [WriteOnly] public NativeArray<float> TargetAttraction;
        public void Execute([EntityIndexInQuery] int entityIndexInQuery, [ReadOnly] in LocalTransform transform, in TargetData targetData)
        {
            TargetPosition[entityIndexInQuery] = transform.Position;
            TargetRadius[entityIndexInQuery] = targetData.TargetRadius;
            TargetAttraction[entityIndexInQuery] = targetData.TargetAttraction;
        }
    }

    [BurstCompile]
    public partial struct InitialPerBoidJob : IJobEntity
    {
        [ReadOnly] public float InverseCellSize;
        [WriteOnly] public NativeParallelMultiHashMap<uint, int>.ParallelWriter SpatialHashMap;
        [WriteOnly] public NativeArray<float3> BoidPositions;
        [WriteOnly] public NativeArray<float3> BoidVelocities;
        public void Execute([EntityIndexInQuery] int entityIndexInQuery, [ReadOnly] in LocalTransform transform, in BoidData birdData)
        {
            var cellHash = BoidMath.Hash(transform.Position, InverseCellSize);
            BoidPositions[entityIndexInQuery] = transform.Position;
            BoidVelocities[entityIndexInQuery] = birdData.Velocity;
            SpatialHashMap.Add(cellHash, entityIndexInQuery);
            //UnityEngine.Debug.Log($"Job: InitialPerBoidJob, Entity Index: {entityIndexInQuery}, Cell Hash: {cellHash}, Position: {localToWorld.Position}");
        }
    }

    [BurstCompile]
    public partial struct KillBoidJob : IJobEntity
    {
        [ReadOnly] public float deltaTime;

        public EntityCommandBuffer.ParallelWriter CommandBuffer;
        public void Execute(in Entity entity, ref BoidData birdData)
        {
            birdData.LifeTime -= deltaTime;
            if (birdData.LifeTime <= 0)
            {
                CommandBuffer.DestroyEntity(entity.Index, entity);
            }
        }
    }
}