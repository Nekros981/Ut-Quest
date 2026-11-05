using UnityEngine;
using UnityEngine.Audio;

public class MenuBackgroundMusic : MonoBehaviour
{
    [SerializeField] AudioClip menuMusicClip;
    [SerializeField] AudioMixer audioMixer;

    void Start()
    {
        var source = GetComponent<AudioSource>();
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();

        source.clip = menuMusicClip;
        source.loop = true;
        source.playOnAwake = false;

        if (audioMixer != null)
        {
            foreach (var group in audioMixer.FindMatchingGroups("Music"))
            {
                if (group != null && group.name == "Music")
                {
                    source.outputAudioMixerGroup = group;
                    break;
                }
            }
        }

        if (source.clip != null && !source.isPlaying)
            source.Play();
    }
}
