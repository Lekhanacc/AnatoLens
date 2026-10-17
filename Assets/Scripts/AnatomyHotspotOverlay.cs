using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Numbered anatomy pins projected from model-local label anchors.</summary>
public sealed class AnatomyHotspotOverlay : MonoBehaviour
{
    private sealed class Hotspot
    {
        public string title;
        public string description;
        public Vector3 normalizedPosition;
        public Transform anchorTransform;
        public Vector3 explicitLocalPosition;
        public bool hasExplicitLocalPosition;
        public RectTransform pin;
    }

    private static readonly Color Ink = new Color32(8, 15, 24, 245);
    private static readonly Color Accent = new Color32(85, 221, 207, 255);
    private static readonly Color PinFill = new Color32(14, 25, 38, 232);
    private static readonly Dictionary<string, Hotspot[]> OrganHotspots = BuildOrganHotspots();

    public static AnatomyHotspotOverlay Active { get; private set; }
    public bool HasHotspots => hotspots != null && hotspots.Count > 0;

    private readonly List<Hotspot> hotspots = new List<Hotspot>();
    private Camera arCamera;
    private Canvas canvas;
    private RectTransform canvasRect;
    private GameObject card;
    private TMP_Text cardTitle;
    private TMP_Text cardDescription;
    private bool visible;
    private bool femaleSurfaceNormalsInitialized;
    private int selectedIndex = -1;
    private string organKey;
    private Bounds localBounds;
    private RectTransform cardRect;
    private Coroutine focusTransition;
    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private Vector3 baseLocalScale;
    private Quaternion baseWorldRotation;
    private bool hasBasePose;

