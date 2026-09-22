using UnityEngine;
using TMPro;

public class BillboardLabel : MonoBehaviour
{
    [Header("Screen-space label layout")]
    [SerializeField, Min(1f)] private float horizontalOffsetPixels = 110f;
    [SerializeField] private float verticalOffsetPixels = 34f;
    [SerializeField, Min(0.1f)] private float lineWidthPixels = 1.5f;
    [SerializeField] private Color leaderLineColor = new Color(0.92f, 0.97f, 1f, 0.9f);

    private Camera cachedCamera;
    private Transform textTransform;
    private Vector3 originalTextLocalScale;
    private LineRenderer leaderLine;
    private Material leaderLineMaterial;

    void Awake()
    {
        CacheTextTransform();
        ConfigureLeaderLine();
    }

    void LateUpdate()
    {
        if (cachedCamera == null)
            cachedCamera = Camera.main;

        if (cachedCamera == null || textTransform == null)
            return;

        // Keep this transform at its authored anatomical anchor.  Only its text child
        // moves, which means the anchor coordinates remain the source of truth.
        Vector3 anchorPosition = transform.position;
        Vector3 anchorOnScreen = cachedCamera.WorldToScreenPoint(anchorPosition);
        bool isInFrontOfCamera = anchorOnScreen.z > 0f;
        SetPresentationVisible(isInFrontOfCamera);
        if (!isInFrontOfCamera)
            return;

        // Place labels outside the model in viewport space.  The horizontal direction
        // is chosen from the anchor's side of the screen, so label paths fan outward.
        float horizontalDirection = anchorOnScreen.x >= Screen.width * 0.5f ? 1f : -1f;
        Vector3 labelOnScreen = anchorOnScreen + new Vector3(
            horizontalDirection * horizontalOffsetPixels,
            verticalOffsetPixels,
            0f);
        Vector3 labelPosition = cachedCamera.ScreenToWorldPoint(labelOnScreen);

        textTransform.position = labelPosition;
        textTransform.rotation = Quaternion.LookRotation(
            labelPosition - cachedCamera.transform.position,
            cachedCamera.transform.up);

        // Pinch-zoom scales the model hierarchy.  Inverting the parent scale keeps
        // text legible instead of shrinking into the mesh or growing with it.
        ApplyConstantTextScale();

        leaderLine.SetPosition(0, anchorPosition);
        leaderLine.SetPosition(1, labelPosition);
        float worldUnitsPerPixel = GetWorldUnitsPerPixel(anchorOnScreen.z);
        float lineWidth = Mathf.Max(0.0001f, worldUnitsPerPixel * lineWidthPixels);
        leaderLine.startWidth = lineWidth;
        leaderLine.endWidth = lineWidth;
    }

    void OnDestroy()
    {
        if (leaderLineMaterial != null)
            Destroy(leaderLineMaterial);
    }

    private void CacheTextTransform()
    {
        TMP_Text labelText = GetComponentInChildren<TMP_Text>(true);
        if (labelText == null)
            return;

        textTransform = labelText.transform;
        originalTextLocalScale = textTransform.localScale;
    }

    private void ConfigureLeaderLine()
    {
        leaderLine = GetComponent<LineRenderer>();
        if (leaderLine == null)
            leaderLine = gameObject.AddComponent<LineRenderer>();

        leaderLine.positionCount = 2;
        leaderLine.useWorldSpace = true;
        leaderLine.alignment = LineAlignment.View;
        leaderLine.startColor = leaderLineColor;
        leaderLine.endColor = leaderLineColor;
        leaderLine.numCapVertices = 2;

        Shader lineShader = Shader.Find("Sprites/Default");
        if (lineShader != null)
        {
            leaderLineMaterial = new Material(lineShader) { color = leaderLineColor };
            leaderLine.material = leaderLineMaterial;
        }
    }

    private void ApplyConstantTextScale()
    {
        if (textTransform.parent == null)
            return;

        Vector3 parentScale = textTransform.parent.lossyScale;
        textTransform.localScale = new Vector3(
            originalTextLocalScale.x / Mathf.Max(Mathf.Abs(parentScale.x), 0.0001f),
            originalTextLocalScale.y / Mathf.Max(Mathf.Abs(parentScale.y), 0.0001f),
            originalTextLocalScale.z / Mathf.Max(Mathf.Abs(parentScale.z), 0.0001f));
    }

    private float GetWorldUnitsPerPixel(float depth)
    {
        if (cachedCamera.orthographic)
            return cachedCamera.orthographicSize * 2f / Mathf.Max(1, Screen.height);

        float viewHeight = 2f * Mathf.Max(depth, 0.01f) *
                           Mathf.Tan(cachedCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        return viewHeight / Mathf.Max(1, Screen.height);
    }

    private void SetPresentationVisible(bool visible)
    {
        if (textTransform != null && textTransform.gameObject.activeSelf != visible)
            textTransform.gameObject.SetActive(visible);

        if (leaderLine != null)
            leaderLine.enabled = visible;
    }
}
