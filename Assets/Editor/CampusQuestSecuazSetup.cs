#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class CampusQuestSecuazSetup
{
    const string SheetPath = "Assets/Sprites/Characters/Secuaces/Secueces.png";
    const string DefeatedSheetPath = "Assets/Sprites/Characters/Secuaces/Secas_Derrotado.png";
    const string Anim = "Assets/Animations/Secuaces";
    const string PlayerSecuazAnim = "Assets/Animations/Player/Secuaces";
    const string ControllerPath = Anim + "/Secuaz.controller";
    const float WalkFrameRate = 8f;
    const float ActionFrameRate = 10f;

    static readonly int[] WalkDown = { 0, 1, 2, 3 };
    static readonly int[] WalkUp = { 8, 9, 10, 11 };
    static readonly int[] WalkSide = { 16, 17, 18, 19 };
    static readonly int[] GolpeDown = { 4, 5, 6, 7 };
    static readonly int[] GolpeUp = { 12, 13, 14, 15 };
    static readonly int[] GolpeSide = { 20, 21, 22, 23 };
    static readonly int[] DisparoDown = { 36, 37, 38, 39 };
    static readonly int[] DisparoUp = { 44, 45, 46, 47 };
    static readonly int[] DisparoSide = { 52, 53, 54, 55 };
    static readonly int[] DefeatedFrames = { 0, 1, 2, 3, 4, 5, 6, 7, 9 };

    [MenuItem("Campus Quest/Secuaces/Crear animaciones y controller")]
    public static void GenerateAll()
    {
        if (!File.Exists(SheetPath))
        {
            EditorUtility.DisplayDialog("Falta spritesheet", "No existe: " + SheetPath, "Aceptar");
            return;
        }

        Directory.CreateDirectory(Anim);
        ApplySheetImport(SheetPath);
        var all = LoadOrderedSheetSprites(SheetPath);
        if (all.Length < 56)
        {
            EditorUtility.DisplayDialog("Spritesheet incompleto",
                $"Se esperaban al menos 56 sprites; hay {all.Length}.", "Aceptar");
            return;
        }

        Sprite[] Pick(int[] indices) => indices.Select(i => all[i]).ToArray();

        MakeClip("Secuaz_Walk_Down", Pick(WalkDown), WalkFrameRate, true);
        MakeClip("Secuaz_Walk_Up", Pick(WalkUp), WalkFrameRate, true);
        MakeClip("Secuaz_Walk_Side", Pick(WalkSide), WalkFrameRate, true);
        MakeClip("Secuaz_Idle_Down", new[] { all[WalkDown[0]] }, WalkFrameRate, false);
        MakeClip("Secuaz_Idle_Up", new[] { all[WalkUp[0]] }, WalkFrameRate, false);
        MakeClip("Secuaz_Idle_Side", new[] { all[WalkSide[0]] }, WalkFrameRate, false);

        MakeClip("Secuaz_Golpe_Down", Pick(GolpeDown), ActionFrameRate, false);
        MakeClip("Secuaz_Golpe_Up", Pick(GolpeUp), ActionFrameRate, false);
        MakeClip("Secuaz_Golpe_Side", Pick(GolpeSide), ActionFrameRate, false);
        MakeClip("Secuaz_Disparo_Down", Pick(DisparoDown), ActionFrameRate, false);
        MakeClip("Secuaz_Disparo_Up", Pick(DisparoUp), ActionFrameRate, false);
        MakeClip("Secuaz_Disparo_Side", Pick(DisparoSide), ActionFrameRate, false);

        GenerateDefeatedClip();

        BuildController();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Campus Quest] Secuaces: clips y Secuaz.controller en " + Anim);
    }

    [MenuItem("Campus Quest/Secuaces/Crear animacion derrotado")]
    public static void GenerateDefeatedOnly()
    {
        if (GenerateDefeatedClip())
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller != null)
                WireDefeatedState(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    static bool GenerateDefeatedClip()
    {
        if (!File.Exists(DefeatedSheetPath))
        {
            EditorUtility.DisplayDialog("Falta spritesheet", "No existe: " + DefeatedSheetPath, "Aceptar");
            return false;
        }

        Directory.CreateDirectory(PlayerSecuazAnim);
        ApplySheetImport(DefeatedSheetPath);
        var sprites = LoadOrderedSheetSprites(DefeatedSheetPath);
        if (sprites.Length < 10)
        {
            EditorUtility.DisplayDialog("Spritesheet incompleto",
                $"En derrotado hay {sprites.Length} sprites; se necesitan 10.", "Aceptar");
            return false;
        }

        var frames = DefeatedFrames.Select(i => sprites[i]).ToArray();
        MakeClipIn(PlayerSecuazAnim, "Secuaz_Derrotado", frames, ActionFrameRate, false);
        Debug.Log("[Campus Quest] Secuaz_Derrotado en " + PlayerSecuazAnim);
        return true;
    }

    static void ApplySheetImport(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            throw new Exception("No se pudo importar " + assetPath);

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 32f;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(0.5f, 0f);
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    static Sprite[] LoadOrderedSheetSprites(string assetPath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>()
            .Select(s => new { Sprite = s, Index = ParseTrailingIndex(s.name) })
            .Where(p => p.Index >= 0)
            .OrderBy(p => p.Index)
            .Select(p => p.Sprite)
            .ToArray();
    }

    static int ParseTrailingIndex(string spriteName)
    {
        var match = Regex.Match(spriteName, @"_(\d+)$");
        return match.Success && int.TryParse(match.Groups[1].Value, out var index) ? index : -1;
    }

    static void MakeClip(string name, Sprite[] frames, float frameRate, bool loop) =>
        MakeClipIn(Anim, name, frames, frameRate, loop);

    static void MakeClipIn(string folder, string name, Sprite[] frames, float frameRate, bool loop)
    {
        var path = $"{folder}/{name}.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }

        clip.frameRate = frameRate;
        var keyframes = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
            keyframes[i] = new ObjectReferenceKeyframe { time = i / frameRate, value = frames[i] };
        AnimationUtility.SetObjectReferenceCurve(clip,
            EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keyframes);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
    }

    static void BuildController()
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            AssetDatabase.DeleteAsset(ControllerPath);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Direction", AnimatorControllerParameterType.Int);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Action", AnimatorControllerParameterType.Int);
        controller.AddParameter("Arma", AnimatorControllerParameterType.Int);
        controller.AddParameter("Derrotado", AnimatorControllerParameterType.Bool);

        var sm = controller.layers[0].stateMachine;
        var idleByDir = new Dictionary<int, AnimatorState>();

        void AddLocomotion(string suffix, int dir, bool moving)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                $"{Anim}/Secuaz_{(moving ? "Walk" : "Idle")}_{suffix}.anim");
            var state = sm.AddState($"{(moving ? "Walk" : "Idle")}_{suffix}");
            state.motion = clip;
            if (suffix == "Down" && !moving)
                sm.defaultState = state;
            if (!moving && suffix != "Side")
                idleByDir[dir] = state;
            if (!moving && suffix == "Side")
            {
                idleByDir[1] = state;
                idleByDir[2] = state;
            }

            var transition = sm.AddAnyStateTransition(state);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.Equals, 0, "Action");
            transition.AddCondition(AnimatorConditionMode.IfNot, 0, "Derrotado");
            transition.AddCondition(moving ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "IsMoving");
            if (suffix == "Side")
            {
                transition.AddCondition(AnimatorConditionMode.Equals, 1, "Direction");
                var transitionRight = sm.AddAnyStateTransition(state);
                transitionRight.hasExitTime = false;
                transitionRight.duration = 0f;
                transitionRight.canTransitionToSelf = false;
                transitionRight.AddCondition(AnimatorConditionMode.Equals, 0, "Action");
                transitionRight.AddCondition(AnimatorConditionMode.IfNot, 0, "Derrotado");
                transitionRight.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
                transitionRight.AddCondition(AnimatorConditionMode.Equals, 2, "Direction");
                if (moving)
                {
                    var walkRight = sm.AddAnyStateTransition(state);
                    walkRight.hasExitTime = false;
                    walkRight.duration = 0f;
                    walkRight.canTransitionToSelf = false;
                    walkRight.AddCondition(AnimatorConditionMode.Equals, 0, "Action");
                    walkRight.AddCondition(AnimatorConditionMode.IfNot, 0, "Derrotado");
                    walkRight.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
                    walkRight.AddCondition(AnimatorConditionMode.Equals, 2, "Direction");
                }
            }
            else
            {
                transition.AddCondition(AnimatorConditionMode.Equals, dir, "Direction");
            }
        }

        AddLocomotion("Down", 0, false);
        AddLocomotion("Down", 0, true);
        AddLocomotion("Up", 3, false);
        AddLocomotion("Up", 3, true);
        AddLocomotion("Side", 1, false);
        AddLocomotion("Side", 1, true);

        var actionSpecs = new[]
        {
            ("Golpe_Down", 1, 0, new[] { 0 }),
            ("Golpe_Side", 1, 0, new[] { 1, 2 }),
            ("Golpe_Up", 1, 0, new[] { 3 }),
            ("Disparo_Down", 2, 1, new[] { 0 }),
            ("Disparo_Side", 2, 1, new[] { 1, 2 }),
            ("Disparo_Up", 2, 1, new[] { 3 })
        };

        foreach (var (stateName, actionValue, armaValue, directions) in actionSpecs)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{Anim}/Secuaz_{stateName}.anim");
            var state = sm.AddState(stateName);
            state.motion = clip;

            foreach (var dir in directions)
            {
                var enter = sm.AddAnyStateTransition(state);
                enter.hasExitTime = false;
                enter.duration = 0f;
                enter.canTransitionToSelf = false;
                enter.AddCondition(AnimatorConditionMode.Equals, actionValue, "Action");
                enter.AddCondition(AnimatorConditionMode.Equals, armaValue, "Arma");
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

        WireDefeatedState(controller);
    }

    static void WireDefeatedState(AnimatorController controller)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{PlayerSecuazAnim}/Secuaz_Derrotado.anim");
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
        EditorUtility.SetDirty(controller);
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
}
#endif
