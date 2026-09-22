using UnityEngine;

public class ModelInfo : MonoBehaviour
{
    public static ModelInfo ActiveModel { get; private set; }

    [Tooltip("Used in AI prompts, e.g. 'Heart', 'Lung', 'Kidney'.")]
    public string organName = "Heart";

    void OnEnable()
    {
        ActiveModel = this;
    }
}