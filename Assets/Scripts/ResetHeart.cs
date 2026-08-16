using UnityEngine;

public class ResetHeart : MonoBehaviour
{
    private Vector3 originalScale;
    private Quaternion originalRotation;

    void Start()
    {
        originalScale = transform.localScale;
        originalRotation = transform.localRotation;
    }

    public void ResetModel()
    {
        transform.localScale = originalScale;
        transform.localRotation = originalRotation;
    }
}