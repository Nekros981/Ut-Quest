#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;

public static class SpBosqueGunshotSetup
{
    const string ScenePath = "Assets/Scenes/Sp_Bosque.unity";
    const string PistolClipPath = "Assets/Audio/SFX/pistol-shot-1.wav";
    const string MeleeClipPath = "Assets/Audio/SFX/Mele.wav";
    const string DamageClipPath = "Assets/Audio/SFX/Damage.wav";
    const string MixerPath = "Assets/Audio/MainMixer.mixer";
    const string RootName = "CampusQuest_Gunshot";

    [MenuItem("Campus Quest/SP/Sonido de disparo")]
    public static void SetupFromMenu()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        WireScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Campus Quest] " + RootName + " en Sp_Bosque (disparo, melee y daño).");
    }

    public static void WireScene()
    {
        var go = GameObject.Find(RootName);
        if (go == null)
        {
            go = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(go, RootName);
        }

        var gunshot = go.GetComponent<CampusQuestGunshot>();
        if (gunshot == null)
            gunshot = go.AddComponent<CampusQuestGunshot>();

        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        var so = new SerializedObject(gunshot);
        so.FindProperty("pistolClip").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<AudioClip>(PistolClipPath);
        so.FindProperty("meleeClip").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<AudioClip>(MeleeClipPath);
        so.FindProperty("damageClip").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<AudioClip>(DamageClipPath);
        so.FindProperty("mixer").objectReferenceValue = mixer;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
