using UnityEngine;

public sealed class CampusQuestAmbush : MonoBehaviour
{
    [SerializeField] GameObject[] ambushEnemies;

    BoxCollider2D trigger;
    bool triggered;

    void Awake()
    {
        trigger = GetComponent<BoxCollider2D>();
        if (trigger != null)
            trigger.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered)
            return;

        if (other.GetComponentInParent<CampusQuestMateoPlayerInput>() == null)
            return;

        triggered = true;
        if (trigger != null)
            trigger.enabled = false;

        if (ambushEnemies == null)
            return;

        foreach (var enemy in ambushEnemies)
        {
            if (enemy != null)
                enemy.SetActive(true);
        }
    }
}
