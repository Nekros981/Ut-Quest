using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CampusQuestHeartBar))]
public sealed class CampusQuestHeartRegen : MonoBehaviour
{
    const float RegenDuration = 2f;

    CampusQuestHeartBar bar;
    int lastActiveEnemyCount = -1;
    Coroutine regenRoutine;

    void Awake() => bar = GetComponent<CampusQuestHeartBar>();

    void Update()
    {
        if (bar == null || bar.CurrentHearts <= 0)
        {
            StopRegen();
            return;
        }

        int alive = CountActiveEnemies();

        if (lastActiveEnemyCount < 0)
        {
            lastActiveEnemyCount = alive;
            return;
        }

        if (lastActiveEnemyCount > 0 && alive == 0 && bar.CurrentHearts < bar.MaxHearts)
            StartRegenIfNeeded();
        else if (alive > 0)
            StopRegen();

        lastActiveEnemyCount = alive;
    }

    void StartRegenIfNeeded()
    {
        if (regenRoutine != null)
            return;

        regenRoutine = StartCoroutine(RegenHeartsRoutine());
    }

    void StopRegen()
    {
        if (regenRoutine == null)
            return;

        StopCoroutine(regenRoutine);
        regenRoutine = null;
    }

    IEnumerator RegenHeartsRoutine()
    {
        int missing = bar.MaxHearts - bar.CurrentHearts;
        if (missing <= 0)
        {
            regenRoutine = null;
            yield break;
        }

        float stepSeconds = RegenDuration / missing;
        var wait = new WaitForSeconds(stepSeconds);

        for (int i = 0; i < missing; i++)
        {
            yield return wait;

            if (bar.CurrentHearts <= 0 || CountActiveEnemies() > 0)
            {
                regenRoutine = null;
                yield break;
            }

            bar.SetHearts(bar.CurrentHearts + 1);
        }

        regenRoutine = null;
    }

    static int CountActiveEnemies()
    {
        int count = 0;
        foreach (var brain in FindObjectsByType<CampusQuestSecuazBrain>(FindObjectsSortMode.None))
        {
            if (brain == null || !brain.isActiveAndEnabled)
                continue;

            var visual = brain.GetComponentInChildren<CampusQuestSecuazVisualDriver>(true);
            if (visual != null && visual.IsDefeated)
                continue;

            count++;
        }

        return count;
    }
}
