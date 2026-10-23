using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class ChatbotController : MonoBehaviour
{
    public static ChatbotController Instance { get; private set; }

    [Header("UI References")]
    public GameObject panelRoot;         // drag ChatPanel here
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

    void Awake()
    {
        Instance = this;
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
    panelRoot.SetActive(true);
}
    public void CloseChat()
    {
        panelRoot.SetActive(false);
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
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

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
                AppendToHistory("Assistant", "Sorry, I couldn't respond. Check your connection.");
                Debug.LogError("[Chatbot] Request failed: " + request.error + "\n" + request.downloadHandler.text);
            }
        }

        isLoading = false;
    }

    void AppendToHistory(string speaker, string message)
    {
        chatHistoryText.text += $"\n<b>{speaker}:</b> {message}\n";
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
