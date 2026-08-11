using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;
using System.Linq;

[RequireComponent(typeof(ARCameraManager))]
public class ARCameraConfigSelector : MonoBehaviour
{
    private ARCameraManager cameraManager;

    void Awake()
    {
        cameraManager = GetComponent<ARCameraManager>();
    }

    void OnEnable()
    {
        cameraManager.frameReceived += OnFrameReceived;
    }

    void OnDisable()
    {
        cameraManager.frameReceived -= OnFrameReceived;
    }

    void OnFrameReceived(ARCameraFrameEventArgs args)
    {
        using (NativeArray<XRCameraConfiguration> configs =
               cameraManager.GetConfigurations(Allocator.Temp))
        {
            if (!configs.IsCreated || configs.Length == 0)
                return;

            XRCameraConfiguration lowest = configs
                .OrderBy(c => c.width * c.height)
                .First();

            if (cameraManager.currentConfiguration == null ||
                cameraManager.currentConfiguration.Value.width != lowest.width ||
                cameraManager.currentConfiguration.Value.height != lowest.height)
            {
                cameraManager.currentConfiguration = lowest;
                Debug.Log($"Camera config switched to lowest resolution: {lowest.width}x{lowest.height}");
            }
        }

        // Only need to do this once.
        cameraManager.frameReceived -= OnFrameReceived;
    }
}