using UnityEngine;

public class ResetButtonController : MonoBehaviour
{
    public void ResetHeart()
    {
        // Label focus moves/scales the tracked prefab root. ResetHeart lives on
        // the model child, so dismiss labels first to restore the actual scan pose.
        AnatomyHotspotOverlay.HideActive();

        ResetHeart heart = FindFirstObjectByType<ResetHeart>();

        if (heart != null)
        {
            heart.ResetModel();
        }

        MenuToggle.CloseOpenMenu();
    }
}
