using CampusQuest.Dialogue;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class SpBunkerBossEncounter : MonoBehaviour
{
    [SerializeField] CampusQuestBunkerDoor door;
    [SerializeField] DialogueSession session;
    [SerializeField] DialogueDefinition introDialogue;
    [SerializeField] Behaviour[] combatBrains = System.Array.Empty<Behaviour>();
    [SerializeField] CampusQuestMateoPlayerInput mateoInput;

    bool triggered;

    void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        if (door == null)
            door = GameObject.Find("Puerta_Bunker")?.GetComponent<CampusQuestBunkerDoor>();
        if (mateoInput == null)
            mateoInput = FindAnyObjectByType<CampusQuestMateoPlayerInput>();
        if (combatBrains == null || combatBrains.Length == 0)
            CollectCombatBrains();
        SetCombatActive(false);
    }

    void OnEnable()
    {
        if (session != null)
            session.Completed += OnDialogueCompleted;
    }

    void OnDisable()
    {
        if (session != null)
            session.Completed -= OnDialogueCompleted;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered || other == null)
            return;

        if (other.GetComponentInParent<CampusQuestMateoPlayerInput>() == null)
            return;

        triggered = true;
        door?.CloseReverse();

        if (session == null || introDialogue == null)
        {
            SetCombatActive(true);
            return;
        }

        if (!session.TryBegin(introDialogue))
            SetCombatActive(true);
    }

    void OnDialogueCompleted(string dialogueId, bool first)
    {
        if (introDialogue != null && dialogueId == introDialogue.dialogueId)
            SetCombatActive(true);
    }

    void CollectCombatBrains()
    {
        var list = new System.Collections.Generic.List<Behaviour>();
        foreach (var brain in FindObjectsByType<CampusQuestSecuazBrain>(FindObjectsSortMode.None))
        {
            if (brain != null)
                list.Add(brain);
        }

        combatBrains = list.ToArray();
    }

    void SetCombatActive(bool active)
    {
        if (combatBrains == null)
            return;

        foreach (var behaviour in combatBrains)
        {
            if (behaviour != null)
                behaviour.enabled = active;
        }
    }
}
