using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuToggle : MonoBehaviour
{
    public static MenuToggle Instance { get; private set; }

    public GameObject menuPanel;

    private RectTransform menuPanelRect;
    private RectTransform[] menuActions;
    private RectTransform fixAction;
    private GameObject beatAction;
    private GameObject labelsAction;
    private RectTransform chatShortcut;
    private RectTransform voiceShortcut;
    private Sprite shortcutShape;
    private Sprite chatIcon;
    private Sprite microphoneIcon;
    private ModelInfo lastActiveModel;
    private bool lastModelAvailable;
    private bool lastLabelsAvailable;
    private bool hasModelState;

    private const float EdgeInset = 24f;
    private const float LauncherBottomInset = 48f;
    private const float LauncherWidth = 144f;
    private const float LauncherHeight = 104f;
    private const float PanelGap = 12f;
    private const float PanelPadding = 20f;
    private const float ActionWidth = 360f;
    private const float ActionHeight = 96f;
    private const float ActionSpacing = 12f;
    private const float ShortcutBottomInset = 176f;

    void Awake()
    {
        Instance = this;
        ConfigureTouchFriendlyLayout();
        RefreshModelActions(force: true);
    }

    void Update()
    {
        ModelInfo activeModel = ModelInfo.ActiveModel;
        bool modelAvailable = activeModel != null && activeModel.isActiveAndEnabled;
        bool labelsAvailable = modelAvailable && ModelHasLabels(activeModel);

        if (!hasModelState || activeModel != lastActiveModel ||
            modelAvailable != lastModelAvailable || labelsAvailable != lastLabelsAvailable)
            RefreshModelActions(force: true);
    }

    public void ToggleMenu()
    {
        RefreshModelActions(force: false);

        bool openingMenu = !menuPanel.activeSelf;
        if (openingMenu)
        {
            // Keep exactly one screen overlay open at a time.
            ChatbotController.CloseOpenChat();
            ExplanationPanelController.CloseOpenExplanation();
            LabelToggleController.HideOpenLabels();
        }

        menuPanel.SetActive(openingMenu);
    }

    private void ConfigureTouchFriendlyLayout()
    {
        RectTransform launcher = GetComponent<RectTransform>();
        if (launcher != null)
        {
            launcher.anchorMin = Vector2.zero;
            launcher.anchorMax = Vector2.zero;
            launcher.pivot = Vector2.zero;
            launcher.anchoredPosition = new Vector2(EdgeInset, LauncherBottomInset);
            launcher.sizeDelta = new Vector2(LauncherWidth, LauncherHeight);
        }

        TMP_Text launcherText = GetComponentInChildren<TMP_Text>(true);
        if (launcherText != null)
        {
            RectTransform textRect = launcherText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = Vector2.zero;

            launcherText.text = "Menu";
            launcherText.alignment = TextAlignmentOptions.Center;
            launcherText.fontSize = 30f;
            launcherText.fontStyle = FontStyles.Bold;
            launcherText.color = Color.white;
        }

        Image launcherImage = GetComponent<Image>();
        if (launcherImage != null)
            launcherImage.color = new Color(0.12f, 0.29f, 0.47f, 1f);

        if (menuPanel == null)
            return;

        menuPanelRect = menuPanel.GetComponent<RectTransform>();
        if (menuPanelRect != null)
        {
            menuPanelRect.anchorMin = Vector2.zero;
            menuPanelRect.anchorMax = Vector2.zero;
            menuPanelRect.pivot = Vector2.zero;
            menuPanelRect.anchoredPosition = new Vector2(
                EdgeInset,
                LauncherBottomInset + LauncherHeight + PanelGap);
            menuPanelRect.sizeDelta = new Vector2(ActionWidth + PanelPadding * 2f, PanelPadding * 2f);
        }

        Image panelImage = menuPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.color = new Color(0.055f, 0.075f, 0.11f, 0.94f);
            panelImage.raycastTarget = true;
        }

        chatShortcut = FindMenuAction("OpenChatButton");
        voiceShortcut = FindMenuAction("MicButton");

        menuActions = new[]
        {
            FindMenuAction("ExplainButton"),
            FindMenuAction("FixButton"),
            FindMenuAction("LabelsToggleButton"),
            FindMenuAction("HeartToggleButton")
        };
        fixAction = menuActions[1];
        // FixButton is a sibling of MenuPanel in the scene hierarchy, unlike
        // the other menu actions, so the panel-relative lookup may not find it.
        if (fixAction == null)
        {
            GameObject sceneFixButton = GameObject.Find("FixButton");
            if (sceneFixButton != null)
                fixAction = sceneFixButton.GetComponent<RectTransform>();
        }

        labelsAction = menuActions[2] != null ? menuActions[2].gameObject : null;
        beatAction = menuActions[3] != null ? menuActions[3].gameObject : null;

        foreach (RectTransform action in menuActions)
        {
            if (action == null)
                continue;

            action.anchorMin = Vector2.zero;
            action.anchorMax = Vector2.zero;
            action.pivot = Vector2.zero;
            action.sizeDelta = new Vector2(ActionWidth, ActionHeight);

            TMP_Text actionText = action.GetComponentInChildren<TMP_Text>(true);
            if (actionText != null)
            {
                RectTransform textRect = actionText.rectTransform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.pivot = new Vector2(0.5f, 0.5f);
                textRect.offsetMin = new Vector2(12f, 8f);
                textRect.offsetMax = new Vector2(-12f, -8f);

                actionText.alignment = TextAlignmentOptions.Center;
                actionText.fontSize = 32f;
                actionText.fontStyle = FontStyles.Bold;
            }
        }

        ConfigureAssistantShortcuts();
        LayoutVisibleActions();
    }

    private void ConfigureAssistantShortcuts()
    {
        Transform canvasRoot = menuPanel != null ? menuPanel.transform.parent : null;
        if (canvasRoot == null || chatShortcut == null || voiceShortcut == null)
            return;

        shortcutShape = CreateRoundedSprite();
        GameObject widget = new GameObject("AnatoLens Assistant Widget", typeof(RectTransform));
        widget.transform.SetParent(canvasRoot, false);
        RectTransform widgetRect = widget.GetComponent<RectTransform>();
        widgetRect.anchorMin = widgetRect.anchorMax = Vector2.zero;
        widgetRect.pivot = Vector2.zero;
        widgetRect.sizeDelta = new Vector2(168f, 260f);
        widgetRect.anchoredPosition = new Vector2(
            ((RectTransform)canvasRoot).rect.width - widgetRect.sizeDelta.x - EdgeInset,
            ShortcutBottomInset);
        widget.AddComponent<AssistantWidgetDragHandler>();

        GameObject dock = new GameObject("AnatoLens Assistant Controls",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dock.transform.SetParent(widget.transform, false);
        RectTransform dockRect = dock.GetComponent<RectTransform>();
        dockRect.pivot = Vector2.zero;
        dockRect.anchorMin = dockRect.anchorMax = Vector2.zero;
        dockRect.anchoredPosition = Vector2.zero;
        dockRect.sizeDelta = new Vector2(168f, 78f);

        Image dockImage = dock.GetComponent<Image>();
        dockImage.sprite = shortcutShape;
        dockImage.type = Image.Type.Sliced;
        dockImage.color = new Color(0.035f, 0.045f, 0.065f, 0.94f);
        dockImage.raycastTarget = true;

        chatIcon = CreateAssistantIconSprite(microphone: false);
        microphoneIcon = CreateAssistantIconSprite(microphone: true);
        ConfigureShortcutButton(chatShortcut, dock.transform, "Chat", 10f, chatIcon);
        ConfigureShortcutButton(voiceShortcut, dock.transform, "Voice", 90f, microphoneIcon);

        GameObject robot = new GameObject("AnatoLens Robot Mascot",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        robot.transform.SetParent(widget.transform, false);
        RectTransform robotRect = robot.GetComponent<RectTransform>();
        robotRect.anchorMin = robotRect.anchorMax = new Vector2(1f, 0f);
        robotRect.pivot = new Vector2(1f, 0f);
        robotRect.anchoredPosition = new Vector2(-6f, 88f);
        robotRect.sizeDelta = new Vector2(156f, 172f);

        Image robotImage = robot.GetComponent<Image>();
        Texture2D robotTexture = Resources.Load<Texture2D>("AnatoLensRobot");
        if (robotTexture != null)
        {
            robotImage.sprite = Sprite.Create(robotTexture,
                new Rect(0f, 0f, robotTexture.width, robotTexture.height),
                new Vector2(0.5f, 0.5f), 100f);
            robotImage.preserveAspect = true;
        }
        else
        {
            Debug.LogWarning("[UI] Could not load Resources/AnatoLensRobot.png.");
        }
        robotImage.raycastTarget = true;
        robot.AddComponent<AssistantRobotIdleAnimation>();
        widget.transform.SetAsLastSibling();
    }

    public void SetFixButtonLocked(bool locked)
    {
        if (fixAction == null)
            return;

        TMP_Text label = fixAction.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = locked ? "RELEASE" : "FIX";
    }

    private void ConfigureShortcutButton(RectTransform buttonRect, Transform parent,
        string label, float x, Sprite icon)
    {
        buttonRect.SetParent(parent, false);
        buttonRect.anchorMin = buttonRect.anchorMax = Vector2.zero;
        buttonRect.pivot = Vector2.zero;
        buttonRect.anchoredPosition = new Vector2(x, 5f);
        buttonRect.sizeDelta = new Vector2(68f, 68f);

        Image buttonImage = buttonRect.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.sprite = shortcutShape;
            buttonImage.type = Image.Type.Sliced;
            buttonImage.color = new Color(0.13f, 0.19f, 0.28f, 1f);
        }

        TMP_Text buttonText = buttonRect.GetComponentInChildren<TMP_Text>(true);
        if (buttonText != null)
        {
            buttonText.text = string.Empty;
            buttonText.enabled = false;
            buttonText.raycastTarget = false;
        }

        GameObject iconObject = new GameObject(label + " Icon",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObject.transform.SetParent(buttonRect, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(34f, 34f);
        Image iconImage = iconObject.GetComponent<Image>();
        iconImage.sprite = icon;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
    }

    private static Sprite CreateAssistantIconSprite(bool microphone)
    {
        const int size = 64;
        const float thickness = 4.2f;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = microphone ? "AnatoLens Microphone Icon" : "AnatoLens Chat Icon";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            bool filled;
            if (!microphone)
            {
                filled = IsInsideRoundedRect(x, y, 12f, 20f, 40f, 28f, 8f) ||
                         IsInsideTriangle(x, y, new Vector2(19f, 15f),
                             new Vector2(29f, 23f), new Vector2(19f, 26f));
            }
            else
            {
                filled = IsInsideRoundedRect(x, y, 25f, 30f, 14f, 24f, 7f) ||
                         IsNearSegment(x, y, new Vector2(17f, 37f), new Vector2(17f, 27f), thickness) ||
                         IsNearSegment(x, y, new Vector2(47f, 37f), new Vector2(47f, 27f), thickness) ||
                         IsNearSegment(x, y, new Vector2(17f, 27f), new Vector2(20f, 21f), thickness) ||
                         IsNearSegment(x, y, new Vector2(20f, 21f), new Vector2(26f, 17f), thickness) ||
                         IsNearSegment(x, y, new Vector2(26f, 17f), new Vector2(32f, 16f), thickness) ||
                         IsNearSegment(x, y, new Vector2(32f, 16f), new Vector2(38f, 17f), thickness) ||
                         IsNearSegment(x, y, new Vector2(38f, 17f), new Vector2(44f, 21f), thickness) ||
                         IsNearSegment(x, y, new Vector2(44f, 21f), new Vector2(47f, 27f), thickness) ||
                         IsNearSegment(x, y, new Vector2(32f, 16f), new Vector2(32f, 8f), thickness) ||
                         IsNearSegment(x, y, new Vector2(23f, 8f), new Vector2(41f, 8f), thickness);
            }

            pixels[y * size + x] = filled
                ? new Color32(255, 255, 255, 255)
                : new Color32(255, 255, 255, 0);
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f), 100f);
    }

    private static bool IsInsideRoundedRect(float x, float y,
        float left, float bottom, float width, float height, float radius)
    {
        float dx = Mathf.Max(Mathf.Max(left + radius - x, 0f), x - (left + width - radius));
        float dy = Mathf.Max(Mathf.Max(bottom + radius - y, 0f), y - (bottom + height - radius));
        return dx * dx + dy * dy <= radius * radius;
    }

    private static bool IsInsideTriangle(float x, float y, Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 point = new Vector2(x, y);
        float d1 = (point.x - b.x) * (a.y - b.y) - (a.x - b.x) * (point.y - b.y);
        float d2 = (point.x - c.x) * (b.y - c.y) - (b.x - c.x) * (point.y - c.y);
        float d3 = (point.x - a.x) * (c.y - a.y) - (c.x - a.x) * (point.y - a.y);
        return !((d1 < 0f || d2 < 0f || d3 < 0f) && (d1 > 0f || d2 > 0f || d3 > 0f));
    }

    private static bool IsNearSegment(float x, float y, Vector2 start, Vector2 end, float width)
    {
        Vector2 point = new Vector2(x, y);
        Vector2 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        float t = lengthSquared > 0f
            ? Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared)
            : 0f;
        return Vector2.Distance(point, start + segment * t) <= width * 0.5f;
    }

    private static Sprite CreateRoundedSprite()
    {
        const int size = 64;
        const float radius = 19f;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "AnatoLens Rounded UI Shape";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = Mathf.Max(Mathf.Max(radius - x, 0f), x - (size - 1 - radius));
            float dy = Mathf.Max(Mathf.Max(radius - y, 0f), y - (size - 1 - radius));
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            byte alpha = distance <= radius - 1f ? (byte)255 :
                distance >= radius ? (byte)0 : (byte)128;
            pixels[y * size + x] = new Color32(255, 255, 255, alpha);
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
            new Vector4(radius, radius, radius, radius));
    }

    private RectTransform FindMenuAction(string actionName)
    {
        Transform action = menuPanel.transform.Find(actionName);
        return action != null ? action.GetComponent<RectTransform>() : null;
    }

    private void RefreshModelActions(bool force)
    {
        ModelInfo activeModel = ModelInfo.ActiveModel;
        bool modelAvailable = activeModel != null && activeModel.isActiveAndEnabled;

        if (!force && hasModelState && activeModel == lastActiveModel && modelAvailable == lastModelAvailable)
            return;

        lastActiveModel = activeModel;
        lastModelAvailable = modelAvailable;
        hasModelState = true;

        bool hasHeartbeat = modelAvailable &&
            activeModel.GetComponentInChildren<HeartbeatAnimator>(true) != null;
        bool hasLabels = modelAvailable &&
            ModelHasLabels(activeModel);

        SetVisible(beatAction, hasHeartbeat);
        SetVisible(labelsAction, hasLabels);
        lastLabelsAvailable = hasLabels;
        LayoutVisibleActions();
    }

    private static bool ModelHasLabels(ModelInfo model)
    {
        return model != null &&
            (model.GetComponentInChildren<LabelToggleController>(true) != null ||
             model.GetComponentInChildren<AnatomyHotspotOverlay>(true) != null ||
             model.GetComponentInParent<AnatomyHotspotOverlay>() != null);
    }

    private static void SetVisible(GameObject item, bool visible)
    {
        if (item != null && item.activeSelf != visible)
            item.SetActive(visible);
    }

    private void LayoutVisibleActions()
    {
        if (menuActions == null)
            return;

        int visibleCount = 0;
        foreach (RectTransform action in menuActions)
        {
            if (action == null || !action.gameObject.activeSelf)
                continue;

            action.anchoredPosition = new Vector2(
                PanelPadding,
                PanelPadding + visibleCount * (ActionHeight + ActionSpacing));
            visibleCount++;
        }

        if (menuPanelRect != null)
        {
            float contentHeight = visibleCount == 0
                ? 0f
                : visibleCount * ActionHeight + (visibleCount - 1) * ActionSpacing;
            menuPanelRect.sizeDelta = new Vector2(
                ActionWidth + PanelPadding * 2f,
                contentHeight + PanelPadding * 2f);
        }
    }

    public void CloseMenu()
    {
        if (menuPanel != null && menuPanel.activeSelf)
            menuPanel.SetActive(false);
    }

    public static void CloseOpenMenu()
    {
        if (Instance != null)
            Instance.CloseMenu();
    }
}
