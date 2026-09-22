using UnityEngine;

public class ResetButtonController : MonoBehaviour
{
    public void ResetHeart()
    {
        ResetHeart heart = FindFirstObjectByType<ResetHeart>();

        if (heart != null)
        {
            heart.ResetModel();
        }

        MenuToggle.CloseOpenMenu();
    }
}
