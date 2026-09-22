using UnityEngine;

public class LabelsToggleButtonBridge : MonoBehaviour
{
    public void OnToggleLabelsPressed()
    {
        if (LabelToggleController.Instance != null)
        {
            LabelToggleController.Instance.ToggleLabels();
        }
        else
        {
            Debug.Log("[Labels] No heart model spawned yet — scan the marker first.");
        }
    }
} 