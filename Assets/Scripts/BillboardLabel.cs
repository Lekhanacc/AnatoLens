using UnityEngine;

public class BillboardLabel : MonoBehaviour
{
    private Transform cam;
    private Vector3 originalLocalScale;

    void Start()
    {
        if (Camera.main != null) cam = Camera.main.transform;
        originalLocalScale = transform.localScale;
    }

    void LateUpdate()
    {
        if (cam == null)
        {
            if (Camera.main != null) cam = Camera.main.transform;
            else return;
        }

        // Cancel out the parent's scale (from pinch-zoom) so text size stays constant.
        if (transform.parent != null)
        {
            Vector3 parentLossy = transform.parent.lossyScale;
            transform.localScale = new Vector3(
                originalLocalScale.x / Mathf.Max(parentLossy.x, 0.0001f),
                originalLocalScale.y / Mathf.Max(parentLossy.y, 0.0001f),
                originalLocalScale.z / Mathf.Max(parentLossy.z, 0.0001f)
            );
        }

        // Always face the camera, ignoring the heart's own rotation.
        transform.rotation = Quaternion.LookRotation(transform.position - cam.position, Vector3.up);
    }
}