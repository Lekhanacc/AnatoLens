using UnityEngine;

public class PinchZoom : MonoBehaviour
{
    public float zoomSpeed = 0.005f;
    public float minScale = 0.5f;
    public float maxScale = 2.0f;

    void Update()
    {
        if (Input.touchCount == 2)
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            Vector2 previousTouch1 = touch1.position - touch1.deltaPosition;
            Vector2 previousTouch2 = touch2.position - touch2.deltaPosition;

            float previousDistance = Vector2.Distance(
                previousTouch1,
                previousTouch2
            );

            float currentDistance = Vector2.Distance(
                touch1.position,
                touch2.position
            );

            float difference = currentDistance - previousDistance;

            Vector3 newScale = transform.localScale +
                               Vector3.one * difference * zoomSpeed;

            newScale.x = Mathf.Clamp(newScale.x, minScale, maxScale);
            newScale.y = Mathf.Clamp(newScale.y, minScale, maxScale);
            newScale.z = Mathf.Clamp(newScale.z, minScale, maxScale);

            transform.localScale = newScale;
            HeartbeatAnimator beatAnim = GetComponent<HeartbeatAnimator>();
            if (beatAnim != null) beatAnim.RefreshBaseScale();
        }
    }
}