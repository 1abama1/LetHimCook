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
    public Image  suspicionFill;
    public Color  lowSuspicionColor  = Color.green;
    public Color  highSuspicionColor = Color.red;

    [Header("Phase Timer")]
    public TextMeshProUGUI phaseTimerText;
    public TextMeshProUGUI phaseLabel;
    public Animator        phaseIconAnimator;

    [Header("Reputation")]
    public Image[]            starImages;
    public Sprite             fullStarSprite;
    public Sprite             emptyStarSprite;
    public GameObject         gameOverPanel;

    [Header("Stealth Kill")]
    public Image      stealthKillFill;
    public GameObject stealthKillUI;

    [Header("Flashlight Battery")]
    public Image              batteryFill;
    public TextMeshProUGUI    batteryText;
    public Sprite             batteryGreenSprite;
    public Sprite             batteryYellowSprite;
    public Sprite             batteryRedSprite;
    public GameObject         flashlightBatteryGroup;

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
    private int                   _lastSecond = -1;
    private GamePhase             _lastPhase = (GamePhase)(-1);

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

        InspectorAI inspector = FindFirstObjectByType<InspectorAI>();
        if (inspector != null)
            inspector.onSuspicionChanged.AddListener(UpdateSuspicion);

        CopNPC[] cops = FindObjectsByType<CopNPC>(FindObjectsSortMode.None);
        foreach (var cop in cops)
            cop.onSuspicionChanged.AddListener(UpdateSuspicion);

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
        UpdateSuspicion(0f);
        UpdateReputation(RestaurantReputation.Instance?.reputation ?? 50f);
        UpdateBattery(1f);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        ApplyGlobalFonts();

        bool isNight = GameManager.Instance?.currentPhase == GamePhase.Night;
        SetBatteryGroupVisible(isNight);
        HideDialogue();
    }

    void Update()
    {
        if (GameManager.Instance != null)
        {
            float t = GameManager.Instance.phaseTimer;
            int currentSecond = Mathf.FloorToInt(t);
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
                    phaseLabel.text = _lastPhase == GamePhase.Night ? "НОЧЬ" : "ДЕНЬ";
            }
        }

        if (_murderSystem != null && stealthKillUI != null)
        {
            float prog = _murderSystem.StealthKillProgress;
            stealthKillUI.SetActive(prog > 0.01f);
            if (stealthKillFill != null) stealthKillFill.fillAmount = prog;
        }

        if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsActive)
        {
            bool clicked = Mouse.current?.leftButton.wasPressedThisFrame ?? false;
            bool spaced  = Keyboard.current?.spaceKey.wasPressedThisFrame ?? false;
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
        if (suspicionFill != null)
        {
            suspicionFill.fillAmount = value / 100f;
            suspicionFill.color = Color.Lerp(lowSuspicionColor, highSuspicionColor, value / 100f);
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
        int maxStars = starImages.Length;
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

        if (choices == null || choices.Count == 0)
        {
            Button btn = Instantiate(choiceButtonPrefab, choiceButtonContainer);
            btn.gameObject.SetActive(true);
            TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) 
            {
                StoryManager.ApplyFont(label);
                label.text = "Далее...";
                label.color = Color.white;
            }
            
            btn.onClick.AddListener(() => DialogueSystem.Instance?.SelectChoice(0));
            _choiceButtons.Add(btn);
            return;
        }

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
                StoryManager.ApplyFont(label);
                label.text = choices[i].buttonLabel;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
                label.color = Color.white;
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

    private void ApplyGlobalFonts()
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
