#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class CampusQuestBossSetup
{
    const string MoveSheet = "Assets/Sprites/Bosses/SP/Move_Boss.png";
    const string GrenadeSheet = "Assets/Sprites/Bosses/SP/Throw_Granade.png";
    const string DefeatedSheet = "Assets/Sprites/Bosses/SP/Fallen_Coruption.png";
    const string Anim = "Assets/Animations/Bosses";
    const string ControllerPath = Anim + "/Boss.controller";
    const float WalkFrameRate = 8f;
    const float ActionFrameRate = 10f;
    const float DefeatedFrameRate = 12f;

    static readonly int[] WalkDown = { 0, 1, 2, 3 };
    static readonly int[] WalkUp = { 8, 9, 10, 11 };
    static readonly int[] WalkSide = { 16, 17, 18, 19 };
    static readonly int[] GolpeDown = { 4, 5, 6, 7 };
    static readonly int[] GolpeUp = { 12, 13, 14, 15 };
    static readonly int[] GolpeSide = { 20, 21, 22 };
    static readonly int[] GranadaDown = { 0, 1, 2, 3, 4, 5 };
    static readonly int[] GranadaUp = { 6, 7, 8, 9 };
    static readonly int[] GranadaSide = { 10, 11, 12, 13, 14 };

    [MenuItem("Campus Quest/SP/Animaciones del jefe")]
    public static void GenerateAll()
    {
        if (!File.Exists(MoveSheet) || !File.Exists(GrenadeSheet) || !File.Exists(DefeatedSheet))
        {
            EditorUtility.DisplayDialog("Boss SP", "Faltan Move_Boss, Throw_Granade o Fallen_Coruption en Sprites/Bosses/SP.", "Aceptar");
            return;
        }

        Directory.CreateDirectory(Anim);
        ApplyFeetPivotImport(MoveSheet);
        ApplyFeetPivotImport(GrenadeSheet);
        ApplyFeetPivotImport(DefeatedSheet);

        var move = LoadOrderedSheetSprites(MoveSheet);
        var grenade = LoadOrderedSheetSprites(GrenadeSheet);
        var fallen = LoadOrderedSheetSprites(DefeatedSheet);

        if (move.Length < 23)
        {
            EditorUtility.DisplayDialog("Boss SP", $"Move_Boss tiene {move.Length} sprites; se necesitan al menos 23.", "Aceptar");
            return;
        }

        if (grenade.Length < 15)
        {
            EditorUtility.DisplayDialog("Boss SP", $"Throw_Granade tiene {grenade.Length} sprites; se necesitan al menos 15.", "Aceptar");
            return;
        }

        if (fallen.Length < 1)
        {
            EditorUtility.DisplayDialog("Boss SP", "Fallen_Coruption sin sprites.", "Aceptar");
            return;
        }

        Sprite[] Pick(Sprite[] all, int[] indices) => indices.Select(i => all[i]).ToArray();

        MakeClip("Boss_Walk_Down", Pick(move, WalkDown), WalkFrameRate, true);
        MakeClip("Boss_Walk_Up", Pick(move, WalkUp), WalkFrameRate, true);
        MakeClip("Boss_Walk_Side", Pick(move, WalkSide), WalkFrameRate, true);
        MakeClip("Boss_Idle_Down", new[] { move[WalkDown[0]] }, WalkFrameRate, false);
        MakeClip("Boss_Idle_Up", new[] { move[WalkUp[0]] }, WalkFrameRate, false);
        MakeClip("Boss_Idle_Side", new[] { move[WalkSide[0]] }, WalkFrameRate, false);

        MakeClip("Boss_Golpe_Down", Pick(move, GolpeDown), ActionFrameRate, false);
        MakeClip("Boss_Golpe_Up", Pick(move, GolpeUp), ActionFrameRate, false);
        MakeClip("Boss_Golpe_Side", Pick(move, GolpeSide), ActionFrameRate, false);

        MakeClip("Boss_Granada_Down", Pick(grenade, GranadaDown), ActionFrameRate, false);
        MakeClip("Boss_Granada_Up", Pick(grenade, GranadaUp), ActionFrameRate, false);
        MakeClip("Boss_Granada_Side", Pick(grenade, GranadaSide), ActionFrameRate, false);

        MakeClip("Boss_Derrotado", fallen, DefeatedFrameRate, false);

        BuildController();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Campus Quest] Boss: clips y Boss.controller en " + Anim);
    }

    static void ApplyFeetPivotImport(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException("No se pudo importar " + assetPath);

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

    static Sprite[] LoadOrderedSheetSprites(string assetPath) =>
        AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>()
            .Select(s => new { Sprite = s, Index = ParseTrailingIndex(s.name) })
            .Where(p => p.Index >= 0)
            .OrderBy(p => p.Index)
            .Select(p => p.Sprite)
            .ToArray();

    static int ParseTrailingIndex(string spriteName)
    {
        var match = Regex.Match(spriteName, @"_(\d+)$");
        return match.Success && int.TryParse(match.Groups[1].Value, out var index) ? index : -1;
    }

    static void MakeClip(string name, Sprite[] frames, float frameRate, bool loop)
    {
        var path = $"{Anim}/{name}.anim";
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
        controller.AddParameter("Derrotado", AnimatorControllerParameterType.Bool);

        var sm = controller.layers[0].stateMachine;
        var idleByDir = new Dictionary<int, AnimatorState>();

        void AddLocomotion(string suffix, int dir, bool moving)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                $"{Anim}/Boss_{(moving ? "Walk" : "Idle")}_{suffix}.anim");
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

        var actionSpecs = new (string stateName, int actionValue, int[] directions)[]
        {
            ("Golpe_Down", 1, new[] { 0 }),
            ("Golpe_Side", 1, new[] { 1, 2 }),
            ("Golpe_Up", 1, new[] { 3 }),
            ("Granada_Down", 2, new[] { 0 }),
            ("Granada_Side", 2, new[] { 1, 2 }),
            ("Granada_Up", 2, new[] { 3 })
        };

        foreach (var (stateName, actionValue, directions) in actionSpecs)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{Anim}/Boss_{stateName}.anim");
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

        WireDefeatedState(controller);
    }

    static void WireDefeatedState(AnimatorController controller)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{Anim}/Boss_Derrotado.anim");
        if (clip == null)
            return;

        var sm = controller.layers[0].stateMachine;
        var defeated = sm.AddState("Derrotado");
        defeated.motion = clip;

        var enter = sm.AddAnyStateTransition(defeated);
        enter.hasExitTime = false;
        enter.duration = 0f;
        enter.canTransitionToSelf = false;
        enter.AddCondition(AnimatorConditionMode.If, 0, "Derrotado");
        EditorUtility.SetDirty(controller);
    }
}
#endif
