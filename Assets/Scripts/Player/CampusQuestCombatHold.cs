using UnityEngine;
using UnityEngine.SceneManagement;

public static class CampusQuestCombatHold
{
    const string ForestScene = "Sp_Bosque";

    static bool released;
    static bool sceneHooked;

    public static bool EnemiesHeld =>
        SceneManager.GetActiveScene().name == ForestScene && !released;

    public static void Release()
    {
        if (released)
            return;

        released = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AfterSceneLoad()
    {
        if (!sceneHooked)
        {
            sceneHooked = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == ForestScene)
            released = false;
    }
}
