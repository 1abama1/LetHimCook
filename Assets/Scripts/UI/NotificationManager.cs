using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }

    private GameObject _container;
    private VerticalLayoutGroup _layoutGroup;
    
    // Переменные для отслеживания дельты (разницы)
    private int _prevMoney = -1;
    private Dictionary<ResourceType, int> _prevResources = new Dictionary<ResourceType, int>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;


        BuildContainer();
    }

    void Start()
    {
        // Автоматически подписываемся на экономику игрока
        if (PlayerEconomy.Instance != null)
        {
            _prevMoney = PlayerEconomy.Instance.Money;
            
            // Запоминаем стартовые значения ресурсов
            _prevResources[ResourceType.FreshIngredient] = PlayerEconomy.Instance.FreshIngredients;
            _prevResources[ResourceType.SpoiledIngredient] = PlayerEconomy.Instance.SpoiledIngredients;
            _prevResources[ResourceType.JunkLoot] = PlayerEconomy.Instance.JunkLoot;

            PlayerEconomy.Instance.onMoneyChanged.AddListener(OnMoneyChanged);
            PlayerEconomy.Instance.onResourceChanged.AddListener(OnResourceChanged);
        }
    }

    // ── Авто-определение изменения денег ───────────────────────────
    private void OnMoneyChanged(int newTotal)
    {
        if (_prevMoney == -1) { _prevMoney = newTotal; return; }
        int delta = newTotal - _prevMoney;
        _prevMoney = newTotal;

        if (delta > 0)
            ShowNotification($"+${delta}", new Color(0.2f, 0.85f, 0.3f)); // Зеленый
        else if (delta < 0)
            ShowNotification($"-${Mathf.Abs(delta)}", new Color(0.9f, 0.2f, 0.2f)); // Красный
    }

    // ── Авто-определение изменения ресурсов (Лут/Мясо) ──────────────
    private void OnResourceChanged(ResourceType type, int newTotal)
    {
        if (!_prevResources.ContainsKey(type)) { _prevResources[type] = newTotal; return; }
        int delta = newTotal - _prevResources[type];
        _prevResources[type] = newTotal;

        if (delta == 0) return;

        string resourceName = "";
        Color textColor = Color.white;

        switch (type)
        {
            case ResourceType.FreshIngredient:
                resourceName = "Fresh Meat";
                textColor = new Color(0.3f, 0.75f, 1f); // Голубой
                break;
            case ResourceType.SpoiledIngredient:
                resourceName = "Spoiled Meat";
                textColor = new Color(0.7f, 0.5f, 0.3f); // Коричневый
                break;
            case ResourceType.JunkLoot:
                resourceName = "Valuable Junk";
                textColor = new Color(0.95f, 0.8f, 0.2f); // Золотой
                break;
        }

        if (delta > 0)
            ShowNotification($"+{delta} {resourceName}", textColor);
        else
            ShowNotification($"-{Mathf.Abs(delta)} {resourceName}", Color.gray);
    }

    // ── Ручной вызов (например, для надписей вроде "Убийство!") ─────
    public void ShowCustom(string message, Color color)
    {
        ShowNotification(message, color);
    }

    // ── Генерация Уведомления ──────────────────────────────────────
    private void ShowNotification(string text, Color color)
    {
        if (_container == null) BuildContainer();

        // Создаем плашку уведомления СРАЗУ как RectTransform (чтобы текст не схлопывался!)
        GameObject itemGo = new GameObject("NotifItem", typeof(RectTransform));
        itemGo.transform.SetParent(_container.transform, false);

        // Красивый темный полупрозрачный фон для читаемости
        Image bg = itemGo.AddComponent<Image>();
        bg.color = new Color(0.07f, 0.07f, 0.09f, 0.85f);

        LayoutElement le = itemGo.AddComponent<LayoutElement>();
        le.preferredHeight = 40f;
        le.preferredWidth = 280f;

        // Создаем текст
        GameObject textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(itemGo.transform, false);
        
        RectTransform textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(15f, 0f);
        textRt.offsetMax = new Vector2(-15f, 0f);

        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        StoryManager.ApplyFont(tmp); // Применяем кастомный шрифт
        tmp.text = text;
        tmp.fontSize = 16f;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.fontStyle = FontStyles.Bold;

        // CanvasGroup для плавного затухания
        CanvasGroup cg = itemGo.AddComponent<CanvasGroup>();
        
        StartCoroutine(AnimateNotification(itemGo, cg));
    }

    private IEnumerator AnimateNotification(GameObject item, CanvasGroup cg)
    {
        // Плавное появление
        float t = 0f;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0f, 1f, t / 0.15f);
            yield return null;
        }
        cg.alpha = 1f;

        // Время удержания на экране
        yield return new WaitForSeconds(3.0f);

        // Плавное исчезновение
        t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, t / 0.25f);
            yield return null;
        }

        if (item != null) Destroy(item);
    }

    // ── Построение контейнера на Canvas ─────────────────────────────
    private void BuildContainer()
    {
        // Ищем существующий HUD Canvas на сцене
        Canvas hudCanvas = null;
        Canvas[] allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach(var c in allCanvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.name.Contains("HUD"))
            {
                hudCanvas = c;
                break;
            }
        }
        
        if (hudCanvas == null && allCanvases.Length > 0) hudCanvas = allCanvases[0];

        Transform parentTransform = hudCanvas != null ? hudCanvas.transform : null;

        // Если Canvas не найден, делаем свой временный overlay
        if (parentTransform == null)
        {
            GameObject canvasGo = new GameObject("[FallbackNotifCanvas]", typeof(RectTransform));
            Canvas c = canvasGo.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 999;
            CanvasScaler cs = canvasGo.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            parentTransform = canvasGo.transform;

        }

        _container = new GameObject("NotificationContainer", typeof(RectTransform));
        _container.transform.SetParent(parentTransform, false);

        RectTransform rt = _container.GetComponent<RectTransform>();
        // Позиционируем в ПРАВЫЙ ВЕРХНИЙ угол экрана (чуть ниже таймера фаз)
        rt.anchorMin = new Vector2(1f, 0.4f);
        rt.anchorMax = new Vector2(1f, 0.8f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(300f, 400f);
        rt.anchoredPosition = new Vector2(-20f, -50f);

        // Вертикальная автоматическая укладка плашек
        _layoutGroup = _container.AddComponent<VerticalLayoutGroup>();
        _layoutGroup.childAlignment = TextAnchor.UpperRight;
        _layoutGroup.spacing = 6f;
        _layoutGroup.childControlHeight = true;
        _layoutGroup.childControlWidth = true;
        _layoutGroup.childForceExpandHeight = false;
        _layoutGroup.childForceExpandWidth = true;
    }
}
