#if UNITY_EDITOR
using CampusQuest.IDGS;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public static class IDGSWorldSetup
{
    const string ExteriorPath = "Assets/Scenes/IDGS_Exterior.unity";
    const string CmdPath = "Assets/Scenes/IDGS_CMD_Interior.unity";
    const string DocenciaPath = "Assets/Scenes/IDGS_Docencia_Interior.unity";
    const string MusicClipPath = "Assets/Audio/Musica/Quiet_Geometry.wav";
    const string MixerPath = "Assets/Audio/MainMixer.mixer";
    const string InputActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";
    const string MusicObjectName = "IDGS_Music";
    const string ConfigFolder = "Assets/Resources/IDGS";
    const string ConfigPath = "Assets/Resources/IDGS/WorldConfig.asset";

    [MenuItem("Campus Quest/IDGS/Configurar música y mando")]
    public static void ConfigureAll()
    {
        EnsureWorldConfig();
        ConfigureScene(ExteriorPath);
        ConfigureScene(CmdPath);
        ConfigureScene(DocenciaPath);
        AssetDatabase.SaveAssets();
        Debug.Log("[Campus Quest] IDGS: Quiet_Geometry y Input Actions en exterior, CMD y Docencia.");
    }

    static void EnsureWorldConfig()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(ConfigFolder))
            AssetDatabase.CreateFolder("Assets/Resources", "IDGS");

        var config = AssetDatabase.LoadAssetAtPath<IDGSWorldConfig>(ConfigPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<IDGSWorldConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
        }

        config.musicClip = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicClipPath);
        config.audioMixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        EditorUtility.SetDirty(config);
    }

    static void ConfigureScene(string scenePath)
    {
        if (!System.IO.File.Exists(scenePath))
        {
            Debug.LogWarning("[Campus Quest] Falta escena: " + scenePath);
            return;
        }

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        EnsureMusic();
        WirePlayerInput();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            Debug.LogError("[Campus Quest] No se pudo guardar " + scenePath);
    }

    static void EnsureMusic()
    {
        var musicGo = GameObject.Find(MusicObjectName);
        if (musicGo == null)
        {
            musicGo = new GameObject(MusicObjectName);
            Undo.RegisterCreatedObjectUndo(musicGo, MusicObjectName);
        }

        var music = musicGo.GetComponent<IDGSQuietGeometry>();
        if (music == null)
            music = Undo.AddComponent<IDGSQuietGeometry>(musicGo);

        var so = new SerializedObject(music);
        so.FindProperty("musicClip").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<AudioClip>(MusicClipPath);
        so.FindProperty("audioMixer").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void WirePlayerInput()
    {
        var motor = Object.FindAnyObjectByType<IDGSPlayerMotor>();
        if (motor == null)
        {
            Debug.LogWarning("[Campus Quest] Sin IDGSPlayerMotor en " + SceneManager.GetActiveScene().path);
            return;
        }

        var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
        var so = new SerializedObject(motor);
        so.FindProperty("inputActions").objectReferenceValue = inputActions;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(motor);
    }
}
#endif
