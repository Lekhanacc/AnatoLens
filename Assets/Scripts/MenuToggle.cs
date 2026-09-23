using UnityEngine;

public class MenuToggle : MonoBehaviour
{
    public static MenuToggle Instance { get; private set; }

    public GameObject menuPanel;

    void Awake()
    {
        Instance = this;
    }

    public void ToggleMenu()
    {
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
