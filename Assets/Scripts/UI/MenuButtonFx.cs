using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MenuButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    public enum Variant { Primary, Standard, Danger }

    private const float HoverScale = 1.035f;
    private const float PressedScale = 0.985f;
    private const float HoverOffset = 6f;
    private const float AnimationDuration = 0.11f;

    private Button button;
    private RectTransform rect;
    private Image background;
    private Image accent;
    private TMP_Text label;
    private Vector3 baseScale;
    private Vector2 basePosition;
    private Color normalBackground, hoverBackground, pressedBackground, normalText, hoverText, accentColor;
    private bool hovered;
    private Coroutine animationRoutine;

    public void Configure(Button targetButton, Variant variant)
    {
        if (targetButton == null) return;

        button = targetButton;
        rect = targetButton.transform as RectTransform;
        background = targetButton.targetGraphic as Image ?? targetButton.GetComponent<Image>();
        label = targetButton.GetComponentInChildren<TMP_Text>(true);

        if (rect == null || background == null) return;

        baseScale = rect.localScale;
        basePosition = rect.anchoredPosition;

        rect.sizeDelta = new Vector2(Mathf.Max(rect.sizeDelta.x, 190f), Mathf.Max(rect.sizeDelta.y, 40f));
        button.transition = Selectable.Transition.None;
        button.targetGraphic = background;

        ApplyPalette(variant);
        ConfigureBackground();
        ConfigureLabel();
        ConfigureAccent();
        ApplyImmediate(baseScale, basePosition, normalBackground, normalText, WithAlpha(accentColor, 0.72f));
    }

    private void ApplyPalette(Variant variant)
    {
        switch (variant)
        {
            case Variant.Primary:
                normalBackground = Hex("17130F");
                hoverBackground = Hex("2B1D10");
                pressedBackground = Hex("3A2410");
                normalText = Hex("F4E8D7");
                hoverText = Color.white;
                accentColor = Hex("FFAA45");
                break;
            case Variant.Danger:
                normalBackground = Hex("190F11");
                hoverBackground = Hex("321519");
                pressedBackground = Hex("42191E");
                normalText = Hex("EEDBDD");
                hoverText = Color.white;
                accentColor = Hex("E85B66");
                break;
            default:
                normalBackground = Hex("11161A");
                hoverBackground = Hex("1E272D");
                pressedBackground = Hex("28343B");
                normalText = Hex("E1E5E7");
                hoverText = Color.white;
                accentColor = Hex("C8B99F");
                break;
        }
    }

    private void ConfigureBackground()
    {
        background.color = normalBackground;
        background.raycastTarget = true;

        Outline outline = GetComponent<Outline>();
        if (outline == null) outline = gameObject.AddComponent<Outline>();

        outline.effectColor = new Color(0f, 0f, 0f, 0.72f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        outline.useGraphicAlpha = true;
    }

    private void ConfigureLabel()
    {
        if (label == null) return;

        label.text = label.text.Trim().ToUpperInvariant();
        label.fontStyle = FontStyles.Bold;
        label.enableAutoSizing = true;
        label.fontSizeMin = 13f;
        label.fontSizeMax = 19f;
        label.characterSpacing = 4f;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.margin = new Vector4(18f, 0f, 8f, 0f);
        label.raycastTarget = false;

        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = Vector2.zero;
    }

    private void ConfigureAccent()
    {
        Transform existing = transform.Find("MenuAccent");
        if (existing != null)
        {
            accent = existing.GetComponent<Image>();
            return;
        }

        GameObject accentObject = new GameObject("MenuAccent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform accentRect = accentObject.GetComponent<RectTransform>();
        accentRect.SetParent(transform, false);
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(0f, 1f);
        accentRect.pivot = new Vector2(0f, 0.5f);
        accentRect.anchoredPosition = new Vector2(3f, 0f);
        accentRect.sizeDelta = new Vector2(4f, -8f);

        accent = accentObject.GetComponent<Image>();
        accent.raycastTarget = false;
        accentObject.transform.SetAsLastSibling();
    }

    public void OnPointerEnter(PointerEventData eventData) { if (!CanAnimate()) return; hovered = true; AnimateToHover(); }
    public void OnPointerExit(PointerEventData eventData) { hovered = false; if (CanAnimate()) AnimateToNormal(); }
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanAnimate()) return;
        Animate(ScaleFromBase(PressedScale), basePosition + new Vector2(HoverOffset * 0.55f, 0f), pressedBackground, hoverText, accentColor);
    }
    public void OnPointerUp(PointerEventData eventData) { if (!CanAnimate()) return; if (hovered) AnimateToHover(); else AnimateToNormal(); }
    public void OnSelect(BaseEventData eventData) { if (!CanAnimate()) return; hovered = true; AnimateToHover(); }
    public void OnDeselect(BaseEventData eventData) { hovered = false; if (CanAnimate()) AnimateToNormal(); }

    private void AnimateToHover() => Animate(ScaleFromBase(HoverScale), basePosition + new Vector2(HoverOffset, 0f), hoverBackground, hoverText, accentColor);
    private void AnimateToNormal() => Animate(baseScale, basePosition, normalBackground, normalText, WithAlpha(accentColor, 0.72f));

    private void Animate(Vector3 targetScale, Vector2 targetPosition, Color targetBackground, Color targetText, Color targetAccent)
    {
        if (animationRoutine != null) StopCoroutine(animationRoutine);
        animationRoutine = StartCoroutine(AnimateRoutine(targetScale, targetPosition, targetBackground, targetText, targetAccent));
    }

    private IEnumerator AnimateRoutine(Vector3 targetScale, Vector2 targetPosition, Color targetBackground, Color targetText, Color targetAccent)
    {
        Vector3 startScale = rect.localScale;
        Vector2 startPosition = rect.anchoredPosition;
        Color startBackground = background.color;
        Color startText = label != null ? label.color : targetText;
        Color startAccent = accent != null ? accent.color : targetAccent;
        float elapsed = 0f;

        while (elapsed < AnimationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / AnimationDuration);
            t = t * t * (3f - 2f * t);

            rect.localScale = Vector3.LerpUnclamped(startScale, targetScale, t);
            rect.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, t);
            background.color = Color.LerpUnclamped(startBackground, targetBackground, t);
            if (label != null) label.color = Color.LerpUnclamped(startText, targetText, t);
            if (accent != null) accent.color = Color.LerpUnclamped(startAccent, targetAccent, t);
            yield return null;
        }

        ApplyImmediate(targetScale, targetPosition, targetBackground, targetText, targetAccent);
        animationRoutine = null;
    }

    private void ApplyImmediate(Vector3 targetScale, Vector2 targetPosition, Color targetBackground, Color targetText, Color targetAccent)
    {
        rect.localScale = targetScale;
        rect.anchoredPosition = targetPosition;
        background.color = targetBackground;
        if (label != null) label.color = targetText;
        if (accent != null) accent.color = targetAccent;
    }

    private bool CanAnimate() => button != null && button.interactable && rect != null && background != null;

    private Vector3 ScaleFromBase(float multiplier) => new Vector3(baseScale.x * multiplier, baseScale.y * multiplier, baseScale.z);

    private void OnDisable()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        if (rect != null && background != null)
            ApplyImmediate(baseScale, basePosition, normalBackground, normalText, WithAlpha(accentColor, 0.72f));
    }

    private static Color WithAlpha(Color color, float alpha) { color.a = alpha; return color; }
    private static Color Hex(string value) => ColorUtility.TryParseHtmlString("#" + value, out Color color) ? color : Color.white;
}
