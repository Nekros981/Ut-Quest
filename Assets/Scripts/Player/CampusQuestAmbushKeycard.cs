using UnityEngine;

public sealed class CampusQuestAmbushKeycard : CampusQuestInteractable
{
    const int KeycardObjectiveStep = 6;

    static bool reclamada;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetForPlay() => reclamada = false;

    [SerializeField] CampusQuestSecuazVisualDriver secuaz;

    public override string PromptText => "Interactuar";

    void Awake()
    {
        if (secuaz == null)
            secuaz = GetComponentInChildren<CampusQuestSecuazVisualDriver>();
    }

    public override bool CanInteract(GameObject interactor)
    {
        if (!isActiveAndEnabled || reclamada || secuaz == null || !secuaz.IsDefeated)
            return false;

        var objectives = CampusQuestObjectives.Instance;
        return objectives != null && objectives.CurrentStep == KeycardObjectiveStep;
    }

    public override void Interact(GameObject interactor)
    {
        if (interactor == null || !CanInteract(interactor))
            return;

        reclamada = true;
        CampusQuestObjectives.Instance?.NotifyKeycard();
    }
}
