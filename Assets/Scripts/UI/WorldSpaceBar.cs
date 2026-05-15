using UnityEngine;
using UnityEngine.UI;

public class WorldSpaceBar : MonoBehaviour
{
    private Slider slider;
    private GameObject visualRoot;

    void Awake()
    {
        // Проверяем, есть ли RectTransform. На обычных префабах (Transform) 
        // нельзя программно добавить Canvas, так как он требует RectTransform.
        if (GetComponent<RectTransform>() == null)
        {
            Debug.LogError($"[WorldSpaceBar] Ошибка на {name}: Скрипт должен висеть на объекте с RectTransform (UI-объект).");
            enabled = false;
            return;
        }

        SetupCanvas();
        CreateUI();
    }

    void SetupCanvas()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(50f, 15f);
        rt.localScale = Vector3.one * 0.01f; // масштаб для world space

        CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>() 
                           ?? gameObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
    }

    void CreateUI()
    {
        // --- visualRoot ---
        visualRoot = new GameObject("VisualRoot");
        visualRoot.transform.SetParent(transform, false);

        RectTransform rootRt = visualRoot.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        // --- Фон ---
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(visualRoot.transform, false);

        RectTransform bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

        // --- Slider ---
        GameObject sliderGo = new GameObject("Slider");
        sliderGo.transform.SetParent(visualRoot.transform, false);

        RectTransform sliderRt = sliderGo.AddComponent<RectTransform>();
        sliderRt.anchorMin = Vector2.zero;
        sliderRt.anchorMax = Vector2.one;
        sliderRt.offsetMin = new Vector2(2f, 2f);
        sliderRt.offsetMax = new Vector2(-2f, -2f);

        slider = sliderGo.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;
        slider.interactable = false;
        slider.wholeNumbers = false;
        slider.direction = Slider.Direction.LeftToRight;

        // Background area слайдера
        GameObject sliderBg = new GameObject("SliderBackground");
        sliderBg.transform.SetParent(sliderGo.transform, false);

        RectTransform sliderBgRt = sliderBg.AddComponent<RectTransform>();
        sliderBgRt.anchorMin = Vector2.zero;
        sliderBgRt.anchorMax = Vector2.one;
        sliderBgRt.offsetMin = Vector2.zero;
        sliderBgRt.offsetMax = Vector2.zero;

        Image sliderBgImg = sliderBg.AddComponent<Image>();
        sliderBgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        slider.targetGraphic = sliderBgImg;

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGo.transform, false);

        RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.offsetMin = Vector2.zero;
        fillAreaRt.offsetMax = Vector2.zero;

        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);

        RectTransform fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = new Vector2(1f, 1f);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;

        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.85f, 0.35f, 1f); // зелёный прогресс

        slider.fillRect = fillRt;

        // Handle Slide Area (пустой, без ручки)
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderGo.transform, false);

        RectTransform handleAreaRt = handleArea.AddComponent<RectTransform>();
        handleAreaRt.anchorMin = Vector2.zero;
        handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.offsetMin = Vector2.zero;
        handleAreaRt.offsetMax = Vector2.zero;

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);

        RectTransform handleRt = handle.AddComponent<RectTransform>();
        handleRt.sizeDelta = Vector2.zero; // невидимая ручка
        handle.AddComponent<Image>().color = Color.clear;

        slider.handleRect = handleRt;

        // По умолчанию скрыт
        visualRoot.SetActive(false);
    }

    void Update()
    {
        if (Camera.main != null)
        {
            transform.LookAt(
                transform.position + Camera.main.transform.rotation * Vector3.forward,
                Camera.main.transform.rotation * Vector3.up
            );
        }
    }

    public void UpdateProgress(float progress)
    {
        if (visualRoot != null)
            visualRoot.SetActive(progress > 0f && progress < 1f);

        if (slider != null)
            slider.value = progress;
    }
}