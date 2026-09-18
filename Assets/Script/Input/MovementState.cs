using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity6Demo.DOTS.Boid;
using Unity6Demo.DOTS.Spawn;
using Unity6Demo.Input.Core;
using UnityEngine;
using UnityEngine.UI;
namespace Unity6Demo.Input.State
{
    public class MovementState : IInputState
    {
        private readonly IInputRouter router;
        private EntityManager entityManager;
        private Entity destinationEntity;
        private Entity spawnConfigEntity;
        private System.Random random;
        private Button target;
        private float moveSpeed = 50f;
        private float rotateSpeed = 3f;
        private float verticalClamp = 80f;   // Maximum vertical angle in degrees

        private Vector3 currentMovement = Vector3.zero;
        //private Vector3 currentVRMovement = Vector3.zero;
        private float smoothTime = 0.1f;
        private Vector3 currentVelocity;
        private Transform currentTransform;
        //private Vector3 lastRotation = Vector3.zero;
        private Vector3 currentRotation = Vector3.zero;

        public MovementState(IInputRouter router)
        {
            this.router = router; random = new System.Random();
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            spawnConfigEntity = GetSpawnConfigEntity();
            destinationEntity = entityManager.CreateSingleton<BoidDestinationSingleton>();
            currentTransform = CameraManager.Instance.GetActiveCameraTransform();
            if (CameraManager.Instance.IsXREnable)
            {
                currentTransform = currentTransform.parent;
            }
        }

        //Remove this in the future with interactable on UI element
        public void SetButton(Button target)
        {
            this.target = target;
        }

        public void Enter()
        {
            router.Subscribe(InputActionType.Move, HandleMove);
            router.Subscribe(InputActionType.RiseFall, HandleRiseFall);
            router.Subscribe(InputActionType.Trigger, HandleTrigger);
            router.Subscribe(InputActionType.Grip, HandleGrip);
            router.Subscribe(InputActionType.Target, HandleTarget);
            router.Subscribe(InputActionType.MainButton, HandleButton);
            router.Subscribe(InputActionType.Rotate, HandleRotate);
            router.Subscribe(InputActionType.TargetButton, HandleTargetButton);
        }

        public void Exit()
        {
            router.Unsubscribe(InputActionType.Move, HandleMove);
            router.Unsubscribe(InputActionType.RiseFall, HandleRiseFall);
            router.Unsubscribe(InputActionType.Trigger, HandleTrigger);
            router.Unsubscribe(InputActionType.Grip, HandleGrip);
            router.Unsubscribe(InputActionType.Target, HandleTarget);
            router.Unsubscribe(InputActionType.MainButton, HandleButton);
            router.Unsubscribe(InputActionType.Rotate, HandleRotate);
            router.Unsubscribe(InputActionType.TargetButton, HandleTargetButton);
        }


        public void Update()
        {
            if (currentMovement != Vector3.zero)
            {
                UpdateMovement();
            }
        }

        private void HandleMove(InputData data)
        {
            if (data.TryGetValue<MoveInputValue>(out var moveValue))
            {
                if (data.IsVRInput)
                {
                    if (data.IsLeftHand)
                    {
                        // Handle left controller movement
                        currentMovement += new Vector3(moveValue.Movement.x, 0, moveValue.Movement.y);
                        //HandleLeftControllerMovement(moveValue.Movement);
                    }
                    else if (data.IsRightHand)
                    {
                        // Handle right controller movement
                        //HandleRightControllerMovement(moveValue.Movement);
                    }
                }
                else
                {
                    currentMovement += new Vector3(moveValue.Movement.x, 0, moveValue.Movement.y);
                    //UpdateMovement(moveValue.Movement);
                }
            }
        }

        private void HandleButton(InputData data)
        {
            if (data.TryGetValue<ButtonInputValue>(out var button))
            {
                if (!data.IsVRInput)
                {
                    SpawnBoid();
                }
            }
        }

        private void HandleRiseFall(InputData data)
        {
            if (data.TryGetValue<RiseFallInputValue>(out var riseFall))
            {
                currentMovement += new Vector3(0, riseFall.RiseFall, 0);
            }
        }

