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

        // Use screen-relative swipe directions and world axes so an authored model
        // orientation (such as Brain's) cannot change which axis each swipe uses.
        Vector2 delta = touch.deltaPosition;
        float rotY = -delta.y * rotationSpeed;
        float rotX = delta.x * rotationSpeed;

        transform.Rotate(Vector3.up, rotY, Space.World);
        transform.Rotate(Vector3.right, rotX, Space.World);
    }
}
