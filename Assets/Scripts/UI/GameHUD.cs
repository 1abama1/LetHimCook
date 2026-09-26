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
    private readonly List<Button> _choiceButtons = new List<Button>();
    private readonly List<Button> _choiceButtonPool = new List<Button>();
    private readonly WaitForSeconds _typingWait = new WaitForSeconds(0.035f);
    private int                  _advanceCooldownFrames;
    private int                  _lastSecond = -1;
    private GamePhase            _lastPhase = (GamePhase)(-1);
    private float                _lastSuspicion = -1f;

    private CopNPC[] _allCops;
    private InspectorAI _inspector;

    private Coroutine _typewriterRoutine;
    private bool _isTyping;
    private string _currentFullText;

    // Кеш GameManager для безопасного OnDisable/OnDestroy.
    private GameManager _boundGameManager;

    // Кешированные хэши Animator-триггеров фазы.
    private static readonly int NightHash = Animator.StringToHash("Night");
    private static readonly int DayHash   = Animator.StringToHash("Day");


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

        // Именованные методы — легко снять через RemoveListener.
        _boundGameManager = GameManager.Instance;
        if (_boundGameManager != null)
        {
            _boundGameManager.onNightBegin.AddListener(OnNightBegin);
            _boundGameManager.onDayBegin.AddListener(OnDayBegin);
        }

        // Initial pull — подписчик читает состояние сам, не зависит от SEO.
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

    void OnDestroy()
    {
        if (_boundGameManager != null)
        {
            _boundGameManager.onNightBegin.RemoveListener(OnNightBegin);
            _boundGameManager.onDayBegin.RemoveListener(OnDayBegin);
        }

        if (PlayerEconomy.Instance != null)
        {
            PlayerEconomy.Instance.onMoneyChanged.RemoveListener(UpdateMoney);
            PlayerEconomy.Instance.onResourceChanged.RemoveListener(UpdateResource);
        }

        if (_flashlight != null)
            _flashlight.onBatteryChanged.RemoveListener(UpdateBattery);

        if (RestaurantReputation.Instance != null)
        {
            RestaurantReputation.Instance.onReputationChanged.RemoveListener(UpdateReputation);
            RestaurantReputation.Instance.onGameOver.RemoveListener(ShowGameOver);
        }

        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.onLineStart.RemoveListener(ShowDialogueLine);
            DialogueSystem.Instance.onShowChoices.RemoveListener(ShowChoices);
            DialogueSystem.Instance.onDialogueEnd.RemoveListener(HideDialogue);
        }
    }

    // Именованные обработчики фазы с хешированными Animator-триггерами.
    private void OnNightBegin()
    {
        SetBatteryGroupVisible(true);
        if (phaseIconAnimator != null) phaseIconAnimator.SetTrigger(NightHash);
    }

    private void OnDayBegin()
    {
        SetBatteryGroupVisible(false);
        if (phaseIconAnimator != null) phaseIconAnimator.SetTrigger(DayHash);
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
                {
                    if (_isTyping)
                    {
                        if (_typewriterRoutine != null) StopCoroutine(_typewriterRoutine);
                        _isTyping = false;
                        if (dialogueBodyText != null) dialogueBodyText.maxVisibleCharacters = _currentFullText.Length;
                    }
                    else
                    {
                        DialogueSystem.Instance.AdvanceLine();
                    }
                }
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
        // Используем кешированную ссылку из Start() — никаких FindFirstObjectByType в Update.
        float max = (_inspector != null) ? _inspector.suspicion : 0f;

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

    void ShowDialogueLine(DialogueLine line)
    {
        if (dialoguePanel    != null) dialoguePanel.SetActive(true);
        if (speakerNameText  != null) speakerNameText.text  = line.speakerName;

        if (portraitImage != null)
        {
            portraitImage.sprite = line.portrait;
            portraitImage.gameObject.SetActive(line.portrait != null);
        }

        ClearChoiceButtons();
        _advanceCooldownFrames = 2;

        if (_typewriterRoutine != null) StopCoroutine(_typewriterRoutine);
        _typewriterRoutine = StartCoroutine(TypewriterEffect(line));
    }

    System.Collections.IEnumerator TypewriterEffect(DialogueLine line)
    {
        _isTyping = true;
        _currentFullText = line.text ?? string.Empty;
        if (dialogueBodyText != null)
        {
            dialogueBodyText.text = _currentFullText;
            dialogueBodyText.maxVisibleCharacters = 0;
        }

        const int charsPerBlip = 2;

        for (int i = 0; i <= _currentFullText.Length; i++)
        {
            if (dialogueBodyText != null)
                dialogueBodyText.maxVisibleCharacters = i;

            if (line.voiceClip != null && i < _currentFullText.Length && i % charsPerBlip == 0)
            {
                char c = _currentFullText[i];
                if (c != ' ' && c != '\n')
                {
                    AudioManager.Instance?.PlayClip2D(
                        line.voiceClip, 0.6f, Random.Range(0.85f, 1.15f)
                    );
                }
            }

            yield return _typingWait;
        }
        _isTyping = false;
        _typewriterRoutine = null;
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
            Button btn;
            if (i < _choiceButtonPool.Count && _choiceButtonPool[i] != null)
            {
                btn = _choiceButtonPool[i];
            }
            else
            {
                btn = Instantiate(choiceButtonPrefab, choiceButtonContainer);
                if (i < _choiceButtonPool.Count)
                    _choiceButtonPool[i] = btn;
                else
                    _choiceButtonPool.Add(btn);
            }

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

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => DialogueSystem.Instance?.SelectChoice(idx));
            _choiceButtons.Add(btn);
        }

        // Deactivate excess pooled buttons
        for (int i = choices.Count; i < _choiceButtonPool.Count; i++)
        {
            if (_choiceButtonPool[i] != null)
                _choiceButtonPool[i].gameObject.SetActive(false);
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
        for (int i = 0; i < _choiceButtonPool.Count; i++)
        {
            if (_choiceButtonPool[i] != null)
                _choiceButtonPool[i].gameObject.SetActive(false);
        }
        _choiceButtons.Clear();
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
