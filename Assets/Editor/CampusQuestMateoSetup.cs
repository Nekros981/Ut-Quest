#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class CampusQuestMateoSetup
{
    private const string Root = "Assets/Sprites/Characters/Mateo";
    private const string Frames = Root + "/Frames";
    private const string Anim = "Assets/Animations/Player/Mateo";
    private const string ActionRoot = Root + "/Animaciones";
    private const string ControllerPath = Anim + "/Mateo.controller";
    private const float ActionFrameRate = 18f;
    private const int ActionFrameCount = 9;

    const string DefeatedSheetPath = ActionRoot + "/Derrotado/Derrotado.png";
    const int DefeatedFrameCount = 12;

    static readonly (string clipSuffix, string sheetFile)[] ActionSheets =
    {
        ("Disparo_Down", "Disparo/Disparo_Abajo.png"),
        ("Disparo_Side", "Disparo/Disparo_Lado.png"),
        ("Disparo_Up", "Disparo/Disparo_Arriba.png"),
        ("Golpe_Down", "Ataque/Golpe_Abajo.png"),
        ("Golpe_Side", "Ataque/Golpe_Lado.png"),
        ("Golpe_Up", "Ataque/Golpe_Arriba.png")
    };

    [MenuItem("Campus Quest/Mateo/1 - Crear animaciones desde 16 PNG")]
    public static void Generate()
    {
        Directory.CreateDirectory(Anim);
        var names = new[] { "Down", "Left", "Right", "Up" };
        var prefixes = new[] { "caminataA", "caminataIzq", "caminataD", "caminataUp" };
        var sprites = new Sprite[4][];
        for (int d = 0; d < 4; d++)
        {
            sprites[d] = new Sprite[4];
            for (int i = 0; i < 4; i++)
            {
                var path = $"{Frames}/{prefixes[d]}{i + 1}.png";
                if (!File.Exists(path))
                {
                    EditorUtility.DisplayDialog("Faltan sprites", "No existe: " + path + "\nPrimero ejecuta Descargar_Mateo.ps1.", "Aceptar");
                    return;
                }
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new Exception("No se pudo importar " + path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 110f; // Referencia inicial: ajustar despues de probar escala.
                var settings = new TextureImporterSettings();

                importer.ReadTextureSettings(settings);

                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(0.5f, 0f);

                importer.SetTextureSettings(settings);
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                sprites[d][i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprites[d][i] == null) throw new Exception("No se pudo cargar " + path);
            }
        }

        for (int d = 0; d < 4; d++)
        {
            MakeClip($"Mateo_Walk_{names[d]}", sprites[d], true);
            MakeClip($"Mateo_Idle_{names[d]}", new[] { sprites[d][0] }, false);
        }

        // Replace only our own controller, never another controller.
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            AssetDatabase.DeleteAsset(ControllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Direction", AnimatorControllerParameterType.Int);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        var sm = controller.layers[0].stateMachine;
        for (int d = 0; d < 4; d++)
        {
            foreach (bool moving in new[] { false, true })
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{Anim}/Mateo_{(moving ? "Walk" : "Idle")}_{names[d]}.anim");
                var state = sm.AddState($"{(moving ? "Walk" : "Idle")}_{names[d]}");
                state.motion = clip;
                if (d == 0 && !moving) sm.defaultState = state;
                var transition = sm.AddAnyStateTransition(state);
                transition.hasExitTime = false;
                transition.duration = 0f;
                transition.canTransitionToSelf = false;
                transition.AddCondition(AnimatorConditionMode.Equals, d, "Direction");
                transition.AddCondition(moving ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "IsMoving");
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Campus Quest] Mateo: 8 clips y Animator generados en " + Anim);
        EditorUtility.DisplayDialog("Mateo listo", "Se generaron 4 clips Walk, 4 clips Idle y Mateo.controller.\nSelecciona Mateo_Provisional y usa el menu 2.", "Aceptar");
    }

    private static void MakeClip(string name, Sprite[] frames, bool loop)
    {
        string path = $"{Anim}/{name}.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }
        WriteSpriteClip(clip, frames, 8f, loop);
    }

    static void WriteSpriteClip(AnimationClip clip, Sprite[] frames, float frameRate, bool loop)
    {
        clip.frameRate = frameRate;
        var keyframes = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
            keyframes[i] = new ObjectReferenceKeyframe { time = i / frameRate, value = frames[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keyframes);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
    }

    [MenuItem("Campus Quest/Mateo/3 - Crear disparo y golpe")]
    public static void GenerateActionClipsAndWireAnimator()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            EditorUtility.DisplayDialog("Falta Mateo", "Ejecuta primero el menú 1 para crear Mateo.controller.", "Aceptar");
            return;
        }

        Directory.CreateDirectory(Anim);
        foreach (var (clipSuffix, relativePath) in ActionSheets)
        {
            var path = $"{ActionRoot}/{relativePath}";
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog("Faltan sprites", "No existe: " + path, "Aceptar");
                return;
            }

            ApplyActionSheetImport(path);
            var sprites = LoadOrderedSheetSprites(path);
            if (sprites.Length < ActionFrameCount)
            {
                EditorUtility.DisplayDialog("Sprites incompletos",
                    $"En {relativePath} hay {sprites.Length} sprites; se necesitan {ActionFrameCount}.", "Aceptar");
                return;
            }

            var clipPath = $"{Anim}/Mateo_{clipSuffix}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, clipPath);
            }

            WriteSpriteClip(clip, sprites, ActionFrameRate, false);
        }

        WireActionStates(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Campus Quest] Mateo: disparo y golpe en " + Anim);
    }

    [MenuItem("Campus Quest/Mateo/5 - Derrotado")]
    public static void GenerateDefeatedAndWire()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            EditorUtility.DisplayDialog("Falta Mateo", "Ejecuta primero el menú 1 para crear Mateo.controller.", "Aceptar");
            return;
        }

        if (!File.Exists(DefeatedSheetPath))
        {
            EditorUtility.DisplayDialog("Faltan sprites", "No existe: " + DefeatedSheetPath, "Aceptar");
            return;
        }

        Directory.CreateDirectory(Anim);
        ApplyActionSheetImport(DefeatedSheetPath);
        var sprites = LoadOrderedSheetSprites(DefeatedSheetPath);
        if (sprites.Length < DefeatedFrameCount)
        {
            EditorUtility.DisplayDialog("Sprites incompletos",
                $"En Derrotado hay {sprites.Length} sprites; se necesitan {DefeatedFrameCount}.", "Aceptar");
            return;
        }

        var clipPath = $"{Anim}/Mateo_Derrotado.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        WriteSpriteClip(clip, sprites, ActionFrameRate, false);
        WireMateoDefeated(controller);
        EnsureDerrotadoGuards(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Campus Quest] Mateo: Derrotado en " + Anim);
    }

    static void ApplyActionSheetImport(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            throw new Exception("No se pudo importar " + assetPath);

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 110f;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.isReadable = true;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        importer.SetTextureSettings(settings);

        importer.SaveAndReimport();
        ApplyFootPivotToSheet(assetPath);
    }

    [MenuItem("Campus Quest/Mateo/4 - Pivote y tiempo de golpe/disparo")]
    public static void RefreshActionPivotsAndClipSpeed()
    {
        var prefixes = new[] { "caminataA", "caminataIzq", "caminataD", "caminataUp" };
        for (int d = 0; d < prefixes.Length; d++)
        for (int i = 1; i <= 4; i++)
        {
            var walkPath = $"{Frames}/{prefixes[d]}{i}.png";
            if (File.Exists(walkPath))
                RestoreWalkFramePivot(walkPath);
        }

        foreach (var (_, relativePath) in ActionSheets)
        {
            var path = $"{ActionRoot}/{relativePath}";
            if (!File.Exists(path))
                continue;
            ApplyActionSheetImport(path);
        }

        Directory.CreateDirectory(Anim);
        foreach (var (clipSuffix, relativePath) in ActionSheets)
        {
            var path = $"{ActionRoot}/{relativePath}";
            if (!File.Exists(path))
                continue;

            var sprites = LoadOrderedSheetSprites(path);
            if (sprites.Length < ActionFrameCount)
                continue;

            var clipPath = $"{Anim}/Mateo_{clipSuffix}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, clipPath);
            }

            WriteSpriteClip(clip, sprites, ActionFrameRate, false);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Campus Quest] Mateo: caminata (0.5,0), ancla de piernas en ataque, clips a " + ActionFrameRate + " FPS.");
    }

    static void RestoreWalkFramePivot(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(0.5f, 0f);
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    static Texture2D LoadReadableTexture(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return null;

        if (!importer.isReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
    }

    static void ApplyFootPivotToSheet(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        var texture = LoadReadableTexture(assetPath);
        if (texture == null)
            return;

        importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        var sheet = importer.spritesheet;
        if (sheet == null || sheet.Length == 0)
            return;

        var serialized = new SerializedObject(importer);
        var spritesProp = serialized.FindProperty("m_SpriteSheet.m_Sprites");
        if (spritesProp == null || !spritesProp.isArray)
            return;

        int count = Mathf.Min(sheet.Length, spritesProp.arraySize);
        for (int i = 0; i < count; i++)
        {
            var element = spritesProp.GetArrayElementAtIndex(i);
            if (!TryComputeFootPivot(texture, sheet[i].rect, out var pivot))
                continue;

            var alignment = element.FindPropertyRelative("m_Alignment");
            var pivotProp = element.FindPropertyRelative("m_Pivot");
            if (alignment != null)
                alignment.intValue = (int)SpriteAlignment.Custom;
            if (pivotProp != null)
                pivotProp.vector2Value = pivot;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        importer.SaveAndReimport();
    }

    const float FootAlphaThreshold = 0.1f;
    const float LegBandHeightFraction = 0.35f;

    static bool TryComputeFootPivot(Texture2D texture, Rect rect, out Vector2 pivot)
    {
        pivot = new Vector2(0.5f, 0f);
        if (texture == null || rect.width < 1f || rect.height < 1f)
            return false;

        int x0 = Mathf.FloorToInt(rect.x);
        int y0 = Mathf.FloorToInt(rect.y);
        int w = Mathf.FloorToInt(rect.width);
        int h = Mathf.FloorToInt(rect.height);

        int footRow = -1;
        for (int row = y0; row < y0 + h; row++)
        {
            for (int col = x0; col < x0 + w; col++)
            {
                if (texture.GetPixel(col, row).a > FootAlphaThreshold)
                {
                    footRow = row;
                    break;
                }
            }

            if (footRow >= 0)
                break;
        }

        if (footRow < 0)
            return false;

        int bandRows = Mathf.Max(1, Mathf.CeilToInt(h * LegBandHeightFraction));
        int bandTop = Mathf.Min(y0 + bandRows, y0 + h);

        int minX = x0 + w;
        int maxX = x0;
        bool anyInBand = false;
        for (int row = y0; row < bandTop; row++)
        {
            for (int col = x0; col < x0 + w; col++)
            {
                if (texture.GetPixel(col, row).a <= FootAlphaThreshold)
                    continue;
                anyInBand = true;
                if (col < minX) minX = col;
                if (col > maxX) maxX = col;
            }
        }

        if (!anyInBand)
            return false;

        float centerX = (minX + maxX) * 0.5f;
        pivot = new Vector2((centerX - x0) / w, (footRow - y0) / (float)h);
        return true;
    }

    static Sprite[] LoadOrderedSheetSprites(string assetPath)
    {
        var matches = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>()
            .Select(s => new { Sprite = s, Index = ParseTrailingIndex(s.name) })
            .Where(p => p.Index >= 0)
            .OrderBy(p => p.Index)
            .ToList();
        return matches.Select(p => p.Sprite).ToArray();
    }

    static int ParseTrailingIndex(string spriteName)
    {
        var match = Regex.Match(spriteName, @"_(\d+)$");
        return match.Success && int.TryParse(match.Groups[1].Value, out var index) ? index : -1;
    }

    static void WireActionStates(AnimatorController controller)
    {
        EnsureParameter(controller, "Action", AnimatorControllerParameterType.Int);
        var sm = controller.layers[0].stateMachine;

        RequireLocomotionOnlyWhenIdle(controller, sm);

        var idleByDir = new Dictionary<int, AnimatorState>();
        foreach (var child in sm.states)
        {
            if (child.state.name.StartsWith("Idle_", StringComparison.Ordinal))
            {
                var dirName = child.state.name.Substring("Idle_".Length);
                if (TryDirectionName(dirName, out var dir))
                    idleByDir[dir] = child.state;
            }
        }

        RemoveActionStates(sm);

        var actionSpecs = new[]
        {
            ("Disparo_Down", 1, new[] { 0 }),
            ("Disparo_Side", 1, new[] { 1, 2 }),
            ("Disparo_Up", 1, new[] { 3 }),
            ("Golpe_Down", 2, new[] { 0 }),
            ("Golpe_Side", 2, new[] { 1, 2 }),
            ("Golpe_Up", 2, new[] { 3 })
        };

        foreach (var (stateName, actionValue, directions) in actionSpecs)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{Anim}/Mateo_{stateName}.anim");
            if (clip == null)
                throw new Exception("Falta clip " + stateName);

            var state = sm.AddState(stateName);
            state.motion = clip;

            foreach (var dir in directions)
            {
                var enter = sm.AddAnyStateTransition(state);
                enter.hasExitTime = false;
                enter.duration = 0f;
                enter.canTransitionToSelf = false;
                enter.AddCondition(AnimatorConditionMode.Equals, actionValue, "Action");
                enter.AddCondition(AnimatorConditionMode.IfNot, 0, "Derrotado");
                enter.AddCondition(AnimatorConditionMode.Equals, dir, "Direction");
            }

            foreach (var dir in directions)
            {
                if (!idleByDir.TryGetValue(dir, out var idle))
                    continue;
                var exit = state.AddTransition(idle);
                exit.hasExitTime = true;
                exit.exitTime = 0.95f;
                exit.duration = 0f;
                exit.AddCondition(AnimatorConditionMode.Equals, 0, "Action");
                exit.AddCondition(AnimatorConditionMode.Equals, dir, "Direction");
            }
        }

        EnsureDerrotadoGuards(controller);
        EditorUtility.SetDirty(controller);
    }

    static void WireMateoDefeated(AnimatorController controller)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{Anim}/Mateo_Derrotado.anim");
        if (clip == null)
            return;

        EnsureParameter(controller, "Derrotado", AnimatorControllerParameterType.Bool);
        var sm = controller.layers[0].stateMachine;

        AnimatorState defeated = null;
        foreach (var child in sm.states)
        {
            if (child.state.name == "Derrotado")
            {
                defeated = child.state;
                break;
            }
        }

        if (defeated == null)
        {
            defeated = sm.AddState("Derrotado");
            defeated.motion = clip;
        }
        else
            defeated.motion = clip;

        foreach (var transition in sm.anyStateTransitions)
        {
            if (transition?.destinationState == defeated)
                return;
        }

        var enter = sm.AddAnyStateTransition(defeated);
        enter.hasExitTime = false;
        enter.duration = 0f;
        enter.canTransitionToSelf = false;
        enter.AddCondition(AnimatorConditionMode.If, 0, "Derrotado");
    }

    static void EnsureDerrotadoGuards(AnimatorController controller)
    {
        EnsureParameter(controller, "Derrotado", AnimatorControllerParameterType.Bool);
        var sm = controller.layers[0].stateMachine;

        foreach (var transition in sm.anyStateTransitions)
        {
            if (transition == null || transition.destinationState == null)
                continue;
            var dstName = transition.destinationState.name;
            if (dstName == "Derrotado")
                continue;
            if (!dstName.StartsWith("Idle_", StringComparison.Ordinal) &&
                !dstName.StartsWith("Walk_", StringComparison.Ordinal) &&
                !dstName.StartsWith("Disparo_", StringComparison.Ordinal) &&
                !dstName.StartsWith("Golpe_", StringComparison.Ordinal))
                continue;
            if (HasCondition(transition, "Derrotado", AnimatorConditionMode.IfNot, 0))
                continue;
            transition.AddCondition(AnimatorConditionMode.IfNot, 0, "Derrotado");
        }
    }

    static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in controller.parameters)
        {
            if (p.name == name)
                return;
        }

        controller.AddParameter(name, type);
    }

    static void RequireLocomotionOnlyWhenIdle(AnimatorController controller, AnimatorStateMachine sm)
    {
        foreach (var transition in sm.anyStateTransitions)
        {
            if (transition == null || transition.destinationState == null)
                continue;
            var dstName = transition.destinationState.name;
            if (!dstName.StartsWith("Idle_", StringComparison.Ordinal) &&
                !dstName.StartsWith("Walk_", StringComparison.Ordinal))
                continue;
            if (HasCondition(transition, "Action", AnimatorConditionMode.Equals, 0))
                continue;
            transition.AddCondition(AnimatorConditionMode.Equals, 0, "Action");
        }
    }

    static bool HasCondition(AnimatorStateTransition transition, string param, AnimatorConditionMode mode, float threshold)
    {
        foreach (var condition in transition.conditions)
        {
            if (condition.mode == mode && condition.parameter == param && Mathf.Approximately(condition.threshold, threshold))
                return true;
        }

        return false;
    }

    static void RemoveActionStates(AnimatorStateMachine sm)
    {
        var toRemove = sm.states.Where(s => s.state.name.StartsWith("Disparo_", StringComparison.Ordinal) ||
                                            s.state.name.StartsWith("Golpe_", StringComparison.Ordinal)).ToList();
        foreach (var child in toRemove)
            sm.RemoveState(child.state);
    }

    static bool TryDirectionName(string name, out int direction)
    {
        switch (name)
        {
            case "Down": direction = 0; return true;
            case "Left": direction = 1; return true;
            case "Right": direction = 2; return true;
            case "Up": direction = 3; return true;
            default: direction = 0; return false;
        }
    }

    [MenuItem("Campus Quest/Mateo/2 - Aplicar visual al personaje seleccionado")]
    private static void Apply()
    {
        var target = Selection.activeGameObject;
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        if (target == null || controller == null)
        {
            EditorUtility.DisplayDialog("Seleccion requerida", "Selecciona Mateo_Provisional en Hierarchy y ejecuta primero el paso 1.", "Aceptar");
            return;
        }
        if (!EditorUtility.DisplayDialog("Aplicar solo visual", $"Agregar animaciones a '{target.name}' sin cambiar su movimiento ni colisiones?", "Aplicar", "Cancelar")) return;
        Undo.RegisterFullObjectHierarchyUndo(target, "Crear visual Mateo");
        var child = target.transform.Find("Mateo_Visual");
        if (child == null)
        {
            var go = new GameObject("Mateo_Visual");
            Undo.RegisterCreatedObjectUndo(go, "Crear Mateo_Visual");
            go.transform.SetParent(target.transform, false);
            child = go.transform;
        }
        child.localPosition = Vector3.zero;
        child.localScale = Vector3.one;
        var renderer = child.GetComponent<SpriteRenderer>() ?? Undo.AddComponent<SpriteRenderer>(child.gameObject);
        renderer.sortingOrder = 50;
        var animator = child.GetComponent<Animator>() ?? Undo.AddComponent<Animator>(child.gameObject);
        animator.runtimeAnimatorController = controller;
        var driver = child.GetComponent<CampusQuestMateoVisualDriver>() ?? Undo.AddComponent<CampusQuestMateoVisualDriver>(child.gameObject);
        driver.motionRoot = target.transform;
        // Disable the square's renderer only, preserving scripts, colliders and movement.
        var oldRenderer = target.GetComponent<SpriteRenderer>();
        if (oldRenderer != null) oldRenderer.enabled = false;
        EditorUtility.SetDirty(target);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(target.scene);
        Debug.Log("[Campus Quest] Visual de Mateo aplicado. Guarda la escena (Ctrl+S).");
    }
}
#endif
