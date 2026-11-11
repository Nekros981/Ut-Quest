using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class CampusQuestBunkerStoryInteract : CampusQuestInteractable
{
    const int StoryStep = 5;
    const int OpenHatchStep = 7;
    const int EnterStep = 8;

    [SerializeField] CampusQuestHatchDoor hatch;

    bool entryUsed;

    public override string PromptText => "Interactuar";

    public override bool CanInteract(GameObject interactor)
    {
        if (!isActiveAndEnabled)
            return false;

        var objectives = CampusQuestObjectives.Instance;
        if (objectives == null)
            return false;

        return objectives.CurrentStep switch
        {
            StoryStep => true,
            OpenHatchStep => hatch != null && !hatch.IsOpening && !hatch.HasOpened,
            EnterStep => !entryUsed,
            _ => false
        };
    }

    public override void Interact(GameObject interactor)
    {
        if (interactor == null || !CanInteract(interactor))
            return;

        var objectives = CampusQuestObjectives.Instance;
        if (objectives == null)
            return;

        switch (objectives.CurrentStep)
        {
            case StoryStep:
                objectives.AdvanceTo(6);
                break;
            case OpenHatchStep:
                hatch?.Open();
                break;
            case EnterStep:
                entryUsed = true;
                CampusQuestRunState.CaptureFromScene();
                SceneManager.LoadScene("Sp_Bunker");
                break;
        }
    }
}