    private bool UsesFocusedLabelPresentation =>
        string.Equals(organKey, "Female_Reproductive_system", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(organKey, "Heart", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(organKey, "Brain", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(organKey, "Eye", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(organKey, "Kidney", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(organKey, "Lung", StringComparison.OrdinalIgnoreCase);

    // ARCore owns the device camera pose. Repositioning the model relative to
    // that camera creates a smooth, per-label orbit and zoom while preserving AR.
    private const float FocusDistance = 0.48f;
    private const float FocusZoom = 2.65f;
    private const float FocusDuration = 0.7f;
    private const float CardWidth = 620f;
    private const float CardHeight = 460f;

    private void OnEnable()
    {
        if (HasHotspots)
            Active = this;
    }

    public void Configure(string key)
    {
        organKey = key;
        CreateHotspots();
        CreateOverlay();
        if (HasHotspots)
        {
            Active = this;
            // Hide the older heart-only text labels; the new pins are the label UI.
            LabelToggleController legacy = GetComponentInChildren<LabelToggleController>(true);
            if (legacy != null && legacy.labelsContainer != null)
                legacy.labelsContainer.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (focusTransition != null)
            StopCoroutine(focusTransition);
        focusTransition = null;
        if (hasBasePose && UsesFocusedLabelPresentation)
        {
            ApplyBasePose();
            hasBasePose = false;
        }
        SetScreenLockPositionPaused(false);
        if (canvas != null)
            canvas.gameObject.SetActive(false);
        visible = false;
        if (Active == this)
            Active = null;
    }

    private void OnDestroy()
    {
        if (canvas != null)
            Destroy(canvas.gameObject);
        if (Active == this)
            Active = null;
    }

    private void Update()
    {
        if (!visible || canvas == null)
            return;
        if (arCamera == null)
            arCamera = Camera.main;
        if (arCamera == null)
            return;

        for (int i = 0; i < hotspots.Count; i++)
        {
            Vector3 localPoint = HotspotLocalPoint(hotspots[i]);
            Vector3 screen = arCamera.WorldToScreenPoint(transform.TransformPoint(localPoint));
            bool onScreen = screen.z > 0f && screen.x >= 0f && screen.x <= Screen.width &&
                            screen.y >= 0f && screen.y <= Screen.height;
            if (onScreen && IsFemaleSurfaceHotspot(hotspots[i]))
            {
                Vector3 toCamera = arCamera.transform.position - hotspots[i].anchorTransform.position;
                onScreen = toCamera.sqrMagnitude > 0.000001f &&
                    Vector3.Dot(hotspots[i].anchorTransform.forward, toCamera.normalized) > 0f;
            }
            hotspots[i].pin.gameObject.SetActive(onScreen);
            if (onScreen && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, screen, null, out Vector2 local))
                hotspots[i].pin.anchoredPosition = local;
        }

        if (selectedIndex >= 0 && selectedIndex < hotspots.Count &&
            card != null && card.activeSelf)
            PositionInfoCard(hotspots[selectedIndex].pin.anchoredPosition);
    }

    public void Toggle()
    {
        if (!HasHotspots)
            return;
        if (visible)
        {
            Hide();
            return;
        }

        ChatbotController.CloseOpenChat();
        ExplanationPanelController.CloseOpenExplanation();
        MenuToggle.CloseOpenMenu();
        if (focusTransition != null)
        {
            StopCoroutine(focusTransition);
            focusTransition = null;
            if (hasBasePose)
                ApplyBasePose();
            SetScreenLockPositionPaused(false);
        }
        CaptureBasePose();
        if (arCamera == null)
            arCamera = Camera.main;
        if (!femaleSurfaceNormalsInitialized && arCamera != null &&
            string.Equals(organKey, "Female_Reproductive_system", StringComparison.OrdinalIgnoreCase))
        {
            OrientFemaleSurfacePinsToCurrentView();
            femaleSurfaceNormalsInitialized = true;
        }
        visible = true;
        selectedIndex = -1;
        card.SetActive(false);
        canvas.gameObject.SetActive(true);
        Update();
    }

    public void Hide()
    {
        visible = false;
        selectedIndex = -1;
        if (focusTransition != null)
            StopCoroutine(focusTransition);
        focusTransition = null;
        if (hasBasePose && UsesFocusedLabelPresentation)
        {
            SetScreenLockPositionPaused(true);
            focusTransition = StartCoroutine(TransitionToPose(
                baseLocalPosition, baseLocalRotation, baseLocalScale, true));
        }
        if (canvas != null)
            canvas.gameObject.SetActive(false);
    }

    public static void ToggleActive()
    {
        if (Active != null)
            Active.Toggle();
        else
            Debug.Log("[Labels] Scan an anatomy image first.");
    }

    public static void HideActive()
    {
        if (Active != null)
            Active.Hide();
    }

    public static bool IsScreenPointOverOverlay(Vector2 screenPoint)
    {
        if (Active == null || !Active.visible)
            return false;
        if (Active.card != null && Active.card.activeInHierarchy &&
            RectTransformUtility.RectangleContainsScreenPoint(
                Active.card.GetComponent<RectTransform>(), screenPoint, null))
            return true;
        foreach (Hotspot hotspot in Active.hotspots)
            if (hotspot.pin != null && hotspot.pin.gameObject.activeInHierarchy &&
                RectTransformUtility.RectangleContainsScreenPoint(hotspot.pin, screenPoint,
                    null))
                return true;
        return false;
    }

    private void CreateHotspots()
    {
        if (!OrganHotspots.TryGetValue(organKey, out Hotspot[] definitions))
        {
            Debug.LogWarning("[Labels] No anatomy hotspot definitions for '" + organKey + "'.");
            return;
        }

        CalculateLocalBounds();
        for (int i = 0; i < definitions.Length; i++)
        {
            Hotspot definition = definitions[i];
            Transform anchor = FindHeartAnchor(definition.title);
            if (anchor == null && string.Equals(organKey, "Female_Reproductive_system",
                    StringComparison.OrdinalIgnoreCase))
                anchor = FindFemaleLabelAnchor(i + 1);
            if (anchor != null && string.Equals(organKey,
                    "Female_Reproductive_system", StringComparison.OrdinalIgnoreCase))
            {
                if (i == 0)
                    AttachFemaleSurfaceAnchor(anchor, "Ovaries_Edit_Ovaries_M_0",
                        0.5f, 0.5f, 0.85f);
                else if (i == 1)
                    AttachFemaleSurfaceAnchor(anchor, "Fallopian_Edits_Ovaries_M_0",
                        0.25f, 0.3f, 0.85f);
                else if (i == 2)
                    AttachFemaleSurfaceAnchor(anchor, "Fallopian_Edits_Ovaries_M_0",
                        0.25f, 0.3f, 0.15f);
                else if (i == 3)
                    AttachFemaleSurfaceAnchor(anchor, "UterusVagina_Ovaries_M_0",
                        0.25f, 0.8f, 0.5f);
                else if (i == 4)
                    AttachFemaleSurfaceAnchor(anchor,
                        "Uterus_CutAway_FNL_Uterus_and_vagina", 0.5f, 0.55f, 0.6f);
                else if (i == 5)
                    AttachFemaleSurfaceAnchor(anchor, "UterusVagina_Ovaries_M_0",
                        0.5f, 0.55f, 0.8f);
                else if (i == 6)
                    AttachFemaleSurfaceAnchor(anchor,
                        "Uterus_CutAway_FNL_Uterus_and_vagina", 0.5f, 0.32f, 0.5f);
                else if (i == 7)
                    AttachFemaleSurfaceAnchor(anchor,
                        "Uterus_CutAway_FNL_Uterus_and_vagina", 0.25f, 0.1f, 0.5f);
            }

            Vector3 supplementalPosition = Vector3.zero;
            bool hasSupplementalAnchor = anchor == null &&
                TryGetHeartSupplementalAnchor(definition.title, out supplementalPosition);
            hotspots.Add(new Hotspot
            {
                title = definition.title,
                description = definition.description,
                normalizedPosition = definition.normalizedPosition,
                anchorTransform = anchor,
                explicitLocalPosition = hasSupplementalAnchor ? supplementalPosition : Vector3.zero,
                hasExplicitLocalPosition = hasSupplementalAnchor
            });
        }
    }

    private Transform FindFemaleLabelAnchor(int labelNumber)
    {
        string anchorName = "LabelAnchor_" + labelNumber.ToString("00");
        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
            if (child.name == anchorName)
                return child;
        return null;
    }

    private void AttachFemaleSurfaceAnchor(Transform anchor, string meshName,
        float depthFraction, float heightFraction, float lateralFraction)
    {
        MeshFilter target = null;
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter filter in meshFilters)
        {
            if (filter.sharedMesh == null)
                continue;
            if (filter.name.IndexOf(meshName,
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                target = filter;
                break;
            }
        }

        if (target == null)
        {
            Debug.LogWarning("[Labels] Could not find the reproductive-system mesh '" +
                meshName + "'.");
            return;
        }

        Mesh mesh = target.sharedMesh;
        Vector3[] vertices;
        try
        {
            vertices = mesh.vertices;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[Labels] Could not read mesh vertices for '" + meshName + "': " +
                exception.Message);
            return;
        }

        if (vertices == null || vertices.Length == 0)
            return;

        // Snap the anchor to an actual vertex on the named anatomy mesh. Parenting it
        // here makes the pin inherit the anatomical surface's transform.
        Bounds bounds = mesh.bounds;
        Vector3 surfaceTarget = new Vector3(
            bounds.min.x + bounds.size.x * lateralFraction,
            bounds.min.y + bounds.size.y * depthFraction,
            bounds.min.z + bounds.size.z * heightFraction);
        int closest = 0;
        float closestDistance = float.PositiveInfinity;
        for (int i = 0; i < vertices.Length; i++)
        {
            float distance = (vertices[i] - surfaceTarget).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = i;
            }
        }

        anchor.SetParent(target.transform, false);
        anchor.localPosition = vertices[closest];
        Vector3[] normals = mesh.normals;
        Vector3 surfaceNormal = normals != null && normals.Length == vertices.Length
            ? normals[closest]
            : Vector3.forward;
        if (arCamera == null)
            arCamera = Camera.main;
        Camera viewCamera = arCamera != null ? arCamera : Camera.main;
        if (viewCamera != null && Vector3.Dot(target.transform.TransformDirection(surfaceNormal),
                viewCamera.transform.position - anchor.position) < 0f)
            surfaceNormal = -surfaceNormal;
        anchor.localRotation = Quaternion.FromToRotation(Vector3.forward, surfaceNormal);
        anchor.localScale = Vector3.one;
    }

    private bool IsFemaleSurfaceHotspot(Hotspot hotspot)
    {
        return hotspot != null && hotspot.anchorTransform != null &&
            string.Equals(organKey, "Female_Reproductive_system", StringComparison.OrdinalIgnoreCase);
    }

    private void OrientFemaleSurfacePinsToCurrentView()
    {
        foreach (Hotspot hotspot in hotspots)
        {
            if (hotspot == null || hotspot.anchorTransform == null)
                continue;

            Vector3 toCamera = arCamera.transform.position - hotspot.anchorTransform.position;
            if (toCamera.sqrMagnitude > 0.000001f &&
                Vector3.Dot(hotspot.anchorTransform.forward, toCamera.normalized) < 0f)
                hotspot.anchorTransform.rotation =
                    Quaternion.AngleAxis(180f, hotspot.anchorTransform.up) *
                    hotspot.anchorTransform.rotation;
        }
    }

    private Transform FindHeartAnchor(string title)
    {
        if (!string.Equals(organKey, "Heart", StringComparison.OrdinalIgnoreCase))
            return null;

        string anchorName;
        switch (title)
        {
            case "Aorta": anchorName = "Label_Aorta"; break;
            case "Right atrium": anchorName = "Label_RightAtrium"; break;
            case "Left atrium": anchorName = "Label_LeftAtrium"; break;
            case "Right ventricle": anchorName = "Label_RightVentricle"; break;
            case "Left ventricle": anchorName = "Label_Leftventricle"; break;
            default: return null;
        }

        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
            if (child.name == anchorName)
                return child;
        return null;
    }

    private Vector3 HotspotLocalPoint(Hotspot hotspot)
    {
        if (hotspot.anchorTransform != null)
            return transform.InverseTransformPoint(hotspot.anchorTransform.position);
        return hotspot.hasExplicitLocalPosition
            ? hotspot.explicitLocalPosition
            : BoundsPoint(hotspot.normalizedPosition);
    }

    private bool TryGetHeartSupplementalAnchor(string title, out Vector3 localPosition)
    {
        localPosition = Vector3.zero;
        if (!string.Equals(organKey, "Heart", StringComparison.OrdinalIgnoreCase))
            return false;

        Vector3 aorta = GetHeartAnchorLocalPosition("Label_Aorta");
        Vector3 rightVentricle = GetHeartAnchorLocalPosition("Label_RightVentricle");
        Vector3 leftVentricle = GetHeartAnchorLocalPosition("Label_Leftventricle");
        Vector3 ventricleCenter = (rightVentricle + leftVentricle) * 0.5f;

        switch (title)
        {
            case "Pulmonary trunk":
                localPosition = Vector3.Lerp(aorta, ventricleCenter, 0.3f);
                return true;
            case "Anterior interventricular sulcus":
                localPosition = ventricleCenter;
                return true;
            case "Apex":
                Vector3 atriaCenter = (GetHeartAnchorLocalPosition("Label_RightAtrium") +
                                       GetHeartAnchorLocalPosition("Label_LeftAtrium")) * 0.5f;
                Vector3 towardApex = (ventricleCenter - atriaCenter).normalized;
                localPosition = ventricleCenter + towardApex * 0.25f;
                return true;
            default:
                return false;
        }
    }

    private Vector3 GetHeartAnchorLocalPosition(string anchorName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
            if (child.name == anchorName)
                return transform.InverseTransformPoint(child.position);
        return Vector3.zero;
    }

    private void CalculateLocalBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        bool found = false;
        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;
        foreach (Renderer renderer in renderers)
        {
            Bounds b = renderer.bounds;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = transform.InverseTransformPoint(b.center +
                    Vector3.Scale(b.extents, new Vector3(x, y, z)));
                min = Vector3.Min(min, corner);
                max = Vector3.Max(max, corner);
                found = true;
            }
        }

        localBounds = found ? new Bounds((min + max) * 0.5f, max - min) :
            new Bounds(Vector3.zero, Vector3.one * 0.15f);
    }

    private Vector3 BoundsPoint(Vector3 normalized)
    {
        return localBounds.center + Vector3.Scale(localBounds.extents, normalized);
    }

    private void CreateOverlay()
    {
        GameObject canvasObject = new GameObject("Anatomy Hotspots - " + organKey,
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 70;
        canvasRect = canvasObject.GetComponent<RectTransform>();
        Stretch(canvasRect);

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        Sprite circle = CreateRingSprite();
        for (int i = 0; i < hotspots.Count; i++)
            hotspots[i].pin = CreatePin(i, circle);
        CreateInfoCard();
        canvasObject.SetActive(false);
    }

    private RectTransform CreatePin(int index, Sprite circle)
    {
        GameObject buttonObject = new GameObject("Hotspot " + (index + 1),
            typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(canvasRect, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(68f, 68f);

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = circle;
        image.color = Color.white;
        image.raycastTarget = true;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => SelectHotspot(index));

        GameObject fillObject = new GameObject("Number", typeof(RectTransform), typeof(TextMeshProUGUI));
        fillObject.transform.SetParent(buttonObject.transform, false);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        Stretch(fillRect);
        TMP_Text number = fillObject.GetComponent<TMP_Text>();
        number.text = (index + 1).ToString();
        number.font = TMP_Settings.defaultFontAsset;
        number.fontSize = 27f;
        number.fontStyle = FontStyles.Bold;
        number.color = Color.white;
        number.alignment = TextAlignmentOptions.Center;
        number.raycastTarget = false;
        return rect;
    }

    private void CreateInfoCard()
    {
        card = CreatePanel(canvasRect, "Hotspot Information", Ink);
        cardRect = card.GetComponent<RectTransform>();
        bool cameraFocusStyle = UsesFocusedLabelPresentation;
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = cardRect.anchorMin;
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = cameraFocusStyle
            ? new Vector2(Mathf.Min(CardWidth, Mathf.Max(240f, canvasRect.rect.width - 48f)),
                Mathf.Min(CardHeight, Mathf.Max(260f, canvasRect.rect.height * 0.34f)))
            : new Vector2(Mathf.Min(980f, Mathf.Max(240f, canvasRect.rect.width - 64f)), 360f);

        cardTitle = CreateText(card.transform, "Part Name", "", 36f, Accent,
            TextAlignmentOptions.MidlineLeft);
        RectTransform titleRect = cardTitle.rectTransform;
        titleRect.anchorMin = new Vector2(0f, cameraFocusStyle ? 0.68f : 0.66f);
        titleRect.anchorMax = new Vector2(0.82f, 1f);
        titleRect.offsetMin = new Vector2(22f, 0f);
        titleRect.offsetMax = new Vector2(-8f, -8f);

        cardDescription = CreateText(card.transform, "Part Description", "",
            cameraFocusStyle ? 25f : 27f,
            Color.white, TextAlignmentOptions.TopLeft);
        RectTransform descriptionRect = cardDescription.rectTransform;
        descriptionRect.anchorMin = new Vector2(0f, 0.08f);
        descriptionRect.anchorMax = new Vector2(1f, cameraFocusStyle ? 0.65f : 0.66f);
        descriptionRect.offsetMin = new Vector2(22f, 16f);
        descriptionRect.offsetMax = new Vector2(-22f, -4f);

        Button close = CreateButton(card.transform, "Close", "×", 58f, Accent);
        RectTransform closeRect = close.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.86f, 0.75f);
        closeRect.anchorMax = new Vector2(0.98f, 0.96f);
        closeRect.offsetMin = closeRect.offsetMax = Vector2.zero;
        close.onClick.AddListener(() => card.SetActive(false));
        cardDescription.enableAutoSizing = true;
        cardDescription.fontSizeMin = 16f;
        cardDescription.fontSizeMax = cameraFocusStyle ? 25f : 27f;
        cardDescription.overflowMode = TextOverflowModes.Truncate;
        cardTitle.enableAutoSizing = true;
        cardTitle.fontSizeMin = 20f;
        cardTitle.fontSizeMax = 36f;
        cardTitle.overflowMode = TextOverflowModes.Ellipsis;
        card.SetActive(false);
    }

    private void SelectHotspot(int index)
    {
        if (index < 0 || index >= hotspots.Count)
            return;
        selectedIndex = index;
        Hotspot hotspot = hotspots[index];
        cardTitle.text = UsesFocusedLabelPresentation
            ? hotspot.title : (index + 1) + ". " + hotspot.title;
        cardDescription.text = hotspot.description;
        card.SetActive(true);

        if (UsesFocusedLabelPresentation)
            FocusOnHotspot(hotspot);
        PositionInfoCard(hotspots[index].pin.anchoredPosition);
    }

    private void CaptureBasePose()
    {
        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;
        baseLocalScale = transform.localScale;
        baseWorldRotation = transform.rotation;
        hasBasePose = true;
    }

    private void ApplyBasePose()
    {
        transform.localPosition = baseLocalPosition;
        transform.localRotation = baseLocalRotation;
        transform.localScale = baseLocalScale;
    }

    private void FocusOnHotspot(Hotspot hotspot)
    {
        if (arCamera == null)
            arCamera = Camera.main;
        if (arCamera == null || !hasBasePose)
            return;

        Vector3 localPoint = HotspotLocalPoint(hotspot);
        // Turn the selected anchor's actual surface toward the viewer, then
        // center that exact landmark in front of the device camera.
        Vector3 localSurfaceNormal = transform.InverseTransformDirection(
            hotspot.anchorTransform.forward);
        if (localSurfaceNormal.sqrMagnitude < 0.0001f)
            localSurfaceNormal = Vector3.forward;
        Vector3 currentOutward = (baseWorldRotation * localSurfaceNormal).normalized;
        Quaternion targetWorldRotation =
            Quaternion.FromToRotation(currentOutward, -arCamera.transform.forward) * baseWorldRotation;
        Transform parent = transform.parent;
        Quaternion targetLocalRotation = parent != null
            ? Quaternion.Inverse(parent.rotation) * targetWorldRotation
            : targetWorldRotation;
        Vector3 targetLocalScale = baseLocalScale * FocusZoom;

        Vector3 pointOffset = targetLocalRotation * Vector3.Scale(localPoint, targetLocalScale);
        if (parent != null)
            pointOffset = parent.TransformVector(pointOffset);
        else
            pointOffset = targetWorldRotation * Vector3.Scale(localPoint, targetLocalScale);

        Vector3 targetAnchor = arCamera.transform.position +
                               arCamera.transform.forward * FocusDistance;
        Vector3 targetWorldPosition = targetAnchor - pointOffset;
        Vector3 targetLocalPosition = parent != null
            ? parent.InverseTransformPoint(targetWorldPosition)
            : targetWorldPosition;

        if (focusTransition != null)
            StopCoroutine(focusTransition);
        SetScreenLockPositionPaused(true);
        focusTransition = StartCoroutine(TransitionToPose(
            targetLocalPosition, targetLocalRotation, targetLocalScale, false));
    }

    private IEnumerator TransitionToPose(Vector3 targetPosition, Quaternion targetRotation,
        Vector3 targetScale, bool clearBaseAfter)
    {
        Vector3 startPosition = transform.localPosition;
        Quaternion startRotation = transform.localRotation;
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < FocusDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / FocusDuration);
            float eased = t * t * (3f - 2f * t);
            transform.localPosition = Vector3.Lerp(startPosition, targetPosition, eased);
            transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, eased);
            transform.localScale = Vector3.Lerp(startScale, targetScale, eased);
            yield return null;
        }

