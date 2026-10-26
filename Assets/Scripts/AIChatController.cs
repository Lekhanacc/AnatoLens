using UnityEngine;

public class AIChatController : MonoBehaviour
{
    public GameObject chatPanel;
    public GameObject aiChatButton;

    public void ToggleChat()
    {
        bool showChat = !chatPanel.activeSelf;

        chatPanel.SetActive(showChat);

        // Hide AI CHAT button while chat panel is open
        aiChatButton.SetActive(!showChat);
    }
}