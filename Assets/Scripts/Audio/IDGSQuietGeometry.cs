using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public sealed class IDGSQuietGeometry : MonoBehaviour
{
    static readonly HashSet<string> IdgsScenes = new HashSet<string>
    {
        "IDGS_Exterior",
        "IDGS_CMD_Interior",
        "IDGS_Docencia_Interior",
    };

    static IDGSQuietGeometry instance;

    [SerializeField] AudioClip musicClip;
    [SerializeField] AudioMixer audioMixer;

    AudioSource source;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        ApplyConfigDefaults();
        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureSource();
        SceneManager.sceneLoaded += OnSceneLoaded;
        PlayIfNeeded();
    }

    void OnDestroy()
    {
        if (instance != this)
            return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }

    void ApplyConfigDefaults()
    {
        if (musicClip != null && audioMixer != null)
            return;

        var config = Resources.Load<IDGSWorldConfig>("IDGS/WorldConfig");
        if (config == null)
            return;

        if (musicClip == null)
            musicClip = config.musicClip;
        if (audioMixer == null)
            audioMixer = config.audioMixer;
    }

    void EnsureSource()
    {
        source = GetComponent<AudioSource>();
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();

        source.clip = musicClip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 0.65f;

        if (audioMixer == null)
            return;

        foreach (var group in audioMixer.FindMatchingGroups("Music"))
        {
            if (group != null && group.name == "Music")
            {
                source.outputAudioMixerGroup = group;
                break;
            }
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IdgsScenes.Contains(scene.name))
            PlayIfNeeded();
        else
            Teardown();
    }

    void PlayIfNeeded()
    {
        if (source == null || source.clip == null || source.isPlaying)
            return;

        source.Play();
    }

    void Teardown()
    {
        if (source != null)
            source.Stop();

        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this)
            instance = null;

        Destroy(gameObject);
    }
}
