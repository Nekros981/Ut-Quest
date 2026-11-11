using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace CampusQuest.IDGS
{
    static class IDGSWorldBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AfterSceneLoad()
        {
            var scene = SceneManager.GetActiveScene();
            if (!IsIdgsScene(scene.name))
                return;

            if (Object.FindAnyObjectByType<IDGSQuietGeometry>() == null)
            {
                var musicGo = GameObject.Find("IDGS_Music") ?? new GameObject("IDGS_Music");
                if (musicGo.GetComponent<IDGSQuietGeometry>() == null)
                    musicGo.AddComponent<IDGSQuietGeometry>();
            }

            var motor = Object.FindAnyObjectByType<IDGSPlayerMotor>();
            if (motor != null)
                motor.TryBindDefaultInput();
        }

        internal static bool IsIdgsScene(string sceneName)
        {
            return sceneName == "IDGS_Exterior"
                || sceneName == "IDGS_CMD_Interior"
                || sceneName == "IDGS_Docencia_Interior";
        }
    }
}
