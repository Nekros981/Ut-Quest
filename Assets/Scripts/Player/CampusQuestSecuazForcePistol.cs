using UnityEngine;

public sealed class CampusQuestSecuazForcePistol : MonoBehaviour
{
    [SerializeField] CampusQuestSecuazVisualDriver visual;

    void Awake()
    {
        if (visual == null)
            visual = GetComponentInChildren<CampusQuestSecuazVisualDriver>(true);
        visual?.SetArma(CampusQuestSecuazVisualDriver.ArmaPistola);
    }
}
