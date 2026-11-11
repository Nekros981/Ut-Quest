using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "IDGSWorldConfig", menuName = "Campus Quest/IDGS World Config")]
public sealed class IDGSWorldConfig : ScriptableObject
{
    public AudioClip musicClip;
    public AudioMixer audioMixer;
}
