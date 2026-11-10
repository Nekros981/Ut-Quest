using UnityEngine;
using TMPro;

public sealed class CampusQuestObjectives : MonoBehaviour
{
    public static CampusQuestObjectives Instance { get; private set; }

    static readonly string[] StepText =
    {
        "Recoge el tolete.",
        "Derrota a un secuaz.",
        "Interactúa con el secuaz derrotado para conseguir el arma.",
        "Atraviesa el bosque.",
        "Encuentra el bunker.",
        "Interactúa con el bunker.",
        "Consigue la keycard interactuando con un secuaz derrotado de la emboscada.",
        "Abre el bunker.",
        "Adéntrate en el bunker."
    };

    [SerializeField] TextMeshProUGUI label;
    int step;

    void Awake()
    {
        Instance = this;
        Refresh();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public int CurrentStep => step;

    public void AdvanceTo(int targetStep)
    {
        if (targetStep <= step)
            return;
        step = Mathf.Clamp(targetStep, 0, StepText.Length - 1);
        Refresh();
    }

    public void TryAdvanceAfterFirstDefeat()
    {
        if (step == 1)
            AdvanceTo(2);
    }

    public void NotifyKeycard() => AdvanceTo(7);

    public void NotifyBunkerOpened() => AdvanceTo(8);

    public void NotifyEnteredBunker()
    {
        if (step < StepText.Length - 1)
            AdvanceTo(StepText.Length - 1);
        Refresh();
    }

    void Refresh()
    {
        if (label == null)
            return;
        if (step < 0 || step >= StepText.Length)
            label.text = string.Empty;
        else
            label.text = StepText[step];
    }
}
