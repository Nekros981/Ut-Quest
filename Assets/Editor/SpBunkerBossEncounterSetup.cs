#if UNITY_EDITOR
using System.IO;
using System.Linq;
using CampusQuest.Dialogue;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SpBunkerBossEncounterSetup
{
    const string ScenePath = "Assets/Scenes/Sp_Bunker.unity";
    const string DialoguePath = "Assets/Data/Dialogs/Bunker/Chaka_Entrada.asset";
    const string ThanksDialoguePath = "Assets/Data/Dialogs/Bunker/Chaka_Gracias.asset";
    const string SealSheetPath = "Assets/Sprites/Enviroment/Sp/PixelLab/Animacion_Sellar_Sp.png";
    const string SealClipPath = "Assets/Animations/Sp/Animacion_Sellar_Sp.anim";
    const string SealControllerPath = "Assets/Animations/Sp/Animacion_Sellar_Sp.controller";
    const string SealOverlayName = "CampusQuest_SealOverlay";
    static readonly Color SealBackdropGray = new Color(0.45f, 0.45f, 0.45f, 1f);
    const string BossControllerPath = "Assets/Animations/Bosses/Boss.controller";
    const string SecuazPrefabPath = "Assets/Prefabs/Characters/Secuas/Secuaz_Der.prefab";
    const string ExplosionFolder = "Assets/Sprites/Enviroment/Sp/Free Pixel Art Explosions/PNG/Explosion";
    const string GrenadeSfxPath = "Assets/Audio/SFX/Granade_Explosion.wav";
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
        EnsureThanksDialogueAsset();
        var definition = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(DialoguePath);
        var thanksDefinition = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(ThanksDialoguePath);
        var session = EnsureDialogueSession();
        var sealController = EnsureSealAnimationAssets();
        var door = GameObject.Find("Puerta_Bunker")?.GetComponent<CampusQuestBunkerDoor>();
        var mateo = Object.FindAnyObjectByType<CampusQuestMateoPlayerInput>();
        var encounterStub = EnsureEncounterStub();
        var explosionFrames = LoadExplosionSprites();
        var grenadeSfx = AssetDatabase.LoadAssetAtPath<AudioClip>(GrenadeSfxPath);
        var bossBrain = PlaceChaka(encounterStub, explosionFrames, grenadeSfx, session, thanksDefinition);
        var secuaces = PlaceSecuaces();
        var chakaIdle = GameObject.Find(ChakaName)?.GetComponentInChildren<CampusQuestBossIdle>(true);
        var sealOverlay = EnsureSealOverlay(mateo, sealController);
        var encounter = EnsureEncounterTrigger(
            door, session, definition, thanksDefinition, sealOverlay, mateo, secuaces, chakaIdle, bossBrain);

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

    static void EnsureThanksDialogueAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(ThanksDialoguePath);
        if (existing != null)
            return;

        Directory.CreateDirectory("Assets/Data/Dialogs/Bunker");
        var asset = ScriptableObject.CreateInstance<DialogueDefinition>();
        asset.dialogueId = SpBunkerBossEncounter.ThanksDialogueId;
        asset.defaultSpeaker = "El Chaka";
        asset.lines = new[]
        {
            new DialogueLine
            {
                speaker = "El Chaka",
                text = "Gracias por salvernos ten tu sello"
            }
        };
        AssetDatabase.CreateAsset(asset, ThanksDialoguePath);
    }

    static RuntimeAnimatorController EnsureSealAnimationAssets()
    {
        Directory.CreateDirectory("Assets/Animations/Sp");
        var sprites = AssetDatabase.LoadAllAssetsAtPath(SealSheetPath)
            .OfType<Sprite>()
            .Where(s => s.name.StartsWith("Animacion_Sellar_Sp_"))
            .OrderBy(s => s.name, System.StringComparer.Ordinal)
            .ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogWarning("[Campus Quest] Falta sheet Animacion_Sellar_Sp.");
            return null;
        }

        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(SealClipPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, SealClipPath);
        }

        WriteUiSpriteClip(clip, sprites, 8f, false);

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(SealControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(SealControllerPath);

        var sm = controller.layers[0].stateMachine;
        AnimatorState sellarState = null;
        foreach (var child in sm.states)
        {
            if (child.state.name == "Sellar")
            {
                sellarState = child.state;
                break;
            }
        }

        if (sellarState == null)
        {
            sellarState = sm.AddState("Sellar", new Vector3(300f, 0f, 0f));
            sm.defaultState = sellarState;
        }

        sellarState.motion = clip;
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    static void WriteUiSpriteClip(AnimationClip clip, Sprite[] frames, float frameRate, bool loop)
    {
        clip.frameRate = frameRate;
        var keyframes = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
            keyframes[i] = new ObjectReferenceKeyframe { time = i / frameRate, value = frames[i] };

        AnimationUtility.SetObjectReferenceCurve(
            clip,
            EditorCurveBinding.PPtrCurve(string.Empty, typeof(Image), "m_Sprite"),
            keyframes);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
    }

    static CampusQuestBunkerSealOverlay EnsureSealOverlay(
        CampusQuestMateoPlayerInput mateo,
        RuntimeAnimatorController sealController)
    {
        var hud = GameObject.Find("CampusQuest_HUD");
        if (hud == null)
        {
            Debug.LogWarning("[Campus Quest] No hay CampusQuest_HUD para el overlay del sello.");
            return null;
        }

        var overlayRoot = Child(hud.transform, SealOverlayName, true).gameObject;
        var rootRect = (RectTransform)overlayRoot.transform;
        Stretch(rootRect, 0, 0, 0, 0);
        rootRect.SetAsLastSibling();

        var rootImage = Get<Image>(overlayRoot.transform);
        rootImage.color = SealBackdropGray;
        rootImage.raycastTarget = true;

        var canvas = overlayRoot.GetComponent<Canvas>();
        if (canvas == null)
            canvas = Undo.AddComponent<Canvas>(overlayRoot);
        canvas.overrideSorting = true;
        canvas.sortingOrder = 200;

        var scaler = overlayRoot.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = Undo.AddComponent<CanvasScaler>(overlayRoot);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        if (overlayRoot.GetComponent<GraphicRaycaster>() == null)
            Undo.AddComponent<GraphicRaycaster>(overlayRoot);

        Sprite firstSprite = null;
        if (sealController != null)
        {
            foreach (var clip in sealController.animationClips)
            {
                if (clip == null)
                    continue;
                var binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(Image), "m_Sprite");
                var curve = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                if (curve != null && curve.Length > 0 && curve[0].value is Sprite sprite)
                {
                    firstSprite = sprite;
                    break;
                }
            }
        }

        var art = Child(overlayRoot.transform, "Sello_Art", true);
        var artRect = (RectTransform)art;
        artRect.anchorMin = artRect.anchorMax = new Vector2(0.5f, 0.5f);
        artRect.pivot = new Vector2(0.5f, 0.5f);
        artRect.sizeDelta = new Vector2(640f, 640f);
        artRect.anchoredPosition = Vector2.zero;

        var artImage = Get<Image>(art);
        artImage.raycastTarget = false;
        artImage.preserveAspect = true;
        if (firstSprite != null)
            artImage.sprite = firstSprite;

        var animator = Get<Animator>(art);
        animator.runtimeAnimatorController = sealController;
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixeloidFontPath);
        var hint = Child(overlayRoot.transform, "Sello_Hint", true);
        var hintRect = (RectTransform)hint;
        hintRect.anchorMin = new Vector2(0.5f, 0f);
        hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.sizeDelta = new Vector2(900f, 64f);
        hintRect.anchoredPosition = new Vector2(0f, 48f);
        var hintLabel = Get<TextMeshProUGUI>(hint);
        if (font != null)
            hintLabel.font = font;
        hintLabel.fontSize = 28;
        hintLabel.color = Gold;
        hintLabel.alignment = TextAlignmentOptions.Center;
        hintLabel.raycastTarget = false;
        hintLabel.enabled = false;

        var overlay = Get<CampusQuestBunkerSealOverlay>(overlayRoot.transform);
        var overlaySo = new SerializedObject(overlay);
        overlaySo.FindProperty("sealAnimator").objectReferenceValue = animator;
        overlaySo.FindProperty("mateoInput").objectReferenceValue = mateo;
        overlaySo.FindProperty("playStateName").stringValue = "Sellar";
        overlaySo.FindProperty("continueHint").objectReferenceValue = hintLabel;
        overlaySo.ApplyModifiedPropertiesWithoutUndo();

        overlayRoot.SetActive(false);
        return overlay;
    }

    static SpBunkerBossEncounter EnsureEncounterStub()
    {
        var go = GameObject.Find(EncounterName);
        if (go == null)
        {
            go = new GameObject(EncounterName);
            Undo.RegisterCreatedObjectUndo(go, EncounterName);
        }

        var encounter = go.GetComponent<SpBunkerBossEncounter>();
        return encounter != null ? encounter : Undo.AddComponent<SpBunkerBossEncounter>(go);
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
        DialogueDefinition thanksDefinition,
        CampusQuestBunkerSealOverlay sealOverlay,
        CampusQuestMateoPlayerInput mateo,
        CampusQuestSecuazBrain[] secuaces,
        CampusQuestBossIdle chakaIdle,
        CampusQuestBossBrain bossBrain)
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
        so.FindProperty("thanksDialogue").objectReferenceValue = thanksDefinition;
        so.FindProperty("sealOverlay").objectReferenceValue = sealOverlay;
        so.FindProperty("mateoInput").objectReferenceValue = mateo;

        var secuazPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SecuazPrefabPath);
        so.FindProperty("secuazPrefab").objectReferenceValue = secuazPrefab;

        var combatCount = secuaces.Length + (bossBrain != null ? 1 : 0);
        var brainsProp = so.FindProperty("combatBrains");
        brainsProp.arraySize = combatCount;
        int brainIndex = 0;
        for (int i = 0; i < secuaces.Length; i++, brainIndex++)
            brainsProp.GetArrayElementAtIndex(brainIndex).objectReferenceValue = secuaces[i];
        if (bossBrain != null)
            brainsProp.GetArrayElementAtIndex(brainIndex).objectReferenceValue = bossBrain;

        so.ApplyModifiedPropertiesWithoutUndo();

        if (bossBrain != null)
        {
            var bossSo = new SerializedObject(bossBrain);
            bossSo.FindProperty("encounter").objectReferenceValue = encounter;
            bossSo.ApplyModifiedPropertiesWithoutUndo();
        }

        var sessionSo = new SerializedObject(session);
        var suspend = sessionSo.FindProperty("suspendWhileOpen");
        suspend.arraySize = secuaces.Length + (mateo != null ? 1 : 0) + (chakaIdle != null ? 1 : 0)
            + (bossBrain != null ? 1 : 0);
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

        if (bossBrain != null)
        {
            suspend.GetArrayElementAtIndex(index).objectReferenceValue = bossBrain;
            index++;
        }

        for (int i = 0; i < secuaces.Length; i++, index++)
            suspend.GetArrayElementAtIndex(index).objectReferenceValue = secuaces[i];
        sessionSo.ApplyModifiedPropertiesWithoutUndo();

        return encounter;
    }

    static Sprite[] LoadExplosionSprites()
    {
        var frames = new Sprite[10];
        int count = 0;
        for (int i = 1; i <= 10; i++)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ExplosionFolder}/Explosion{i}.png");
            if (sprite == null)
                continue;
            frames[count++] = sprite;
        }

        if (count == frames.Length)
            return frames;

        var trimmed = new Sprite[count];
        for (int i = 0; i < count; i++)
            trimmed[i] = frames[i];
        return trimmed;
    }

    static CampusQuestBossBrain PlaceChaka(
        SpBunkerBossEncounter encounter,
        Sprite[] explosionFrames,
        AudioClip grenadeSfx,
        DialogueSession session,
        DialogueDefinition thanksDefinition)
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

        var bossVisual = visualGo.GetComponent<CampusQuestBossVisualDriver>();
        if (bossVisual == null)
            bossVisual = Undo.AddComponent<CampusQuestBossVisualDriver>(visualGo);
        var visualSo = new SerializedObject(bossVisual);
        visualSo.FindProperty("motionRoot").objectReferenceValue = root.transform;
        visualSo.ApplyModifiedPropertiesWithoutUndo();

        var body = root.GetComponent<Rigidbody2D>();
        if (body == null)
            body = Undo.AddComponent<Rigidbody2D>(root);
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;

        foreach (var circle in root.GetComponents<CircleCollider2D>())
            Undo.DestroyObjectImmediate(circle);

        var box = root.GetComponent<BoxCollider2D>();
        if (box == null)
            box = Undo.AddComponent<BoxCollider2D>(root);
        box.isTrigger = false;

        var bossBrain = root.GetComponent<CampusQuestBossBrain>();
        if (bossBrain == null)
            bossBrain = Undo.AddComponent<CampusQuestBossBrain>(root);

        var brainSo = new SerializedObject(bossBrain);
        brainSo.FindProperty("visual").objectReferenceValue = bossVisual;
        brainSo.FindProperty("encounter").objectReferenceValue = encounter;
        var framesProp = brainSo.FindProperty("explosionFrames");
        framesProp.arraySize = explosionFrames != null ? explosionFrames.Length : 0;
        for (int i = 0; i < framesProp.arraySize; i++)
            framesProp.GetArrayElementAtIndex(i).objectReferenceValue = explosionFrames[i];
        brainSo.FindProperty("grenadeExplosionClip").objectReferenceValue = grenadeSfx;
        brainSo.ApplyModifiedPropertiesWithoutUndo();
        bossBrain.enabled = false;

        var idleSprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Bosses/SP/Move_Boss.png")
            .OfType<Sprite>()
            .FirstOrDefault(s => s.name == "Move_Boss_0");
        if (idleSprite != null)
            sr.sprite = idleSprite;

        ApplyBossHitBox(sr, box);

        var interactRoot = root.transform.Find("Boss_Interact")?.gameObject;
        if (interactRoot == null)
        {
            interactRoot = new GameObject("Boss_Interact");
            Undo.RegisterCreatedObjectUndo(interactRoot, "Boss_Interact");
            interactRoot.transform.SetParent(root.transform, false);
        }

        interactRoot.transform.localPosition = new Vector3(0f, 0.85f, 0f);

        var thanksInteract = interactRoot.GetComponent<CampusQuestBossThanksInteract>();
        if (thanksInteract == null)
            thanksInteract = Undo.AddComponent<CampusQuestBossThanksInteract>(interactRoot);

        var thanksSo = new SerializedObject(thanksInteract);
        thanksSo.FindProperty("boss").objectReferenceValue = bossBrain;
        thanksSo.FindProperty("session").objectReferenceValue = session;
        thanksSo.FindProperty("thanksDialogue").objectReferenceValue = thanksDefinition;
        thanksSo.ApplyModifiedPropertiesWithoutUndo();

        return bossBrain;
    }

    static void ApplyBossHitBox(SpriteRenderer spriteRenderer, BoxCollider2D box)
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null || box == null)
            return;

        var bounds = spriteRenderer.sprite.bounds;
        box.size = bounds.size;
        box.offset = bounds.center;
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

            StripForcePistolFromInstance(instance);
            ConfigureBossRoomMinionLoot(instance);

            brains[i] = instance.GetComponent<CampusQuestSecuazBrain>();
            if (brains[i] == null)
                brains[i] = instance.GetComponentInChildren<CampusQuestSecuazBrain>(true);

            if (brains[i] != null)
            {
                brains[i].ConfigureBossMinion(pistol: false);
                var brainSo = new SerializedObject(brains[i]);
                brainSo.FindProperty("omiteBotin").boolValue = true;
                brainSo.ApplyModifiedPropertiesWithoutUndo();
                brains[i].enabled = false;

                var visual = brains[i].GetComponentInChildren<CampusQuestSecuazVisualDriver>(true);
                if (visual != null)
                {
                    var visualSo = new SerializedObject(visual);
                    visualSo.FindProperty("armaInicial").intValue = CampusQuestSecuazVisualDriver.ArmaTolete;
                    visualSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        return brains;
    }

    static void StripForcePistolFromInstance(GameObject instance)
    {
        if (instance == null)
            return;

        var forces = instance.GetComponentsInChildren<CampusQuestSecuazForcePistol>(true);
        foreach (var force in forces)
        {
            if (force == null)
                continue;

            if (PrefabUtility.IsAddedComponentOverride(force))
                PrefabUtility.RevertAddedComponent(force, InteractionMode.AutomatedAction);
            else
                Undo.DestroyObjectImmediate(force);
        }

        EditorUtility.SetDirty(instance);
    }

    static void ConfigureBossRoomMinionLoot(GameObject instance)
    {
        var loot = instance.GetComponent<CampusQuestSecuazLoot>();
        if (loot == null)
            loot = instance.GetComponentInChildren<CampusQuestSecuazLoot>(true);
        if (loot == null)
            return;

        loot.enabled = false;
        EditorUtility.SetDirty(loot);
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
