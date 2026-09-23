using UnityEngine;

public class TouchRotate : MonoBehaviour
{
    public float rotationSpeed = 0.8f;

    void Update()
    {
        if (Input.touchCount != 1)
            return;

        Touch touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Moved)
            return;

        // Touch delta is already expressed in screen coordinates. Keep horizontal
        // swipes mapped to world-Y yaw in portrait and landscape orientations.
        Vector2 delta = touch.deltaPosition;
        float rotY = -delta.x * rotationSpeed;
        float rotX = delta.y * rotationSpeed;

        transform.Rotate(Vector3.up, rotY, Space.World);
        transform.Rotate(Vector3.right, rotX, Space.Self);
    }
}
