using UnityEngine;

public sealed class SpBunkerArrival : MonoBehaviour
{
    const int DefaultHearts = 6;

    void Start()
    {
        var mateo = FindAnyObjectByType<CampusQuestMateoPlayerInput>();
        if (mateo != null)
            ApplyLoadout(mateo);

        if (CampusQuestRunState.TryConsume(out var hearts, out var defeats))
        {
            var bar = FindAnyObjectByType<CampusQuestHeartBar>();
            if (bar != null)
                bar.SetHearts(hearts);

            var counter = FindAnyObjectByType<CampusQuestDefeatCounter>();
            counter?.SetCount(defeats);
        }
        else
            FindAnyObjectByType<CampusQuestHeartBar>()?.SetHearts(DefaultHearts);

        CampusQuestObjectives.Instance?.NotifyEnteredBunker();
    }

    static void ApplyLoadout(CampusQuestMateoPlayerInput mateo)
    {
        var loadout = mateo.GetComponent<CampusQuestLoadout>();
        if (loadout == null)
            return;

        loadout.GiveClub();
        loadout.GivePistol();
    }
}
