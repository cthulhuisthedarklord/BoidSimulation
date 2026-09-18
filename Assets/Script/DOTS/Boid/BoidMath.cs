using System.Diagnostics;
using Unity.Collections;
using Unity.Mathematics;

namespace Unity6Demo.DOTS.Boid
{
    public static class BoidMath
    {
        // Constants moved to static readonly fields for better performance
        private static readonly float3 UP_VECTOR = new float3(0, 1, 0);
        private static readonly float MIN_VELOCITY_THRESHOLD = 0.001f;
        private static readonly float MIN_VELOCITY_THRESHOLD_SQ = 1e-6f;
        private static readonly float MIN_BOUNCE_VELOCITY = 2.0f;
        //private static readonly float MAX_BANK_ANGLE = math.PI / 6;
        private static readonly float ELASTICITY = 0.8f;
        private static readonly float RANDOMNESS = 0.1f;
        private static readonly float SAFETY_MARGIN = 0.1f;

        //private static readonly float TURN_FORCE = 10f;

        public static uint Hash(float3 position, float inverseCellSize)
        {
            int3 result = (int3)math.floor(position * inverseCellSize);
            return math.hash(result);
        }

        public static float3 ClampMax(float3 clamped, float max)
        {
            float magnitude = math.length(clamped);
            float3 result = math.select(
                                clamped,
                                math.normalizesafe(clamped) * max,
                                magnitude > max
                                );
            return result;
        }

        public static BoundaryCollisionResult CalculateBoundaryCollision(
            float3 currentPosition,
            float3 currentVelocity,
            BoidBound boidBounds)
        {
            float3 displacement = currentPosition - boidBounds.center;
            float3 boundary = (boidBounds.extent - boidBounds.center) - SAFETY_MARGIN;

            // Calculate absolute displacement once
            float3 absDisplacement = math.abs(displacement);

            // Calculate collision masks using vectorized operations
            bool3 isOutsideBounds = absDisplacement >= boundary;
            bool3 isNearBoundary = absDisplacement >= boundary * 0.99f;
            bool3 isMovingOutward = math.sign(currentVelocity) == math.sign(displacement);

            // Combine conditions
            bool3 needsCorrection = isOutsideBounds | (isNearBoundary & isMovingOutward);

            // Early exit if no collision
            if (!math.any(needsCorrection))
            {
                return new BoundaryCollisionResult
                {
                    Position = currentPosition,
                    Velocity = currentVelocity,
                    CollisionOccurred = false
                };
            }

            // Generate random perturbation once
            Random random = new Random((uint)(math.hash(currentPosition) % uint.MaxValue));
            float3 randomOffset = new float3(
                random.NextFloat(-1f, 1f),
                random.NextFloat(-1f, 1f),
                random.NextFloat(-1f, 1f)
            ) * RANDOMNESS;

            // Calculate normal vectors
            float3 normal = math.select(float3.zero, math.sign(displacement), needsCorrection);
            normal += randomOffset;
            normal = math.normalize(normal);

            // Calculate reflection
            float3 reflectedVelocity = currentVelocity - 2 * math.dot(currentVelocity, normal) * normal;

            // Ensure minimum velocity using vectorized operations
            float currentSpeed = math.length(reflectedVelocity);
            float3 newVelocity = math.select(
                reflectedVelocity,
                math.normalize(reflectedVelocity) * MIN_BOUNCE_VELOCITY,
                currentSpeed < MIN_BOUNCE_VELOCITY
            ) * ELASTICITY;

            // Position correction
            float3 correction = math.select(
                float3.zero,
                (boundary - absDisplacement + SAFETY_MARGIN) * -math.sign(displacement),
                needsCorrection
            );

            float3 newPosition = currentPosition + correction;

            return new BoundaryCollisionResult
            {
                Position = newPosition,
                Velocity = newVelocity,
                CollisionOccurred = true
            };
        }

