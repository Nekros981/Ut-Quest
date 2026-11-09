using UnityEngine;

public sealed class CampusQuestLoadout : MonoBehaviour
{
    public bool HasClub { get; private set; }
    public bool HasPistol { get; private set; }

    public bool GiveClub()
    {
        if (HasClub)
            return false;
        HasClub = true;
        return true;
    }

    public bool GivePistol()
    {
        if (HasPistol)
            return false;
        HasPistol = true;
        return true;
    }
}
