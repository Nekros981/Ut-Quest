using CampusQuest.Dialogue;
using TMPro;
using UnityEngine;

public sealed class CampusQuestGameplayConfig : ScriptableObject
{
    public TMP_FontAsset font;
    public Sprite guidanceArrow;
    public Sprite[] sealFrames;
    public float[] sealSeconds;
    public DialogueDefinition archiveSuccess;
}
