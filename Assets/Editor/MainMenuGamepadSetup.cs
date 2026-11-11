#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class MainMenuGamepadSetup
{
    const string InputActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";

    [MenuItem("Campus Quest/Wire Main Menu Gamepad")]
    public static void WireMainMenuGamepad()
    {
        var panel = GameObject.Find("Canvas/MainMenu_Content/Panel");
        var template = GameObject.Find("Canvas/MainMenu_Content/Panel/Btn_Opciones");
        var mainMenu = Object.FindAnyObjectByType<MainMenu>();
        var optionsMenu = Object.FindAnyObjectByType<OptionsMenu>();

        if (panel == null || template == null || mainMenu == null)
        {
            Debug.LogError("MainMenuGamepadSetup: missing Panel, Btn_Opciones or MainMenu.");
            return;
        }

        var btnSalir = panel.transform.Find("Btn_Salir");
        if (btnSalir == null)
        {
            var salirGo = Object.Instantiate(template, panel.transform);
            salirGo.name = "Btn_Salir";
            SetButtonLabel(salirGo, "SALIR");
            btnSalir = salirGo.transform;
        }

        var panelRect = panel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.sizeDelta = new Vector2(460f, 425f);
            panelRect.anchoredPosition = new Vector2(0f, -195f);
        }

        var iniciar = panel.transform.Find("Btn_Iniciar")?.GetComponent<Button>();
        var opciones = panel.transform.Find("Btn_Opciones")?.GetComponent<Button>();
        var niveles = panel.transform.Find("Btn_Levels")?.GetComponent<Button>();
        var salir = btnSalir.GetComponent<Button>();

        MainMenu.ApplyMainMenuNavigation(iniciar, opciones, niveles, salir);
        WireButton(salir, mainMenu, nameof(MainMenu.Quit));

        var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
        var mainMenuSo = new SerializedObject(mainMenu);
        mainMenuSo.FindProperty("inputActions").objectReferenceValue = inputActions;
        mainMenuSo.FindProperty("optionsMenu").objectReferenceValue = optionsMenu;
        mainMenuSo.FindProperty("btnIniciar").objectReferenceValue =
            iniciar != null ? iniciar.gameObject : null;
        mainMenuSo.ApplyModifiedPropertiesWithoutUndo();

        var es = Object.FindAnyObjectByType<EventSystem>();
        if (es != null && iniciar != null)
            es.firstSelectedGameObject = iniciar.gameObject;

        var uiModule = Object.FindAnyObjectByType<InputSystemUIInputModule>();
        if (uiModule != null && inputActions != null)
        {
            uiModule.actionsAsset = inputActions;
            uiModule.AssignDefaultActions();
            EditorUtility.SetDirty(uiModule);
        }

        if (optionsMenu != null)
        {
            var frameRoot = optionsMenu.transform.Find("OptionsFrame/Fondo_Oscuro");
            if (frameRoot != null)
            {
                var c = frameRoot.Find("TabRow/Btn_Controles")?.GetComponent<Button>();
                var a = frameRoot.Find("TabRow/Btn_Audio")?.GetComponent<Button>();
                var p = frameRoot.Find("TabRow/Btn_Pantalla")?.GetComponent<Button>();
                var i = frameRoot.Find("TabRow/Btn_Iluminacion")?.GetComponent<Button>();
                OptionsMenu.ApplyTabRowNavigation(c, a, p, i);
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Main menu gamepad wired.");
    }

    static void SetButtonLabel(GameObject button, string label)
    {
        var tmp = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
            tmp.text = label;
    }

    static void WireButton(Button button, MainMenu target, string method)
    {
        if (button == null || target == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            var m = typeof(MainMenu).GetMethod(method);
            m?.Invoke(target, null);
        });

        var so = new SerializedObject(button);
        var calls = so.FindProperty("m_OnClick").FindPropertyRelative("m_PersistentCalls.m_Calls");
        calls.arraySize = 1;
        var call0 = calls.GetArrayElementAtIndex(0);
        call0.FindPropertyRelative("m_Target").objectReferenceValue = target;
        call0.FindPropertyRelative("m_MethodName").stringValue = method;
        call0.FindPropertyRelative("m_Mode").enumValueIndex = 1;
        call0.FindPropertyRelative("m_CallState").enumValueIndex = 2;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
