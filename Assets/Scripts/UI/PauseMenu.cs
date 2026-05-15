using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Паузное меню. Привяжите кнопку Pause к OnPauseButtonPressed()
/// или она сработает автоматически по клавише Escape.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    public CanvasGroup    pausePanel;
    public Button         resumeButton;
    public Button         restartButton;
    public Button         mainMenuButton;
    public Slider         musicVolumeSlider;
    public Slider         sfxVolumeSlider;
    public TextMeshProUGUI musicValueText;
    public TextMeshProUGUI sfxValueText;

    [Header("Settings")]
    public string mainMenuScene = "MainMenu";
    public string gameScene     = "Game";

    private bool _isPaused;

    void Awake()
    {
        if (pausePanel != null)
        {
            pausePanel.alpha          = 0f;
            pausePanel.blocksRaycasts = false;
            pausePanel.interactable   = false;
        }

        if (resumeButton   != null) resumeButton.onClick.AddListener(Resume);
        if (restartButton  != null) restartButton.onClick.AddListener(Restart);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMainMenu);

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.value    = PlayerPrefs.GetFloat("MusicVolume", 0.4f);
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }
    }

    void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame ?? false)
            TogglePause();
    }

    // ─── Пауза ───────────────────────────────────────────────────────────────
    public void TogglePause()
    {
        if (_isPaused) Resume();
        else           Pause();
    }

    public void Pause()
    {
        _isPaused       = true;
        Time.timeScale  = 0f;
        ShowPanel(true);
        AudioManager.Instance?.SetMusicVolume(0.15f);
    }

    public void Resume()
    {
        _isPaused       = false;
        Time.timeScale  = 1f;
        ShowPanel(false);
        float vol = PlayerPrefs.GetFloat("MusicVolume", 0.4f);
        AudioManager.Instance?.SetMusicVolume(vol);
    }

    void ShowPanel(bool show)
    {
        if (pausePanel == null) return;
        StopAllCoroutines();
        StartCoroutine(FadePanel(show ? 1f : 0f, show));
    }

    IEnumerator FadePanel(float targetAlpha, bool interactive)
    {
        float start   = pausePanel.alpha;
        float elapsed = 0f;
        float dur     = 0.2f;

        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            pausePanel.alpha = Mathf.Lerp(start, targetAlpha, elapsed / dur);
            yield return null;
        }
        pausePanel.alpha          = targetAlpha;
        pausePanel.blocksRaycasts = interactive;
        pausePanel.interactable   = interactive;
    }

    // ─── Слайдеры ─────────────────────────────────────────────────────────────
    void OnMusicVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
        AudioManager.Instance?.SetMusicVolume(value);
        if (musicValueText != null) musicValueText.text = $"{Mathf.RoundToInt(value * 100)}%";
    }

    // ─── Кнопки ──────────────────────────────────────────────────────────────
    void Restart()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(gameScene);
    }

    void GoToMainMenu()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuScene);
    }

    // Привяжите к кнопке паузы через инспектор
    public void OnPauseButtonPressed() => TogglePause();
}
