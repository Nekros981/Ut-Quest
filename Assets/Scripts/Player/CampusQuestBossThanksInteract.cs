using CampusQuest.Dialogue;
using UnityEngine;

public sealed class CampusQuestBossThanksInteract : CampusQuestInteractable
{
    [SerializeField] CampusQuestBossBrain boss;
    [SerializeField] DialogueSession session;
    [SerializeField] DialogueDefinition thanksDialogue;

    bool spoken;

    public override string PromptText => "Hablar";

    void Awake()
    {
        if (boss == null)
            boss = GetComponentInParent<CampusQuestBossBrain>();
    }

    public override bool CanInteract(GameObject interactor)
    {
        if (!isActiveAndEnabled || spoken)
            return false;

        return boss != null && boss.IsDefeated;
    }

    public override void Interact(GameObject interactor)
    {
        if (interactor == null || !CanInteract(interactor))
            return;

        if (session == null || thanksDialogue == null)
            return;

        if (!session.TryBegin(thanksDialogue))
            return;

        spoken = true;
    }
}
