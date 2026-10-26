using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class ChatbotController : MonoBehaviour
{
    public static ChatbotController Instance { get; private set; }

    [Header("UI References")]
    public GameObject panelRoot;         // drag ChatPanel here
    public GameObject chatLauncherButton;
    public TMP_Text chatHistoryText;     // drag ChatHistoryText here
    public TMP_InputField inputField;    // drag ChatInputField here

    [Header("API Settings")]
    [Tooltip("NEVER ship this key in a public build.")]
    public string apiKey = "YOUR_API_KEY_HERE";
    private const string apiUrl = "https://api.openai.com/v1/chat/completions";

    private class ChatMessage
    {
        public string role;
        public string content;
    }

    private List<ChatMessage> conversation = new List<ChatMessage>();
    private bool isLoading = false;
    private bool chatLayoutConfigured;
    private ScrollRect chatScrollRect;

    void Awake()
    {
        Instance = this;
        ConfigureChatLayout();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private string lastOrganName = null;

    public void OpenChat()
    {
        string organName = ModelInfo.ActiveModel != null
            ? ModelInfo.ActiveModel.organName
            : "this organ";

        if (organName != lastOrganName)
        {
            conversation.Clear();

            conversation.Add(new ChatMessage
            {
                role = "system",
                content = $"You are a friendly assistant helping a student understand the {organName} in an AR app. Keep answers short (2-4 sentences) and simple."
            });

            chatHistoryText.text = "";
            lastOrganName = organName;
        }

        // Chat owns the overlay while it is open.
        LabelToggleController.HideOpenLabels();
        ExplanationPanelController.CloseOpenExplanation();
        MenuToggle.CloseOpenMenu();
        ConfigureChatLayout();
        panelRoot.SetActive(true);
        if (chatLauncherButton != null)
            chatLauncherButton.SetActive(false);
        ScrollChatToBottom();
    }
    public void CloseChat()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
        if (chatLauncherButton != null)
            chatLauncherButton.SetActive(true);
    }

    public void ShowNotice(string message)
    {
        OpenChat();
        if (chatHistoryText != null)
            AppendToHistory("AnatoLens", message);
    }

    public static void CloseOpenChat()
    {
        if (Instance != null)
            Instance.CloseChat();
    }

    // Wire this to the Send button's OnClick.
    public void OnSendPressed()
    {
        string userText = inputField.text.Trim();
        if (string.IsNullOrEmpty(userText) || isLoading) return;

        if (!IsOpenAIKeyConfigured())
        {
            AppendToHistory("Assistant",
                "Add your OpenAI API key to the ChatbotController component on AIChatPanel in the Unity Inspector. The key must start with sk-.");
            return;
        }

        AppendToHistory("You", userText);
        conversation.Add(new ChatMessage { role = "user", content = userText });
        inputField.text = "";

        StartCoroutine(SendConversation());
    }

    IEnumerator SendConversation()
    {
        isLoading = true;
        AppendToHistory("Assistant", "Thinking...");

        StringBuilder messagesJson = new StringBuilder("[");
        for (int i = 0; i < conversation.Count; i++)
        {
            if (i > 0) messagesJson.Append(",");
            messagesJson.Append("{\"role\":\"").Append(conversation[i].role)
                        .Append("\",\"content\":\"").Append(EscapeJson(conversation[i].content))
                        .Append("\"}");
        }
        messagesJson.Append("]");

        string jsonBody = "{\"model\":\"gpt-4o-mini\",\"messages\":" + messagesJson + "}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey.Trim());

            yield return request.SendWebRequest();

            RemoveLastLine(); // remove the "Thinking..." placeholder

            if (request.result == UnityWebRequest.Result.Success)
            {
                string reply = ParseContent(request.downloadHandler.text);
                conversation.Add(new ChatMessage { role = "assistant", content = reply });
                AppendToHistory("Assistant", reply);
            }
            else
            {
                string message = request.responseCode == 401
                    ? "OpenAI rejected the API key. Check that it is active and copied correctly."
                    : request.responseCode == 429
                        ? "OpenAI rate or billing limit reached. Check your API usage and billing settings."
                        : "I couldn't reach OpenAI. Check your internet connection and try again.";
                AppendToHistory("Assistant", message);
                Debug.LogError("[Chatbot] Request failed (HTTP " + request.responseCode + "): " + request.error);
            }
        }

        isLoading = false;
    }

    void AppendToHistory(string speaker, string message)
    {
        chatHistoryText.text += $"\n<b>{speaker}:</b> {message}\n";
        StartCoroutine(RefreshChatLayoutAndScroll());
    }

    private bool IsOpenAIKeyConfigured()
    {
        string key = apiKey != null ? apiKey.Trim() : "";
        return key.StartsWith("sk-") && key.Length > 20;
    }

    private void ConfigureChatLayout()
    {
        if (chatLayoutConfigured || panelRoot == null)
            return;

        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.anchorMin = new Vector2(0.035f, 0.055f);
            panelRect.anchorMax = new Vector2(0.965f, 0.945f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
        }

        Image panelImage = panelRoot.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.color = new Color32(8, 17, 31, 248);
            panelImage.raycastTarget = true;
        }

        RectTransform scrollRectTransform = GetChildRect("ChatScrollView");
        SetStretchedRect(scrollRectTransform, new Vector2(0.04f, 0.18f),
            new Vector2(0.96f, 0.82f));
        if (scrollRectTransform != null)
        {
            chatScrollRect = scrollRectTransform.GetComponent<ScrollRect>();
            if (chatScrollRect != null)
            {
                chatScrollRect.horizontal = false;
                chatScrollRect.vertical = true;
                chatScrollRect.verticalScrollbarVisibility =
                    ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

                if (chatScrollRect.content != null)
                {
                    ContentSizeFitter contentFitter =
                        chatScrollRect.content.GetComponent<ContentSizeFitter>();
                    if (contentFitter != null)
                        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                }
            }

            Image scrollBackground = scrollRectTransform.GetComponent<Image>();
            if (scrollBackground != null)
                scrollBackground.color = new Color32(15, 31, 47, 235);
        }

        RectTransform inputRect = GetChildRect("ChatInputField");
        SetStretchedRect(inputRect, new Vector2(0.04f, 0.035f),
            new Vector2(0.75f, 0.14f));
        if (inputRect != null)
        {
            Image inputBackground = inputRect.GetComponent<Image>();
            if (inputBackground != null)
                inputBackground.color = new Color32(239, 244, 248, 255);
        }

        if (inputField != null)
        {
            StyleInputText(inputField.textComponent, new Color32(20, 30, 40, 255));
            StyleInputText(inputField.placeholder as TMP_Text,
                new Color32(100, 115, 128, 255));
        }

        RectTransform sendRect = GetChildRect("SendButton");
        SetStretchedRect(sendRect, new Vector2(0.78f, 0.035f),
            new Vector2(0.96f, 0.14f));
        if (sendRect != null)
        {
            Image sendImage = sendRect.GetComponent<Image>();
            if (sendImage != null)
                sendImage.color = new Color32(20, 126, 133, 255);

            TMP_Text sendLabel = sendRect.GetComponentInChildren<TMP_Text>(true);
            if (sendLabel != null)
            {
                StretchTextRect(sendLabel.rectTransform);
                sendLabel.text = "Send";
                sendLabel.fontSize = 27f;
                sendLabel.fontStyle = FontStyles.Bold;
                sendLabel.alignment = TextAlignmentOptions.Center;
                sendLabel.color = Color.white;
            }
        }

        RectTransform closeRect = GetChildRect("CloseChatButton");
        if (closeRect == null)
            closeRect = CreateCloseButton();
        if (closeRect != null)
        {
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.96f, 0.94f);
            closeRect.pivot = new Vector2(0.5f, 0.5f);
            closeRect.anchoredPosition = Vector2.zero;
            closeRect.sizeDelta = new Vector2(92f, 92f);

            TMP_Text closeLabel = closeRect.GetComponentInChildren<TMP_Text>(true);
            if (closeLabel != null)
            {
                StretchTextRect(closeLabel.rectTransform);
                closeLabel.text = "×";
                closeLabel.fontSize = 42f;
                closeLabel.alignment = TextAlignmentOptions.Center;
            }
        }

        if (chatHistoryText != null)
        {
            chatHistoryText.fontSize = 30f;
            chatHistoryText.enableAutoSizing = false;
            chatHistoryText.enableWordWrapping = true;
            chatHistoryText.overflowMode = TextOverflowModes.Overflow;
            chatHistoryText.alignment = TextAlignmentOptions.TopLeft;
            chatHistoryText.raycastTarget = false;

            RectTransform historyRect = chatHistoryText.rectTransform;
            historyRect.anchorMin = new Vector2(0f, 1f);
            historyRect.anchorMax = new Vector2(1f, 1f);
            historyRect.pivot = new Vector2(0f, 1f);
            historyRect.anchoredPosition = new Vector2(24f, -20f);
            historyRect.sizeDelta = new Vector2(-48f, 100f);

            ContentSizeFitter textFitter =
                chatHistoryText.GetComponent<ContentSizeFitter>();
            if (textFitter == null)
                textFitter = chatHistoryText.gameObject.AddComponent<ContentSizeFitter>();
            textFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            textFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        Transform header = panelRoot.transform.Find("ChatHeader");
        if (header == null)
        {
            GameObject headerObject = new GameObject("ChatHeader", typeof(RectTransform),
                typeof(TextMeshProUGUI));
            headerObject.transform.SetParent(panelRoot.transform, false);
            header = headerObject.transform;
        }

        RectTransform headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin = headerRect.anchorMax = new Vector2(0.06f, 0.93f);
        headerRect.pivot = new Vector2(0f, 0.5f);
        headerRect.anchoredPosition = new Vector2(20f, 0f);
        headerRect.sizeDelta = new Vector2(650f, 80f);
        TMP_Text headerText = header.GetComponent<TMP_Text>();
        headerText.text = "Ask AnatoLens";
        headerText.font = TMP_Settings.defaultFontAsset;
        headerText.fontSize = 36f;
        headerText.fontStyle = FontStyles.Bold;
        headerText.color = Color.white;
        headerText.alignment = TextAlignmentOptions.MidlineLeft;
        headerText.raycastTarget = false;

        chatLayoutConfigured = true;
    }

    private RectTransform CreateCloseButton()
    {
        GameObject buttonObject = new GameObject("CloseChatButton",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(panelRoot.transform, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color32(20, 126, 133, 255);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(CloseChat);

        GameObject labelObject = new GameObject("CloseIcon",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        StretchTextRect(labelRect);

        TMP_Text label = labelObject.GetComponent<TMP_Text>();
        label.text = "×";
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = 42f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        return buttonObject.GetComponent<RectTransform>();
    }

    private RectTransform GetChildRect(string childName)
    {
        Transform child = panelRoot != null ? panelRoot.transform.Find(childName) : null;
        return child != null ? child.GetComponent<RectTransform>() : null;
    }

    private static void SetStretchedRect(RectTransform rect, Vector2 anchorMin,
        Vector2 anchorMax)
    {
        if (rect == null)
            return;

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void StretchTextRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void StyleInputText(TMP_Text text, Color color)
    {
        if (text == null)
            return;

        text.fontSize = 28f;
        text.enableAutoSizing = false;
        text.color = color;
    }

    private void ScrollChatToBottom()
    {
        if (chatScrollRect != null)
            StartCoroutine(RefreshChatLayoutAndScroll());
    }

    private IEnumerator RefreshChatLayoutAndScroll()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (chatHistoryText != null)
            chatHistoryText.rectTransform.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical, chatHistoryText.preferredHeight + 24f);
        Canvas.ForceUpdateCanvases();
        if (chatScrollRect != null)
            chatScrollRect.verticalNormalizedPosition = 0f;
    }

    void RemoveLastLine()
    {
        int lastNewline = chatHistoryText.text.LastIndexOf("\n<b>Assistant:</b> Thinking...\n");
        if (lastNewline >= 0)
            chatHistoryText.text = chatHistoryText.text.Substring(0, lastNewline);
    }

    string ParseContent(string json)
    {
        try
        {
            int contentIndex = json.IndexOf("\"content\":\"");
            if (contentIndex == -1) return "Received an unexpected response format.";

            int start = contentIndex + "\"content\":\"".Length;
            int end = json.IndexOf("\"", start);
            while (end > 0 && json[end - 1] == '\\')
                end = json.IndexOf("\"", end + 1);

            string raw = json.Substring(start, end - start);
            return raw.Replace("\\n", "\n").Replace("\\\"", "\"");
        }
        catch
        {
            return "Couldn't parse the response.";
        }
    }

    string EscapeJson(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
    }
}
