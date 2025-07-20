using UnityEngine;
using System.Collections;

public class UIPulseEffect : MonoBehaviour
{
    private Vector3 originalScale;
    private Coroutine pulseCoroutine;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void PlayPulse(float pulseScale = 1.5f, float growDuration = 0.1f, float shrinkDuration = 0.2f)
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            transform.localScale = originalScale; // Resetea la escala si se interrumpe
        }
        pulseCoroutine = StartCoroutine(PulseCoroutine(pulseScale, growDuration, shrinkDuration));
    }

    private IEnumerator PulseCoroutine(float pulseScale, float growDuration, float shrinkDuration)
    {
        // Fase de crecimiento
        float timer = 0f;
        while (timer < growDuration)
        {
            transform.localScale = Vector3.Lerp(originalScale, originalScale * pulseScale, timer / growDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        transform.localScale = originalScale * pulseScale;

        // Fase de encogimiento
        timer = 0f;
        while (timer < shrinkDuration)
        {
            transform.localScale = Vector3.Lerp(originalScale * pulseScale, originalScale, timer / shrinkDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        transform.localScale = originalScale;
        pulseCoroutine = null;
    }
}