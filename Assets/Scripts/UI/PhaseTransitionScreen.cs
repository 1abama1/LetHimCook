using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Экран перехода между фазами (Ночь ↔ День).
/// Все переходы используют SmoothStep для ease-in/out.
/// </summary>
public class PhaseTransitionScreen : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup                   canvasGroup;
    public UnityEngine.UI.Image          backgroundImage;
    public TMPro.TextMeshProUGUI         phaseLabel;
    public TMPro.TextMeshProUGUI         subtitleLabel;

    [Header("Fade Settings")]
    public float fadeInDuration  = 0.5f;
    public float holdDuration    = 1.5f;
    public float fadeOutDuration = 0.8f;

    [Header("Colors")]
    public Color nightColor = new Color(0.05f, 0.05f, 0.15f, 1f);
    public Color dayColor   = new Color(0.95f, 0.85f, 0.5f,  1f);

    [Header("Texts")]
    public string nightTitle    = "НОЧЬ";
    public string nightSubtitle = "Найди продукты. Не попадись.";
    public string dayTitle      = "ДЕНЬ";
    public string daySubtitle   = "Готовь. Корми. Не попались.";

    public UnityEvent onTransitionComplete;

    private Coroutine _currentTransition;

    void Start()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha          = 0f;
        canvasGroup.blocksRaycasts = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.onNightBegin.AddListener(ShowNightTransition);
            GameManager.Instance.onDayBegin.AddListener(ShowDayTransition);
        }
    }

    public void ShowNightTransition() =>
        PlayTransition(nightTitle, nightSubtitle, nightColor);

    public void ShowDayTransition() =>
        PlayTransition(dayTitle, daySubtitle, dayColor);

    private void PlayTransition(string title, string subtitle, Color bgColor)
    {
        if (_currentTransition != null)
            StopCoroutine(_currentTransition);
        _currentTransition = StartCoroutine(TransitionRoutine(title, subtitle, bgColor));
    }

    IEnumerator TransitionRoutine(string title, string subtitle, Color bgColor)
    {
        if (backgroundImage != null) backgroundImage.color = bgColor;
        if (phaseLabel      != null) phaseLabel.text       = title;
        if (subtitleLabel   != null) subtitleLabel.text    = subtitle;

        canvasGroup.blocksRaycasts = true;

        // Fade In (ease-in/out)
        yield return StartCoroutine(FadeSmooth(0f, 1f, fadeInDuration));

        // Hold
        yield return new WaitForSeconds(holdDuration);

        // Fade Out (ease-in/out)
        yield return StartCoroutine(FadeSmooth(1f, 0f, fadeOutDuration));

        canvasGroup.blocksRaycasts = false;
        _currentTransition = null;
        onTransitionComplete?.Invoke();
    }

    /// <summary>Fade с SmoothStep (ease-in/out), работает при любом timeScale.</summary>
    IEnumerator FadeSmooth(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t      = Mathf.Clamp01(elapsed / duration);
            float smooth = t * t * (3f - 2f * t);
            canvasGroup.alpha = Mathf.Lerp(from, to, smooth);
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}
