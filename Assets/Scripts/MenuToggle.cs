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
        menuPanel.SetActive(!menuPanel.activeSelf);
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
