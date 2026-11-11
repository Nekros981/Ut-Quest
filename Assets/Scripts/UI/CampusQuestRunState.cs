using UnityEngine;

public static class CampusQuestRunState
{
    public static bool HasPendingTransfer { get; private set; }

    public static int PendingHearts { get; private set; } = 6;
    public static int PendingDefeats { get; private set; }

    public static void CaptureFromScene()
    {
        var bar = Object.FindAnyObjectByType<CampusQuestHeartBar>();
        var counter = CampusQuestDefeatCounter.Instance;

        PendingHearts = bar != null ? bar.CurrentHearts : 6;
        PendingDefeats = counter != null ? counter.Count : 0;
        HasPendingTransfer = true;
    }

    public static bool TryConsume(out int hearts, out int defeats)
    {
        if (!HasPendingTransfer)
        {
            hearts = 0;
            defeats = 0;
            return false;
        }

        hearts = PendingHearts;
        defeats = PendingDefeats;
        Clear();
        return true;
    }

    public static void Clear()
    {
        HasPendingTransfer = false;
        PendingHearts = 6;
        PendingDefeats = 0;
    }
}
