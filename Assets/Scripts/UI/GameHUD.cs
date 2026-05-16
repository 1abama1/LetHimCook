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
    public Image suspicionFill;
    public Color lowSuspicionColor  = Color.green;
    public Color highSuspicionColor = Color.red;

    [Header("Phase Timer")]
    public TextMeshProUGUI phaseTimerText;
    public TextMeshProUGUI phaseLabel;
    public Animator        phaseIconAnimator;

    [Header("Reputation")]
    public Image[]    starImages;
    public Sprite     fullStarSprite;
    public Sprite     emptyStarSprite;
    public GameObject gameOverPanel;

    [Header("Stealth Kill")]
    public Image      stealthKillFill;
    public GameObject stealthKillUI;

    [Header("Flashlight Battery")]
    public Image           batteryFill;
    public TextMeshProUGUI batteryText;
    public Sprite          batteryGreenSprite;
    public Sprite          batteryYellowSprite;
    public Sprite          batteryRedSprite;
    public GameObject      flashlightBatteryGroup;

    [Header("Dialogue")]
    public GameObject      dialoguePanel;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueBodyText;
    public Image           portraitImage;
    public Transform       choiceButtonContainer;
    public Button          choiceButtonPrefab;

    private MurderSystem         _murderSystem;
    private FlashlightController _flashlight;
    private List<Button>         _choiceButtons = new List<Button>();
    private int                  _advanceCooldownFrames;
    private int                  _lastSecond = -1;
    private GamePhase            _lastPhase = (GamePhase)(-1);
    private float                _lastSuspicion = -1f;

    private CopNPC[] _allCops;
    private InspectorAI _inspector;

    void Start()
    {
        _murderSystem = FindFirstObjectByType<MurderSystem>();

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

        _inspector = FindFirstObjectByType<InspectorAI>();
        _allCops = FindObjectsByType<CopNPC>(FindObjectsSortMode.None);

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

        if (GameManager.Instance != null)
        {
            GameManager.Instance.onNightBegin.AddListener(() =>
            {
                SetBatteryGroupVisible(true);
                if (phaseIconAnimator != null) phaseIconAnimator.SetTrigger("Night");
            });
            GameManager.Instance.onDayBegin.AddListener(() =>
            {
                SetBatteryGroupVisible(false);
                if (phaseIconAnimator != null) phaseIconAnimator.SetTrigger("Day");
            });
        }

        UpdateMoney(PlayerEconomy.Instance?.Money ?? 0);
        UpdateSuspicion();
        UpdateReputation(RestaurantReputation.Instance?.reputation ?? 50f);
        UpdateBattery(1f);

        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        bool isNight = GameManager.Instance?.currentPhase == GamePhase.Night;
        SetBatteryGroupVisible(isNight);

        ApplyGlobalFonts();
        HideDialogue();
    }

    void Update()
    {
        if (GameManager.Instance != null)
        {
            int currentSecond = Mathf.FloorToInt(GameManager.Instance.phaseTimer);
            if (currentSecond != _lastSecond)
            {
                _lastSecond = currentSecond;
                if (phaseTimerText != null)
                    phaseTimerText.text = $"{currentSecond / 60:D2}:{currentSecond % 60:D2}";
            }

            if (GameManager.Instance.currentPhase != _lastPhase)
            {
                _lastPhase = GameManager.Instance.currentPhase;
                if (phaseLabel != null)
                    phaseLabel.text = _lastPhase == GamePhase.Night ? "NIGHT" : "DAY";
            }
        }

        // Поиск максимального Suspicion среди копов и инспектора
        // Обновление Suspicion теперь происходит внутри UpdateSuspicion() без аргументов
        UpdateSuspicion();

        if (_murderSystem != null && stealthKillUI != null)
        {
            float prog = _murderSystem.StealthKillProgress;
            stealthKillUI.SetActive(prog > 0.01f);
            if (stealthKillFill != null) stealthKillFill.fillAmount = prog;
        }

        if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsActive)
        {
            if (_advanceCooldownFrames > 0)
            {
                _advanceCooldownFrames--;
            }
            else if (_choiceButtons.Count == 0)
            {
                bool clicked = Mouse.current?.leftButton.wasPressedThisFrame ?? false;
                bool spaced  = Keyboard.current?.spaceKey.wasPressedThisFrame ?? false;
                if (clicked || spaced)
                    DialogueSystem.Instance.AdvanceLine();
            }
            else
            {
                // Горячие клавиши 1, 2, 3... для выбора ответа, если UI кнопки не нажимаются
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.digit1Key.wasPressedThisFrame && _choiceButtons.Count > 0) DialogueSystem.Instance.SelectChoice(0);
                    if (Keyboard.current.digit2Key.wasPressedThisFrame && _choiceButtons.Count > 1) DialogueSystem.Instance.SelectChoice(1);
                    if (Keyboard.current.digit3Key.wasPressedThisFrame && _choiceButtons.Count > 2) DialogueSystem.Instance.SelectChoice(2);
                    if (Keyboard.current.digit4Key.wasPressedThisFrame && _choiceButtons.Count > 3) DialogueSystem.Instance.SelectChoice(3);
                }
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

    void UpdateSuspicion()
    {
        float max = 0f;

        var inspector = FindFirstObjectByType<InspectorAI>();
        if (inspector != null && inspector.suspicion > max)
            max = inspector.suspicion;

        if (Mathf.Abs(max - _lastSuspicion) > 0.5f)
        {
            _lastSuspicion = max;
            if (suspicionFill != null) suspicionFill.fillAmount = max / 100f;
        }
    }

    void UpdateBattery(float percent)
    {
        if (batteryFill != null)
        {
            batteryFill.fillAmount = percent;

            if (batteryGreenSprite != null && batteryYellowSprite != null && batteryRedSprite != null)
            {
                if (percent > 0.6f)
                    batteryFill.sprite = batteryGreenSprite;
                else if (percent > 0.25f)
                    batteryFill.sprite = batteryYellowSprite;
                else
                    batteryFill.sprite = batteryRedSprite;

                batteryFill.color = Color.white;
            }
        }

        if (batteryText != null)
            batteryText.text = $"{Mathf.RoundToInt(percent * 100f)}%";
    }

    void SetBatteryGroupVisible(bool visible)
    {
        if (flashlightBatteryGroup != null) flashlightBatteryGroup.SetActive(visible);
    }

    void UpdateReputation(float value)
    {
        if (starImages == null || starImages.Length == 0) return;
        int maxStars    = starImages.Length;
        int activeStars = Mathf.RoundToInt((value / 100f) * maxStars);

        for (int i = 0; i < maxStars; i++)
        {
            if (starImages[i] != null)
                starImages[i].sprite = (i < activeStars) ? fullStarSprite : emptyStarSprite;
        }
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
            portraitImage.sprite = portrait;
            portraitImage.gameObject.SetActive(portrait != null);
        }

        ClearChoiceButtons();
        _advanceCooldownFrames = 2;
    }

    void ShowChoices(List<DialogueChoice> choices)
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        ClearChoiceButtons();

        if (choiceButtonPrefab == null || choiceButtonContainer == null) return;
        if (choices == null || choices.Count == 0) return;

        _advanceCooldownFrames = 3;

        for (int i = 0; i < choices.Count; i++)
        {
            int idx = i;
            Button btn = Instantiate(choiceButtonPrefab, choiceButtonContainer);
            btn.gameObject.SetActive(true);

            TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                StoryManager.ApplyFont(label);
                label.text             = choices[i].buttonLabel;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.overflowMode     = TextOverflowModes.Overflow;
                label.color            = new Color(0.1f, 0.1f, 0.1f, 1f);
                label.fontSize         = 18f;
            }

            btn.onClick.AddListener(() => DialogueSystem.Instance?.SelectChoice(idx));
            _choiceButtons.Add(btn);
        }

        if (_choiceButtons.Count > 0 && UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(_choiceButtons[0].gameObject);
        }
    }

    void HideDialogue()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        ClearChoiceButtons();
        _advanceCooldownFrames = 0;
    }

    void ClearChoiceButtons()
    {
        foreach (var btn in _choiceButtons)
        {
            if (btn != null) 
            {
                btn.transform.SetParent(null);
                Destroy(btn.gameObject);
            }
        }
        _choiceButtons.Clear();

        if (choiceButtonContainer != null)
        {
            for (int i = choiceButtonContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = choiceButtonContainer.GetChild(i);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
        }
    }

    public void OnBribeButtonPressed() => FindFirstObjectByType<InspectorAI>()?.TryBribe();

    void ApplyGlobalFonts()
    {
        StoryManager.ApplyFont(moneyText);
        StoryManager.ApplyFont(freshCountText);
        StoryManager.ApplyFont(spoiledCountText);
        StoryManager.ApplyFont(lootCountText);
        StoryManager.ApplyFont(phaseTimerText);
        StoryManager.ApplyFont(phaseLabel);
        StoryManager.ApplyFont(batteryText);
        StoryManager.ApplyFont(speakerNameText);
        StoryManager.ApplyFont(dialogueBodyText);
    }
}
