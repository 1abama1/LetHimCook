using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Полоска прогресса в мировом пространстве.
/// Работает на любом GameObject (не требует RectTransform).
/// Сама создает дочерний Canvas-объект.
/// </summary>
public class WorldSpaceBar : MonoBehaviour
{
    private Slider     _slider;
    private GameObject _visualRoot;
    private Transform  _canvasTransform;

    void Awake()
    {
        BuildCanvas();
    }

    void BuildCanvas()
    {
        // Создаём дочерний объект, который будет Canvas-ом
        var canvasGo = new GameObject("WSB_Canvas");
        canvasGo.transform.SetParent(transform, false);
        _canvasTransform = canvasGo.transform;

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var rt = canvasGo.GetComponent<RectTransform>();
        rt.sizeDelta  = new Vector2(50f, 15f);
        rt.localScale = Vector3.one * 0.01f;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        canvasGo.AddComponent<GraphicRaycaster>();

        BuildUI(rt);
    }

    void BuildUI(RectTransform canvasRt)
    {
        // --- Корень ---
        _visualRoot = new GameObject("VisualRoot");
        _visualRoot.transform.SetParent(_canvasTransform, false);

        var rootRt       = _visualRoot.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = rootRt.offsetMax = Vector2.zero;

        // --- Фон ---
        var bg   = new GameObject("BG");
        bg.transform.SetParent(_visualRoot.transform, false);
        var bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

        // --- Slider ---
        var sliderGo = new GameObject("Slider");
        sliderGo.transform.SetParent(_visualRoot.transform, false);

        var sliderRt    = sliderGo.AddComponent<RectTransform>();
        sliderRt.anchorMin = Vector2.zero;
        sliderRt.anchorMax = Vector2.one;
        sliderRt.offsetMin = new Vector2(2f, 2f);
        sliderRt.offsetMax = new Vector2(-2f, -2f);

        _slider             = sliderGo.AddComponent<Slider>();
        _slider.minValue    = 0f;
        _slider.maxValue    = 1f;
        _slider.value       = 0f;
        _slider.interactable = false;
        _slider.direction   = Slider.Direction.LeftToRight;

        // Slider background
        var sliderBg   = new GameObject("SliderBG");
        sliderBg.transform.SetParent(sliderGo.transform, false);
        var sliderBgRt = sliderBg.AddComponent<RectTransform>();
        sliderBgRt.anchorMin = Vector2.zero;
        sliderBgRt.anchorMax = Vector2.one;
        sliderBgRt.offsetMin = sliderBgRt.offsetMax = Vector2.zero;
        var sliderBgImg      = sliderBg.AddComponent<Image>();
        sliderBgImg.color    = new Color(0.2f, 0.2f, 0.2f, 1f);
        _slider.targetGraphic = sliderBgImg;

        // Fill Area
        var fillArea   = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGo.transform, false);
        var faRt       = fillArea.AddComponent<RectTransform>();
        faRt.anchorMin = Vector2.zero;
        faRt.anchorMax = Vector2.one;
        faRt.offsetMin = faRt.offsetMax = Vector2.zero;

        // Fill
        var fill      = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillRt    = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
        fill.AddComponent<Image>().color = new Color(0.2f, 0.85f, 0.35f, 1f);
        _slider.fillRect = fillRt;

        // Handle (невидимый)
        var handleArea = new GameObject("Handle Area");
        handleArea.transform.SetParent(sliderGo.transform, false);
        var haRt       = handleArea.AddComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero;
        haRt.anchorMax = Vector2.one;
        haRt.offsetMin = haRt.offsetMax = Vector2.zero;

        var handle   = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        var handleRt = handle.AddComponent<RectTransform>();
        handleRt.sizeDelta = Vector2.zero;
        handle.AddComponent<Image>().color = Color.clear;
        _slider.handleRect = handleRt;

        // Скрыт по умолчанию
        _visualRoot.SetActive(false);
    }

    void Update()
    {
        // Полоска всегда смотрит на камеру
        if (_canvasTransform != null && Camera.main != null)
        {
            _canvasTransform.LookAt(
                _canvasTransform.position + Camera.main.transform.rotation * Vector3.forward,
                Camera.main.transform.rotation * Vector3.up);
        }
    }

    public void UpdateProgress(float progress)
    {
        if (_visualRoot != null)
            _visualRoot.SetActive(progress > 0f && progress < 1f);

        if (_slider != null)
            _slider.value = progress;
    }
}