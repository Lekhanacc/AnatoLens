using UnityEngine;
using System.Text;

public class OrganSwitcher : MonoBehaviour
{
    [Header("Drag your Organ GameObjects here")]
    public GameObject[] organModels;

    // Call this function from your UI Menu Buttons
    public void SwitchToOrgan(int organIndex)
    {
        for (int i = 0; i < organModels.Length; i++)
        {
            if (organModels[i] != null)
            {
                // Turn on the selected organ, turn off the rest
                organModels[i].SetActive(i == organIndex);
                if (i == organIndex)
                    EnsureLabels(organModels[i]);
            }
        }
    }

    private static void EnsureLabels(GameObject organModel)
    {
        AnatomyHotspotOverlay overlay =
            organModel.GetComponentInChildren<AnatomyHotspotOverlay>(true);
        if (overlay == null)
            overlay = organModel.AddComponent<AnatomyHotspotOverlay>();
        if (overlay.HasHotspots)
            return;

        ModelInfo info = organModel.GetComponentInChildren<ModelInfo>(true);
        string sourceName = info != null && !string.IsNullOrWhiteSpace(info.organName)
            ? info.organName
            : organModel.name;
        StringBuilder compactName = new StringBuilder();
        foreach (char character in sourceName)
            if (char.IsLetterOrDigit(character))
                compactName.Append(char.ToLowerInvariant(character));

        string compactKey = compactName.ToString()
            .Replace("model", string.Empty)
            .Replace("prefab", string.Empty);
        string key;
        switch (compactKey)
        {
            case "plantcell": key = "Plant_cell"; break;
            case "animalcell": key = "Animal_cell"; break;
            case "nephron": key = "Nephron"; break;
            case "femalereproductivesystem": key = "Female_Reproductive_system"; break;
            case "digestivesystem": key = "Digestive_system"; break;
            default:
                key = sourceName.Trim().Replace(' ', '_');
                break;
        }

        if (info == null)
        {
            info = organModel.AddComponent<ModelInfo>();
            info.organName = key.Replace('_', ' ');
        }
        overlay.Configure(key);
        if (!overlay.HasHotspots)
            Debug.LogWarning("[Labels] No labels configured for switched model '" +
                sourceName + "' (key '" + key + "').", organModel);
    }
}
