using UnityEngine;

public sealed class CampusQuestWeaponPickup : CampusQuestInteractable
{
    public override bool CanInteract(GameObject interactor) => isActiveAndEnabled;

    public override void Interact(GameObject interactor)
    {
        if (interactor == null)
            return;

        var loadout = interactor.GetComponent<CampusQuestLoadout>();
        if (loadout == null || !loadout.GiveClub())
            return;

        CampusQuestObjectives.Instance?.AdvanceTo(1);
        gameObject.SetActive(false);
    }
}