        transform.localPosition = targetPosition;
        transform.localRotation = targetRotation;
        transform.localScale = targetScale;
        focusTransition = null;
        if (clearBaseAfter)
        {
            hasBasePose = false;
            SetScreenLockPositionPaused(false);
        }
    }

    private void SetScreenLockPositionPaused(bool paused)
    {
        ScreenLockFollower follower = GetComponent<ScreenLockFollower>();
        if (follower != null)
            follower.SetPositionFollowingPaused(paused);
    }

    private void PositionInfoCard(Vector2 pinPosition)
    {
        if (cardRect == null || canvasRect == null)
            return;

        float halfWidth = canvasRect.rect.width * 0.5f;
        float halfHeight = canvasRect.rect.height * 0.5f;
        float cardWidth = cardRect.rect.width;
        float cardHeight = cardRect.rect.height;
        float cardHalfHeight = cardHeight * 0.5f;
        const float edgeMargin = 24f;
        const float pinGap = 28f;

        bool leftFits = pinPosition.x - pinGap - cardWidth >= -halfWidth + edgeMargin;
        bool rightFits = pinPosition.x + pinGap + cardWidth <= halfWidth - edgeMargin;
        bool useHorizontalSide = leftFits || rightFits;
        bool placeLeft = pinPosition.x > 0f ? leftFits : !rightFits && leftFits;
        float x;
        float y;

        if (useHorizontalSide)
        {
            cardRect.pivot = placeLeft ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
            x = placeLeft
                ? Mathf.Clamp(pinPosition.x - pinGap, -halfWidth + cardWidth + edgeMargin,
                    halfWidth - edgeMargin)
                : Mathf.Clamp(pinPosition.x + pinGap, -halfWidth + edgeMargin,
                    halfWidth - cardWidth - edgeMargin);
            y = Mathf.Clamp(pinPosition.y, -halfHeight + cardHalfHeight + edgeMargin,
                halfHeight - cardHalfHeight - edgeMargin);
        }
        else
        {
            // A narrow portrait display cannot fit the panel beside a centered
            // hotspot. Put it above or below the pin instead of covering it.
            bool aboveFits = pinPosition.y + pinGap + cardHeight <= halfHeight - edgeMargin;
            bool belowFits = pinPosition.y - pinGap - cardHeight >= -halfHeight + edgeMargin;
            bool placeAbove = aboveFits || !belowFits;
            cardRect.pivot = placeAbove ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 1f);
            x = Mathf.Clamp(pinPosition.x, -halfWidth + cardWidth * 0.5f + edgeMargin,
                halfWidth - cardWidth * 0.5f - edgeMargin);
            y = placeAbove
                ? Mathf.Clamp(pinPosition.y + pinGap, -halfHeight + cardHeight + edgeMargin,
                    halfHeight - edgeMargin)
                : Mathf.Clamp(pinPosition.y - pinGap, -halfHeight + edgeMargin,
                    halfHeight - cardHeight - edgeMargin);
        }
        cardRect.anchoredPosition = new Vector2(x, y);
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject result = new GameObject(name, typeof(RectTransform), typeof(Image));
        result.transform.SetParent(parent, false);
        Image image = result.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return result;
    }

    private static Button CreateButton(Transform parent, string name, string label, float size, Color color)
    {
        GameObject result = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        result.transform.SetParent(parent, false);
        result.GetComponent<Image>().color = PinFill;
        Button button = result.GetComponent<Button>();
        button.targetGraphic = result.GetComponent<Image>();
        TMP_Text text = CreateText(result.transform, "Text", label, size, color,
            TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
        return button;
    }

    private static TMP_Text CreateText(Transform parent, string name, string value, float size,
        Color color, TextAlignmentOptions alignment)
    {
        GameObject result = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        result.transform.SetParent(parent, false);
        TMP_Text text = result.GetComponent<TMP_Text>();
        text.text = value;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
    }

    private static Sprite CreateRingSprite()
    {
        const int resolution = 96;
        Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        texture.name = "Anatomy Hotspot Ring";
        texture.filterMode = FilterMode.Bilinear;
        Color32[] pixels = new Color32[resolution * resolution];
        float center = (resolution - 1) * 0.5f;
        float outer = resolution * 0.47f;
        float inner = resolution * 0.39f;
        for (int y = 0; y < resolution; y++)
        for (int x = 0; x < resolution; x++)
        {
            float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
            byte alpha = distance >= inner && distance <= outer ? (byte)255 :
                distance < inner && distance <= outer ? (byte)190 : (byte)0;
            pixels[y * resolution + x] = distance < inner && distance <= outer
                ? new Color32(14, 25, 38, alpha)
                : new Color32(255, 255, 255, alpha);
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f));
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Dictionary<string, Hotspot[]> BuildOrganHotspots()
    {
        return new Dictionary<string, Hotspot[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Heart"] = new[]
            {
                H("Aorta", "The aorta carries oxygen-rich blood from the left ventricle to the body.", 0.0f, 0.78f, 0.45f),
                H("Pulmonary trunk", "The pulmonary trunk leaves the right ventricle and divides into the pulmonary arteries that carry blood to the lungs.", 0.36f, 0.58f, 0.55f),
                H("Right atrium", "The right atrium receives oxygen-poor blood returning from the body.", -0.42f, 0.25f, 0.62f),
                H("Left atrium", "The left atrium receives oxygen-rich blood from the lungs.", 0.42f, 0.25f, 0.62f),
                H("Right ventricle", "The right ventricle pumps blood toward the lungs for oxygen.", -0.35f, -0.25f, 0.7f),
                H("Left ventricle", "The left ventricle pumps oxygen-rich blood into the aorta.", 0.35f, -0.28f, 0.7f),
                H("Anterior interventricular sulcus", "This external groove marks the boundary between the right and left ventricles on the front of the heart.", 0.0f, -0.08f, 0.85f),
                H("Apex", "The apex is the pointed inferior tip of the heart, formed mainly by the left ventricle.", 0.0f, -0.82f, 0.4f)
            },
            ["Brain"] = new[]
            {
                H("Frontal lobe", "The frontal lobe supports planning, decision-making, movement, and speech.", 0.0f, 0.45f, 0.75f),
                H("Parietal lobe", "The parietal lobe processes touch and helps map the body's position.", 0.48f, 0.12f, 0.45f),
                H("Temporal lobe", "The temporal lobe helps process sound, language, and memory.", -0.55f, -0.12f, 0.5f),
                H("Occipital lobe", "The occipital lobe processes visual information.", 0.0f, 0.0f, -0.95f),
                H("Cerebellum", "The cerebellum helps coordinate balance and precise movement.", 0.0f, -0.68f, -0.65f),
                H("Brainstem", "The brainstem connects the brain with the spinal cord and regulates essential functions.", 0.0f, -0.82f, 0.1f),
                H("Cerebrum", "The cerebrum is the largest brain region and supports thought, sensation, and voluntary action.", 0.0f, 0.0f, 0.95f)
            },
            ["Eye"] = new[]
            {
                H("Sclera", "The sclera is the tough outer coat that protects the eyeball.", 0.7f, 0.42f, 0.55f),
                H("Cornea", "The clear cornea covers the front of the eye and bends incoming light.", 0.0f, 0.08f, 1.1f),
                H("Iris", "The iris controls pupil size and gives the eye much of its visible color.", 0.0f, 0.02f, 1.16f),
                H("Pupil", "The pupil is the opening that lets light enter the eye.", 0.0f, 0.02f, 1.2f),
                H("Lens", "The lens changes shape to focus light onto the retina.", 0.0f, 0.0f, 0.65f),
                H("Retina", "The retina senses light and converts it into signals for the brain.", 0.0f, 0.0f, -0.92f),
                H("Optic nerve", "The optic nerve carries visual signals from the retina to the brain.", 0.0f, -0.4f, -1.1f)
            },
            ["Lung"] = new[]
            {
                H("Trachea", "The trachea carries air between the throat and the bronchi.", 0.0f, 0.88f, 0.3f),
                H("Right lung", "The right lung contains three lobes and exchanges oxygen and carbon dioxide.", -0.48f, 0.05f, 0.65f),
                H("Left lung", "The left lung contains two lobes and leaves space for the heart.", 0.48f, 0.05f, 0.65f),
                H("Main bronchi", "The bronchi branch from the trachea and carry air into each lung.", 0.0f, 0.55f, 0.8f),
                H("Lung base", "The lower lung surfaces rest on the diaphragm, the main breathing muscle.", 0.0f, -0.86f, 0.4f),
                H("Pleural surface", "The pleura is a thin membrane that surrounds each lung and reduces friction during breathing.", 0.78f, -0.2f, 0.3f)
            },
            ["Kidney"] = new[]
            {
                H("Renal cortex", "The cortex is the kidney's outer layer, where blood filtration begins.", -0.58f, 0.55f, 0.5f),
                H("Renal medulla", "The medulla contains structures that help concentrate urine.", 0.12f, 0.35f, 0.65f),
                H("Renal artery", "The renal artery brings blood to the kidney for filtration.", 0.8f, 0.15f, 0.5f),
                H("Renal vein", "The renal vein carries filtered blood away from the kidney.", 0.8f, -0.05f, 0.5f),
                H("Renal pelvis", "The renal pelvis collects urine and funnels it toward the ureter.", 0.18f, -0.15f, 0.8f),
                H("Ureter", "The ureter carries urine from the kidney to the bladder.", 0.15f, -0.85f, 0.6f),
                H("Renal capsule", "The renal capsule is a thin protective covering around the kidney.", -0.72f, -0.2f, 0.45f)
            },
            ["Skeleton"] = new[]
            {
                H("Skull", "The skull protects the brain and supports the structures of the face.", 0f, 0.9f, 0.7f),
                H("Cervical vertebrae", "Seven cervical vertebrae support the head and allow neck movement.", 0f, 0.7f, 0.4f),
                H("Clavicle", "The clavicle links the sternum to the shoulder and helps support the arm.", -0.22f, 0.55f, 0.4f),
                H("Scapula", "The scapula is the shoulder blade, where several shoulder muscles attach.", 0.22f, 0.48f, -0.8f),
                H("Sternum", "The sternum is the breastbone at the front of the rib cage.", 0f, 0.32f, 0.9f),
                H("Ribs", "The ribs form a protective cage around the heart and lungs.", -0.38f, 0.3f, 0.6f),
                H("Humerus", "The humerus is the long bone of the upper arm.", -0.58f, 0.48f, 0.25f),
                H("Radius", "The radius is a forearm bone on the thumb side.", -0.82f, 0.45f, 0.3f),
                H("Ulna", "The ulna is the forearm bone on the little-finger side.", 0.82f, 0.42f, 0.3f),
                H("Hand bones", "The wrist, palm, and finger bones work together for grasping and fine movement.", 0.96f, 0.4f, 0.5f),
                H("Thoracic vertebrae", "Twelve thoracic vertebrae connect with the ribs.", 0f, 0.18f, -0.8f),
                H("Lumbar vertebrae", "Five lumbar vertebrae bear much of the upper body's load.", 0f, -0.08f, -0.75f),
                H("Pelvis", "The pelvis supports the trunk and connects it to the lower limbs.", 0f, -0.18f, 0.65f),
                H("Sacrum", "The sacrum connects the spine with the pelvis.", 0f, -0.2f, -0.7f),
                H("Femur", "The femur is the thigh bone and the longest bone in the body.", -0.18f, -0.48f, 0.45f),
                H("Patella", "The patella, or kneecap, protects the knee joint.", 0.18f, -0.62f, 0.8f),
                H("Tibia", "The tibia is the larger, weight-bearing bone of the lower leg.", -0.18f, -0.82f, 0.4f),
                H("Fibula", "The fibula is the smaller bone alongside the tibia.", 0.2f, -0.82f, 0.35f),
                H("Foot bones", "The ankle, midfoot, and toe bones support standing and walking.", 0f, -0.96f, 0.55f)
            },
            ["Neuron"] = new[]
            {
                H("Dendrites", "Dendrites receive signals from other neurons and carry them toward the cell body.", -0.72f, 0.42f, 0.35f),
                H("Cell body", "The cell body contains the neuron's nucleus and maintains the cell.", -0.12f, 0.05f, 0.8f),
                H("Nucleus", "The nucleus stores genetic material and helps regulate the neuron's activity.", -0.15f, 0.08f, 1f),
                H("Nissl bodies", "Nissl bodies are rough endoplasmic reticulum that helps make proteins in the neuron.", -0.36f, -0.2f, 0.85f),
                H("Axon", "The axon carries electrical signals away from the cell body toward other cells.", 0.48f, -0.18f, 0.5f),
                H("Myelin sheath", "The myelin sheath insulates the axon and helps signals travel faster.", 0.58f, 0.14f, 0.65f),
                H("Schwann cell", "A Schwann cell makes myelin around axons in the peripheral nervous system.", 0.7f, 0.36f, 0.45f),
                H("Node of Ranvier", "A node of Ranvier is a gap between myelin segments where the signal is renewed.", 0.85f, -0.02f, 0.5f),
                H("Axon terminals", "Axon terminals pass signals to a target cell, often by releasing neurotransmitters.", 0.9f, -0.45f, 0.35f),
                H("Synaptic knobs", "Synaptic knobs are enlarged terminal endings that release neurotransmitters into a synapse.", 0.98f, -0.66f, 0.25f)
            },
            ["Female_Reproductive_system"] = new[]
            {
                H("Ovaries", "The ovaries produce egg cells and hormones including estrogen and progesterone.", -0.62f, 0.3f, 0.55f),
                H("Fimbriae", "Fimbriae are small projections that help guide an egg from the ovary into the uterine tube.", -0.72f, 0.48f, 0.4f),
                H("Uterine tubes", "The uterine tubes carry an egg toward the uterus; fertilization often occurs here.", -0.5f, 0.62f, 0.45f),
                H("Uterus", "The uterus is a muscular organ where a fertilized egg can implant and develop.", 0f, 0.12f, 0.7f),
                H("Endometrium", "The endometrium is the inner uterine lining that changes during the menstrual cycle.", 0f, 0.2f, 0.95f),
                H("Myometrium", "The myometrium is the thick muscular wall of the uterus.", 0.25f, 0.0f, 0.65f),
                H("Cervix", "The cervix is the lower narrow part of the uterus that opens into the vagina.", 0f, -0.48f, 0.75f),
                H("Vagina", "The vagina is an elastic canal connecting the cervix to the outside of the body.", 0f, -0.82f, 0.55f)
            }
        };
    }

    private static Hotspot H(string title, string description, float x, float y, float z)
    {
        return new Hotspot { title = title, description = description,
            normalizedPosition = new Vector3(x, y, z) };
    }
}
