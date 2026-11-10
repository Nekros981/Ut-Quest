#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

public static class MainMixerSetup
{
    const string MixerPath = "Assets/Audio/MainMixer.mixer";

    [MenuItem("UtQuest/Setup Main Mixer Groups")]
    public static void SetupGroupsAndExpose()
    {
        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        if (mixer == null)
        {
            Debug.LogError("Missing " + MixerPath);
            return;
        }

        var controllerType = mixer.GetType();
        var createNewGroup = controllerType.GetMethod(
            "CreateNewGroup",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (createNewGroup == null)
        {
            Debug.LogError("CreateNewGroup not found on " + controllerType.FullName);
            return;
        }

        EnsureGroup(mixer, createNewGroup, "Music");
        EnsureGroup(mixer, createNewGroup, "SFX");
        EnsureGroup(mixer, createNewGroup, "Combat");

        var exposeParam = controllerType.GetMethod(
            "ExposeParameter",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var effectType = typeof(AudioMixer).Assembly
            .GetType("UnityEditor.Audio.AudioMixerEffectController");
        if (exposeParam == null || effectType == null)
        {
            Debug.LogWarning("Could not expose mixer parameters automatically.");
            EditorUtility.SetDirty(mixer);
            AssetDatabase.SaveAssets();
            return;
        }

        ExposeGroupAttenuation(mixer, effectType, exposeParam, "Music", "MusicVolume");
        ExposeGroupAttenuation(mixer, effectType, exposeParam, "SFX", "SfxVolume");
        ExposeGroupAttenuation(mixer, effectType, exposeParam, "Combat", "CombatVolume");

        EditorUtility.SetDirty(mixer);
        AssetDatabase.SaveAssets();
        Debug.Log("MainMixer groups Music/SFX/Combat with MusicVolume, SfxVolume and CombatVolume.");
    }

    static void EnsureGroup(AudioMixer mixer, MethodInfo createNewGroup, string groupName)
    {
        foreach (var g in mixer.FindMatchingGroups(groupName))
        {
            if (g != null && g.name == groupName)
                return;
        }

        createNewGroup.Invoke(mixer, new object[] { groupName, false });
    }

    static void ExposeGroupAttenuation(
        AudioMixer mixer,
        System.Type effectControllerType,
        MethodInfo exposeParameter,
        string groupName,
        string exposedName)
    {
        AudioMixerGroup group = null;
        foreach (var g in mixer.FindMatchingGroups(groupName))
        {
            if (g != null && g.name == groupName)
            {
                group = g;
                break;
            }
        }

        if (group == null)
            return;

        if (mixer.GetFloat(exposedName, out _))
            return;

        var subAssets = AssetDatabase.LoadAllAssetsAtPath(MixerPath);
        foreach (var sub in subAssets)
        {
            if (sub == null || !effectControllerType.IsInstanceOfType(sub))
                continue;

            var groupProp = effectControllerType.GetProperty(
                "group",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (groupProp == null)
                continue;

            if (groupProp.GetValue(sub) as AudioMixerGroup != group)
                continue;

            if (sub.name != "Attenuation" && sub.name != string.Empty)
                continue;

            var getParam = effectControllerType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m => m.Name == "GetParameter" && m.GetParameters().Length == 1);
            if (getParam == null)
                continue;

            var param = getParam.Invoke(sub, new object[] { "Volume" });
            if (param == null)
                continue;

            exposeParameter.Invoke(mixer, new[] { exposedName, param });
            return;
        }

        Debug.LogWarning("Could not expose " + exposedName + " on group " + groupName);
    }
}
#endif
