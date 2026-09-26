using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuSettingsPanel : MonoBehaviour
{
    private const string MusicVolumeKey = "MusicVolume";
    private const string MasterVolumeKey = "MasterVolume";
    private const string FullscreenKey = "Fullscreen";
    private const string VSyncKey = "VSync";

    private static readonly Color OverlayColor = new Color(0.025f, 0.03f, 0.035f, 0.82f);
    private static readonly Color CardColor = new Color(0.055f, 0.065f, 0.07f, 0.97f);
    private static readonly Color SurfaceColor = new Color(0.09f, 0.105f, 0.11f, 1f);
    private static readonly Color SurfaceHoverColor = new Color(0.14f, 0.16f, 0.17f, 1f);
    private static readonly Color AccentColor = new Color(1f, 0.67f, 0.27f, 1f);
    private static readonly Color MainTextColor = new Color(0.95f, 0.92f, 0.86f, 1f);
    private static readonly Color MutedTextColor = new Color(0.62f, 0.64f, 0.63f, 1f);

    public CanvasGroup Group { get; private set; }
    public Slider MusicSlider { get; private set; }
    public Button BackButton { get; private set; }

    private TMP_FontAsset font;
    private Slider masterSlider;
    private TMP_Text musicValueText;
    private TMP_Text masterValueText;
    private TMP_Text resolutionValueText;
    private TMP_Text fullscreenValueText;
    private TMP_Text vSyncValueText;
    private Button fullscreenButton;
    private Button vSyncButton;

    private readonly List<Vector2Int> resolutions = new List<Vector2Int>(16);
    private int resolutionIndex;

    public static MainMenuSettingsPanel Create(Transform parent, TMP_FontAsset sourceFont)
    {
        GameObject root = new GameObject(
            "SettingsPanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup),
            typeof(MainMenuSettingsPanel));

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.SetParent(parent, false);
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.SetAsLastSibling();

        MainMenuSettingsPanel panel = root.GetComponent<MainMenuSettingsPanel>();
        panel.font = sourceFont != null ? sourceFont : TMP_Settings.defaultFontAsset;
        panel.Build();
        return panel;
    }

    private void Build()
    {
        Image overlay = GetComponent<Image>();
        overlay.color = OverlayColor;
        overlay.raycastTarget = true;

        Group = GetComponent<CanvasGroup>();
        Group.alpha = 0f;
        Group.interactable = false;
        Group.blocksRaycasts = false;

        RectTransform card = CreateRect("SettingsCard", transform, new Vector2(640f, 650f), Vector2.zero);
        Image cardImage = card.gameObject.AddComponent<Image>();
        cardImage.color = CardColor;

        Outline outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.78f);
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = true;

        RectTransform accent = CreateRect("Accent", card, new Vector2(5f, 590f), new Vector2(-302f, 0f));
        Image accentImage = accent.gameObject.AddComponent<Image>();
        accentImage.color = AccentColor;
        accentImage.raycastTarget = false;

        TMP_Text eyebrow = CreateText(
            "Eyebrow",
            card,
            "SYSTEM / CONFIGURATION",
            13f,
            MutedTextColor,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(510f, 28f),
            new Vector2(-20f, 274f));
        eyebrow.characterSpacing = 3f;

        TMP_Text title = CreateText(
            "Title",
            card,
            "SETTINGS",
            38f,
            MainTextColor,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(510f, 58f),
            new Vector2(-20f, 232f));
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 5f;

        TMP_Text subtitle = CreateText(
            "Subtitle",
            card,
            "Changes are saved automatically",
            14f,
            MutedTextColor,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(510f, 28f),
            new Vector2(-20f, 192f));

        CreateDivider(card, 158f);

        CreateSectionLabel(card, "AUDIO", 130f);
        MusicSlider = CreateSliderRow(card, "MUSIC", 86f, out musicValueText);
        masterSlider = CreateSliderRow(card, "MASTER", 22f, out masterValueText);

        CreateDivider(card, -22f);

        CreateSectionLabel(card, "DISPLAY", -52f);
        CreateResolutionRow(card, -100f);
        fullscreenButton = CreateToggleRow(card, "FULLSCREEN", -164f, out fullscreenValueText);
        vSyncButton = CreateToggleRow(card, "V-SYNC", -224f, out vSyncValueText);

        BackButton = CreateButton(card, "BackButton", "BACK", new Vector2(220f, 46f), new Vector2(-150f, -286f), TextAlignmentOptions.MidlineLeft);
        BackButton.GetComponentInChildren<TMP_Text>().margin = new Vector4(18f, 0f, 8f, 0f);

        TMP_Text hint = CreateText(
            "Hint",
            card,
            "ESC / B  •  BACK",
            12f,
            MutedTextColor,
            TextAlignmentOptions.MidlineRight,
            new Vector2(230f, 30f),
            new Vector2(155f, -286f));
        hint.characterSpacing = 2f;

        SetupResolutions();
        LoadAndApplyValues();
        BindEvents();
    }

    private void BindEvents()
    {
        MusicSlider.onValueChanged.AddListener(OnMusicChanged);
        masterSlider.onValueChanged.AddListener(OnMasterChanged);
        fullscreenButton.onClick.AddListener(ToggleFullscreen);
        vSyncButton.onClick.AddListener(ToggleVSync);
    }

    private void LoadAndApplyValues()
    {
        float music = PlayerPrefs.GetFloat(MusicVolumeKey, 0.4f);
        float master = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);

        MusicSlider.SetValueWithoutNotify(music);
        masterSlider.SetValueWithoutNotify(master);

        AudioListener.volume = master;
        AudioManager.Instance?.SetMusicVolume(music);

        bool fullscreen = PlayerPrefs.HasKey(FullscreenKey)
            ? PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1
            : Screen.fullScreen;

        if (fullscreen != Screen.fullScreen)
            Screen.fullScreen = fullscreen;

        int vSync = PlayerPrefs.GetInt(VSyncKey, QualitySettings.vSyncCount > 0 ? 1 : 0);
        QualitySettings.vSyncCount = vSync == 1 ? 1 : 0;

        UpdateMusicLabel(music);
        UpdateMasterLabel(master);
        UpdateFullscreenLabel(fullscreen);
        UpdateVSyncLabel(vSync == 1);
        UpdateResolutionLabel();
    }

    private void OnMusicChanged(float value)
    {
        PlayerPrefs.SetFloat(MusicVolumeKey, value);
        AudioManager.Instance?.SetMusicVolume(value);
        UpdateMusicLabel(value);
    }

    private void OnMasterChanged(float value)
    {
        PlayerPrefs.SetFloat(MasterVolumeKey, value);
        AudioListener.volume = value;
        UpdateMasterLabel(value);
    }

    private void ToggleFullscreen()
    {
        bool next = !Screen.fullScreen;
        Screen.fullScreen = next;
        PlayerPrefs.SetInt(FullscreenKey, next ? 1 : 0);
        PlayerPrefs.Save();
        UpdateFullscreenLabel(next);
    }

    private void ToggleVSync()
    {
        bool enabled = QualitySettings.vSyncCount == 0;
        QualitySettings.vSyncCount = enabled ? 1 : 0;
        PlayerPrefs.SetInt(VSyncKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        UpdateVSyncLabel(enabled);
    }

    private void SetupResolutions()
    {
        resolutions.Clear();

        Resolution[] available = Screen.resolutions;
        for (int i = 0; i < available.Length; i++)
        {
            Vector2Int candidate = new Vector2Int(available[i].width, available[i].height);
            bool duplicate = false;

            for (int j = 0; j < resolutions.Count; j++)
            {
                if (resolutions[j] == candidate)
                {
                    duplicate = true;
                    break;
                }
            }

            if (!duplicate)
                resolutions.Add(candidate);
        }

        if (resolutions.Count == 0)
            resolutions.Add(new Vector2Int(Screen.width, Screen.height));

        resolutionIndex = 0;
        int bestDistance = int.MaxValue;

        for (int i = 0; i < resolutions.Count; i++)
        {
            int dx = resolutions[i].x - Screen.width;
            int dy = resolutions[i].y - Screen.height;
            int distance = Mathf.Abs(dx) + Mathf.Abs(dy);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                resolutionIndex = i;
            }
        }
    }

    private void ChangeResolution(int direction)
    {
        if (resolutions.Count == 0)
            return;

        resolutionIndex += direction;

        if (resolutionIndex < 0)
            resolutionIndex = resolutions.Count - 1;
        else if (resolutionIndex >= resolutions.Count)
            resolutionIndex = 0;

        Vector2Int resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.x, resolution.y, Screen.fullScreenMode);

        PlayerPrefs.SetInt("ResolutionWidth", resolution.x);
        PlayerPrefs.SetInt("ResolutionHeight", resolution.y);
        PlayerPrefs.Save();

        UpdateResolutionLabel();
    }

    private void UpdateMusicLabel(float value)
    {
        if (musicValueText != null)
            musicValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    private void UpdateMasterLabel(float value)
    {
        if (masterValueText != null)
            masterValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    private void UpdateResolutionLabel()
    {
        if (resolutionValueText == null || resolutions.Count == 0)
            return;

        Vector2Int resolution = resolutions[resolutionIndex];
        resolutionValueText.text = resolution.x + " × " + resolution.y;
    }

    private void UpdateFullscreenLabel(bool enabled)
    {
        if (fullscreenValueText == null)
            return;

        fullscreenValueText.text = enabled ? "ON" : "OFF";
        fullscreenValueText.color = enabled ? AccentColor : MutedTextColor;
    }

    private void UpdateVSyncLabel(bool enabled)
    {
        if (vSyncValueText == null)
            return;

        vSyncValueText.text = enabled ? "ON" : "OFF";
        vSyncValueText.color = enabled ? AccentColor : MutedTextColor;
    }

    private Slider CreateSliderRow(Transform parent, string labelText, float y, out TMP_Text valueText)
    {
        CreateText(
            labelText + "Label",
            parent,
            labelText,
            16f,
            MainTextColor,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(160f, 34f),
            new Vector2(-205f, y));

        RectTransform sliderRect = CreateRect(labelText + "Slider", parent, new Vector2(285f, 34f), new Vector2(74f, y));
        Slider slider = sliderRect.gameObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;

        RectTransform backgroundRect = CreateStretchRect("Background", sliderRect, new Vector2(0f, 12f), new Vector2(0f, -12f));
        Image background = backgroundRect.gameObject.AddComponent<Image>();
        background.color = SurfaceColor;
        background.raycastTarget = false;

        RectTransform fillArea = CreateStretchRect("FillArea", sliderRect, new Vector2(4f, 13f), new Vector2(-4f, -13f));
        RectTransform fillRect = CreateStretchRect("Fill", fillArea, Vector2.zero, Vector2.zero);
        Image fill = fillRect.gameObject.AddComponent<Image>();
        fill.color = AccentColor;
        fill.raycastTarget = false;

        RectTransform handleArea = CreateStretchRect("HandleArea", sliderRect, new Vector2(8f, 0f), new Vector2(-8f, 0f));
        RectTransform handleRect = CreateRect("Handle", handleArea, new Vector2(16f, 28f), Vector2.zero);
        handleRect.anchorMin = new Vector2(0f, 0.5f);
        handleRect.anchorMax = new Vector2(0f, 0.5f);
        Image handle = handleRect.gameObject.AddComponent<Image>();
        handle.color = MainTextColor;

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;

        valueText = CreateText(
            labelText + "Value",
            parent,
            "0%",
            14f,
            MutedTextColor,
            TextAlignmentOptions.MidlineRight,
            new Vector2(70f, 34f),
            new Vector2(255f, y));

        return slider;
    }

    private void CreateResolutionRow(Transform parent, float y)
    {
        CreateText(
            "ResolutionLabel",
            parent,
            "RESOLUTION",
            16f,
            MainTextColor,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(160f, 40f),
            new Vector2(-205f, y));

        Button previous = CreateButton(parent, "PreviousResolution", "‹", new Vector2(46f, 40f), new Vector2(28f, y), TextAlignmentOptions.Center);
        Button next = CreateButton(parent, "NextResolution", "›", new Vector2(46f, 40f), new Vector2(268f, y), TextAlignmentOptions.Center);

        RectTransform valuePlate = CreateRect("ResolutionPlate", parent, new Vector2(182f, 40f), new Vector2(148f, y));
        Image plateImage = valuePlate.gameObject.AddComponent<Image>();
        plateImage.color = SurfaceColor;
        plateImage.raycastTarget = false;

        resolutionValueText = CreateText(
            "ResolutionValue",
            valuePlate,
            "",
            15f,
            MainTextColor,
            TextAlignmentOptions.Center,
            new Vector2(176f, 36f),
            Vector2.zero);

        previous.onClick.AddListener(() => ChangeResolution(-1));
        next.onClick.AddListener(() => ChangeResolution(1));
    }

    private Button CreateToggleRow(Transform parent, string labelText, float y, out TMP_Text valueText)
    {
        CreateText(
            labelText + "Label",
            parent,
            labelText,
            16f,
            MainTextColor,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(180f, 40f),
            new Vector2(-195f, y));

        Button button = CreateButton(
            parent,
            labelText + "Button",
            "OFF",
            new Vector2(150f, 40f),
            new Vector2(198f, y),
            TextAlignmentOptions.Center);

        valueText = button.GetComponentInChildren<TMP_Text>();
        valueText.fontStyle = FontStyles.Bold;
        valueText.characterSpacing = 3f;
        return button;
    }

    private Button CreateButton(
        Transform parent,
        string name,
        string text,
        Vector2 size,
        Vector2 position,
        TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent, size, position);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = SurfaceColor;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.28f, 1.28f, 1.28f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TMP_Text buttonText = CreateText(
            "Label",
            rect,
            text,
            15f,
            MainTextColor,
            alignment,
            size - new Vector2(8f, 4f),
            Vector2.zero);
        buttonText.raycastTarget = false;

        return button;
    }

    private void CreateSectionLabel(Transform parent, string text, float y)
    {
        TMP_Text section = CreateText(
            text + "Section",
            parent,
            text,
            12f,
            AccentColor,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(500f, 24f),
            new Vector2(-20f, y));
        section.fontStyle = FontStyles.Bold;
        section.characterSpacing = 4f;
    }

    private void CreateDivider(Transform parent, float y)
    {
        RectTransform line = CreateRect("Divider", parent, new Vector2(530f, 1f), new Vector2(-5f, y));
        Image image = line.gameObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.1f);
        image.raycastTarget = false;
    }

    private TMP_Text CreateText(
        string name,
        Transform parent,
        string text,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment,
        Vector2 size,
        Vector2 position)
    {
        RectTransform rect = CreateRect(name, parent, size, position);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.font = font;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = alignment;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private static RectTransform CreateStretchRect(string name, Transform parent, Vector2 minOffset, Vector2 maxOffset)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = minOffset;
        rect.offsetMax = maxOffset;
        return rect;
    }
}
