using UnityEngine;
using UnityEngine.SceneManagement;

namespace CampusQuest.IDGS
{
    static class IDGSWorldBootstrap
    {
        static bool sceneHooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AfterSceneLoad()
        {
            if (!sceneHooked)
            {
                sceneHooked = true;
                SceneManager.sceneLoaded += OnSceneLoaded;
            }

            TryPrepareIdgsScene(SceneManager.GetActiveScene());
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryPrepareIdgsScene(scene);
        }

        static void TryPrepareIdgsScene(Scene scene)
        {
            if (!IsIdgsScene(scene.name))
                return;

            EnsureMusic();

            var motor = Object.FindAnyObjectByType<IDGSPlayerMotor>();
            if (motor != null)
                motor.TryBindDefaultInput();
        }

        static void EnsureMusic()
        {
            if (Object.FindAnyObjectByType<IDGSQuietGeometry>() != null)
                return;

            var musicGo = GameObject.Find("IDGS_Music") ?? new GameObject("IDGS_Music");
            if (musicGo.GetComponent<IDGSQuietGeometry>() == null)
                musicGo.AddComponent<IDGSQuietGeometry>();
        }

        internal static bool IsIdgsScene(string sceneName)
        {
            return sceneName == "IDGS_Exterior"
                || sceneName == "IDGS_CMD_Interior"
                || sceneName == "IDGS_Docencia_Interior";
        }
    }
}
