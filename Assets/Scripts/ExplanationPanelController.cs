using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Text;

public class ExplanationPanelController : MonoBehaviour
{
    public static ExplanationPanelController Instance { get; private set; }

    [Header("UI References")]
    public GameObject panelRoot;      // drag ExplanationPanel here
    public TMP_Text explanationText;  // drag ExplanationText here

    [Header("API Settings")]
    [Tooltip("NEVER ship this key in a public build. See notes in chat.")]
    public string apiKey = "YOUR_API_KEY_HERE";
    private const string apiUrl = "https://api.openai.com/v1/chat/completions";

    private bool isLoading = false;

    void Awake()
    {
        Instance = this;
    }

    // Call this from the Explain button OR the tap detector.
    public void RequestExplanation()
    {
        if (isLoading) return; // don't stack multiple requests

        // Explanations are modal content, so clear other overlays first.
        LabelToggleController.HideOpenLabels();
        ChatbotController.CloseOpenChat();
        MenuToggle.CloseOpenMenu();
        panelRoot.SetActive(true);
        explanationText.text = "Thinking...";
        StartCoroutine(FetchExplanation());
    }

    public void HidePanel()
    {
        panelRoot.SetActive(false);
    }

    public static void CloseOpenExplanation()
    {
        if (Instance != null)
            Instance.HidePanel();
    }

    IEnumerator FetchExplanation()
    {
        isLoading = true;

        string organName = ModelInfo.ActiveModel != null ? ModelInfo.ActiveModel.organName : "this organ";

        string prompt = $"Explain, in 3-4 simple sentences suitable for a student, " +
                         $"what the {organName} does and its role in the human body.";

        string jsonBody = "{"
            + "\"model\":\"gpt-4o-mini\","
            + "\"messages\":[{\"role\":\"user\",\"content\":\"" + EscapeJson(prompt) + "\"}]"
            + "}";

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string reply = ParseContent(request.downloadHandler.text);
                explanationText.text = reply;
            }
            else
            {
                explanationText.text = "Couldn't load explanation. Check your internet connection.";
                Debug.LogError("[AI Explain] Request failed: " + request.error + "\n" + request.downloadHandler.text);
            }
        }

        isLoading = false;
    }

    // Minimal manual JSON parsing to avoid pulling in a dependency for one field.
    string ParseContent(string json)
    {
        try
        {
            int contentIndex = json.IndexOf("\"content\":\"");
            if (contentIndex == -1) return "Received an unexpected response format.";

            int start = contentIndex + "\"content\":\"".Length;
            int end = json.IndexOf("\"", start);
            while (end > 0 && json[end - 1] == '\\') // skip escaped quotes
                end = json.IndexOf("\"", end + 1);

            string raw = json.Substring(start, end - start);
            return raw.Replace("\\n", "\n").Replace("\\\"", "\"");
        }
        catch
        {
            return "Couldn't parse the explanation.";
        }
    }

    string EscapeJson(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
