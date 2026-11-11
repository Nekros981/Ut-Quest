#if UNITY_EDITOR
using System.IO;
using System.Linq;
using CampusQuest.Dialogue;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SpBunkerBossEncounterSetup
{
    const string ScenePath = "Assets/Scenes/Sp_Bunker.unity";
    const string DialoguePath = "Assets/Data/Dialogs/Bunker/Chaka_Entrada.asset";
    const string BossControllerPath = "Assets/Animations/Bosses/Boss.controller";
    const string SecuazPrefabPath = "Assets/Prefabs/Characters/Secuas/Secuaz_Der.prefab";
    const string PixeloidFontPath = "Assets/Fonts/PixeloidSans SDF.asset";
    const string EncounterName = "CampusQuest_BunkerBossEncounter";
    const string DialogueRootName = "CampusQuest_BunkerDialogue";
    const string ChakaName = "El_Chaka";

    static readonly Color Gold = new Color32(233, 216, 166, 255);
    static readonly Color Dark = new Color32(27, 32, 45, 255);

    [MenuItem("Campus Quest/SP/Configurar encuentro El Chaka")]
    public static void Configure()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EnsureDialogueAsset();
        var definition = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(DialoguePath);
        var session = EnsureDialogueSession();
        var door = GameObject.Find("Puerta_Bunker")?.GetComponent<CampusQuestBunkerDoor>();
        var mateo = Object.FindAnyObjectByType<CampusQuestMateoPlayerInput>();
        PlaceChaka();
        var secuaces = PlaceSecuaces();
        var chakaIdle = GameObject.Find(ChakaName)?.GetComponentInChildren<CampusQuestBossIdle>(true);
        var encounter = EnsureEncounterTrigger(door, session, definition, mateo, secuaces, chakaIdle);

        EditorUtility.SetDirty(encounter);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Campus Quest] Sp_Bunker: encuentro El Chaka configurado.");
    }

    static void EnsureDialogueAsset()
    {
        Directory.CreateDirectory("Assets/Data/Dialogs/Bunker");
        var existing = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(DialoguePath);
        if (existing != null)
            return;

        var asset = ScriptableObject.CreateInstance<DialogueDefinition>();
        asset.dialogueId = "bunker.chaka.entrada";
        asset.defaultSpeaker = "El Chaka";
        asset.lines = new[]
        {
            new DialogueLine
            {
                speaker = "El Chaka",
                text = "Vaya vaya pero si aqui tenemos al rambo de la cantera"
            },
            new DialogueLine
            {
                speaker = "Mateo",
                text = "Rambo cobraba por misión; yo solo vengo por mi calificación y mi sello. ¿Empezamos o le da frío?"
            }
        };
        AssetDatabase.CreateAsset(asset, DialoguePath);
    }

    static DialogueSession EnsureDialogueSession()
    {
        var root = GameObject.Find(DialogueRootName);
        if (root == null)
        {
            root = new GameObject(DialogueRootName);
            Undo.RegisterCreatedObjectUndo(root, DialogueRootName);
        }

        var session = root.GetComponent<DialogueSession>();
        if (session == null)
            session = Undo.AddComponent<DialogueSession>(root);

        if (root.GetComponent<DialoguePlayerContext>() == null)
            Undo.AddComponent<DialoguePlayerContext>(root);

        var view = root.GetComponent<DialogueView>();
        if (view == null)
            view = Undo.AddComponent<DialogueView>(root);

        BuildDialogueView(root.transform, view);
        session.view = view;

        var mateo = Object.FindAnyObjectByType<CampusQuestMateoPlayerInput>();
        session.playerContext = root.GetComponent<DialoguePlayerContext>();
        session.suspendWhileOpen = mateo != null
            ? new Behaviour[] { mateo }
            : System.Array.Empty<Behaviour>();

        return session;
    }

    static SpBunkerBossEncounter EnsureEncounterTrigger(
        CampusQuestBunkerDoor door,
        DialogueSession session,
        DialogueDefinition definition,
        CampusQuestMateoPlayerInput mateo,
        CampusQuestSecuazBrain[] secuaces,
        CampusQuestBossIdle chakaIdle)
    {
        var go = GameObject.Find(EncounterName);
        if (go == null)
        {
            go = new GameObject(EncounterName);
            Undo.RegisterCreatedObjectUndo(go, EncounterName);
        }

        go.transform.position = new Vector3(8f, 14.25f, 0f);
        var box = go.GetComponent<BoxCollider2D>();
        if (box == null)
            box = Undo.AddComponent<BoxCollider2D>(go);
        box.isTrigger = true;
        box.size = new Vector2(3.2f, 1.2f);

        var encounter = go.GetComponent<SpBunkerBossEncounter>();
        if (encounter == null)
            encounter = Undo.AddComponent<SpBunkerBossEncounter>(go);

        var so = new SerializedObject(encounter);
        so.FindProperty("door").objectReferenceValue = door;
        so.FindProperty("session").objectReferenceValue = session;
        so.FindProperty("introDialogue").objectReferenceValue = definition;
        so.FindProperty("mateoInput").objectReferenceValue = mateo;

        var brains = new Behaviour[secuaces.Length];
        for (int i = 0; i < secuaces.Length; i++)
            brains[i] = secuaces[i];
        var brainsProp = so.FindProperty("combatBrains");
        brainsProp.arraySize = brains.Length;
        for (int i = 0; i < brains.Length; i++)
            brainsProp.GetArrayElementAtIndex(i).objectReferenceValue = brains[i];

        so.ApplyModifiedPropertiesWithoutUndo();

        var sessionSo = new SerializedObject(session);
        var suspend = sessionSo.FindProperty("suspendWhileOpen");
        suspend.arraySize = brains.Length + (mateo != null ? 1 : 0) + (chakaIdle != null ? 1 : 0);
        int index = 0;
        if (mateo != null)
        {
            suspend.GetArrayElementAtIndex(index).objectReferenceValue = mateo;
            index++;
        }

        if (chakaIdle != null)
        {
            suspend.GetArrayElementAtIndex(index).objectReferenceValue = chakaIdle;
            index++;
        }

        for (int i = 0; i < brains.Length; i++, index++)
            suspend.GetArrayElementAtIndex(index).objectReferenceValue = brains[i];
        sessionSo.ApplyModifiedPropertiesWithoutUndo();

        return encounter;
    }

    static void PlaceChaka()
    {
        var spawn = GameObject.Find("SP_JefeSpawn");
        var pos = spawn != null ? spawn.transform.position : new Vector3(8f, 23f, 0f);

        var root = GameObject.Find(ChakaName);
        if (root == null)
        {
            root = new GameObject(ChakaName);
            Undo.RegisterCreatedObjectUndo(root, ChakaName);
        }

        root.transform.position = pos;

        var visualGo = root.transform.Find("Boss_Visual")?.gameObject;
        if (visualGo == null)
        {
            visualGo = new GameObject("Boss_Visual");
            Undo.RegisterCreatedObjectUndo(visualGo, "Boss_Visual");
            visualGo.transform.SetParent(root.transform, false);
        }

        var sr = visualGo.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = Undo.AddComponent<SpriteRenderer>(visualGo);
        sr.sortingOrder = 45;

        var animator = visualGo.GetComponent<Animator>();
        if (animator == null)
            animator = Undo.AddComponent<Animator>(visualGo);
        animator.runtimeAnimatorController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BossControllerPath);

        if (visualGo.GetComponent<CampusQuestBossIdle>() == null)
            Undo.AddComponent<CampusQuestBossIdle>(visualGo);

        var idleSprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Bosses/SP/Move_Boss.png")
            .OfType<Sprite>()
            .FirstOrDefault(s => s.name == "Move_Boss_0");
        if (idleSprite != null)
            sr.sprite = idleSprite;
    }

    static CampusQuestSecuazBrain[] PlaceSecuaces()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SecuazPrefabPath);
        var positions = new[]
        {
            new Vector3(5f, 21f, 0f),
            new Vector3(11f, 21f, 0f)
        };
        var names = new[] { "Secuaz_Bunker_L", "Secuaz_Bunker_R" };
        var brains = new CampusQuestSecuazBrain[positions.Length];

        for (int i = 0; i < positions.Length; i++)
        {
            var existing = GameObject.Find(names[i]);
            GameObject instance;
            if (existing != null)
                instance = existing;
            else if (prefab != null)
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            else
            {
                instance = new GameObject(names[i]);
                Undo.RegisterCreatedObjectUndo(instance, names[i]);
            }

            instance.name = names[i];
            instance.transform.position = positions[i];

            if (instance.GetComponent<CampusQuestSecuazForcePistol>() == null)
                Undo.AddComponent<CampusQuestSecuazForcePistol>(instance);

            brains[i] = instance.GetComponent<CampusQuestSecuazBrain>();
            if (brains[i] == null)
                brains[i] = instance.GetComponentInChildren<CampusQuestSecuazBrain>(true);
        }

        return brains;
    }

    static void BuildDialogueView(Transform parent, DialogueView view)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixeloidFontPath);
        var canvasGo = parent.Find("Dialogo_UI")?.gameObject;
        if (canvasGo == null)
        {
            canvasGo = new GameObject("Dialogo_UI", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(canvasGo, "Dialogo_UI");
            canvasGo.transform.SetParent(parent, false);
        }

        var canvas = canvasGo.GetComponent<Canvas>();
        if (canvas == null)
            canvas = Undo.AddComponent<Canvas>(canvasGo);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        canvas.pixelPerfect = true;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = Undo.AddComponent<CanvasScaler>(canvasGo);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        var frame = Child(canvasGo.transform, "Caja_De_Dialogo", true);
        var frameRect = (RectTransform)frame;
        frameRect.anchorMin = new Vector2(0, 0);
        frameRect.anchorMax = new Vector2(1, 0);
        frameRect.pivot = new Vector2(0.5f, 0);
        frameRect.sizeDelta = new Vector2(-48, 232);
        frameRect.anchoredPosition = new Vector2(0, 16);
        Get<Image>(frame).color = Gold;
        view.panel = Get<CanvasGroup>(frame);
        Child(frame, "Fondo", true);
        Stretch((RectTransform)frame.GetChild(0), 5, 5, 5, 5);
        Get<Image>(frame.GetChild(0)).color = Dark;

        var portraitFrame = Child(frame, "Retrato_Marco", true);
        Fixed((RectTransform)portraitFrame, new Vector2(24, 40), new Vector2(134, 158));
        Get<Image>(portraitFrame).color = Gold;
        var portraitArea = Child(portraitFrame, "Retrato_Fondo", true);
        Stretch((RectTransform)portraitArea, 3, 3, 3, 3);
        Get<Image>(portraitArea).color = new Color32(42, 44, 56, 255);
        var portraitImage = Child(portraitArea, "Imagen", true);
        Stretch((RectTransform)portraitImage, 6, 6, 6, 6);
        view.portraitImage = Get<Image>(portraitImage);
        view.portraitImage.preserveAspect = true;
        view.portraitImage.raycastTarget = false;
        view.portraitPlaceholder = Label(portraitArea, "Retrato_Provisional", 17, Gold, TextAlignmentOptions.Center, font);
        Stretch(view.portraitPlaceholder.rectTransform, 6, 6, 6, 6);

        view.speakerLabel = Label(frame, "Nombre_NPC", 28, Gold, TextAlignmentOptions.Left, font);
        Stretch(view.speakerLabel.rectTransform, 184, 30, 174, 24);
        view.bodyLabel = Label(frame, "Texto", 26, Color.white, TextAlignmentOptions.TopLeft, font);
        Stretch(view.bodyLabel.rectTransform, 184, 30, 60, 66);
        view.bodyLabel.textWrappingMode = TextWrappingModes.Normal;
        view.bodyLabel.enableAutoSizing = true;
        view.bodyLabel.fontSizeMin = 18;
        view.bodyLabel.fontSizeMax = 26;
        var help = Label(frame, "Avanzar", 19, Gold, TextAlignmentOptions.BottomLeft, font);
        Stretch(help.rectTransform, 184, 160, 22, 174);
        help.text = "E / Enter · Continuar";
        view.pageLabel = Label(frame, "Pagina", 19, Gold, TextAlignmentOptions.BottomRight, font);
        Stretch(view.pageLabel.rectTransform, 184, 30, 22, 174);

        view.SetVisible(false);
        view.SetPrompt(string.Empty);
    }

    static Transform Child(Transform parent, string name, bool rect = false)
    {
        var found = parent.Find(name);
        if (found != null)
            return found;
        var go = rect ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Crear " + name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static T Get<T>(Transform root) where T : Component
    {
        var result = root.GetComponent<T>();
        return result != null ? result : Undo.AddComponent<T>(root.gameObject);
    }

    static TextMeshProUGUI Label(
        Transform parent,
        string name,
        float size,
        Color color,
        TextAlignmentOptions alignment,
        TMP_FontAsset font)
    {
        var root = Child(parent, name, true);
        var label = Get<TextMeshProUGUI>(root);
        if (font != null)
            label.font = font;
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;
        label.richText = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        return label;
    }

    static void Fixed(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    static void Stretch(RectTransform rect, float left, float right, float bottom, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }
}
#endif
