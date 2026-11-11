#if UNITY_EDITOR
using System.Linq;
using UnityEditor;

public static class SpBunkerBuildSettings
{
    const string BunkerScene = "Assets/Scenes/Sp_Bunker.unity";

    public static void EnsureSpBunkerInBuild()
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == BunkerScene))
            return;

        scenes.Add(new EditorBuildSettingsScene(BunkerScene, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif
