using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// Главное меню. Привяжите кнопки к публичным методам через Inspector.
/// Имя игровой сцены — gameScene.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Panels")]
    public CanvasGroup mainPanel;
    public CanvasGroup creditsPanel;
    public CanvasGroup settingsPanel;

    [Header("Main Buttons")]
    public Button playButton;
    public Button creditsButton;
    public Button settingsButton;
    public Button quitButton;

    [Header("Credits")]
    public Button creditsBackButton;

    [Header("Settings")]
    public Slider musicSlider;
    public Button settingsBackButton;

    [Header("Scene")]
    public string gameScene = "Game";

    [Header("Animations")]
    public float fadeDuration = 0.4f;

    void Awake()
    {
        // Убеждаемся что Time.timeScale = 1 (мог остаться от предыдущей сессии)
        Time.timeScale = 1f;

        ShowPanel(mainPanel);
        HidePanel(creditsPanel);
        HidePanel(settingsPanel);

        if (playButton     != null) playButton.onClick.AddListener(OnPlay);
        if (creditsButton  != null) creditsButton.onClick.AddListener(() => SwitchTo(creditsPanel));
        if (settingsButton != null) settingsButton.onClick.AddListener(() => SwitchTo(settingsPanel));
        if (quitButton     != null) quitButton.onClick.AddListener(OnQuit);

        if (creditsBackButton  != null) creditsBackButton.onClick.AddListener(() => SwitchTo(mainPanel));
        if (settingsBackButton != null) settingsBackButton.onClick.AddListener(() => SwitchTo(mainPanel));

        if (musicSlider != null)
        {
            musicSlider.minValue = 0f;
            musicSlider.maxValue = 1f;
            musicSlider.value    = PlayerPrefs.GetFloat("MusicVolume", 0.4f);
            musicSlider.onValueChanged.AddListener(v =>
            {
                PlayerPrefs.SetFloat("MusicVolume", v);
                AudioManager.Instance?.SetMusicVolume(v);
            });
        }
    }

    // ─── Кнопки ──────────────────────────────────────────────────────────────
    void OnPlay()
    {
        StartCoroutine(FadeAndLoad());
    }

    void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ─── Переключение панелей ─────────────────────────────────────────────────
    void SwitchTo(CanvasGroup target)
    {
        StopAllCoroutines();
        StartCoroutine(CrossFade(target));
    }

    IEnumerator CrossFade(CanvasGroup target)
    {
        // Находим активную панель
        CanvasGroup[] all = { mainPanel, creditsPanel, settingsPanel };
        foreach (var p in all)
        {
            if (p != null && p.alpha > 0.1f && p != target)
                yield return StartCoroutine(Fade(p, 0f));
        }
        yield return StartCoroutine(Fade(target, 1f));
    }

    IEnumerator Fade(CanvasGroup group, float to)
    {
        if (group == null) yield break;
        float from = group.alpha;
        float t    = 0f;
        group.blocksRaycasts = to > 0.5f;
        group.interactable   = to > 0.5f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        group.alpha = to;
    }

    IEnumerator FadeAndLoad()
    {
        if (mainPanel != null)
            yield return StartCoroutine(Fade(mainPanel, 0f));
        SceneManager.LoadScene(gameScene);
    }

    static void ShowPanel(CanvasGroup g)
    {
        if (g == null) return;
        g.alpha = 1f; g.blocksRaycasts = true; g.interactable = true;
    }

    static void HidePanel(CanvasGroup g)
    {
        if (g == null) return;
        g.alpha = 0f; g.blocksRaycasts = false; g.interactable = false;
    }
}
