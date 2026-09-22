using UnityEngine;

public class TouchRotate : MonoBehaviour
{
    public float rotationSpeed = 0.8f;

void Update()
{
    if (Input.touchCount == 1)
    {
        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Moved)
        {
            Vector2 delta = CompensateForOrientation(touch.deltaPosition);

            float rotY = -delta.x * rotationSpeed;
            float rotX = delta.y * rotationSpeed;

            transform.Rotate(Vector3.up, rotY, Space.World);
            transform.Rotate(Vector3.right, rotX, Space.Self);
        }
    }
}

Vector2 CompensateForOrientation(Vector2 rawDelta)
{
    switch (Screen.orientation)
    {
        case ScreenOrientation.LandscapeLeft:
            return new Vector2(-rawDelta.y, rawDelta.x);

        case ScreenOrientation.LandscapeRight:
            return new Vector2(rawDelta.y, -rawDelta.x);

        case ScreenOrientation.PortraitUpsideDown:
            return new Vector2(-rawDelta.x, -rawDelta.y);

        default:
            return rawDelta;
    }
}
}