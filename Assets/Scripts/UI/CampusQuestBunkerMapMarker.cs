using UnityEngine;

[RequireComponent(typeof(CampusQuestMapMarker))]
public sealed class CampusQuestBunkerMapMarker : MonoBehaviour
{
    const int FindBunkerStep = 4;
    const int InteractBunkerStep = 5;

    CampusQuestMapMarker marker;

    void Awake()
    {
        marker = GetComponent<CampusQuestMapMarker>();
        if (marker != null)
            marker.enabled = false;
    }

    void Update()
    {
        if (marker == null)
            return;

        var objectives = CampusQuestObjectives.Instance;
        bool show = objectives != null
                    && objectives.CurrentStep >= FindBunkerStep
                    && objectives.CurrentStep <= InteractBunkerStep;

        if (marker.enabled != show)
            marker.enabled = show;
    }
}
