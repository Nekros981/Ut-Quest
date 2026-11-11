using UnityEngine;
using UnityEngine.SceneManagement;

public static class CampusQuestGameplayBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { SceneManager.sceneLoaded -= Loaded; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        SceneManager.sceneLoaded -= Loaded; SceneManager.sceneLoaded += Loaded;
        Prepare(SceneManager.GetActiveScene());
    }
    static void Loaded(Scene scene, LoadSceneMode mode) { Prepare(scene); }
    static void Prepare(Scene scene)
    {
        if (CampusQuestPauseMenu.LevelScope(scene.name) == null || Object.FindAnyObjectByType<CampusQuestPauseMenu>() != null) return;
        var config = Resources.Load<CampusQuestGameplayConfig>("Gameplay/CampusQuestGameplayConfig");
        if (config == null) return;
        var root = new GameObject("Campus Quest · Ayudas de juego");
        root.AddComponent<CampusQuestPauseMenu>().Configure(config);
        if (scene.name.StartsWith("IDGS_") || scene.name == "Rectoria_Tutorial_Arte_v01") root.AddComponent<CampusQuestGuidance>().Configure(config);
        if (scene.name == "IDGS_CMD_Interior" || scene.name == "IDGS_Exterior") root.AddComponent<CampusQuest.IDGS.IDGSCompletion>().Configure(config);
    }
}
