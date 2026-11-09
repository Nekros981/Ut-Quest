using UnityEngine;

public sealed class CampusQuestSecuazLoot : CampusQuestInteractable
{
    [SerializeField] CampusQuestSecuazVisualDriver secuaz;

    bool pistolTaken;

    public override string PromptText => "Inspeccionar";

    public override bool CanInteract(GameObject interactor)
    {
        return isActiveAndEnabled && !pistolTaken && secuaz != null && secuaz.IsDefeated;
    }

    void Awake()
    {
        if (secuaz == null)
            secuaz = GetComponentInChildren<CampusQuestSecuazVisualDriver>();
    }

    public override void Interact(GameObject interactor)
    {
        if (interactor == null || pistolTaken || secuaz == null)
            return;

        if (!secuaz.IsDefeated)
            return;

        var loadout = interactor.GetComponent<CampusQuestLoadout>();
        if (loadout == null || !loadout.GivePistol())
            return;

        pistolTaken = true;
    }
}
