using UnityEngine;

public sealed class CampusQuestSecuazLoot : CampusQuestInteractable
{
    static CampusQuestSecuazLoot dueño;
    static bool usado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetForPlay()
    {
        dueño = null;
        usado = false;
    }

    public static void ResetForRestart()
    {
        dueño = null;
        usado = false;
    }

    [SerializeField] CampusQuestSecuazVisualDriver secuaz;

    public override string PromptText => "Interactuar";

    public static bool TryClaimFirstDefeat(CampusQuestSecuazLoot loot)
    {
        if (loot == null || usado || dueño != null)
            return false;

        dueño = loot;
        return true;
    }

    public override bool CanInteract(GameObject interactor)
    {
        return isActiveAndEnabled && !usado && dueño == this && secuaz != null && secuaz.IsDefeated;
    }

    void Awake()
    {
        if (secuaz == null)
            secuaz = GetComponentInChildren<CampusQuestSecuazVisualDriver>();
    }

    public override void Interact(GameObject interactor)
    {
        if (interactor == null || !CanInteract(interactor))
            return;

        var loadout = interactor.GetComponent<CampusQuestLoadout>();
        if (loadout == null || !loadout.GivePistol())
            return;

        CampusQuestObjectives.Instance?.AdvanceTo(3);
        usado = true;
    }
}
