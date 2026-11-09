using UnityEngine;
using UnityEngine.InputSystem;

public sealed class SpBosqueAnimationTest : MonoBehaviour
{
    public CampusQuestMateoVisualDriver mateo;
    public CampusQuestSecuazVisualDriver secuazLeft;
    public CampusQuestSecuazVisualDriver secuazRight;

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.gKey.wasPressedThisFrame)
            mateo?.PlayGolpe();

        if (keyboard.hKey.wasPressedThisFrame)
            mateo?.PlayDisparo();

        if (keyboard.jKey.wasPressedThisFrame)
        {
            secuazLeft?.PlayGolpe();
            secuazRight?.PlayGolpe();
        }

        if (keyboard.kKey.wasPressedThisFrame)
            secuazLeft?.PlayDerrotado();
    }
}
