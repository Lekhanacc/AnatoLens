using System.Collections;
using UnityEngine;

public class AssistantRobotIdleAnimation : MonoBehaviour
{
    private Quaternion restingRotation;
    private Vector3 restingPosition;

    private void Awake()
    {
        restingRotation = transform.localRotation;
        restingPosition = transform.localPosition;
    }

    private IEnumerator Start()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(7f, 11f));
            float duration = 1.15f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float wave = Mathf.Sin(elapsed / duration * Mathf.PI * 6f);
                transform.localRotation = restingRotation * Quaternion.Euler(0f, 0f, wave * 9f);
                transform.localPosition = restingPosition + Vector3.up * (Mathf.Abs(wave) * 5f);
                yield return null;
            }

            transform.localRotation = restingRotation;
            transform.localPosition = restingPosition;
        }
    }
}
