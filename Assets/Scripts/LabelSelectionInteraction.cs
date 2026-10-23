using UnityEngine;

// Kept on the runtime text object so its BoxCollider receives taps directly.
public class LabelSelectionInteraction : MonoBehaviour
{
    private BillboardLabel owner;

    public void Initialize(BillboardLabel label)
    {
        owner = label;
    }

    void OnMouseDown()
    {
        if (owner != null)
            owner.Select();
    }
}
