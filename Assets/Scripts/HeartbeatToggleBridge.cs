using UnityEngine;

public class HeartbeatToggleBridge : MonoBehaviour
{
    public void OnToggleBeatPressed()
    {
        HeartbeatAnimator anim = FindFirstObjectByType<HeartbeatAnimator>();

        if (anim != null)
        {
            anim.ToggleBeating();
        }
        else
        {
            Debug.Log("[Heartbeat] No heart model spawned yet — scan the marker first.");
        }
    }
}