        public static float3 CalculateTurn(float3 currentVelocity, float3 desiredAcceleration, float maxTurnAngle)
        {
            // Early exit if either vector is zero
            if (math.lengthsq(currentVelocity) < MIN_VELOCITY_THRESHOLD_SQ || math.lengthsq(desiredAcceleration) < MIN_VELOCITY_THRESHOLD_SQ)
                return desiredAcceleration;

            // Normalize vectors to work with directions
            float3 currentDir = math.normalize(currentVelocity);
            float3 targetDir = math.normalize(desiredAcceleration);

            // Calculate the angle between current and desired direction
            float dotProduct = math.dot(currentDir, targetDir);
            float radians = math.acos(math.clamp(dotProduct, -1f, 1f));

            // If angle is small enough, return the desired direction
            if (radians < MIN_VELOCITY_THRESHOLD)
                return desiredAcceleration;

            // Calculate the maximum turn angle based on maxTurnAcceleration
            // This effectively limits how many degrees the boid can turn per update
            float maxTurnRadians = maxTurnAngle * math.PI / 180f; // Convert to radians

            // Clamp the actual turn angle to our maximum
            float turnRadians = math.min(radians, maxTurnRadians);

            // Calculate the axis of rotation (perpendicular to both vectors)
            float3 rotationAxis = math.cross(currentDir, targetDir);
            if (math.lengthsq(rotationAxis) < MIN_VELOCITY_THRESHOLD_SQ)
            {
                // If vectors are parallel or anti-parallel, use an arbitrary perpendicular axis
                rotationAxis = math.cross(currentDir, new float3(0, 1, 0));
                if (math.lengthsq(rotationAxis) < MIN_VELOCITY_THRESHOLD_SQ)
                    rotationAxis = math.cross(currentDir, new float3(1, 0, 0));
            }
            rotationAxis = math.normalize(rotationAxis);

            // Create rotation quaternion for our limited angle
            quaternion rotation = quaternion.AxisAngle(rotationAxis, turnRadians);

            // Apply rotation to current direction
            float3 newDirection = math.rotate(rotation, currentDir);

            // Maintain the original desired speed
            float desiredSpeed = math.length(desiredAcceleration);
            float3 limitedTurn = newDirection * desiredSpeed;

            // Calculate acceleration needed to achieve this turn
            float3 acceleration = limitedTurn - currentVelocity;
            return acceleration;
        }

        public static quaternion CalculateBankRotation(float3 velocity, float3 acceleration, float bankingIntensity)
        {
            float velocityMagnitudeSq = math.lengthsq(velocity);
            float accelerationMagnitudeSq = math.lengthsq(acceleration);

            // Early exit using math.select
            bool shouldCalculate = velocityMagnitudeSq >= MIN_VELOCITY_THRESHOLD &&
                                 accelerationMagnitudeSq >= MIN_VELOCITY_THRESHOLD;

            float3 forward = math.normalize(velocity);
            float3 right = math.normalize(math.cross(forward, UP_VECTOR));
            float bankForce = math.dot(acceleration, right);

            quaternion result = quaternion.AxisAngle(forward, -bankForce * bankingIntensity);

            return shouldCalculate ? quaternion.identity : result;
        }

        public static void NearestPosition(NativeArray<float3> targets, float3 position, out int nearestPositionIndex, out float nearestDistance)
        {
            nearestPositionIndex = 0;
            nearestDistance = math.lengthsq(position - targets[0]);
            for (int i = 1; i < targets.Length; i++)
            {
                var targetPosition = targets[i];
                var distance = math.lengthsq(position - targetPosition);
                var nearest = distance < nearestDistance;

                nearestDistance = math.select(nearestDistance, distance, nearest);
                nearestPositionIndex = math.select(nearestPositionIndex, i, nearest);
            }
            nearestDistance = math.sqrt(nearestDistance);
        }

        public static quaternion LookAt(quaternion currentRotation, float3 lookDirection, float rotateSpeed, float angleThreshold, float deltaTime)
        {
            lookDirection = math.normalizesafe(lookDirection);
            float3 currentForward = math.forward(currentRotation);
            float dot = math.dot(currentForward, lookDirection);
            float angleDifference = math.acos(math.clamp(dot, -1f, 1f));

            // Always calculate target rotation
            quaternion targetRotation = quaternion.LookRotationSafe(lookDirection, math.up());

            // If angle is below threshold, return current rotation
            if (angleDifference <= math.radians(angleThreshold))
            {
                return currentRotation;
            }

            // Calculate smooth rotation factor based on angle difference
            float rotationSpeed = math.lerp(0.1f * rotateSpeed, rotateSpeed,
                angleDifference / math.PI); // Adjust rotation speed based on angle
            float rotate = math.clamp(deltaTime * rotationSpeed, 0, 1);

            // Smoothly interpolate to target rotation
            return math.slerp(currentRotation, targetRotation, rotate);
        }

        public struct BoundaryCollisionResult
        {
            public float3 Position;
            public float3 Velocity;
            public bool CollisionOccurred;
        }
    }
}