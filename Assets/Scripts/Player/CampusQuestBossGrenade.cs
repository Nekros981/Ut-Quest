using UnityEngine;

public sealed class CampusQuestBossGrenade : MonoBehaviour
{
    const float Speed = 7f;
    const float ArrivalThreshold = 0.08f;

    Vector3 target;
    Sprite[] explosionFrames;
    AudioClip explosionClip;

    public static void Launch(Vector3 origin, Vector3 targetPosition, Sprite[] frames, AudioClip clip)
    {
        var go = new GameObject("Boss_Grenade");
        go.transform.position = origin;
        var grenade = go.AddComponent<CampusQuestBossGrenade>();
        grenade.target = targetPosition;
        grenade.explosionFrames = frames;
        grenade.explosionClip = clip;
    }

    void Update()
    {
            if (CampusQuestPauseMenu.BlocksInput) return;
        var position = transform.position;
        var next = Vector3.MoveTowards(position, target, Speed * Time.deltaTime);
        transform.position = next;

        if (Vector3.Distance(next, target) > ArrivalThreshold)
            return;

        CampusQuestBossExplosion.Spawn(target, explosionFrames, explosionClip);
        Destroy(gameObject);
    }
}
