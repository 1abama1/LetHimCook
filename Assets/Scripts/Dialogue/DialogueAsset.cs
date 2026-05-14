using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class DialogueLine
{
    public string speakerName;
    [TextArea(2, 5)]
    public string text;
    public AudioClip voiceClip;
}

[System.Serializable]
public class DialogueChoice
{
    public string buttonLabel;
    public string nextDialogueId;
    public string callbackMethod;
}

[System.Serializable]
public class DialogueNode
{
    public string id;
    public List<DialogueLine> lines   = new List<DialogueLine>();
    public List<DialogueChoice> choices = new List<DialogueChoice>();
    public bool autoAdvance = false;
}

[CreateAssetMenu(menuName = "Dialogue/Dialogue Asset")]
public class DialogueAsset : ScriptableObject
{
    public List<DialogueNode> nodes = new List<DialogueNode>();

    public DialogueNode GetNode(string id) => nodes.Find(n => n.id == id);
}
