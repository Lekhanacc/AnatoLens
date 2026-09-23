using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Freezes the object's world position/rotation whenever the underlying
// AR session's tracking confidence is poor.
public class AnchorStabilizer : MonoBehaviour
{
    private Vector3 lastGoodPosition;
    private Quaternion lastGoodRotation;
    private bool hasGoodPose = false;

    void LateUpdate()
    {
        bool trackingGood = ARSession.notTrackingReason == NotTrackingReason.None;

        if (trackingGood)
        {
            lastGoodPosition = transform.position;
            lastGoodRotation = transform.rotation;
            hasGoodPose = true;
        }
        else if (hasGoodPose)
        {
            transform.position = lastGoodPosition;
            transform.rotation = lastGoodRotation;
        }
    }
}       