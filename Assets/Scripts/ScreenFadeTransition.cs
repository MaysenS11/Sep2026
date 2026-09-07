using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFadeTransition : MonoBehaviour
{
    public static ScreenFadeTransition Instance { get; private set; }

    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float defaultFadeDuration = 0.25f;
    [SerializeField] private float blackHoldDuration = 0.05f;

    public bool IsTransitioning { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        EnsureFadeOverlay();
    }

    private void EnsureFadeOverlay()
    {
        if (fadeCanvasGroup != null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindAnyObjectByType<Canvas>();
        }

        if (canvas == null) return;

        Transform existingOverlay = canvas.transform.Find("ScreenFadeOverlay");
        GameObject overlayGo;

        if (existingOverlay != null)
        {
            overlayGo = existingOverlay.gameObject;
        }
        else
        {
            overlayGo = new GameObject("ScreenFadeOverlay");
            overlayGo.transform.SetParent(canvas.transform, false);

            RectTransform rect = overlayGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            Image img = overlayGo.AddComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = false;
        }

        fadeCanvasGroup = overlayGo.GetComponent<CanvasGroup>();
        if (fadeCanvasGroup == null)
        {
            fadeCanvasGroup = overlayGo.AddComponent<CanvasGroup>();
        }

        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
        fadeCanvasGroup.interactable = false;
        overlayGo.transform.SetAsLastSibling();
    }

    public IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        if (fadeCanvasGroup == null) yield break;

        float startAlpha = fadeCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            fadeCanvasGroup.alpha = Mathf.SmoothStep(startAlpha, targetAlpha, t);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
        fadeCanvasGroup.blocksRaycasts = targetAlpha > 0f;
    }

    public IEnumerator PlayTransition(Action onBlackout, float fadeDuration = -1f)
    {
        if (IsTransitioning) yield break;
        IsTransitioning = true;

        float duration = fadeDuration > 0 ? fadeDuration : defaultFadeDuration;

        yield return StartCoroutine(FadeRoutine(1f, duration));

        onBlackout?.Invoke();

        if (blackHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(blackHoldDuration);
        }

        yield return StartCoroutine(FadeRoutine(0f, duration));

        IsTransitioning = false;
    }
}
