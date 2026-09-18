using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
namespace Unity6Demo.DOTS.Spawn
{
    public class SpawnAuthoring : MonoBehaviour
    {
        [SerializeField] public GameObject prefab;
        [SerializeField] public int AmountToSpawn;
        [SerializeField] public float Speed;
        [SerializeField] public float RotateSpeed;
        [SerializeField] public float Scale;
        [SerializeField] public Vector3 Initial = Vector3.zero;
        [SerializeField] public Vector3 Destination = Vector3.zero;
        [SerializeField] public float LifeTime;
        [SerializeField] public float Drag;
        [SerializeField] public float MaxSpeed;
        [SerializeField] public float MaxAcceleration;
        [SerializeField] public float MaxTurnAngle;

        public SpawnConfig_Mono spawnConfig_mono;

        private void Awake()
        {
            spawnConfig_mono = new SpawnConfig_Mono
            {
                ballPrefab = prefab,
                amountToSpawn = AmountToSpawn,
                initial = Initial,
                destination = Destination,
                Speed = Speed,
                RotateSpeed = RotateSpeed,
                Scale = Scale,
                ballLifeTime = LifeTime
            };
        }

        private class Baker : Baker<SpawnAuthoring>
        {
            public override void Bake(SpawnAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new SpawnConfig
                {
                    BoidPrefabEntity = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic),
                    AmountToSpawn = authoring.AmountToSpawn,
                    Initial = authoring.Initial,
                    Destination = authoring.Destination,
                    Speed = authoring.Speed,
                    RotateSpeed = authoring.RotateSpeed,
                    Scale = authoring.Scale,
                    BallLifeTime = authoring.LifeTime,
                    Drag = authoring.Drag,
                    MaxAcceleration = authoring.MaxAcceleration,
                    MaxTurnAngle = authoring.MaxTurnAngle,
                    MaxSpeed = authoring.MaxSpeed,
                });
            }
        }
    }

    public struct SpawnConfig : IComponentData
    {
        public Entity BoidPrefabEntity;
        public float3 Initial;
        public float3 Destination;
        public float Speed;
        public float RotateSpeed;
        public float MaxSpeed;
        public float MaxAcceleration;
        public float Drag;
        public float Scale;
        public int AmountToSpawn;
        public float BallLifeTime;
        public float MinTurnRadius;
        public float BankingIntensity;
        public float TurnForce;
        public float MaxTurnAngle;
    }

    public struct SpawnConfig_Mono
    {
        public GameObject ballPrefab;
        public float3 initial;
        public float3 destination;
        public float Speed;
        public float RotateSpeed;
        public float Scale;
        public int amountToSpawn;
        public float ballLifeTime;
    }
}