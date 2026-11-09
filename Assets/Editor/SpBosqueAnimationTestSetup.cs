#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SpBosqueAnimationTestSetup
{
    const string ScenePath = "Assets/Scenes/Sp_Bosque.unity";
    const string ToleteSheet = "Assets/Sprites/Enviroment/Sp/Armas/Toletes.png";
    const string MateoController = "Assets/Animations/Player/Mateo/Mateo.controller";
    const string MateoIdleSprite = "Assets/Sprites/Characters/Mateo/Frames/caminataA1.png";
    const string SecuazController = "Assets/Animations/Secuaces/Secuaz.controller";

    [MenuItem("Campus Quest/SP/Preparar prueba de animaciones")]
    public static void SetupTest()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var root = GameObject.Find("SP_AnimationTest");
        if (root == null)
        {
            root = new GameObject("SP_AnimationTest");
            Undo.RegisterCreatedObjectUndo(root, "SP_AnimationTest");
        }

        var toleteMarker = FindMarker("SP_Tolete", new Vector3(12.5f, 11.5f, 0f));
        EnsureGroundTolete(toleteMarker.position);

        var mateo = EnsureMateo(new Vector3(24.5f, 2f, 0f));
        var secuazLeft = EnsureSecuaz("Secuaz_Izq", new Vector3(20.5f, 6f, 0f));
        var secuazRight = EnsureSecuaz("Secuaz_Der", new Vector3(28.5f, 6f, 0f));

        var test = root.GetComponent<SpBosqueAnimationTest>() ?? Undo.AddComponent<SpBosqueAnimationTest>(root);
        test.mateo = mateo.GetComponentInChildren<CampusQuestMateoVisualDriver>();
        test.secuazLeft = secuazLeft.GetComponentInChildren<CampusQuestSecuazVisualDriver>();
        test.secuazRight = secuazRight.GetComponentInChildren<CampusQuestSecuazVisualDriver>();

        var cam = Camera.main;
        if (cam != null)
        {
            var follow = cam.GetComponent<SpCameraFollow>() ?? Undo.AddComponent<SpCameraFollow>(cam.gameObject);
            follow.target = mateo.transform;
            follow.smoothTime = 0.12f;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Campus Quest: prueba lista (WASD+Shift Mateo, clic golpe/disparo, F acción, J/K secuaces).");
    }

    static Transform FindMarker(string name, Vector3 fallback)
    {
        var markers = GameObject.Find("SP_MapMarkers");
        if (markers == null)
            return null;

        var child = markers.transform.Find(name);
        if (child != null)
            return child;

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, name);
        go.transform.SetParent(markers.transform, false);
        go.transform.position = fallback;
        return go.transform;
    }

    static void EnsureGroundTolete(Vector3 position)
    {
        const string name = "SP_Tolete_Pickup";
        var existing = GameObject.Find(name);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);

        var sprite = LoadToleteSprite(36);
        if (sprite == null)
        {
            Debug.LogError("Campus Quest: no se encontró Toletes_36.");
            return;
        }

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, name);
        go.transform.position = position;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 15;
    }

    static Sprite LoadToleteSprite(int index)
    {
        return AssetDatabase.LoadAllAssetsAtPath(ToleteSheet).OfType<Sprite>()
            .FirstOrDefault(s => s.name == "Toletes_" + index);
    }

    static GameObject EnsureMateo(Vector3 position)
    {
        const string name = "Mateo_Provisional";
        var go = GameObject.Find(name);
        if (go == null)
        {
            go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, name);
        }

        go.transform.position = position;

        SpBosqueCollisionSetup.EnsureMateoPhysics(go);

        var visual = go.transform.Find("Mateo_Visual");
        if (visual == null)
        {
            var visualGo = new GameObject("Mateo_Visual");
            Undo.RegisterCreatedObjectUndo(visualGo, "Mateo_Visual");
            visualGo.transform.SetParent(go.transform, false);
            visual = visualGo.transform;
        }

        var renderer = EnsureComponent<SpriteRenderer>(visual.gameObject);
        renderer.sortingOrder = 50;
        if (renderer.sprite == null)
        {
            var idle = AssetDatabase.LoadAssetAtPath<Sprite>(MateoIdleSprite);
            if (idle != null)
                renderer.sprite = idle;
        }

        var animator = EnsureComponent<Animator>(visual.gameObject);
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MateoController);

        var driver = EnsureComponent<CampusQuestMateoVisualDriver>(visual.gameObject);
        driver.motionRoot = go.transform;

        var squareRenderer = go.GetComponent<SpriteRenderer>();
        if (squareRenderer != null)
            squareRenderer.enabled = false;

        return go;
    }

    static GameObject EnsureSecuaz(string name, Vector3 position)
    {
        var go = GameObject.Find(name);
        if (go == null)
        {
            go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, name);
        }

        go.transform.position = position;

        var visual = go.transform.Find("Secuaz_Visual");
        if (visual == null)
        {
            var visualGo = new GameObject("Secuaz_Visual");
            Undo.RegisterCreatedObjectUndo(visualGo, "Secuaz_Visual");
            visualGo.transform.SetParent(go.transform, false);
            visual = visualGo.transform;
        }

        var renderer = EnsureComponent<SpriteRenderer>(visual.gameObject);
        renderer.sortingOrder = 45;

        var animator = EnsureComponent<Animator>(visual.gameObject);
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SecuazController);

        var driver = EnsureComponent<CampusQuestSecuazVisualDriver>(visual.gameObject);
        driver.motionRoot = go.transform;

        var sheet = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Characters/Secuaces/Secueces.png").OfType<Sprite>()
            .FirstOrDefault(s => s.name == "Secueces_0");
        if (renderer.sprite == null && sheet != null)
            renderer.sprite = sheet;

        return go;
    }

    static T EnsureComponent<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        if (c != null)
            return c;

        c = go.AddComponent<T>();
        Undo.RegisterCreatedObjectUndo(c, typeof(T).Name);
        return c;
    }
}
#endif
