using UnityEngine;

public class ScreenLockFollower : MonoBehaviour
{
    [Tooltip("How far in front of the camera the model sits, in meters.")]
    public float distance = 0.4f;

    [Tooltip("Time, in seconds, used to filter small camera-pose jitter while locked.")]
    [Min(0.01f)]
    public float positionSmoothTime = 0.1f;

    [Tooltip("Ignore tiny AR pose changes so camera tracking noise does not shake the model.")]
    public float followDeadZone = 0.003f;

    private Camera cam;
    private Vector3 positionVelocity;
    private Renderer[] modelRenderers;
    private Vector3 visualCenterLocal;
    private bool positionFollowingPaused;

    void Awake()
    {
        // Cache the AR camera once when this dynamically-added component is created.
        cam = Camera.main;
        modelRenderers = GetComponentsInChildren<Renderer>(true);
        visualCenterLocal = CalculateVisualCenterLocal();
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

    public void SetPositionFollowingPaused(bool paused)
    {
        positionFollowingPaused = paused;
        positionVelocity = Vector3.zero;
    }

    void LateUpdate()
    {
        if (cam == null)
            cam = Camera.main;

        if (cam == null)
            return;

        if (positionFollowingPaused)
            return;

        Vector3 targetPosition = GetTargetPosition();
        if ((targetPosition - transform.position).sqrMagnitude <= followDeadZone * followDeadZone)
        {
            positionVelocity = Vector3.zero;
            return;
        }

        // Run after the camera's pose is updated, and only follow position so TouchRotate
        // remains independent. Follow the cached local bounds center, not each renderer's
        // changing world-space AABB; the latter shifts as the model rotates and causes jitter.
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref positionVelocity,
            positionSmoothTime
        );
    }

    private Vector3 GetTargetPosition()
    {
        Vector3 desiredVisualCenter =
            cam.transform.position + cam.transform.forward * distance;

        // HeartPrefab's visible mesh is offset beneath its root. Keep its actual
        // visual center in front of the camera without letting rotation change the
        // measured world AABB and feed a false position correction back into the model.
        Vector3 currentVisualCenter = transform.TransformPoint(visualCenterLocal);
        return transform.position + (desiredVisualCenter - currentVisualCenter);
    }

    private Vector3 CalculateVisualCenterLocal()
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

            Bounds meshBounds;
            SkinnedMeshRenderer skinned = renderer as SkinnedMeshRenderer;
            if (skinned != null)
            {
                meshBounds = skinned.localBounds;
            }
            else
            {
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                    continue;
                meshBounds = filter.sharedMesh.bounds;
            }

            Vector3 extents = meshBounds.extents;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 meshCorner = meshBounds.center +
                    Vector3.Scale(extents, new Vector3(x, y, z));
                Vector3 rootLocalCorner = transform.InverseTransformPoint(
                    renderer.transform.TransformPoint(meshCorner));
                if (!hasBounds)
                {
                    combinedBounds = new Bounds(rootLocalCorner, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(rootLocalCorner);
                }
            }
        }

        return hasBounds ? combinedBounds.center : Vector3.zero;
    }
}
