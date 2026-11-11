using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

public static class CampusQuestActiveControls
{
    public const string KeyboardMouse = "Keyboard&Mouse";
    public const string GamepadScheme = "Gamepad";

    const float StickDeadzone = 0.2f;
    const float MenuRepeatDelay = 0.18f;

    static string activeScheme = KeyboardMouse;
    static InputActionAsset asset;
    static InputAction submitAction;
    static InputAction interactAction;
    static InputAction cancelAction;
    static InputAction moveAction;
    static bool listening;
    static Vector2Int lastMenuDirection;
    static float menuHoldSeconds;

    public static string ActiveScheme => activeScheme;
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState()
    {
        if (listening)
        {
            InputSystem.onEvent -= OnInputEvent;
            listening = false;
        }

        activeScheme = KeyboardMouse;
        asset = null;
        submitAction = null;
        interactAction = null;
        cancelAction = null;
        moveAction = null;
        lastMenuDirection = Vector2Int.zero;
        menuHoldSeconds = 0f;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureListening()
    {
        if (listening)
            return;
        InputSystem.onEvent += OnInputEvent;
        listening = true;
    }

    public static void Bind(InputActionAsset actions)
    {
        if (actions == null)
            return;

        EnsureListening();
        asset = actions;
        var uiMap = actions.FindActionMap("UI", false);
        if (uiMap != null)
            uiMap.Enable();
        submitAction = uiMap?.FindAction("Submit", false);
        cancelAction = uiMap?.FindAction("Cancel", false);
        var playerMap = actions.FindActionMap("Player", false);
        if (playerMap != null)
            playerMap.Enable();
        interactAction = playerMap?.FindAction("Interact", false);
        moveAction = playerMap?.FindAction("Move", false);
    }

    public static bool InteractPressedThisFrame()
    {
        if (interactAction != null && interactAction.enabled && interactAction.WasPressedThisFrame())
            return true;

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            return true;

        var pad = Gamepad.current;
        return pad != null && pad.buttonWest.wasPressedThisFrame;
    }

    public static bool PuzzleConfirmPressedThisFrame()
    {
        if (InteractPressedThisFrame())
            return true;

        var keyboard = Keyboard.current;
        return keyboard != null
            && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame);
    }

    public static bool CancelPressedThisFrame()
    {
        if (cancelAction != null && cancelAction.enabled && cancelAction.WasPressedThisFrame())
            return true;

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            return true;

        var pad = Gamepad.current;
        return pad != null && pad.buttonEast.wasPressedThisFrame;
    }

    public static bool MenuDirectionThisFrame(out Vector2Int direction)
    {
        direction = Vector2Int.zero;
        var move = moveAction != null && moveAction.enabled ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        if (move.sqrMagnitude < StickDeadzone * StickDeadzone)
        {
            lastMenuDirection = Vector2Int.zero;
            menuHoldSeconds = 0f;
            return false;
        }

        var axis = DominantAxis(move);
        if (axis == Vector2Int.zero)
            return false;

        if (axis != lastMenuDirection)
        {
            lastMenuDirection = axis;
            menuHoldSeconds = 0f;
            direction = axis;
            return true;
        }

        menuHoldSeconds += Time.unscaledDeltaTime;
        if (menuHoldSeconds < MenuRepeatDelay)
            return false;

        menuHoldSeconds = 0f;
        direction = axis;
        return true;
    }

    static Vector2Int DominantAxis(Vector2 move)
    {
        if (Mathf.Abs(move.x) > Mathf.Abs(move.y))
            return move.x > 0f ? Vector2Int.right : Vector2Int.left;

        return move.y > 0f ? Vector2Int.up : Vector2Int.down;
    }

    public static bool SubmitPressedThisFrame()
    {
        if (submitAction != null)
            return submitAction.WasPressedThisFrame();

        var keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.eKey.wasPressedThisFrame
                || keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
            return true;

        var pad = Gamepad.current;
        return pad != null && pad.buttonWest.wasPressedThisFrame;
    }

    public static string BindingLabel(string mapName, string actionName)
    {
        if (asset == null)
            return activeScheme == GamepadScheme ? "X" : "E / Enter";

        var map = asset.FindActionMap(mapName, false);
        var action = map?.FindAction(actionName, false);
        if (action == null)
            return activeScheme == GamepadScheme ? "X" : "E / Enter";

        string group = activeScheme == GamepadScheme ? GamepadScheme : KeyboardMouse;
        return action.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontUseShortDisplayNames, group: group);
    }

    static void OnInputEvent(InputEventPtr eventPtr, InputDevice device)
    {
        if (!eventPtr.valid || device == null)
            return;

        if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
            return;

        if (device is Keyboard keyboard)
        {
            if (keyboard.anyKey.isPressed)
                SetScheme(KeyboardMouse);
            return;
        }

        if (device is Mouse mouse)
        {
            if (mouse.leftButton.isPressed || mouse.rightButton.isPressed || mouse.middleButton.isPressed
                || mouse.delta.ReadValue().sqrMagnitude > 1f)
                SetScheme(KeyboardMouse);
            return;
        }

        if (device is Gamepad pad && GamepadHasMeaningfulInput(pad))
            SetScheme(GamepadScheme);
    }

    static bool GamepadHasMeaningfulInput(Gamepad pad)
    {
        foreach (var control in pad.allControls)
        {
            if (control is ButtonControl button && button.isPressed)
                return true;
        }

        float deadzoneSq = StickDeadzone * StickDeadzone;
        if (pad.leftStick.ReadValue().sqrMagnitude > deadzoneSq)
            return true;
        if (pad.rightStick.ReadValue().sqrMagnitude > deadzoneSq)
            return true;
        if (pad.leftTrigger.ReadValue() > StickDeadzone || pad.rightTrigger.ReadValue() > StickDeadzone)
            return true;

        return false;
    }

    static void SetScheme(string scheme)
    {
        if (scheme == activeScheme)
            return;
        activeScheme = scheme;
        Changed?.Invoke();
    }
}
