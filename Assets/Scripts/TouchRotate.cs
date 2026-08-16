using UnityEngine;

public class TouchRotate : MonoBehaviour
{
    public float rotationSpeed = 0.8f;

    void Update()
    {
        // Only rotate with one finger.
        // Two fingers are reserved for pinch zoom.
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Moved)
            {
                float horizontal = touch.deltaPosition.x;
                float vertical = touch.deltaPosition.y;

                // Left / Right → Y-axis rotation
                float rotY = -horizontal * rotationSpeed;

                // Up / Down → X-axis rotation
                float rotX = vertical * rotationSpeed;

                transform.Rotate(
                    Vector3.up,
                    rotY,
                    Space.World
                );

                transform.Rotate(
                    Vector3.right,
                    rotX,
                    Space.Self
                );
            }
        }
    }
}