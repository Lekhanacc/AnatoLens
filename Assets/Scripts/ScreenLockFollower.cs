using UnityEngine;

public class ScreenLockFollower : MonoBehaviour
{
    [Tooltip("How far in front of the camera the model sits, in meters.")]
    public float distance = 0.4f;

    [Tooltip("Time, in seconds, used to filter small camera-pose jitter while locked.")]
    [Min(0.01f)]
    public float positionSmoothTime = 0.06f;

    private Camera cam;
    private Vector3 positionVelocity;

    void Awake()
    {
        // Cache the AR camera once when this dynamically-added component is created.
        cam = Camera.main;
    }

    public void SnapToCamera()
    {
        if (cam == null) return;

        transform.position = GetTargetPosition();
        positionVelocity = Vector3.zero;
    }

    void LateUpdate()
    {
        if (cam == null) return;

        // Run after the camera's pose is updated, and only follow position so TouchRotate
        // remains independent. A short SmoothDamp filters high-frequency pose noise without
        // making ordinary camera movement feel laggy.
        transform.position = Vector3.SmoothDamp(
            transform.position,
            GetTargetPosition(),
            ref positionVelocity,
            positionSmoothTime
        );
    }

    private Vector3 GetTargetPosition()
    {
        return cam.transform.position + cam.transform.forward * distance;
    }
}
