using UnityEngine;
using UnityEngine.InputSystem;

public sealed class SpBosqueAnimationTest : MonoBehaviour
{
    public CampusQuestMateoVisualDriver mateo;
    public CampusQuestSecuazVisualDriver secuazLeft;
    public CampusQuestSecuazVisualDriver secuazRight;

    void Update()
    {
            if (CampusQuestPauseMenu.BlocksInput) return;
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.jKey.wasPressedThisFrame)
        {
            secuazLeft?.PlayGolpe();
            secuazRight?.PlayGolpe();
        }

        if (keyboard.kKey.wasPressedThisFrame)
            secuazLeft?.PlayDerrotado();
    }
}
