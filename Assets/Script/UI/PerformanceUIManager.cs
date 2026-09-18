using TMPro;
using Unity.Entities;
using Unity6Demo.Input;
using Unity6Demo.Input.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
namespace Unity6Demo.UI
{
    public class PerformanceUIManager : MonoBehaviour
    {
        [Header("Performance Display")]
        [SerializeField] private TextMeshProUGUI entityCountText;
        [SerializeField] private TextMeshProUGUI fpsText;
        [SerializeField] private TextMeshProUGUI jobTimeText;
        [SerializeField] private TextMeshProUGUI memoryText;

        [SerializeField] private XRRayInteractor rightRay;
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private Button showButton;
        [SerializeField] private GameObject leftController;
        private FollowMeUI followMeUI;
        private readonly float[] fpsBuffer = new float[30];
        private int fpsBufferIndex = 0;
        private float nextUpdateTime;
        private const float UPDATE_INTERVAL = 0.5f;
        private bool isXREnable = false;
        private bool uiEnabled = true;

        // Entity management
        private EntityManager entityManager;
        private Entity uiEntity;

        private void Start()
        {
            isXREnable = CameraManager.Instance.IsXREnable;
            InitializeComponents();
            InitializeEntitySystem();
        }

        private void InitializeComponents()
        {
            isXREnable = CameraManager.Instance.IsXREnable;
            showButton.onClick.AddListener(ToggleUI);

            // Add and configure FollowMeUI
            if (!TryGetComponent<FollowMeUI>(out followMeUI))
            {
                followMeUI = gameObject.AddComponent<FollowMeUI>();
            }

            if (isXREnable)
            {
                //GameObject leftController = GameObject.Find("Left Controller");
                followMeUI.SetAttachTreansform(leftController.transform);
            }
        }

        private void InitializeEntitySystem()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            uiEntity = entityManager.CreateSingleton<UIDataSingleton>();
        }

        private void Update()
        {
            UpdatePerformanceMetrics();
            UpdateEntityCount();
        }

        private void UpdatePerformanceMetrics()
        {
            // Update FPS buffer
            fpsBuffer[fpsBufferIndex] = 1f / Time.deltaTime;
            fpsBufferIndex = (fpsBufferIndex + 1) % fpsBuffer.Length;

            if (Time.time >= nextUpdateTime)
            {
                float averageFps = CalculateAverageFPS();
                float jobTime = Time.deltaTime * 1000f;
                float totalMemoryMB = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);

                // Update UI with formatted strings
                if (fpsText != null) fpsText.text = $"FPS: {averageFps:F1}";
                if (jobTimeText != null) jobTimeText.text = $"Frame: {jobTime:F1}ms";
                if (memoryText != null) memoryText.text = $"Mem: {totalMemoryMB:F1}MB";

                nextUpdateTime = Time.time + UPDATE_INTERVAL;
            }
        }

        private float CalculateAverageFPS()
        {
            float sum = 0f;
            foreach (float fps in fpsBuffer)
                sum += fps;
            return sum / fpsBuffer.Length;
        }

        private void UpdateEntityCount()
        {
            if (entityManager != null && entityManager.Exists(uiEntity))
            {
                var uiData = entityManager.GetComponentData<UIDataSingleton>(uiEntity);
                if (entityCountText != null)
                    entityCountText.text = $"Entities: {uiData.EntityCount:N0}";
            }
        }

        public void OnClickShowButton()
        {
            showButton.onClick?.Invoke();
        }

        public void ToggleUI()
        {
            if (CheckUIValid())
            {
                uiEnabled = !uiEnabled;
                mainPanel.SetActive(uiEnabled);
            }
        }

        private bool CheckUIValid()
        {
            if (isXREnable)
            {
                if (rightRay.TryGetCurrent3DRaycastHit(out var hit))
                {
                    if (hit.transform == showButton.transform)
                    {
                        return true;
                    }
                }
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    public struct UIDataSingleton : IComponentData
    {
        public float EntityCount;
    }
}