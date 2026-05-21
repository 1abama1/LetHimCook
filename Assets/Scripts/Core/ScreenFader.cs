using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton для затемнения экрана.
/// Стартует с полностью чёрным экраном и плавно открывается.
/// Все переходы используют SmoothStep для ease-in/out.
/// </summary>
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image fadeImage;

    // Можно сменить цвет затемнения через инспектор
    [SerializeField] private Color fadeColor = Color.black;

    private Coroutine _currentFade;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            EnsureUI();

            // Стартуем с чёрным экраном — он откроется после первого BeginPhase
            canvasGroup.alpha          = 1f;
            canvasGroup.blocksRaycasts = true;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    private void EnsureUI()
    {
        if (canvasGroup != null) return;

        GameObject canvasGO = new GameObject("[ScreenFader]");
        canvasGO.transform.SetParent(transform);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGroup = canvasGO.AddComponent<CanvasGroup>();

        GameObject imgGO = new GameObject("FadeImage");
        imgGO.transform.SetParent(canvasGO.transform, false);
        fadeImage       = imgGO.AddComponent<Image>();
        fadeImage.color = fadeColor;

        RectTransform rt = imgGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Public API
    // ──────────────────────────────────────────────────────────────────────────

    public void SetBlack()
    {
        EnsureUI();
        StopCurrentFade();
        canvasGroup.alpha          = 1f;
        canvasGroup.blocksRaycasts = true;
    }

    public void SetTransparent()
    {
        EnsureUI();
        StopCurrentFade();
        canvasGroup.alpha          = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    /// <summary>Плавное затемнение к чёрному (ease-in/out).</summary>
    public IEnumerator FadeToBlack(float duration)
    {
        EnsureUI();
        StopCurrentFade();
        canvasGroup.blocksRaycasts = true;
        _currentFade = StartCoroutine(FadeRoutine(canvasGroup.alpha, 1f, duration));
        yield return _currentFade;
    }

    /// <summary>Плавное открытие от чёрного (ease-in/out).</summary>
    public IEnumerator FadeFromBlack(float duration)
    {
        EnsureUI();
        StopCurrentFade();
        _currentFade = StartCoroutine(FadeRoutine(canvasGroup.alpha, 0f, duration));
        yield return _currentFade;
        canvasGroup.blocksRaycasts = false;
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Internal
    // ──────────────────────────────────────────────────────────────────────────

    private void StopCurrentFade()
    {
        if (_currentFade != null)
        {
            StopCoroutine(_currentFade);
            _currentFade = null;
        }
    }

    /// <summary>Универсальная корутина с SmoothStep (ease-in/out).</summary>
    private IEnumerator FadeRoutine(float from, float to, float duration)
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
            float t = Mathf.Clamp01(elapsed / duration);
            // SmoothStep: плавный старт и финиш
            float smooth = t * t * (3f - 2f * t);
            canvasGroup.alpha = Mathf.Lerp(from, to, smooth);
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}
