using UnityEngine;
using UnityEngine.UI;

public class HotspotButton : MonoBehaviour
{
    [Header("Match this ID to your OrganHotspotHandler list")]
    public int hotspotId;

    [Header("Reference to the Manager")]
    public OrganHotspotHandler manager;

    void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(TriggerHotspot);
        }
    }

    public void TriggerHotspot()
    {
        if (manager != null)
        {
            manager.OnHotspotClicked(hotspotId);
        }
        else
        {
            Debug.LogWarning("OrganHotspotHandler reference missing on pin: " + gameObject.name);
        }
    }
}