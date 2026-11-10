#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SpBosqueAmbushSetup
{
    const string ScenePath = "Assets/Scenes/Sp_Bosque.unity";
    const string SecuazController = "Assets/Animations/Secuaces/Secuaz.controller";
    const string BulletSheet = "Assets/Sprites/Enviroment/Sp/Armas/Bala.png";
    const string HeartsName = "Corazones";
    const string EmboscadaRoot = "Emboscada_1";
    const string TriggerName = "Emboscada_Trigger";

    [MenuItem("Campus Quest/SP/Emboscada 1")]
    public static void SetupEmboscada()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var hearts = GameObject.Find(HeartsName);
        if (hearts != null)
        {
            if (hearts.GetComponent<CampusQuestHeartRegen>() == null)
                Undo.AddComponent<CampusQuestHeartRegen>(hearts);
        }
        else
            Debug.LogWarning("[Campus Quest] No se encontró " + HeartsName + "; ejecuta el menú de corazones primero.");

        var root = GameObject.Find(EmboscadaRoot);
        if (root == null)
        {
            root = new GameObject(EmboscadaRoot);
            Undo.RegisterCreatedObjectUndo(root, EmboscadaRoot);
        }

        var meleSur = EnsureAmbushSecuaz(root.transform, "Emboscada_Mele_Sur", new Vector3(8.5f, 35.5f, 0f), false);
        var meleWest = EnsureAmbushSecuaz(root.transform, "Emboscada_Mele_Oeste", new Vector3(2.5f, 42.5f, 0f), false);
        var shootNorth = EnsureAmbushSecuaz(root.transform, "Emboscada_Tiro_Norte", new Vector3(14.5f, 50.5f, 0f), true);
        var shootEast = EnsureAmbushSecuaz(root.transform, "Emboscada_Tiro_Este", new Vector3(22.5f, 46.5f, 0f), true);

        var triggerGo = GameObject.Find(TriggerName);
        if (triggerGo == null)
        {
            triggerGo = new GameObject(TriggerName);
            Undo.RegisterCreatedObjectUndo(triggerGo, TriggerName);
        }

        triggerGo.transform.position = new Vector3(24f, 33.5f, 0f);
        var box = triggerGo.GetComponent<BoxCollider2D>();
        if (box == null)
            box = triggerGo.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(8f, 1.5f);

        var ambush = triggerGo.GetComponent<CampusQuestAmbush>();
        if (ambush == null)
            ambush = triggerGo.AddComponent<CampusQuestAmbush>();
        var so = new SerializedObject(ambush);
        var enemies = so.FindProperty("ambushEnemies");
        enemies.arraySize = 4;
        enemies.GetArrayElementAtIndex(0).objectReferenceValue = meleSur;
        enemies.GetArrayElementAtIndex(1).objectReferenceValue = meleWest;
        enemies.GetArrayElementAtIndex(2).objectReferenceValue = shootNorth;
        enemies.GetArrayElementAtIndex(3).objectReferenceValue = shootEast;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Campus Quest] Emboscada 1: 4 secuaces (apagados), trigger en y=33.5 y regeneración en Corazones.");
    }

    [MenuItem("Campus Quest/SP/Emboscada 2")]
    public static void SetupEmboscada2()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        const string rootName = "Emboscada_2";
        var root = GameObject.Find(rootName);
        if (root == null)
        {
            root = new GameObject(rootName);
            Undo.RegisterCreatedObjectUndo(root, rootName);
        }

        var meleSouth = EnsureAmbushSecuaz(root.transform, "Emboscada2_Mele_Sur", new Vector3(15.5f, 64.5f, 0f), false);
        var meleWest = EnsureAmbushSecuaz(root.transform, "Emboscada2_Mele_Oeste", new Vector3(4.5f, 69.5f, 0f), false);
        var shootEast = EnsureAmbushSecuaz(root.transform, "Emboscada2_Tiro_Este", new Vector3(26.5f, 69.5f, 0f), true);
        var shootNorth = EnsureAmbushSecuaz(root.transform, "Emboscada2_Tiro_Norte", new Vector3(26.5f, 73.5f, 0f), true);

        const string triggerName = "Emboscada_2_Trigger";
        var triggerTransform = root.transform.Find(triggerName);
        GameObject triggerGo;
        if (triggerTransform == null)
        {
            triggerGo = new GameObject(triggerName);
            Undo.RegisterCreatedObjectUndo(triggerGo, triggerName);
            triggerGo.transform.SetParent(root.transform, false);
        }
        else
            triggerGo = triggerTransform.gameObject;

        triggerGo.transform.position = new Vector3(11.5f, 67f, 0f);
        var box = triggerGo.GetComponent<BoxCollider2D>();
        if (box == null)
            box = triggerGo.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(8f, 1.2f);

        var ambush = triggerGo.GetComponent<CampusQuestAmbush>();
        if (ambush == null)
            ambush = triggerGo.AddComponent<CampusQuestAmbush>();
        var so = new SerializedObject(ambush);
        var enemies = so.FindProperty("ambushEnemies");
        enemies.arraySize = 4;
        enemies.GetArrayElementAtIndex(0).objectReferenceValue = meleSouth;
        enemies.GetArrayElementAtIndex(1).objectReferenceValue = meleWest;
        enemies.GetArrayElementAtIndex(2).objectReferenceValue = shootEast;
        enemies.GetArrayElementAtIndex(3).objectReferenceValue = shootNorth;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Campus Quest] Emboscada 2: 4 secuaces (apagados) y trigger en y=67.");
    }

    static GameObject EnsureAmbushSecuaz(Transform parent, string name, Vector3 position, bool usePistol)
    {
        var existing = parent.Find(name);
        GameObject go;
        if (existing != null)
            go = existing.gameObject;
        else
        {
            go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, name);
            go.transform.SetParent(parent, false);
        }

        go.transform.position = position;
        SpBosqueCollisionSetup.EnsureSecuazPhysics(go);

        var loot = go.GetComponent<CampusQuestSecuazLoot>();
        if (loot != null)
            Undo.DestroyObjectImmediate(loot);

        var visual = go.transform.Find("Secuaz_Visual");
        if (visual == null)
        {
            var visualGo = new GameObject("Secuaz_Visual", typeof(SpriteRenderer), typeof(Animator));
            Undo.RegisterCreatedObjectUndo(visualGo, "Secuaz_Visual");
            visualGo.transform.SetParent(go.transform, false);
            visual = visualGo.transform;
        }
        else
        {
            if (visual.GetComponent<SpriteRenderer>() == null)
                Undo.AddComponent<SpriteRenderer>(visual.gameObject);
            if (visual.GetComponent<Animator>() == null)
                Undo.AddComponent<Animator>(visual.gameObject);
        }

        var renderer = visual.GetComponent<SpriteRenderer>();
        renderer.sortingOrder = 45;

        var animator = visual.GetComponent<Animator>();
        animator.runtimeAnimatorController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SecuazController);

        CampusQuestSecuazVisualDriver driver;
        if (visual.GetComponent<CampusQuestSecuazVisualDriver>() == null)
            driver = Undo.AddComponent<CampusQuestSecuazVisualDriver>(visual.gameObject);
        else
            driver = visual.GetComponent<CampusQuestSecuazVisualDriver>();
        driver.motionRoot = go.transform;

        var visualSo = new SerializedObject(driver);
        visualSo.FindProperty("armaInicial").intValue = usePistol
            ? CampusQuestSecuazVisualDriver.ArmaPistola
            : CampusQuestSecuazVisualDriver.ArmaTolete;
        visualSo.ApplyModifiedPropertiesWithoutUndo();

        var brain = go.GetComponent<CampusQuestSecuazBrain>() ?? Undo.AddComponent<CampusQuestSecuazBrain>(go);
        var brainSo = new SerializedObject(brain);
        brainSo.FindProperty("visual").objectReferenceValue = driver;
        var bullet = LoadBulletSprite();
        if (bullet != null)
            brainSo.FindProperty("bulletSprite").objectReferenceValue = bullet;
        brainSo.ApplyModifiedPropertiesWithoutUndo();

        var keycard = go.GetComponent<CampusQuestAmbushKeycard>();
        if (keycard == null)
        {
            keycard = Undo.AddComponent<CampusQuestAmbushKeycard>(go);
            if (keycard == null)
                keycard = go.AddComponent<CampusQuestAmbushKeycard>();
        }

        var keySo = new SerializedObject(keycard);
        keySo.FindProperty("secuaz").objectReferenceValue = driver;
        keySo.ApplyModifiedPropertiesWithoutUndo();

        var sheet = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Characters/Secuaces/Secueces.png")
            .OfType<Sprite>()
            .FirstOrDefault(s => s.name == "Secueces_0");
        if (renderer.sprite == null && sheet != null)
            renderer.sprite = sheet;

        go.SetActive(false);
        return go;
    }

    static Sprite LoadBulletSprite() =>
        AssetDatabase.LoadAllAssetsAtPath(BulletSheet).OfType<Sprite>().FirstOrDefault(s => s.name == "Bala_0");
}
#endif
