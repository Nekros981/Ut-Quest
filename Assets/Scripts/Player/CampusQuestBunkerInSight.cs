using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class CampusQuestBunkerInSight : MonoBehaviour
{
    bool triggered;

    void Awake()
    {
        var box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered)
            return;

        if (other.GetComponentInParent<CampusQuestMateoPlayerInput>() == null)
            return;

        var objectives = CampusQuestObjectives.Instance;
        if (objectives == null || objectives.CurrentStep != 4)
            return;

        triggered = true;
        objectives.AdvanceTo(5);
        enabled = false;
    }
}
