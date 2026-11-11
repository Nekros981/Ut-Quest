using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class CampusQuestBunkerApproach : MonoBehaviour
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

        triggered = true;
        CampusQuestObjectives.Instance?.AdvanceTo(4);
        enabled = false;
    }
}
