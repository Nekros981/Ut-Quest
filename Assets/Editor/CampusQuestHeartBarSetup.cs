#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class CampusQuestHeartBarSetup
{
    const string ScenePath = "Assets/Scenes/Sp_Bosque.unity";
    const string UiBarsSheet =
        "Assets/Sprites/Enviroment/Sp/Top-Down RPG 32x32 by Mixel v1.7/User Interface v1.0/Topdown RPG 32x32 - UI Bars.PNG";
    const string HeartFullName = "Topdown RPG 32x32 - UI Bars_448";
    const string HeartEmptyName = "Topdown RPG 32x32 - UI Bars_396";
    const string RootName = "CampusQuest_HUD";
    const string HeartsName = "Corazones";
    const string DefeatLabelName = "Derrotas";
    const string PixeloidFontPath = "Assets/Fonts/PixeloidSans SDF.asset";

    [MenuItem("Campus Quest/SP/Corazones arriba izquierda")]
    public static void SetupHeartBar()
    {
        var importer = AssetImporter.GetAtPath(UiBarsSheet) as TextureImporter;
        if (importer != null)
        {
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EnsureEventSystem();

        var hudRoot = GameObject.Find(RootName);
        if (hudRoot == null)
        {
            hudRoot = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(hudRoot, RootName);
        }

        if (!hudRoot.TryGetComponent<Canvas>(out var canvas))
            canvas = Undo.AddComponent<Canvas>(hudRoot);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        if (!hudRoot.TryGetComponent<CanvasScaler>(out _))
        {
            var scaler = Undo.AddComponent<CanvasScaler>(hudRoot);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (!hudRoot.TryGetComponent<GraphicRaycaster>(out _))
            Undo.AddComponent<GraphicRaycaster>(hudRoot);

        var heartsRoot = hudRoot.transform.Find(HeartsName)?.gameObject;
        if (heartsRoot == null)
        {
            heartsRoot = new GameObject(HeartsName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(heartsRoot, HeartsName);
            heartsRoot.transform.SetParent(hudRoot.transform, false);
        }

        var heartsRect = heartsRoot.GetComponent<RectTransform>();
        heartsRect.anchorMin = new Vector2(0f, 1f);
        heartsRect.anchorMax = new Vector2(0f, 1f);
        heartsRect.pivot = new Vector2(0f, 1f);
        heartsRect.anchoredPosition = new Vector2(16f, -16f);
        heartsRect.sizeDelta = new Vector2(420f, 56f);

        var layout = heartsRoot.GetComponent<HorizontalLayoutGroup>() ??
                     Undo.AddComponent<HorizontalLayoutGroup>(heartsRoot);
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var heartFull = LoadSprite(HeartFullName);
        var heartEmpty = LoadSprite(HeartEmptyName);
        if (heartFull == null || heartEmpty == null)
        {
            EditorUtility.DisplayDialog("Corazones", "No se encontraron los sprites " + HeartFullName + " / " + HeartEmptyName, "Aceptar");
            return;
        }

        const int heartCount = 6;
        var images = new Image[heartCount];
        for (int i = 0; i < heartCount; i++)
        {
            var childName = "Corazon_" + (i + 1);
            var child = heartsRoot.transform.Find(childName)?.gameObject;
            if (child == null)
            {
                child = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(child, childName);
                child.transform.SetParent(heartsRoot.transform, false);
            }

            var image = child.GetComponent<Image>();
            image.sprite = heartFull;
            image.preserveAspect = true;
            image.raycastTarget = false;

            var rt = child.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(48f, 48f);

            var layoutElement = child.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(child);
            layoutElement.preferredWidth = 48f;
            layoutElement.preferredHeight = 48f;

            images[i] = image;
        }

        var defeatLabel = EnsureDefeatLabel(heartsRoot.transform);
        var counter = heartsRoot.GetComponent<CampusQuestDefeatCounter>() ??
                      Undo.AddComponent<CampusQuestDefeatCounter>(heartsRoot);
        var counterSo = new SerializedObject(counter);
        counterSo.FindProperty("label").objectReferenceValue = defeatLabel;
        counterSo.ApplyModifiedPropertiesWithoutUndo();

        var bar = heartsRoot.GetComponent<CampusQuestHeartBar>() ?? Undo.AddComponent<CampusQuestHeartBar>(heartsRoot);
        var so = new SerializedObject(bar);
        so.FindProperty("maxHearts").intValue = heartCount;
        so.FindProperty("heartFull").objectReferenceValue = heartFull;
        so.FindProperty("heartEmpty").objectReferenceValue = heartEmpty;
        so.FindProperty("heartImages").arraySize = heartCount;
        for (int i = 0; i < heartCount; i++)
            so.FindProperty("heartImages").GetArrayElementAtIndex(i).objectReferenceValue = images[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Campus Quest] Seis corazones en Sp_Bosque (HUD arriba izquierda).");
    }

    static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
            return;

        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        Undo.RegisterCreatedObjectUndo(es, "EventSystem");
    }

    static Sprite LoadSprite(string spriteName)
    {
        return AssetDatabase.LoadAllAssetsAtPath(UiBarsSheet).OfType<Sprite>()
            .FirstOrDefault(s => s.name == spriteName);
    }

    static TMP_Text EnsureDefeatLabel(Transform parent)
    {
        var child = parent.Find(DefeatLabelName)?.gameObject;
        if (child == null)
        {
            child = new GameObject(DefeatLabelName, typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(child, DefeatLabelName);
            child.transform.SetParent(parent, false);
        }

        var layout = child.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(child);
        layout.preferredWidth = 90f;
        layout.preferredHeight = 48f;

        var tmp = child.GetComponent<TextMeshProUGUI>();
        tmp.text = "x00";
        tmp.fontSize = 32f;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;
        tmp.color = Color.white;
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixeloidFontPath);
        if (font != null)
        {
            tmp.font = font;
            if (font.material != null)
                tmp.fontSharedMaterial = font.material;
        }

        return tmp;
    }
}
#endif
