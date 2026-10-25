using UnityEngine;

public class TouchRotate : MonoBehaviour
{
    public float rotationSpeed = 0.8f;
    public bool rotateAroundVisualCenter;

    private Vector3 visualCenterLocal;

    private void Awake()
    {
        if (rotateAroundVisualCenter)
            visualCenterLocal = CalculateVisualCenterLocal();
    }

    void Update()
    {
        if (Input.touchCount != 1)
            return;

        Touch touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Moved)
            return;

        // Use screen-relative swipe directions and world axes so an authored model
        // orientation (such as Brain's) cannot change which axis each swipe uses.
        Vector2 delta = touch.deltaPosition;
        float rotY = -delta.x * rotationSpeed;
        float rotX = delta.y * rotationSpeed;

        if (rotateAroundVisualCenter)
        {
            transform.RotateAround(transform.TransformPoint(visualCenterLocal),
                Vector3.up, rotY);
            transform.RotateAround(transform.TransformPoint(visualCenterLocal),
                Vector3.right, rotX);
        }
        else
        {
            transform.Rotate(Vector3.up, rotY, Space.World);
            transform.Rotate(Vector3.right, rotX, Space.World);
        }
    }

    private Vector3 CalculateVisualCenterLocal()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;
        bool found = false;

        foreach (Renderer renderer in renderers)
        {
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
                Vector3 localCorner = transform.InverseTransformPoint(
                    renderer.transform.TransformPoint(meshCorner));
                min = Vector3.Min(min, localCorner);
                max = Vector3.Max(max, localCorner);
                found = true;
            }
        }

        return found ? (min + max) * 0.5f : Vector3.zero;
    }
}
