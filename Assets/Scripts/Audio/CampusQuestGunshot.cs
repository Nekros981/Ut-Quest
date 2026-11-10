using UnityEngine;
using UnityEngine.Audio;

public sealed class CampusQuestGunshot : MonoBehaviour
{
    public static CampusQuestGunshot Instance { get; private set; }

    const string CombatVolumeParam = "CombatVolume";
    const float CombatGroupDb = 10f;
    const float CombatSpatialBlend = 0f;
    const float CombatSourceVolume = 1f;

    [SerializeField] AudioClip pistolClip;
    [SerializeField] AudioClip meleeClip;
    [SerializeField] AudioClip damageClip;
    [SerializeField] AudioMixer mixer;

    AudioMixerGroup combatGroup;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveCombatGroup();
        ApplyCombatGroupGain();
        PreloadClip(pistolClip);
        PreloadClip(meleeClip);
        PreloadClip(damageClip);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    static void PreloadClip(AudioClip clip)
    {
        if (clip != null)
            clip.LoadAudioData();
    }

    void ResolveCombatGroup()
    {
        if (mixer == null)
            return;

        foreach (var group in mixer.FindMatchingGroups("Combat"))
        {
            if (group != null && group.name == "Combat")
            {
                combatGroup = group;
                break;
            }
        }
    }

    void ApplyCombatGroupGain()
    {
        if (mixer == null)
            return;

        mixer.SetFloat(CombatVolumeParam, CombatGroupDb);
    }

    public static void Play(Vector3 worldPosition) => Instance?.PlayPistol(worldPosition);

    public static void PlayMelee(Vector3 worldPosition) => Instance?.PlayMeleeAt(worldPosition);

    public static void PlayDamage(Vector3 worldPosition) => Instance?.PlayDamageAt(worldPosition);

    void PlayPistol(Vector3 worldPosition) => PlayCombatClip(worldPosition, pistolClip, "GunshotOneShot");

    void PlayMeleeAt(Vector3 worldPosition) => PlayCombatClip(worldPosition, meleeClip, "MeleeOneShot");

    void PlayDamageAt(Vector3 worldPosition) => PlayCombatClip(worldPosition, damageClip, "DamageOneShot");

    void PlayCombatClip(Vector3 worldPosition, AudioClip clip, string objectName)
    {
        if (clip == null)
            return;

        var go = new GameObject(objectName);
        go.transform.position = worldPosition;
        var source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.spatialBlend = CombatSpatialBlend;
        source.volume = CombatSourceVolume;
        source.playOnAwake = false;
        if (combatGroup != null)
            source.outputAudioMixerGroup = combatGroup;
        source.Play();
        Destroy(go, clip.length + 0.1f);
    }
}
