using System;
using UnityEngine;

namespace CampusQuest.Dialogue
{
    [Serializable]
    public sealed class DialogueLine
    {
        public string speaker;
        public Sprite portrait;
        [TextArea(2, 8)] public string text;
    }

    [CreateAssetMenu(menuName = "Campus Quest/Dialogue", fileName = "Dialogue")]
    public sealed class DialogueDefinition : ScriptableObject
    {
        public string dialogueId;
        public string defaultSpeaker;
        public Sprite defaultPortrait;
        public DialogueLine[] lines = Array.Empty<DialogueLine>();

        public bool IsValid
        {
            get
            {
                if (string.IsNullOrWhiteSpace(dialogueId) || lines == null || lines.Length == 0) return false;
                foreach (var line in lines)
                    if (line == null || string.IsNullOrWhiteSpace(line.text)) return false;
                return true;
            }
        }
    }
}
