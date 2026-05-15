using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

public class GameHUD : MonoBehaviour
{
    [Header("Economy")]
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI freshCountText;
    public TextMeshProUGUI spoiledCountText;
    public TextMeshProUGUI lootCountText;

    [Header("Suspicion")]
    public Slider suspicionSlider;
    public Image  suspicionFill;
    public Color  lowSuspicionColor  = Color.green;
    public Color  highSuspicionColor = Color.red;

    [Header("Phase Timer")]
    public TextMeshProUGUI phaseTimerText;
    public TextMeshProUGUI phaseLabel;

    [Header("Reputation")]
    public Slider             reputationSlider;
    public Image              reputationFill;
    public Color              goodReputationColor = new Color(0.2f, 0.8f, 0.4f);
    public Color              badReputationColor  = new Color(0.9f, 0.2f, 0.2f);
    public GameObject         gameOverPanel;

    [Header("Stealth Kill")]
    public Slider     stealthKillSlider;
    public GameObject stealthKillUI;

    [Header("Flashlight Battery")]
    public Slider             batterySlider;
    public Image              batteryFill;
    public TextMeshProUGUI    batteryText;
    public Color              batteryFullColor = new Color(0.3f, 0.9f, 0.4f);
    public Color              batteryLowColor  = new Color(0.9f, 0.2f, 0.1f);
    public GameObject         flashlightBatteryGroup; // вся группа: скрывается днём

    [Header("Dialogue")]
    public GameObject          dialoguePanel;
    public TextMeshProUGUI     speakerNameText;
    public TextMeshProUGUI     dialogueBodyText;
    public Image               portraitImage;
    public Transform           choiceButtonContainer;
    public Button              choiceButtonPrefab;

    private MurderSystem          _murderSystem;
    private FlashlightController  _flashlight;
    private List<Button>          _choiceButtons = new List<Button>();

    void Start()
    {
        _murderSystem = FindFirstObjectByType<MurderSystem>();

        // Фонарик
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            _flashlight = p.GetComponent<FlashlightController>();
            if (_flashlight != null)
                _flashlight.onBatteryChanged.AddListener(UpdateBattery);
        }

        if (PlayerEconomy.Instance != null)
        {
            PlayerEconomy.Instance.onMoneyChanged.AddListener(UpdateMoney);
            PlayerEconomy.Instance.onResourceChanged.AddListener(UpdateResource);
        }

        InspectorAI inspector = FindFirstObjectByType<InspectorAI>();
        if (inspector != null)
            inspector.onSuspicionChanged.AddListener(UpdateSuspicion);

