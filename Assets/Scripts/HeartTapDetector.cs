using UnityEngine;

public class HeartTapDetector : MonoBehaviour
{
    public float maxTapDuration = 0.3f;
    public float maxTapMovement = 25f; // in screen pixels

    private Vector2 touchStartPos;
    private float touchStartTime;
    private bool trackingPossibleTap = false;

    void Update()
    {
        if (Input.touchCount != 1) return; // ignore multi-touch (that's pinch zoom's job)

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            touchStartPos = touch.position;
            touchStartTime = Time.time;
            trackingPossibleTap = true;
        }
        else if (touch.phase == TouchPhase.Ended && trackingPossibleTap)
        {
            float duration = Time.time - touchStartTime;
            float distance = Vector2.Distance(touch.position, touchStartPos);

            if (duration <= maxTapDuration && distance <= maxTapMovement)
            {
                TryTriggerExplanation(touch.position);
            }

            trackingPossibleTap = false;
        }
        else if (touch.phase == TouchPhase.Canceled)
        {
            trackingPossibleTap = false;
        }
    }

    void TryTriggerExplanation(Vector2 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Only fire if the tap actually landed on this heart (or a child of it).
            if (hit.transform.IsChildOf(transform) || hit.transform == transform)
            {
                if (ExplanationPanelController.Instance != null)
                    ExplanationPanelController.Instance.RequestExplanation();
            }
        }
    }
}