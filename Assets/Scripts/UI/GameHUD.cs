using UnityEngine;
using UnityEngine.UI;
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

    [Header("Stealth Kill")]
    public Slider     stealthKillSlider;
    public GameObject stealthKillUI;

    [Header("Dialogue")]
    public GameObject          dialoguePanel;
    public TextMeshProUGUI     speakerNameText;
    public TextMeshProUGUI     dialogueBodyText;
    public Transform           choiceButtonContainer;
    public Button              choiceButtonPrefab;

    private MurderSystem   _murderSystem;
    private List<Button>   _choiceButtons = new List<Button>();

    void Start()
    {
        _murderSystem = FindFirstObjectByType<MurderSystem>();

        if (PlayerEconomy.Instance != null)
        {
            PlayerEconomy.Instance.onMoneyChanged.AddListener(UpdateMoney);
            PlayerEconomy.Instance.onResourceChanged.AddListener(UpdateResource);
        }

        InspectorAI inspector = FindFirstObjectByType<InspectorAI>();
        if (inspector != null)
            inspector.onSuspicionChanged.AddListener(UpdateSuspicion);

        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.onLineStart.AddListener(ShowDialogueLine);
            DialogueSystem.Instance.onShowChoices.AddListener(ShowChoices);
            DialogueSystem.Instance.onDialogueEnd.AddListener(HideDialogue);
        }

        UpdateMoney(PlayerEconomy.Instance?.Money ?? 0);
        UpdateSuspicion(0f);
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

    void ShowDialogueLine(string speakerName, string text)
    {
        if (dialoguePanel    != null) dialoguePanel.SetActive(true);
        if (speakerNameText  != null) speakerNameText.text  = speakerName;
        if (dialogueBodyText != null) dialogueBodyText.text = text;
        ClearChoiceButtons();
    }

    void ShowChoices(List<DialogueChoice> choices)
    {
        ClearChoiceButtons();
        if (choiceButtonPrefab == null || choiceButtonContainer == null) return;
        for (int i = 0; i < choices.Count; i++)
        {
            int idx = i;
            Button btn = Instantiate(choiceButtonPrefab, choiceButtonContainer);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = choices[i].buttonLabel;
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
