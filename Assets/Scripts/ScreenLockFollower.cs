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
    private Renderer[] modelRenderers;

    void Awake()
    {
        // Cache the AR camera once when this dynamically-added component is created.
        cam = Camera.main;
        modelRenderers = GetComponentsInChildren<Renderer>(true);
    }

    public void SnapToCamera()
    {
        if (cam == null)
            cam = Camera.main;

        if (cam == null)
            return;

        transform.position = GetTargetPosition();
        positionVelocity = Vector3.zero;
    }

    void LateUpdate()
    {
        if (cam == null)
            cam = Camera.main;

        if (cam == null)
            return;

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
        Vector3 desiredVisualCenter =
            cam.transform.position + cam.transform.forward * distance;

        // HeartPrefab's visible mesh is offset beneath its root.  Centering the root
        // itself therefore placed the actual heart off-centre.  Correct by the mesh
        // bounds each frame, while explicitly excluding screen-space label renderers.
        Vector3 currentVisualCenter = GetModelBoundsCenter();
        return transform.position + (desiredVisualCenter - currentVisualCenter);
    }

    private Vector3 GetModelBoundsCenter()
    {
        bool hasBounds = false;
        Bounds combinedBounds = new Bounds();

        foreach (Renderer renderer in modelRenderers)
        {
            if (renderer == null || !renderer.enabled ||
                renderer is LineRenderer ||
                renderer.GetComponent<TMPro.TMP_Text>() != null ||
                renderer.gameObject.name == "Label Selection Marker")
                continue;

            if (!hasBounds)
            {
                combinedBounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds ? combinedBounds.center : transform.position;
    }
}
