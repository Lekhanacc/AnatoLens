using UnityEngine;

public class LabelsToggleButtonBridge : MonoBehaviour
{
    public void OnToggleLabelsPressed()
    {
        if (AnatomyHotspotOverlay.Active != null)
        {
            AnatomyHotspotOverlay.ToggleActive();
        }
        else if (LabelToggleController.Instance != null)
        {
            LabelToggleController.Instance.ToggleLabels();
        }
        else
        {
            Debug.Log("[Labels] Scan an anatomy image first.");
        }
    }
}
