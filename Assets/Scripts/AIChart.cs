using UnityEngine;
using TMPro;

public class OrganHotspotHandler : MonoBehaviour
{
    [System.Serializable]
    public struct Hotspot
    {
        public int id;
        public string partName; // e.g., "Retina", "Alveoli", "Bladder", "Rectum"
        [TextArea(3, 5)]
        public string description;
    }

    [Header("Organ Configuration")]
    public string organName; // e.g., "Eye", "Lung", "Kidney", "Female Reproductive"
    public Hotspot[] hotspots;

    [Header("UI References")]
    public GameObject infoPanel;
    public TMP_Text titleText;
    public TMP_Text descriptionText;

    void Start()
    {
        if (infoPanel != null)
            infoPanel.SetActive(false);
    }

    // Called when a pin is clicked
    public void OnHotspotClicked(int hotspotId)
    {
        foreach (var spot in hotspots)
        {
            if (spot.id == hotspotId)
            {
                if (infoPanel != null) infoPanel.SetActive(true);
                if (titleText != null) titleText.text = spot.partName;
                if (descriptionText != null) descriptionText.text = spot.description;
                return;
            }
        }
    }

    // Call this from your UI close button (e.g., the 'X' button)
    public void ClosePanel()
    {
        if (infoPanel != null)
            infoPanel.SetActive(false);
    }
}