using TMPro;
using Unity.Entities;
using UnityEngine;

namespace Unity6Demo.DOTS.Other
{
    public class UIAuthoring : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI textM;
        EntityManager entityManager;
        Entity UIEntity;
        private void Awake()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            UIEntity = entityManager.CreateSingleton<UIDataSingleton>();
        }

        private void Update()
        {
            if (entityManager != null && entityManager.Exists(UIEntity))
            {
                var uiData = entityManager.GetComponentData<UIDataSingleton>(UIEntity);
                textM.text = $"Entity Count: {uiData.EntityCount}";
            }
        }
    }

    public struct UIDataSingleton : IComponentData
    {
        public int EntityCount;
    }
}