using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class CampusQuestUiGamepad
{
    public static readonly Color SelectedGreen = new(0.1764706f, 0.41568628f, 0.30980393f, 1f);

    public static void ConfigureUiInputModule(InputActionAsset inputActions)
    {
        if (inputActions == null)
            return;

        var module = Object.FindAnyObjectByType<InputSystemUIInputModule>();
        if (module == null)
            return;

        module.actionsAsset = inputActions;
        module.AssignDefaultActions();
        CampusQuestActiveControls.Bind(inputActions);
    }

    public static void SetSelectedColor(Selectable selectable)
    {
        if (selectable == null)
            return;

        var colors = selectable.colors;
        colors.selectedColor = SelectedGreen;
        selectable.colors = colors;
    }

    public static void Focus(GameObject target)
    {
        if (target == null)
            return;

        var es = EventSystem.current;
        if (es == null)
            return;

        es.firstSelectedGameObject = target;
        es.SetSelectedGameObject(target);
    }

    public static void SetExplicitVertical(Selectable above, Selectable below)
    {
        if (above != null)
        {
            var nav = above.navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnDown = below;
            above.navigation = nav;
            SetSelectedColor(above);
        }

        if (below != null)
        {
            var nav = below.navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp = above;
            below.navigation = nav;
            SetSelectedColor(below);
        }
    }

    public static void SetExplicitHorizontal(Selectable left, Selectable right)
    {
        if (left != null)
        {
            var nav = left.navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnRight = right;
            left.navigation = nav;
            SetSelectedColor(left);
        }

        if (right != null)
        {
            var nav = right.navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnLeft = left;
            right.navigation = nav;
            SetSelectedColor(right);
        }
    }

    public static void ClearHorizontal(Selectable selectable)
    {
        if (selectable == null)
            return;

        var nav = selectable.navigation;
        nav.selectOnLeft = null;
        nav.selectOnRight = null;
        selectable.navigation = nav;
    }
}
