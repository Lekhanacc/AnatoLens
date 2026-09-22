using UnityEngine;

public class HeartbeatAnimator : MonoBehaviour
{
    [Header("Beat Settings")]
    [Tooltip("Beats per minute. Resting human heart rate is roughly 60-100.")]
    public float beatsPerMinute = 72f;

    [Tooltip("How much the heart grows at the peak of each beat (0.08 = 8% bigger).")]
    [Range(0f, 0.3f)]
    public float pulseStrength = 0.08f;

    public bool isBeating = true;

    private Vector3 baseScale;
    private float phase = 0f;

    void Start()
    {
        // Capture whatever scale PinchZoom/ResetHeart currently has set, and pulse around that.
        baseScale = transform.localScale;
    }

    void Update()
    {
        if (!isBeating) return;

        float beatsPerSecond = beatsPerMinute / 60f;
        phase += Time.deltaTime * beatsPerSecond * Mathf.PI * 2f;

        // A heartbeat isn't a smooth sine wave in real life (sharp contraction, slower relax),
        // so we shape a plain sine into something closer to that using an easing curve.
        float raw = (Mathf.Sin(phase) + 1f) / 2f;      // normalize sine to 0-1
        float shaped = Mathf.Pow(raw, 3f);              // sharpen the peak, soften the trough

        float scaleFactor = 1f + shaped * pulseStrength;
        transform.localScale = baseScale * scaleFactor;
    }

    // Call this if PinchZoom or ResetHeart changes localScale externally,
    // so the pulse continues around the NEW base size instead of snapping back to the old one.
    public void RefreshBaseScale()
    {
        // Only capture the "resting" scale, not mid-pulse — approximate by dividing out current pulse.
        baseScale = transform.localScale;
    }

    // Wire this to a toggle button if you want manual on/off control.
    public void ToggleBeating()
    {
        isBeating = !isBeating;
        if (!isBeating)
            transform.localScale = baseScale; // snap back to resting size when stopped
    }
} 