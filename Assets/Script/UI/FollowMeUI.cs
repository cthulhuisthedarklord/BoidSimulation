using Unity6Demo.Input;
using UnityEngine;
//using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.UI;

namespace Unity6Demo.UI
{
    public class FollowMeUI : MonoBehaviour
    {
        [Header("Follow Settings")]
        [SerializeField] private float defaultDistance = 0.5f;
        [SerializeField] private float followSpeed = 5f;
        [SerializeField] private bool followHorizontalOnly = false;

        [Header("Auto Hide Settings")]
        [SerializeField] private bool autoHide = false;
        [SerializeField] private float hideAngle = 1f;
        [SerializeField] private float fadeDuration = 1f;

        private CanvasGroup canvasGroup;
        private Canvas canvas;
        private Transform attachTransform;

        private void Start()
        {
            SetupComponents();
        }

        private void SetupComponents()
        {
            if (!TryGetComponent<CanvasGroup>(out canvasGroup))
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (!TryGetComponent<Canvas>(out canvas))
                canvas = gameObject.AddComponent<Canvas>();

            canvas.renderMode = RenderMode.WorldSpace;
            //canvas.worldCamera = CameraManager.Instance.GetActiveCamera();
        }

        private void Update()
        {
            UpdatePosition();
            if (autoHide) UpdateVisibility();
        }

        private void UpdatePosition()
        {
            if (attachTransform != null)
            {
                UpdatePositionToAttach();
            }
            else
            {
                UpdatePositionNoAttach();
            }
        }

        private void UpdatePositionToAttach()
        {
            // Calculate target position relative to left controller
            Vector3 targetPosition = attachTransform.position +
                                   attachTransform.right * defaultDistance * 0.5f +
                                   attachTransform.forward * defaultDistance;

            // Update position with smooth follow
            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                Time.deltaTime * followSpeed
            );

            UpdateRotation(Camera.main.transform.position);
        }

        private void UpdatePositionNoAttach()
        {
            /*
            Transform currentCamera = CameraManager.Instance.GetActiveCameraTransform();

            // Calculate target position relative to camera
            Vector3 targetPosition = currentCamera.position +
                                   currentCamera.right * defaultDistance * 0.5f +
                                   currentCamera.forward * defaultDistance;

            if (followHorizontalOnly)
            {
                targetPosition.y = transform.position.y;
            }

            // Update position with smooth follow
            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                Time.deltaTime * followSpeed
            );

            UpdateRotation(currentCamera.position);
            */
        }

        private void UpdateRotation(Vector3 lookSource)
        {
            // Calculate look direction
            Vector3 lookDirection = lookSource - transform.position;
            if (followHorizontalOnly)
            {
                lookDirection.y = 0;
            }

            // Smooth rotation
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                Quaternion.LookRotation(-lookDirection, Vector3.up),
                Time.deltaTime * followSpeed
            );
        }

        private void UpdateVisibility()
        {
            float angle = Vector3.Angle(
                Camera.main.transform.forward,
                transform.position - Camera.main.transform.position
            );

            float targetAlpha = angle > hideAngle ? 0f : 1f;
            canvasGroup.alpha = Mathf.Lerp(
                canvasGroup.alpha,
                targetAlpha,
                Time.deltaTime / fadeDuration
            );
        }

        public void SetAttachTreansform(Transform transformToAttach)
        {
            attachTransform = transformToAttach;
        }

        // Public methods for external control
        public void SetVisibility(bool visible)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
        }

        public void SetFollowSpeed(float speed)
        {
            followSpeed = speed;
        }

        public void SetDistance(float distance)
        {
            defaultDistance = distance;
        }
    }
}