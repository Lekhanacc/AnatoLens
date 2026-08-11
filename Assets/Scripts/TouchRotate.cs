using UnityEngine;

public class TouchRotate : MonoBehaviour
{
    public float rotationSpeed = 0.2f;

    void Update()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Moved)
            {
                float rotY = -touch.deltaPosition.x * rotationSpeed;

                // Rotate around the world Y-axis
                transform.Rotate(Vector3.up, rotY, Space.World);
            }
        }
    }
}