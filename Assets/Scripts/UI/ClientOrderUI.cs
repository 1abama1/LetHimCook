using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Показывает значок заказа над головой клиента в мировом пространстве.
/// Привяжите к дочернему Canvas (WorldSpace) объекта ClientNPC.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class ClientOrderUI : MonoBehaviour
{
    [Header("Settings")]
    public float billboardHeight = 1.8f;
    public Color patienceHighColor = new Color(0.2f, 0.9f, 0.3f);
    public Color patienceLowColor  = new Color(0.9f, 0.2f, 0.2f);

    private TextMeshProUGUI _dishLabel;
    private Image           _patienceBar;
    private Image           _patienceFill;
    private Slider          _patienceSlider;   // кеш — создаётся в Awake, не ищется каждый кадр
    private ClientNPC       _client;
    private Camera          _mainCamera;


    void Awake()
    {
        _client = GetComponentInParent<ClientNPC>();

        // Настраиваем Canvas
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform rt = GetComponent<RectTransform>();
        rt.sizeDelta  = new Vector2(100f, 40f);
        rt.localScale = Vector3.one * 0.012f;
        rt.localPosition = Vector3.up * (billboardHeight / rt.localScale.y);

        // ИСПРАВЛЕНИЕ: Добавлен typeof(RectTransform) для всех вложенных UI-элементов
        GameObject labelGo = new GameObject("DishLabel", typeof(RectTransform));
        labelGo.transform.SetParent(transform, false);
        RectTransform labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin    = new Vector2(0f, 0.5f);
        labelRt.anchorMax    = new Vector2(1f, 1f);
        labelRt.offsetMin    = labelRt.offsetMax = Vector2.zero;

        _dishLabel = labelGo.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(_dishLabel);
        _dishLabel.text      = "?";
        _dishLabel.fontSize  = 16f;
        _dishLabel.alignment = TextAlignmentOptions.Center;
        _dishLabel.fontStyle = FontStyles.Bold;
        _dishLabel.color     = Color.white;

        // --- Patience bar background ---
        GameObject barBg = new GameObject("PatienceBarBg", typeof(RectTransform));
        barBg.transform.SetParent(transform, false);
        RectTransform barBgRt = barBg.GetComponent<RectTransform>();
        barBgRt.anchorMin = new Vector2(0.05f, 0f);
        barBgRt.anchorMax = new Vector2(0.95f, 0.45f);
        barBgRt.offsetMin = barBgRt.offsetMax = Vector2.zero;
        Image bgImg = barBg.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        // --- Patience fill ---
        GameObject barFill = new GameObject("PatienceFill", typeof(RectTransform));
        barFill.transform.SetParent(barBg.transform, false);
        RectTransform fillRt = barFill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(2f, 2f);
        fillRt.offsetMax = new Vector2(-2f, -2f);
        _patienceFill = barFill.AddComponent<Image>();
        _patienceFill.color = patienceHighColor;

        // Создаём слайдер поверх
        GameObject sliderGo = new GameObject("PatienceSlider", typeof(RectTransform));
        sliderGo.transform.SetParent(barBg.transform, false);
        RectTransform sliderRt = sliderGo.GetComponent<RectTransform>();
        sliderRt.anchorMin = Vector2.zero;
        sliderRt.anchorMax = Vector2.one;
        sliderRt.offsetMin = new Vector2(2f, 2f);
        sliderRt.offsetMax = new Vector2(-2f, -2f);

        Slider slider = sliderGo.AddComponent<Slider>();
        slider.interactable = false;
        slider.minValue     = 0f;
        slider.maxValue     = 1f;
        slider.value        = 1f;
        slider.fillRect     = fillRt;
        slider.targetGraphic = bgImg;

        // Пустой handle
        GameObject handleArea = new GameObject("HandleArea", typeof(RectTransform));
        handleArea.transform.SetParent(sliderGo.transform, false);
        handleArea.AddComponent<RectTransform>();
        GameObject handle = new GameObject("Handle", typeof(RectTransform));
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRt = handle.GetComponent<RectTransform>();
        handleRt.sizeDelta = Vector2.zero;
        handle.AddComponent<Image>().color = Color.clear;
        slider.handleRect = handleRt;

        _patienceSlider = slider;   // кешируем — больше не ищем через GetComponentInChildren

        _patienceBar = bgImg;
    }

    void Start()
    {
        _mainCamera = Camera.main;
        if (_client != null && _client.wantedRecipe != null)
            _dishLabel.text = _client.wantedRecipe.recipeName;
    }

    void Update()
    {
        if (_client == null) return;

        // Billboarding
        if (_mainCamera != null)
            transform.LookAt(transform.position + _mainCamera.transform.rotation * Vector3.forward,
                             _mainCamera.transform.rotation * Vector3.up);

        // Обновляем цвет и значение слайдера через кешированные ссылки.
        float t = _client.PatiencePercent;
        if (_patienceFill != null)
            _patienceFill.color = Color.Lerp(patienceLowColor, patienceHighColor, t);

        if (_patienceSlider != null)
            _patienceSlider.value = t;
    }
}
