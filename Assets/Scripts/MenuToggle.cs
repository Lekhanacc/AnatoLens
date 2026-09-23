using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuToggle : MonoBehaviour
{
    public static MenuToggle Instance { get; private set; }

    public GameObject menuPanel;

    private RectTransform menuPanelRect;
    private RectTransform[] menuActions;
    private GameObject beatAction;
    private GameObject labelsAction;
    private ModelInfo lastActiveModel;
    private bool lastModelAvailable;
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

        if (!hasModelState || activeModel != lastActiveModel || modelAvailable != lastModelAvailable)
            RefreshModelActions(force: false);
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

        menuActions = new[]
        {
            FindMenuAction("OpenChatButton"),
            FindMenuAction("ExplainButton"),
            FindMenuAction("FixButton"),
            FindMenuAction("MicButton"),
            FindMenuAction("LabelsToggleButton"),
            FindMenuAction("HeartToggleButton")
        };

        labelsAction = menuActions[4] != null ? menuActions[4].gameObject : null;
        beatAction = menuActions[5] != null ? menuActions[5].gameObject : null;

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

        LayoutVisibleActions();
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
            activeModel.GetComponentInChildren<LabelToggleController>(true) != null;

        SetVisible(beatAction, hasHeartbeat);
        SetVisible(labelsAction, hasLabels);
        LayoutVisibleActions();
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
