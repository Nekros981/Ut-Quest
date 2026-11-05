using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class OptionsMenu : MonoBehaviour
{
    [SerializeField] CanvasGroup windowGroup;
    [SerializeField] CanvasGroup mainMenuContentGroup;
    [SerializeField] TextMeshProUGUI controlsButtonLabel;

    void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    void Start()
    {
        HideImmediate();
        RefreshControlsLabel();
    }

    void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (windowGroup != null && windowGroup.alpha > 0.5f)
            RefreshControlsLabel();
    }

    public void Open()
    {
        if (windowGroup == null)
            return;

        windowGroup.alpha = 1f;
        windowGroup.interactable = true;
        windowGroup.blocksRaycasts = true;

        if (mainMenuContentGroup != null)
        {
            mainMenuContentGroup.interactable = false;
            mainMenuContentGroup.blocksRaycasts = false;
        }

        RefreshControlsLabel();
    }

    public void Close()
    {
        HideImmediate();

        if (mainMenuContentGroup != null)
        {
            mainMenuContentGroup.interactable = true;
            mainMenuContentGroup.blocksRaycasts = true;
        }
    }

    void HideImmediate()
    {
        if (windowGroup == null)
            return;

        windowGroup.alpha = 0f;
        windowGroup.interactable = false;
        windowGroup.blocksRaycasts = false;
    }

    void RefreshControlsLabel()
    {
        if (controlsButtonLabel == null)
            return;

        controlsButtonLabel.text = ResolveControlsLabel();
    }

    static string ResolveControlsLabel()
    {
        if (Gamepad.current != null)
            return "Mando";

        if (Application.isMobilePlatform)
            return "Táctil";

        return "Teclado";
    }
}
