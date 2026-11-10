using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class CampusQuestGameOver : MonoBehaviour
{
    public void Restart()
    {
        CampusQuestSecuazLoot.ResetForRestart();
        CampusQuestAmbushKeycard.ResetForRestart();
        CampusQuestMateoPlayerInput.ResetGameOverFlag();
        SceneManager.LoadScene("Sp_Bosque");
    }

    public void Quit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