        private void HandleTarget(InputData data)
        {
            if (data.TryGetValue<TargetInputValue>(out var targetValue))
            {
                if (data.IsVRInput)
                {
                    if (data.IsRightHand)
                    {
                        entityManager.SetComponentData(destinationEntity, new BoidDestinationSingleton
                        {
                            Destination = new float3(targetValue.Position)
                        });
                    }
                }
                else
                {
                    entityManager.SetComponentData(destinationEntity, new BoidDestinationSingleton
                    {
                        Destination = new float3(targetValue.Position)
                    });
                }
            }
        }

        //Bad solution, UI element should handle interactable event, not input handling.
        private void HandleTargetButton(InputData data)
        {
            if (data.TryGetValue<TargetButtonInputValue>(out var rotateValue))
            {
                if (rotateValue.HitTarget == target.transform)
                {
                    target.onClick?.Invoke();
                }
            }
        }
        private void HandleRotate(InputData data)
        {
            if (data.TryGetValue<RotateInputValue>(out var rotateValue))
            {
                currentRotation.x += rotateValue.Rotation.y * rotateSpeed * Time.deltaTime;
                currentRotation.y -= rotateValue.Rotation.x * rotateSpeed * Time.deltaTime;
                currentRotation.x = Mathf.Clamp(currentRotation.x, -verticalClamp, verticalClamp);
                currentTransform.rotation = Quaternion.Euler(currentRotation);
            }
        }

        private void HandleTrigger(InputData data)
        {
            if (data.TryGetValue<TriggerInputValue>(out var triggerValue))
            {
                if (data.IsLeftHand)
                {

                }
                else
                {

                }
                // Handle trigger
            }
        }

        private void HandleGrip(InputData data)
        {
            if (data.TryGetValue<GripInputValue>(out var grip))
            {
                if (data.IsRightHand)
                {
                    SpawnBoid();
                }
            }
        }

        private void UpdateMovement()
        {
            Quaternion horizontalRotation = Quaternion.Euler(0, currentTransform.rotation.eulerAngles.y, 0);
            Vector3 rotatedMovement = horizontalRotation * currentMovement;

            Vector3 targetPosition = currentTransform.position + moveSpeed * Time.deltaTime * rotatedMovement;
            //Debug.Log($"currentMovement:{currentMovement}, rotatedMovement:{rotatedMovement}, currentPosition:{currentTransform.position},finalPosition:{targetPosition}");
            currentTransform.position = Vector3.SmoothDamp(
                currentTransform.position,
                targetPosition,
                ref currentVelocity,
                smoothTime
            );
            currentMovement = Vector3.zero;
        }

        private void HandleRightControllerMovement(Vector2 input)
        {
            if (input != Vector2.zero)
            {
                Vector3 target = input * rotateSpeed * Time.deltaTime;
                target.y = Mathf.Clamp(target.y, -verticalClamp, verticalClamp);
                currentTransform.rotation = Quaternion.Euler(target);
            }

            if (input != Vector2.zero)
            {
                float yawDelta = input.x * rotateSpeed * Time.deltaTime;
                currentTransform.parent.Rotate(Vector3.up, yawDelta, Space.World);

                float pitchDelta = -input.y * rotateSpeed * Time.deltaTime;
                //pitch = Mathf.Clamp(pitch + pitchDelta, -verticalClamp, verticalClamp);
                //currentTransform.localRotation = Quaternion.Euler(pitch, 0, 0);
            }
        }

        private void SpawnBoid()
        {
            var config = entityManager.GetComponentData<SpawnConfig>(spawnConfigEntity);
            var spawnEventEntity = entityManager.CreateEntity(typeof(SpawnEventBuffer));
            var buffer = entityManager.GetBuffer<SpawnEventBuffer>(spawnEventEntity);
            buffer.Add(new SpawnEventBuffer
            {
                AmountToSpawn = config.AmountToSpawn,
                UniqueSeed = random.Next()
            });
        }

        //In Start(), this won't fail, in Awake(), this fail to find.
        private Entity GetSpawnConfigEntity()
        {
            var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<SpawnConfig>()
                .Build(entityManager);

            // Get all entities with SpawnConfig
            var entities = query.ToEntityArray(Allocator.Temp);

            try
            {
                if (entities.Length > 0)
                {
                    // Take the first SpawnConfig entity we find
                    // You might want to add additional criteria if you have multiple SpawnConfigs
                    return entities[0];
                }

                Debug.LogError("No SpawnConfig entities found!");
                return Entity.Null;
            }
            finally
            {
                // Clean up the native array
                if (entities.IsCreated)
                {
                    entities.Dispose();
                }
                query.Dispose();
            }
        }

    }
}