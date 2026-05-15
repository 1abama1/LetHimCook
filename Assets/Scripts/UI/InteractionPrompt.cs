using UnityEngine;
using TMPro;

/// <summary>
/// Показывает подсказку "[E] Взаимодействие" над интерактивными объектами
/// в мировом пространстве. Прикрепите к тому же объекту что и IInteractable.
/// Требует: дочерний Canvas (WorldSpace) + TextMeshPro.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class InteractionPrompt : MonoBehaviour
{
    [Header("Settings")]
    public string promptText    = "[E]";
    public float  showDistance  = 2f;
    public float  fadeSpeed     = 8f;
    public Color  promptColor   = new Color(1f, 1f, 0.8f);
    public float  bobAmplitude  = 0.05f;
    public float  bobFrequency  = 2f;

    private TextMeshProUGUI _label;
    private CanvasGroup     _group;
    private Transform       _player;
    private Vector3         _baseLocalPos;

    void Awake()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform rt = GetComponent<RectTransform>();
        rt.sizeDelta  = new Vector2(80f, 25f);
        rt.localScale = Vector3.one * 0.012f;

        _group = gameObject.GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        _group.alpha           = 0f;
        _group.blocksRaycasts  = false;
        _group.interactable    = false;

        // Создаём текст
        GameObject textGo = new GameObject("PromptText");
        textGo.transform.SetParent(transform, false);
        RectTransform textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = textRt.offsetMax = Vector2.zero;

        _label                = textGo.AddComponent<TextMeshProUGUI>();
        _label.text           = promptText;
        _label.color          = promptColor;
        _label.fontSize       = 18f;
        _label.alignment      = TextAlignmentOptions.Center;
        _label.fontStyle      = FontStyles.Bold;

        _baseLocalPos = transform.localPosition;
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p) _player = p.transform;
    }

    void Update()
    {
        if (_player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);
        float targetAlpha = dist <= showDistance ? 1f : 0f;
        _group.alpha = Mathf.Lerp(_group.alpha, targetAlpha, fadeSpeed * Time.deltaTime);

        // Боббинг вверх-вниз
        float bob = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        transform.localPosition = _baseLocalPos + Vector3.up * bob;

        // Billboarding — всегда смотрит на камеру
        if (Camera.main != null)
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                             Camera.main.transform.rotation * Vector3.up);
    }
}