        if (RestaurantReputation.Instance != null)
        {
            RestaurantReputation.Instance.onReputationChanged.AddListener(UpdateReputation);
            RestaurantReputation.Instance.onGameOver.AddListener(ShowGameOver);
        }

        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.onLineStart.AddListener(ShowDialogueLine);
            DialogueSystem.Instance.onShowChoices.AddListener(ShowChoices);
            DialogueSystem.Instance.onDialogueEnd.AddListener(HideDialogue);
        }

        // Подписываемся на смену фаз — батарейка видна только ночью
        if (GameManager.Instance != null)
        {
            GameManager.Instance.onNightBegin.AddListener(() => SetBatteryGroupVisible(true));
            GameManager.Instance.onDayBegin.AddListener(()   => SetBatteryGroupVisible(false));
        }

        UpdateMoney(PlayerEconomy.Instance?.Money ?? 0);
        UpdateSuspicion(0f);
        UpdateReputation(RestaurantReputation.Instance?.reputation ?? 50f);
        UpdateBattery(1f);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        bool isNight = GameManager.Instance?.currentPhase == GamePhase.Night;
        SetBatteryGroupVisible(isNight);
        HideDialogue();
    }

    void Update()
    {
        if (GameManager.Instance != null)
        {
            float t = GameManager.Instance.phaseTimer;
            if (phaseTimerText != null)
                phaseTimerText.text = $"{Mathf.FloorToInt(t / 60):D2}:{Mathf.FloorToInt(t % 60):D2}";
            if (phaseLabel != null)
                phaseLabel.text = GameManager.Instance.currentPhase == GamePhase.Night ? "НОЧЬ" : "ДЕНЬ";
        }

        if (_murderSystem != null && stealthKillUI != null)
        {
            float prog = _murderSystem.StealthKillProgress;
            stealthKillUI.SetActive(prog > 0.01f);
            if (stealthKillSlider != null) stealthKillSlider.value = prog;
        }

        // Advance dialogue on left click or Space (only when no choices are shown)
        if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsActive)
        {
            bool clicked = Mouse.current?.leftButton.wasPressedThisFrame    ?? false;
            bool spaced  = Keyboard.current?.spaceKey.wasPressedThisFrame   ?? false;
            if (_choiceButtons.Count == 0 && (clicked || spaced))
            {
                DialogueSystem.Instance.AdvanceLine();
            }
        }
    }

    void UpdateMoney(int amount)
    {
        if (moneyText != null) moneyText.text = $"${amount}";
    }

    void UpdateResource(ResourceType type, int amount)
    {
        switch (type)
        {
            case ResourceType.FreshIngredient:
                if (freshCountText   != null) freshCountText.text   = amount.ToString(); break;
            case ResourceType.SpoiledIngredient:
                if (spoiledCountText != null) spoiledCountText.text = amount.ToString(); break;
            case ResourceType.JunkLoot:
                if (lootCountText    != null) lootCountText.text    = amount.ToString(); break;
        }
    }

    void UpdateSuspicion(float value)
    {
        if (suspicionSlider != null) suspicionSlider.value = value / 100f;
        if (suspicionFill   != null)
            suspicionFill.color = Color.Lerp(lowSuspicionColor, highSuspicionColor, value / 100f);
    }

    void UpdateBattery(float percent)
    {
        if (batterySlider != null) batterySlider.value = percent;
        if (batteryFill   != null)
            batteryFill.color = Color.Lerp(batteryLowColor, batteryFullColor, percent);
        if (batteryText   != null)
            batteryText.text = $"{Mathf.RoundToInt(percent * 100f)}%";
    }

    void SetBatteryGroupVisible(bool visible)
    {
        if (flashlightBatteryGroup != null) flashlightBatteryGroup.SetActive(visible);
    }

    void UpdateReputation(float value)
    {
        float t = value / 100f;
        if (reputationSlider != null) reputationSlider.value = t;
        if (reputationFill   != null)
            reputationFill.color = Color.Lerp(badReputationColor, goodReputationColor, t);
    }

    void ShowGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    void ShowDialogueLine(string speakerName, string text, Sprite portrait)
    {
        if (dialoguePanel    != null) dialoguePanel.SetActive(true);
        if (speakerNameText  != null) speakerNameText.text  = speakerName;
        if (dialogueBodyText != null) dialogueBodyText.text = text;

        if (portraitImage != null)
        {
            if (portrait != null)
            {
                portraitImage.sprite = portrait;
                portraitImage.gameObject.SetActive(true);
            }
            else
            {
                portraitImage.gameObject.SetActive(false);
            }
        }

        ClearChoiceButtons();
    }

    void ShowChoices(List<DialogueChoice> choices)
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        ClearChoiceButtons();
        if (choiceButtonPrefab == null || choiceButtonContainer == null) return;
        for (int i = 0; i < choices.Count; i++)
        {
            int idx = i;
            Button btn = Instantiate(choiceButtonPrefab, choiceButtonContainer);
            btn.gameObject.SetActive(true);

            RectTransform rt = btn.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 0.5f);
            rt.anchorMax        = new Vector2(1f, 0.5f);
            rt.offsetMin        = new Vector2(0f, rt.offsetMin.y);
            rt.offsetMax        = new Vector2(0f, rt.offsetMax.y);
            rt.sizeDelta        = new Vector2(0f, rt.sizeDelta.y > 0 ? rt.sizeDelta.y : 40f);

            TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text         = choices[i].buttonLabel;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
            }

            btn.onClick.AddListener(() => DialogueSystem.Instance?.SelectChoice(idx));
            _choiceButtons.Add(btn);
        }
    }

    void HideDialogue()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        ClearChoiceButtons();
    }

    void ClearChoiceButtons()
    {
        foreach (var btn in _choiceButtons)
            if (btn != null) Destroy(btn.gameObject);
        _choiceButtons.Clear();
    }

    public void OnBribeButtonPressed() => FindFirstObjectByType<InspectorAI>()?.TryBribe();
}
