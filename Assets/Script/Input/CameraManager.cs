using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
namespace Unity6Demo.Input
{
    public class CameraManager : MonoBehaviour
    {
        private static CameraManager instance;
        public static CameraManager Instance => instance;
        [SerializeField] private bool isXREnable = false;
        [SerializeField] private GameObject xrRig;
        [SerializeField] private Camera xrCamera;
        [SerializeField] private GameObject regularPlayerRig;
        [SerializeField] private Camera regularCamera;

        public bool IsXREnable { get => isXREnable; set => isXREnable = value; }

        private void Awake()
        {
            isXREnable = IsVREnabled();
            if (instance == null)
                instance = this;
            else
                Destroy(gameObject);
            SetupCameras();
        }
        
        private bool IsVREnabled()
        {
            
            // First check XR Management system
            var xrSettings = XRGeneralSettings.Instance;
            if (xrSettings?.Manager?.isInitializationComplete == true && xrSettings.Manager.activeLoader != null)
                return true;

            // Additional check for active XR displays
            var xrDisplaySubsystems = new List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(xrDisplaySubsystems);
            foreach (var xrDisplay in xrDisplaySubsystems)
            {
                if (xrDisplay.running)
                    return true;
            }

            return false;
        }

        private void SetupCameras()
        {
            // Enable/disable the appropriate camera setup
            xrRig.SetActive(isXREnable);
            regularPlayerRig.SetActive(!isXREnable);

            // Set the MainCamera tag
            if (isXREnable)
            {
                xrCamera.tag = "MainCamera";
                regularCamera.tag = "Untagged";
                xrRig.transform.parent.gameObject.SetActive(true);
            }
            else
            {
                regularCamera.tag = "MainCamera";
                xrCamera.tag = "Untagged";
                xrRig.transform.parent.gameObject.SetActive(false);
            }
            Camera.SetupCurrent(GetActiveCamera());
            GameObject temp = Camera.main.gameObject;
            // Force Unity to update the main camera reference
            temp.SetActive(false);
            temp.SetActive(true);
            if (Camera.main != GetActiveCamera())
            {
                Debug.LogError("Active Camera Not Main Camera!");
            }
        }

        // Public method to get the current active camera
        public Transform GetActiveCameraTransform()
        {
            return isXREnable ? xrCamera.transform : regularCamera.transform;
        }

        // Public method to get the current active camera
        public Camera GetActiveCamera()
        {
            return isXREnable ? xrCamera : regularCamera;
        }
        
    }
}