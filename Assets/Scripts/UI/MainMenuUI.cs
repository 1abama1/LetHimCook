using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Panels")] public CanvasGroup mainPanel;
    public CanvasGroup creditsPanel;
    public CanvasGroup settingsPanel;

    [Header("Main Buttons")] public Button playButton;
    public Button creditsButton;
    public Button settingsButton;
    public Button quitButton;

    [Header("Credits")] public Button creditsBackButton;
    [Header("Settings")] public Slider musicSlider;
    public Button settingsBackButton;

    [Header("Scene")] public string gameScene = "Game";
    [Header("Animations")] public float fadeDuration = 0.4f;

    private void Awake()
    {
        Time.timeScale = 1f;

        EnsureSettingsPanel();

        ShowPanel(mainPanel);
        HidePanel(creditsPanel);
        HidePanel(settingsPanel);

        if (playButton != null) playButton.onClick.AddListener(OnPlay);
        if (creditsButton != null) creditsButton.onClick.AddListener(() => SwitchTo(creditsPanel));
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuit);
        if (creditsBackButton != null) creditsBackButton.onClick.AddListener(() => SwitchTo(mainPanel));
        if (settingsBackButton != null) settingsBackButton.onClick.AddListener(CloseSettings);

        if (musicSlider != null)
        {
            musicSlider.minValue = 0f;
            musicSlider.maxValue = 1f;
            musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.4f);
            musicSlider.onValueChanged.AddListener(value =>
            {
                PlayerPrefs.SetFloat("MusicVolume", value);
                AudioManager.Instance?.SetMusicVolume(value);
            });
        }

        SetupButtonVisuals();

        if (EventSystem.current != null && playButton != null)
            EventSystem.current.SetSelectedGameObject(playButton.gameObject);
    }

    private void EnsureSettingsPanel()
    {
        if (settingsPanel != null)
            return;

        Transform parent = mainPanel != null ? mainPanel.transform.parent : transform;
        TMP_FontAsset sourceFont = null;

        if (settingsButton != null)
        {
            TMP_Text sourceLabel = settingsButton.GetComponentInChildren<TMP_Text>(true);
            if (sourceLabel != null)
                sourceFont = sourceLabel.font;
        }

        if (sourceFont == null && playButton != null)
        {
            TMP_Text sourceLabel = playButton.GetComponentInChildren<TMP_Text>(true);
            if (sourceLabel != null)
                sourceFont = sourceLabel.font;
        }

        MainMenuSettingsPanel generated = MainMenuSettingsPanel.Create(parent, sourceFont);
        settingsPanel = generated.Group;
        musicSlider = generated.MusicSlider;
        settingsBackButton = generated.BackButton;
    }

    private void SetupButtonVisuals()
    {
        StyleButton(playButton, MenuButtonFx.Variant.Primary);
        StyleButton(creditsButton, MenuButtonFx.Variant.Standard);
        StyleButton(settingsButton, MenuButtonFx.Variant.Standard);
        StyleButton(quitButton, MenuButtonFx.Variant.Danger);
        StyleButton(creditsBackButton, MenuButtonFx.Variant.Standard);
        StyleButton(settingsBackButton, MenuButtonFx.Variant.Standard);
    }

    private static void StyleButton(Button button, MenuButtonFx.Variant variant)
    {
        if (button == null) return;

        MenuButtonFx fx = button.GetComponent<MenuButtonFx>();
        if (fx == null)
            fx = button.gameObject.AddComponent<MenuButtonFx>();

        fx.Configure(button, variant);
    }

    private void OpenSettings()
    {
        SwitchTo(settingsPanel);

        if (EventSystem.current != null && musicSlider != null)
            EventSystem.current.SetSelectedGameObject(musicSlider.gameObject);
    }

    private void CloseSettings()
    {
        SwitchTo(mainPanel);

        if (EventSystem.current != null && settingsButton != null)
            EventSystem.current.SetSelectedGameObject(settingsButton.gameObject);
    }

    private void OnPlay() => StartCoroutine(FadeAndLoad());

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SwitchTo(CanvasGroup target)
    {
        if (target == null)
            return;

        StopAllCoroutines();
        StartCoroutine(CrossFade(target));
    }

    private IEnumerator CrossFade(CanvasGroup target)
    {
        CanvasGroup[] panels = { mainPanel, creditsPanel, settingsPanel };

        foreach (CanvasGroup panel in panels)
        {
            if (panel != null && panel.alpha > 0.1f && panel != target)
                yield return StartCoroutine(Fade(panel, 0f));
        }

        yield return StartCoroutine(Fade(target, 1f));
    }

    private IEnumerator Fade(CanvasGroup group, float to)
    {
        if (group == null)
            yield break;

        float from = group.alpha;
        float elapsed = 0f;

        group.blocksRaycasts = to > 0.5f;
        group.interactable = to > 0.5f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        group.alpha = to;
    }

    private IEnumerator FadeAndLoad()
    {
        if (mainPanel != null)
            yield return StartCoroutine(Fade(mainPanel, 0f));

        SceneManager.LoadScene(gameScene);
    }

    private static void ShowPanel(CanvasGroup group)
    {
        if (group == null)
            return;

        group.alpha = 1f;
        group.blocksRaycasts = true;
        group.interactable = true;
    }

    private static void HidePanel(CanvasGroup group)
    {
        if (group == null)
            return;

        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
    }
}
