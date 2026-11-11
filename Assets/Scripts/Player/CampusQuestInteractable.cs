using UnityEngine;

public abstract class CampusQuestInteractable : MonoBehaviour
{
    public virtual string PromptText => "Interactuar";

    public virtual bool CanInteract(GameObject interactor) => isActiveAndEnabled;

    public abstract void Interact(GameObject interactor);
}
