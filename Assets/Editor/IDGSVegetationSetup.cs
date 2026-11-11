#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CampusQuest.IDGS;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CampusQuest.EditorTools
{
    public static class IDGSVegetationSetup
    {
        public const string SpritePath = "Assets/Sprites/Enviroment/IDGS/Vegetacion/vegeta.png";
        public const string PrefabsPath = "Assets/Prefabs/Enviroment/IDGS/Vegetacion";
        public const string ScenePath = "Assets/Scenes/IDGS_Prueba_Vegetacion.unity";
        public const string PlayerPath = "Assets/Prefabs/Characters/IDGS/Mateo_Prueba_Vegetacion.prefab";
        private const string MaterialPath = "Assets/Materials/IDGS";
        private const string DataPath = "Assets/Data/IDGS/PruebaVegetacion";
        private static Material spriteMaterial;
        private static TMP_FontAsset font;
        private static readonly Color Gold = new Color32(233, 216, 166, 255);

        public static string SpriteName(int row, int column) => "Vegetacion_R" + (row + 1).ToString("00") + "_C" + (column + 1).ToString("00")
            + (row == 7 && column == 7 ? "_Vacia" : "");

        [MenuItem("Campus Quest/IDGS/Preparar prueba de vegetación")]
        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Detén Play Mode antes de configurar.");
            if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("Hay cambios de escena sin guardar. Consérvalos antes de abrir la prueba independiente.");
            foreach (string path in new[] { PrefabsPath + "/Arboles", PrefabsPath + "/Arbustos", PrefabsPath + "/Decoracion", "Assets/Prefabs/Characters/IDGS", MaterialPath, DataPath }) Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
            ImportSheet();
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/PixeloidSans SDF.asset");
            spriteMaterial = Material("Vegetacion_Unlit", Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default"), Color.white);
            var trees = new[]
            {
                Tree("Arbol_Redondo", 0, 0, 0),
                Tree("Arbol_Frondoso", 1, 0, 1),
                Tree("Arbol_Amarillo", 0, 3, 3)
            };
            var shrubs = new[] { Shrub("Arbusto_Verde", 3, 0), Shrub("Arbusto_Dorado", 4, 3) };
            var herbs = Herbs(); var player = Player();
            BuildScene(trees, shrubs, herbs, player);
            AssetDatabase.SaveAssets();
        }

        public static void ImportSheet()
        {
            AssetDatabase.ImportAsset(SpritePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp; importer.maxTextureSize = 256; importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true; importer.isReadable = false; importer.anisoLevel = 0;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; settings.spriteExtrude = 0; importer.SetTextureSettings(settings);
            var factory = new SpriteDataProviderFactories(); factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
            var retained = provider.GetSpriteRects().ToDictionary(s => s.name, s => s.spriteID);
            var texture = new Texture2D(2, 2); ImageConversion.LoadImage(texture, File.ReadAllBytes(SpritePath));
            var pixels = texture.GetPixels32(); var rects = new List<SpriteRect>();
            try
            {
                for (int row = 0; row < 8; row++) for (int col = 0; col < 8; col++)
                {
                    int sourceY = 256 - (row + 1) * 32, firstOpaque = 32;
                    for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
                        if (pixels[(sourceY + y) * 256 + col * 32 + x].a != 0) firstOpaque = Mathf.Min(firstOpaque, y);
                    string name = SpriteName(row, col);
                    rects.Add(new SpriteRect { name = name, rect = new Rect(col * 32, sourceY, 32, 32),
                        alignment = SpriteAlignment.Custom, pivot = new Vector2(.5f, firstOpaque == 32 ? 0 : firstOpaque / 32f),
                        spriteID = retained.TryGetValue(name, out var id) ? id : GUID.Generate() });
                }
                provider.SetSpriteRects(rects.ToArray());
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
                provider.Apply(); importer.SaveAndReimport();
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }

        private static Sprite Sprite(int row, int column) => AssetDatabase.LoadAllAssetsAtPath(SpritePath).OfType<Sprite>().Single(s => s.name == SpriteName(row, column));
        private static GameObject Root(string name, bool sorted)
        {
            var root = new GameObject(name);
            if (sorted) { root.AddComponent<SortingGroup>(); root.AddComponent<IDGSVegetationYSort>(); }
            return root;
        }
        private static SpriteRenderer Render(Transform parent, string name, Sprite sprite, Vector2 position, int order)
        {
            var go = new GameObject(name, typeof(SpriteRenderer)); go.transform.SetParent(parent, false); go.transform.localPosition = position;
            var r = go.GetComponent<SpriteRenderer>(); r.sprite = sprite; r.sharedMaterial = spriteMaterial; r.sortingOrder = order;
            r.spriteSortPoint = SpriteSortPoint.Pivot; return r;
        }
        private static GameObject SavePrefab(GameObject root, string path)
        {
            try { return PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        private static GameObject Tree(string name, int crownRow, int crownColumn, int trunkColumn)
        {
            var root = Root(name, true);
            Render(root.transform, "Tronco", Sprite(2, trunkColumn), Vector2.zero, 0);
            Render(root.transform, "Copa", Sprite(crownRow, crownColumn), new Vector2(0, 18f / 32), 1);
            var collider = root.AddComponent<BoxCollider2D>(); collider.size = new Vector2(8f / 32, 6f / 32); collider.offset = new Vector2(0, 3f / 32);
            return SavePrefab(root, PrefabsPath + "/Arboles/" + name + ".prefab");
        }
        private static GameObject Shrub(string name, int row, int column)
        {
            var root = Root(name, true); Render(root.transform, "Follaje", Sprite(row, column), Vector2.zero, 0);
            return SavePrefab(root, PrefabsPath + "/Arbustos/" + name + ".prefab");
        }
        private static GameObject Herbs()
        {
            var root = Root("Hierbas_Y_Plantas", false);
            Render(root.transform, "Hierbas", Sprite(6, 0), new Vector2(-12f / 32, 0), -10000);
            Render(root.transform, "Planta", Sprite(6, 4), new Vector2(9f / 32, 3f / 32), -9999);
            Render(root.transform, "Hojas", Sprite(5, 2), new Vector2(0, 11f / 32), -9998);
            return SavePrefab(root, PrefabsPath + "/Decoracion/Hierbas_Y_Plantas.prefab");
        }
        private static GameObject Player()
        {
            var root = Root("Mateo_Prueba_IDGS", true);
            var body = root.AddComponent<Rigidbody2D>(); body.gravityScale = 0; body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate; body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var collider = root.AddComponent<CapsuleCollider2D>(); collider.direction = CapsuleDirection2D.Horizontal;
            collider.size = new Vector2(.28f, .18f); collider.offset = new Vector2(0, .09f);
            root.AddComponent<IDGSVegetationTestPlayer>();
            var visual = Render(root.transform, "Mateo_Visual", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Characters/Mateo/Frames/caminataA1.png"), Vector2.zero, 50);
            var animator = visual.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Player/Mateo/Mateo.controller");
            visual.gameObject.AddComponent<CampusQuestMateoVisualDriver>().motionRoot = root.transform;
            return SavePrefab(root, PlayerPath);
        }

        private static Material Material(string name, Shader shader, Color color)
        {
            if (shader == null) throw new InvalidOperationException("No se encontró el shader de la prueba.");
            string path = MaterialPath + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            EditorUtility.SetDirty(m); return m;
        }

        private static void BuildScene(GameObject[] trees, GameObject[] shrubs, GameObject herbs, GameObject player)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var stage = new GameObject("IDGS_Prueba_Vegetacion");
            Ground(stage.transform);
            for (int i = 0; i < 3; i++)
            {
                Place(trees[i], stage.transform, new Vector3((i - 1) * 4, 1, 0));
                Label(stage.transform, new[] { "1 · REDONDO", "2 · FRONDOSO", "3 · AMARILLO" }[i], new Vector3((i - 1) * 4, 2.9f, 0));
            }
            for (int i = 0; i < 2; i++) Place(shrubs[i], stage.transform, new Vector3(-4 + i * 4, -1.5f, 0));
            Place(herbs, stage.transform, new Vector3(4, -1.5f, 0));
            Label(stage.transform, "4 · ARBUSTO VERDE", new Vector3(-4, -2.05f, 0));
            Label(stage.transform, "5 · ARBUSTO DORADO", new Vector3(0, -2.05f, 0));
            Label(stage.transform, "6 · HIERBAS / PLANTAS", new Vector3(4, -2.05f, 0));
            Place(player, stage.transform, new Vector3(-2, -.5f, 0));
            var cameraGo = new GameObject("Camara_Prueba_IDGS", typeof(Camera)); cameraGo.tag = "MainCamera";
            var camera = cameraGo.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 5.625f;
            camera.transform.position = new Vector3(0, .1f, -10); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(17, 25, 23, 255); camera.nearClipPlane = .1f; camera.farClipPlane = 50;
            Hud(stage.transform, camera);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = stage;
        }
        private static void Place(GameObject prefab, Transform parent, Vector3 position)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab); go.transform.SetParent(parent, false); go.transform.position = position;
        }
        private static void Ground(Transform parent)
        {
            var floor = new GameObject("Suelo_Provisional_1u_32px", typeof(MeshFilter), typeof(MeshRenderer)); floor.transform.SetParent(parent, false);
            var verts = new List<Vector3>(); var a = new List<int>(); var b = new List<int>();
            for (int y = -4; y < 4; y++) for (int x = -7; x < 7; x++)
            {
                int i = verts.Count;
                verts.Add(new Vector3(x, y, .2f)); verts.Add(new Vector3(x, y + 1, .2f));
                verts.Add(new Vector3(x + 1, y + 1, .2f)); verts.Add(new Vector3(x + 1, y, .2f));
                var tris = ((x + y) & 1) == 0 ? a : b; tris.AddRange(new[] { i, i + 1, i + 2, i + 2, i + 3, i });
            }
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(DataPath + "/Suelo_Provisional.asset");
            if (mesh == null) { mesh = new Mesh(); AssetDatabase.CreateAsset(mesh, DataPath + "/Suelo_Provisional.asset"); }
            mesh.Clear(); mesh.SetVertices(verts); mesh.subMeshCount = 2; mesh.SetTriangles(a, 0); mesh.SetTriangles(b, 1); mesh.RecalculateNormals(); mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh);
            floor.GetComponent<MeshFilter>().sharedMesh = mesh;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            var renderer = floor.GetComponent<MeshRenderer>(); renderer.sortingOrder = -30000;
            renderer.sharedMaterials = new[] { Material("Suelo_A_Provisional", shader, new Color32(65, 82, 60, 255)), Material("Suelo_B_Provisional", shader, new Color32(69, 86, 63, 255)) };
            foreach (var side in new[] { new Vector4(-7.125f, 0, .25f, 8.5f), new Vector4(7.125f, 0, .25f, 8.5f), new Vector4(0, -4.125f, 14.5f, .25f), new Vector4(0, 4.125f, 14.5f, .25f) })
            {
                var wall = new GameObject("Limite_Prueba", typeof(BoxCollider2D)); wall.transform.SetParent(parent, false);
                wall.transform.position = new Vector3(side.x, side.y); wall.GetComponent<BoxCollider2D>().size = new Vector2(side.z, side.w);
            }
        }
        private static void Label(Transform parent, string text, Vector3 position)
        {
            var go = new GameObject(text, typeof(TextMeshPro)); go.transform.SetParent(parent, false); go.transform.position = position;
            var label = go.GetComponent<TextMeshPro>(); label.font = font; label.text = text; label.fontSize = 1.6f; label.color = Gold;
            label.alignment = TextAlignmentOptions.Center; label.rectTransform.sizeDelta = new Vector2(3.6f, .4f); label.GetComponent<MeshRenderer>().sortingOrder = 20000;
        }
        private static void Hud(Transform parent, Camera camera)
        {
            var root = new GameObject("Informacion_Prueba", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)); root.transform.SetParent(parent, false);
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1; canvas.sortingOrder = 30000;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720);
            UiText(root.transform, "IDGS · PRUEBA DE VEGETACIÓN", true, 27);
            UiText(root.transform, "WASD / Flechas · Caminar     Shift · Correr     R · Inicio\n32 PPU · Escala 1 · Una celda = 1 unidad / 32 px · Colisión solo en la base del tronco", false, 18);
        }
        private static void UiText(Transform parent, string value, bool top, int size)
        {
            var go = new GameObject(top ? "Titulo" : "Controles", typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>(); r.anchorMin = new Vector2(0, top ? 1 : 0); r.anchorMax = new Vector2(1, top ? 1 : 0);
            r.pivot = new Vector2(.5f, top ? 1 : 0); r.sizeDelta = new Vector2(-32, top ? 52 : 74); r.anchoredPosition = new Vector2(0, top ? -12 : 12);
            go.GetComponent<Image>().color = new Color32(22, 29, 34, 245); go.GetComponent<Image>().raycastTarget = false;
            var textGo = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI)); textGo.transform.SetParent(go.transform, false);
            var tr = textGo.GetComponent<RectTransform>(); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(8, 4); tr.offsetMax = new Vector2(-8, -4);
            var text = textGo.GetComponent<TMP_Text>(); text.font = font; text.text = value; text.fontSize = size; text.color = Gold; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
        }
    }
}
#endif
