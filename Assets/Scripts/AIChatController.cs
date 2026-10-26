using UnityEngine;

public class AIChatController : MonoBehaviour
{
    public GameObject chatPanel;
    public GameObject aiChatButton;

    private ChatbotController chatbot;

    private void Awake()
    {
        if (chatPanel != null)
        {
            chatbot = chatPanel.GetComponent<ChatbotController>();
            if (chatbot != null)
                chatbot.chatLauncherButton = aiChatButton;
        }
    }

    public void ToggleChat()
    {
        if (chatPanel == null)
        {
            Debug.LogError("[Chatbot] Chat panel is not assigned.", this);
            return;
        }

        bool showChat = !chatPanel.activeSelf;
        if (chatbot == null)
            chatbot = chatPanel.GetComponent<ChatbotController>();

        if (chatbot == null)
        {
            Debug.LogError("[Chatbot] Chat panel has no ChatbotController component.", chatPanel);
            return;
        }

        chatbot.chatLauncherButton = aiChatButton;
        if (showChat)
            chatbot.OpenChat();
        else
            chatbot.CloseChat();
    }
}
