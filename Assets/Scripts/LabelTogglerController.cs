using UnityEngine;

public class LabelToggleController : MonoBehaviour
{
    public static LabelToggleController Instance { get; private set; }

    public GameObject labelsContainer; // drag the "Labels" object here

    private bool labelsVisible = true;

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