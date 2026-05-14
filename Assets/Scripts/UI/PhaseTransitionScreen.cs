using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Экран перехода между фазами (Ночь ↔ День).
/// Прикрепите к Canvas-объекту поверх всего.
/// Автоматически подписывается на GameManager.onNightBegin / onDayBegin.
/// </summary>
public class PhaseTransitionScreen : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup       canvasGroup;
    public UnityEngine.UI.Image backgroundImage;
    public TMPro.TextMeshProUGUI phaseLabel;
    public TMPro.TextMeshProUGUI subtitleLabel;

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
        StartCoroutine(TransitionRoutine(nightTitle, nightSubtitle, nightColor));

    public void ShowDayTransition() =>
        StartCoroutine(TransitionRoutine(dayTitle, daySubtitle, dayColor));

    IEnumerator TransitionRoutine(string title, string subtitle, Color bgColor)
    {
        if (backgroundImage != null) backgroundImage.color = bgColor;
        if (phaseLabel      != null) phaseLabel.text       = title;
        if (subtitleLabel   != null) subtitleLabel.text    = subtitle;

        canvasGroup.blocksRaycasts = true;

        // Fade In
        yield return StartCoroutine(Fade(0f, 1f, fadeInDuration));

        // Hold
        yield return new WaitForSeconds(holdDuration);

        // Fade Out
        yield return StartCoroutine(Fade(1f, 0f, fadeOutDuration));

        canvasGroup.blocksRaycasts = false;
        onTransitionComplete?.Invoke();
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}
