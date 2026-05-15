using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Полноэкранный Game Over / Victory экран.
/// Подпишитесь на RestaurantReputation.onGameOver в инспекторе,
/// или вызовите ShowGameOver() / ShowVictory() напрямую.
/// </summary>
public class GameOverScreen : MonoBehaviour
{
    [Header("Panels")]
    public CanvasGroup gameOverPanel;
    public CanvasGroup victoryPanel;

    [Header("Game Over Texts")]
    public TextMeshProUGUI gameOverTitleText;
    public TextMeshProUGUI gameOverReasonText;
    public TextMeshProUGUI finalMoneyText;
    public TextMeshProUGUI finalReputationText;

    [Header("Victory Texts")]
    public TextMeshProUGUI victoryTitleText;
    public TextMeshProUGUI victoryScoreText;

    [Header("Buttons")]
    public Button restartButton;
    public Button mainMenuButton;

    [Header("Settings")]
    public float fadeInDuration = 1.2f;
    public string mainMenuScene = "MainMenu";
    public string gameScene     = "Game";

    void Awake()
    {
        if (gameOverPanel != null) { gameOverPanel.alpha = 0f; gameOverPanel.gameObject.SetActive(false); }
        if (victoryPanel  != null) { victoryPanel.alpha  = 0f; victoryPanel.gameObject.SetActive(false);  }

        if (RestaurantReputation.Instance != null)
            RestaurantReputation.Instance.onGameOver.AddListener(ShowGameOver);

        if (restartButton  != null) restartButton.onClick.AddListener(Restart);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMainMenu);
    }

    // ─── Показ экранов ───────────────────────────────────────────────────────
    public void ShowGameOver(string reason = "Репутация ресторана упала до нуля.")
    {
        Time.timeScale = 0f;
        if (gameOverPanel == null) return;

        gameOverPanel.gameObject.SetActive(true);

        if (gameOverReasonText != null) gameOverReasonText.text = reason;
        if (finalMoneyText     != null) finalMoneyText.text     = $"Деньги: ${PlayerEconomy.Instance?.Money ?? 0}";
        if (finalReputationText != null)
            finalReputationText.text = $"Репутация: {Mathf.RoundToInt(RestaurantReputation.Instance?.reputation ?? 0f)}";

        StartCoroutine(FadeIn(gameOverPanel));
        AudioManager.Instance?.Play("game_over");
    }

    // Перегрузка без параметра для UnityEvent
    public void ShowGameOver() => ShowGameOver("Репутация ресторана упала до нуля.");

    public void ShowVictory()
    {
        Time.timeScale = 0f;
        if (victoryPanel == null) return;

        victoryPanel.gameObject.SetActive(true);

        int money = PlayerEconomy.Instance?.Money ?? 0;
        float rep = RestaurantReputation.Instance?.reputation ?? 50f;
        int score = money + Mathf.RoundToInt(rep * 10f);

        if (victoryScoreText != null)
            victoryScoreText.text = $"Деньги: ${money}  |  Репутация: {rep:F0}  |  Счёт: {score}";

        StartCoroutine(FadeIn(victoryPanel));
        AudioManager.Instance?.Play("victory");
    }

    // ─── Анимация ─────────────────────────────────────────────────────────────
    IEnumerator FadeIn(CanvasGroup group)
    {
        float t = 0f;
        group.blocksRaycasts = false;
        while (t < fadeInDuration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(t / fadeInDuration);
            yield return null;
        }
        group.alpha          = 1f;
        group.blocksRaycasts = true;
        group.interactable   = true;
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
}
