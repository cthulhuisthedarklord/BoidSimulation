using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Unity6Demo.DOTS.Boid
{
    [BurstCompile]
    public partial struct MoveBoidJob : IJobEntity
    {
        #region Job Data
        // Input fields
        [ReadOnly] public BoidBound BoidBounds;
        [ReadOnly] public float DeltaTime;
        [WriteOnly] public EntityCommandBuffer.ParallelWriter CommandBuffer;
        [ReadOnly] public BoidSetting BoidSettings;

        //Spatial Data
        [ReadOnly] public NativeParallelMultiHashMap<uint, int>.ReadOnly BoidHashMap;
        [ReadOnly] public float BoidInverseCellSize;
        [ReadOnly] public NativeArray<float3> SingleNeighborOffsets;
        [ReadOnly] public NativeArray<float3> MultiNeighborOffsets;

        //Boid data
        [ReadOnly] public NativeArray<float3> BoidPositions;
        [ReadOnly] public NativeArray<float3> BoidVelocities;

        //Obstacle data
        [ReadOnly] public NativeArray<float3> ObstaclePositions;
        [ReadOnly] public NativeArray<float> ObstacleRadius;
        [ReadOnly] public NativeArray<float> ObstacleForces;

        //Target data
        [ReadOnly] public NativeArray<float3> TargetPositions;
        [ReadOnly] public NativeArray<float> TargetRadius;
        [ReadOnly] public NativeArray<float> TargetForces;


        private const float MIN_DISTANCE_SQR = 0.01f;
        private const float DENSITY_PEAK = 1.5f;
        private const float DENSITY_FALLOFF = 2f;
        private const float OBSTACLE_AVOID_SPEED = 2f;
        #endregion

        public readonly void Execute(ref BoidData boid, ref LocalTransform transform, in Entity entity)
        {
            var movementData = CalculateMovement(boid, transform);
            ApplyMovement(ref boid, ref transform, movementData);
        }

        private readonly MovementData CalculateMovement(BoidData boid, LocalTransform transform)
        {
            float3 currentPosition = transform.Position;
            float3 acceleration = CalculateAcceleration(boid, transform);
            float3 velocity = CalculateVelocity(boid, acceleration);

            // Apply boundary constraints
            var boundaryResult = BoidMath.CalculateBoundaryCollision(
                currentPosition,
                velocity,
                BoidBounds
            );

            // Calculate final rotation
            quaternion newRotation = BoidMath.LookAt(
                transform.Rotation,
                math.normalizesafe(boundaryResult.Velocity),
                boid.RotateSpeed,
                BoidSettings.AngleThreshold,
                DeltaTime
            );

            return new MovementData
            {
                Acceleration = acceleration,
                Velocity = boundaryResult.Velocity,
                Position = boundaryResult.Position + boundaryResult.Velocity * DeltaTime,
                Rotation = newRotation
            };
        }

        private readonly float3 CalculateVelocity(BoidData boid, float3 acceleration)
        {
            // Calculate new velocity
            float3 newVelocity = boid.Velocity + acceleration * DeltaTime;
            newVelocity *= math.pow(1.0f - boid.Drag, DeltaTime);
            newVelocity = BoidMath.ClampMax(newVelocity, boid.MaxSpeed);
            return newVelocity;
        }

        private readonly void ApplyMovement(ref BoidData boid, ref LocalTransform transform, MovementData movement)
        {
            boid.Acceleration = movement.Acceleration;
            boid.Velocity = movement.Velocity;
            transform.Position = movement.Position;
            transform.Rotation = movement.Rotation;
        }

        private readonly float3 CalculateAcceleration(BoidData boid, LocalTransform transform)
        {
            float3 currentPosition = transform.Position;
            float3 boidDirection = CalculateBoidDirectionFromHash(currentPosition);
            float3 obstacleAversion = CalculateObstacleDirection(currentPosition);
            float3 targetPosition = CalculateTargetPosition(currentPosition);
            float3 targetVector = targetPosition - currentPosition;
            float3 distanceToTarget = math.distance(targetPosition, currentPosition);

            float3 defaultDirection = math.select(
                transform.Forward(),
                math.normalizesafe(boid.Velocity),
                math.length(boid.Velocity) > BoidSettings.MinVelocityThreshold
            );

            float3 combinedDirection = math.select(
                math.normalizesafe(defaultDirection + BoidSettings.BoidWeight * boidDirection),
                math.normalizesafe(BoidSettings.DestinationWeight * math.normalizesafe(targetVector) + BoidSettings.BoidWeight * boidDirection),
                distanceToTarget < BoidSettings.DestinationPerceptionRadius
            );

            float3 finalDirection = math.select(
                OBSTACLE_AVOID_SPEED * boid.BaseSpeed * math.normalizesafe(obstacleAversion + boidDirection),
                combinedDirection * boid.BaseSpeed,
                math.all(obstacleAversion == float3.zero)
            );

            float3 acceleration = finalDirection - boid.Velocity;
            acceleration = BoidMath.CalculateTurn(boid.Velocity, acceleration, boid.MaxTurnAngle);

            //Debug only
            //Color debugColor = Color.green;
            //debugColor = (math.all(distanceToTarget < BoidSettings.DestinationPerceptionRadius)) ? Color.green : Color.red;
            //debugColor = (math.all(obstacleAversion == float3.zero)) ? debugColor : Color.blue;
            //Debug.DrawLine(currentPosition, currentPosition + boidDirection, debugColor);
            //Debug.DrawLine(currentPosition, currentPosition + (finalDirection - boid.Velocity), Color.white);
            //Debug.DrawLine(currentPosition, currentPosition + acceleration, Color.magenta);
            //return math.normalizesafe(finalDirection - boid.Velocity);
            acceleration = BoidMath.ClampMax(acceleration, boid.MaxAcceleration);
            return acceleration;
        }

        //Not used, change behaviour to escape obstacle at 45 degree angle
        private readonly float3 CalculateObstacleDirection2(float3 currentPosition)
        {
            float3 avoidanceForce = float3.zero;

            for (int i = 0; i < ObstaclePositions.Length; i++)
            {
                float obstacleRadius = ObstacleRadius[i];
                float obstacleForce = ObstacleForces[i];
                float3 toObstacle = ObstaclePositions[i] - currentPosition;
                float distance = math.length(toObstacle);

                if (distance < obstacleRadius)
                {
                    float3 normalizedToObstacle = math.normalize(toObstacle);

                    // Calculate perpendicular directions (left and right of predator approach)
                    float3 perpendicularRight = math.cross(normalizedToObstacle, new float3(0, 1, 0));
                    float3 perpendicularLeft = -perpendicularRight;

                    // Create 45-degree escape vectors by blending perpendicular and away directions
                    float3 escapeRight = math.normalize(perpendicularRight - normalizedToObstacle);
                    float3 escapeLeft = math.normalize(perpendicularLeft - normalizedToObstacle);

                    // Choose escape direction based on current position relative to obstacle
                    // This creates a natural split in the school
                    float3 escapeDirection = math.select(
                        escapeRight,
                        escapeLeft,
                        math.dot(perpendicularRight, currentPosition) < 0
                    );

                    float forceScale = math.saturate(math.abs(obstacleRadius - distance) / obstacleRadius);
                    avoidanceForce += forceScale * obstacleForce * escapeDirection;
                }
            }
            return avoidanceForce;
        }

        private readonly float3 CalculateObstacleDirection(float3 currentPosition)
        {
            float3 avoidanceForce = float3.zero;

            for (int i = 0; i < ObstaclePositions.Length; i++)
            {
                float obstacleRadius = ObstacleRadius[i];
                float obstacleForce = ObstacleForces[i];
                float3 toObstacle = ObstaclePositions[i] - currentPosition;
                float distance = math.length(toObstacle);
                bool isInsideRadius = distance < obstacleRadius;
                float forceScale = math.saturate(math.abs(obstacleRadius - distance) / obstacleRadius);
                float3 scaledForce = forceScale * obstacleForce * -math.normalizesafe(toObstacle); ;
                float3 finalForce = math.select(float3.zero, scaledForce, isInsideRadius);
                avoidanceForce += finalForce;
            }
            return avoidanceForce;
        }

        private readonly float3 CalculateTargetPosition(float3 currentPosition)
        {
            float3 destination;
            int nearestPositionIndex = 0;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < TargetPositions.Length; i++)
            {
                float targetRadius = TargetRadius[i];
                float targetForce = TargetForces[i];
                float3 toTarget = TargetPositions[i] - currentPosition;

                var distance = math.length(toTarget);
                if (distance > targetRadius) continue;
                var targetAttraction = distance / targetForce;
                var nearest = targetAttraction < nearestDistance;

                nearestDistance = math.select(nearestDistance, targetAttraction, nearest);
                nearestPositionIndex = math.select(nearestPositionIndex, i, nearest);
            }
            destination = (nearestPositionIndex == 0 && TargetPositions.Length == 0) ? float3.zero : TargetPositions[nearestPositionIndex];
            return destination;
        }

        //Not used
        private readonly float3 CalculateTargetDirection(float3 currentPosition)
        {

            float3 targetDirection = float3.zero;

            for (int i = 0; i < TargetPositions.Length; i++)
            {
                float targetRadius = TargetRadius[i];
                float targetForce = TargetForces[i];
                float3 toTarget = TargetPositions[i] - currentPosition;
                float distance = math.length(toTarget);

                // Skip if we're too far from this target volume
                if (distance > targetRadius * 2f) continue;

                // If we're inside the target volume, calculate an outward drift
                if (distance <= targetRadius)
                {
                    // Gentle drift direction based on current position in volume
                    float3 driftDir = math.normalizesafe(currentPosition - TargetPositions[i]);
                    // Reduced force when inside volume
                    float insideForce = targetForce * 0.1f;
                    targetDirection += (1f - distance / targetRadius) * insideForce * driftDir;
                    continue;
                }

                // Calculate attraction to nearest point on sphere surface
                float surfaceDistance = distance - targetRadius;
                float attractionStrength = targetForce / (surfaceDistance * surfaceDistance);
                targetDirection += math.normalizesafe(toTarget) * attractionStrength;
            }

            // If no valid targets found, return zero vector
            if (math.all(targetDirection == float3.zero))
            {
                return currentPosition;
            }

            return currentPosition + math.normalizesafe(targetDirection);
        }

        //Not used
        private readonly float3 CalculateBoidDirectionFromAll(float3 currentPosition, float3 currentVelocity)
        {
            float3 separation = float3.zero;
            float3 alignment = float3.zero;
            float3 cohesion = float3.zero;

            int aligmentNeighbor = 0;
            int cohesionNeighbor = 0;

            for (int i = 0; i < BoidPositions.Length; i++)
            {
                float3 otherPosition = BoidPositions[i];
                float3 otherVelocity = BoidVelocities[i];
                float distance = math.distance(currentPosition, otherPosition);

                if (distance > 0)
                {
                    if (distance < BoidSettings.PerceptionRadius)
                    {
                        // Separation: steer to avoid crowding neighbors
                        separation += math.normalizesafe(currentPosition - otherPosition) / math.max(distance * distance, 0.01f);
                        // Alignment: steer towards the average heading of neighbors    
                        alignment += otherVelocity;
                        aligmentNeighbor++;
                        // Cohesion: steer to move towards the average position of neighbors
                        cohesion += otherPosition;
                        cohesionNeighbor++;
                    }
                }

            }

            if (aligmentNeighbor > 0) { alignment = math.normalizesafe(alignment / aligmentNeighbor); }
            if (cohesionNeighbor > 0) { cohesion = math.normalizesafe((cohesion / cohesionNeighbor) - currentPosition); }

            float3 boidDirection = (BoidSettings.SeparationWeight * separation) +
                                    (BoidSettings.AlignmentWeight * alignment) +
                                    (BoidSettings.CohesionWeight * cohesion);
            if (math.length(boidDirection) < 0.1f)
            {
                return float3.zero;
            }
            return math.normalizesafe(boidDirection);
        }

        private readonly float3 CalculateBoidDirectionFromHash(float3 currentPosition)
        {
            BoidNeighborData neighborData = GatherNeighborData(currentPosition);
            float3 obstacle = CalculateObstacleDirection(currentPosition);

            // Note that by reducing the separation forces, the boid can get unstuck from the center of multiple boid more easily.
            // otherwise, the boid can get stuck inside a large school because of multiple separation forces pushing a boid towards the center.

            // Calculate density factor (higher density = stronger separation, weaker cohesion)
            // After accumulating
            float densityFactor = math.saturate(neighborData.localDensity / BoidSettings.OptimalNeighborCount);

            // Modify separation to increase up to a point, then decrease
            float separationCurve = 1f - math.abs(densityFactor - DENSITY_PEAK);
            float separationWeight = BoidSettings.SeparationWeight * (separationCurve);

            // Reduce cohesion more aggressively in high density
            float cohesionWeight = BoidSettings.CohesionWeight * math.exp(-densityFactor * DENSITY_FALLOFF);

            float3 separation = math.select(
                float3.zero,
                //neighborData.separation / math.max(neighborData.separationCount, 1),
                neighborData.separation,
                neighborData.separationCount > 0
            );

            float3 alignment = math.select(
                float3.zero,
                neighborData.alignment / math.max(neighborData.alignmentCount, 1),
                neighborData.alignmentCount > 0
            );

            float3 cohesion = math.select(
                float3.zero,
                (neighborData.cohesion / math.max(neighborData.cohesionCount, 1)) - currentPosition,
                neighborData.cohesionCount > 0
            );

            float3 boidDirection = (BoidSettings.SeparationWeight * separation) +
                                 (BoidSettings.AlignmentWeight * alignment) +
                                 (BoidSettings.CohesionWeight * cohesion);


            return math.select(
                float3.zero,
                math.normalizesafe(boidDirection),
                math.length(boidDirection) >= BoidSettings.MinDirectionMagnitude
            );
        }

        private struct BoidNeighborData
        {
            public float3 separation;
            public float3 alignment;
            public float3 cohesion;
            public int separationCount;
            public int alignmentCount;
            public int cohesionCount;
            public float localDensity;
        }

        private readonly BoidNeighborData GatherNeighborData(float3 currentPosition)
        {
            var data = new BoidNeighborData
            {
                separation = float3.zero,
                alignment = float3.zero,
                cohesion = float3.zero,
                separationCount = 0,
                alignmentCount = 0,
                cohesionCount = 0,
                localDensity = 0
            };

            foreach (float3 offset in MultiNeighborOffsets)
            {
                uint neighborHash = BoidMath.Hash(currentPosition + offset * BoidInverseCellSize, BoidInverseCellSize);
                if (!BoidHashMap.TryGetFirstValue(neighborHash, out int neighborIndex, out var iterator))
                    continue;
                do
                {
                    ProcessNeighbor(neighborIndex, currentPosition, ref data);
                    if (data.separationCount > BoidSettings.MaxNeighbor)
                    {
                        break;
                    }
                }
                while (BoidHashMap.TryGetNextValue(out neighborIndex, ref iterator));
            }
            return data;
        }

        private readonly void ProcessNeighbor(int neighborIndex, float3 currentPosition, ref BoidNeighborData data)
        {
            float3 otherPosition = BoidPositions[neighborIndex];
            float3 otherVelocity = BoidVelocities[neighborIndex];
            float distance = math.distance(currentPosition, otherPosition);

            bool isValidNeighbor = distance > 0 && distance < BoidSettings.PerceptionRadius;
            if (!isValidNeighbor) return;

            // Calculate density influence with quadratic falloff
            float distanceFactor = math.pow(1f - (distance / BoidSettings.PerceptionRadius), 2);
            data.localDensity += distanceFactor;

            //float3 distanceFromTo = currentPosition - otherPosition;
            //data.separation += (distanceFromTo / math.distance(currentPosition, otherPosition));

            float3 directionToNeighbor = (currentPosition - otherPosition);
            data.separation += directionToNeighbor / math.max(distance * distance, 0.01f);
            data.alignment += otherVelocity * distanceFactor;
            data.cohesion += otherPosition;

            data.separationCount++;
            data.alignmentCount++;
            data.cohesionCount++;
        }

        //Not used
        private readonly BoidNeighborData GatherNeighborData_All(float3 currentPosition)
        {
            var data = new BoidNeighborData
            {
                separation = float3.zero,
                alignment = float3.zero,
                cohesion = float3.zero,
                separationCount = 0,
                alignmentCount = 0,
                cohesionCount = 0,
                localDensity = 0
            };

            foreach (float3 offset in SingleNeighborOffsets)
            {
                uint neighborHash = BoidMath.Hash(currentPosition + offset * BoidInverseCellSize, BoidInverseCellSize);

                for (int i = 0; i < BoidPositions.Length; i++)
                {
                    ProcessNeighbor(i, currentPosition, ref data);
                    if (data.separationCount > BoidSettings.MaxNeighbor)
                    {
                        break;
                    }
                }
            }
            return data;
        }

        private struct MovementData
        {
            public float3 Acceleration;
            public float3 Velocity;
            public float3 Position;
            public quaternion Rotation;
        }
    }
}