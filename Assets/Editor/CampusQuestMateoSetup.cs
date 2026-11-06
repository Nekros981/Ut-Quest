#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class CampusQuestMateoSetup
{
    private const string Root = "Assets/Sprites/Characters/Mateo";
    private const string Frames = Root + "/Frames";
    private const string Anim = "Assets/Animations/Player/Mateo";
    private const string ControllerPath = Anim + "/Mateo.controller";

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
        clip.frameRate = 8;
        var keyframes = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
            keyframes[i] = new ObjectReferenceKeyframe { time = i / 8f, value = frames[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keyframes);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
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
