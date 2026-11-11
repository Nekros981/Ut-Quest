using CampusQuest.Dialogue;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class SpBunkerBossEncounter : MonoBehaviour
{
    [SerializeField] CampusQuestBunkerDoor door;
    [SerializeField] DialogueSession session;
    [SerializeField] DialogueDefinition introDialogue;
    [SerializeField] DialogueDefinition thanksDialogue;
    [SerializeField] CampusQuestBunkerSealOverlay sealOverlay;
    [SerializeField] Behaviour[] combatBrains = System.Array.Empty<Behaviour>();

    public const string ThanksDialogueId = "bunker.chaka.gracias";
    [SerializeField] CampusQuestMateoPlayerInput mateoInput;
    [SerializeField] GameObject secuazPrefab;
    [SerializeField] Sprite bulletSprite;
    [SerializeField] Vector3[] phaseTwoPositions =
    {
        new(4f, 18f, 0f),
        new(12f, 18f, 0f)
    };

    bool triggered;
    bool phaseTwoSpawned;

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
        door?.CloseForBossEncounter();

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

        if (thanksDialogue != null && dialogueId == thanksDialogue.dialogueId)
            sealOverlay?.Show();
        else if (dialogueId == ThanksDialogueId)
            sealOverlay?.Show();
    }

    public void SpawnPhaseTwoShooters()
    {
        if (phaseTwoSpawned || secuazPrefab == null || phaseTwoPositions == null)
            return;

        phaseTwoSpawned = true;
        for (int i = 0; i < phaseTwoPositions.Length; i++)
        {
            var instance = Instantiate(secuazPrefab, phaseTwoPositions[i], Quaternion.identity);
            instance.name = "Secuaz_Bunker_Shooter_" + (i + 1);

            var brain = instance.GetComponent<CampusQuestSecuazBrain>();
            if (brain == null)
                brain = instance.GetComponentInChildren<CampusQuestSecuazBrain>(true);
            if (brain == null)
                continue;

            brain.ConfigureBossMinion(pistol: true);
            var force = instance.GetComponent<CampusQuestSecuazForcePistol>();
            if (force != null)
                Destroy(force);

            brain.enabled = true;
        }
    }

    void CollectCombatBrains()
    {
        var list = new System.Collections.Generic.List<Behaviour>();
        foreach (var brain in FindObjectsByType<CampusQuestSecuazBrain>(FindObjectsSortMode.None))
        {
            if (brain != null)
                list.Add(brain);
        }

        var boss = FindAnyObjectByType<CampusQuestBossBrain>();
        if (boss != null)
            list.Add(boss);

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
