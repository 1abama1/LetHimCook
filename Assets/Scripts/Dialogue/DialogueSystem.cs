using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }

    public List<DialogueAsset> dialogueAssets = new List<DialogueAsset>();
    public float autoAdvanceDelay = 0.1f;

    public UnityEvent<string, string, Sprite> onLineStart;
    public UnityEvent<List<DialogueChoice>>   onShowChoices;
    public UnityEvent                       onDialogueEnd;

    private DialogueNode     _currentNode;
    private int              _currentLineIndex;
    private MonoBehaviour    _currentNPC;
    private PlayerController _currentPlayer;
    private bool             _isActive;

    public bool IsActive => _isActive;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void StartDialogue(string nodeId, MonoBehaviour npc, PlayerController player)
    {
        DialogueNode node = FindNode(nodeId);
        if (node == null) return;

        _isActive         = true;
        _currentNPC       = npc;
        _currentPlayer    = player;
        _currentLineIndex = 0;
        _currentNode      = node;

        if (_currentPlayer != null) _currentPlayer.SetDialogueLock(true);

        PlayNode(node);
    }

    void PlayNode(DialogueNode node)
    {
        _currentNode      = node;
        _currentLineIndex = 0;
        ShowCurrentLine();
    }

    void ShowCurrentLine()
    {
        if (_currentLineIndex >= _currentNode.lines.Count) { ProceedToChoices(); return; }

        DialogueLine line = _currentNode.lines[_currentLineIndex];
        onLineStart?.Invoke(line.speakerName, line.text, line.portrait);

        if (line.voiceClip != null)
            AudioSource.PlayClipAtPoint(line.voiceClip, Camera.main.transform.position);

        if (_currentNode.autoAdvance)
            StartCoroutine(AutoAdvance(line.voiceClip != null ? line.voiceClip.length : 2f));
    }

    IEnumerator AutoAdvance(float delay)
    {
        yield return new WaitForSeconds(delay + autoAdvanceDelay);
        AdvanceLine();
    }

    public void AdvanceLine()
    {
        _currentLineIndex++;
        if (_currentLineIndex < _currentNode.lines.Count)
            ShowCurrentLine();
        else
            ProceedToChoices();
    }

    void ProceedToChoices()
    {
        if (_currentNode.choices == null || _currentNode.choices.Count == 0)
            { EndDialogue(); return; }
        onShowChoices?.Invoke(_currentNode.choices);
    }

    public void SelectChoice(int index)
    {
        if (_currentNode.choices == null || index >= _currentNode.choices.Count) return;
        DialogueChoice choice = _currentNode.choices[index];

        if (!string.IsNullOrEmpty(choice.callbackMethod) && _currentNPC != null)
            _currentNPC.SendMessage(choice.callbackMethod, SendMessageOptions.DontRequireReceiver);

        if (!string.IsNullOrEmpty(choice.nextDialogueId))
        {
            DialogueNode next = FindNode(choice.nextDialogueId);
            if (next != null) { PlayNode(next); return; }
        }

        EndDialogue();
    }

    void EndDialogue()
    {
        _isActive = false;
        if (_currentPlayer != null) _currentPlayer.SetDialogueLock(false);
        _currentNPC  = null;
        _currentNode = null;
        onDialogueEnd?.Invoke();
    }

    DialogueNode FindNode(string id)
    {
        foreach (var asset in dialogueAssets)
        {
            DialogueNode node = asset.GetNode(id);
            if (node != null) return node;
        }

        // --- Диагностика: сообщает точно что не найдено ---
        if (dialogueAssets.Count == 0)
        {
            Debug.LogWarning("[DialogueSystem] Нет ни одного DialogueAsset в списке! " +
                             "Добавьте ассет в инспекторе объекта DialogueSystem.");
        }
        else
        {
            var allIds = new System.Text.StringBuilder();
            foreach (var asset in dialogueAssets)
                foreach (var node in asset.nodes)
                    allIds.Append(node.id).Append(", ");

            Debug.LogWarning($"[DialogueSystem] Узел с ID \"{id}\" не найден! " +
                             $"Доступные ID: [{allIds}]");
        }
        return null;
    }
}
