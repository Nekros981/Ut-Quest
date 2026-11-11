using UnityEngine;

public sealed class CampusQuestBunkerDoorInteract : CampusQuestInteractable
{
    [SerializeField] CampusQuestBunkerDoor door;

    void Awake()
    {
        if (door == null)
            door = GetComponentInParent<CampusQuestBunkerDoor>();
    }

    public override bool CanInteract(GameObject interactor)
    {
        if (!isActiveAndEnabled || door == null)
            return false;

        return !door.HasOpened && !door.IsOpening;
    }

    public override void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor))
            return;

        door.Open();
    }
}
