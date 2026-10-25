using UnityEngine;

/// <summary>Per-label authoring data stored on a model-local hotspot anchor.</summary>
public sealed class AnatomyHotspotAnchorSettings : MonoBehaviour
{
    [Min(1)] public int labelNumber = 1;
    public string labelTitle;
    [TextArea] public string description;
    public bool locked;
    [Min(0f)] public float focusZoom = 2.65f;
    public bool useCustomFocusDirection;
    public Vector3 focusDirectionLocal = Vector3.forward;
    public Vector2 popupOffset;
    public Transform visibilityTarget;
}
