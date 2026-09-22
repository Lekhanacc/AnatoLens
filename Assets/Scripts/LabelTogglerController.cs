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
        labelsVisible = !labelsVisible;
        if (labelsContainer != null)
            labelsContainer.SetActive(labelsVisible);
    }
}
