using UnityEngine;

public class LabelToggleController : MonoBehaviour
{
    public static LabelToggleController Instance { get; private set; }

    public GameObject labelsContainer; // drag the "Labels" object here

    // The Labels GameObject starts inactive in HeartPrefab, so this must start false
    // for the first Labels button press to reveal it.
    private bool labelsVisible = false;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void ToggleLabels()
    {
        if (labelsVisible)
        {
            HideLabels();
            return;
        }

        // Labels are a viewing mode, not another panel layered over the UI.
        ChatbotController.CloseOpenChat();
        ExplanationPanelController.CloseOpenExplanation();
        MenuToggle.CloseOpenMenu();
        labelsVisible = true;
        if (labelsContainer != null)
            labelsContainer.SetActive(true);
    }

    public void HideLabels()
    {
        labelsVisible = false;
        if (labelsContainer != null)
            labelsContainer.SetActive(false);
    }

    public static void HideOpenLabels()
    {
        if (Instance != null)
            Instance.HideLabels();
    }
}
