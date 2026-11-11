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

namespace CampusQuest.EditorTools
{
    public static class IDGSLargeVegetationSetup
    {
        public const string SheetPath = "Assets/Sprites/Enviroment/IDGS/Vegetacion/arbolotes.png";
        public const string PrefabPath = IDGSVegetationSetup.PrefabsPath + "/ArbolesGrandes";
        public static readonly string[] Names = { "Arbol_Grande_Redondo", "Arbol_Grande_Estratificado", "Arbol_Grande_Irregular", "Arbol_Grande_Dorado" };

        [MenuItem("Campus Quest/IDGS/Integrar árboles grandes en la comparación")]
        public static void Configure()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty || scene.path != IDGSVegetationSetup.ScenePath)
                throw new InvalidOperationException("Abre la prueba de vegetación guardada fuera de Play Mode.");
            Directory.CreateDirectory(PrefabPath); AssetDatabase.Refresh();
            var importer = (TextureImporter)AssetImporter.GetAtPath(SheetPath);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp; importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 256; importer.isReadable = false;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteMeshType = SpriteMeshType.FullRect; settings.spriteExtrude = 0; importer.SetTextureSettings(settings);
            var factory = new SpriteDataProviderFactories(); factory.Init(); var provider = factory.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
            var old = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
            var texture = new Texture2D(2, 2); ImageConversion.LoadImage(texture, File.ReadAllBytes(SheetPath));
            if (texture.width != 256 || texture.height != 256) throw new InvalidOperationException("La hoja analizada debe medir 256×256.");
            var pixels = texture.GetPixels32(); var rects = new List<SpriteRect>(); var widths = new int[4];
            try
            {
                for (int index = 0; index < 4; index++)
                {
                    int x0 = index % 2 * 128, y0 = index < 2 ? 128 : 0;
                    int minX = 256, minY = 256, maxX = -1, maxY = -1;
                    for (int y = y0; y < y0 + 128; y++) for (int x = x0; x < x0 + 128; x++) if (pixels[y * 256 + x].a != 0)
                    { minX = Math.Min(minX, x); minY = Math.Min(minY, y); maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y); }
                    if (maxX < minX) throw new InvalidOperationException("No se encontró árbol en una región analizada.");
                    int leftFoot = 256, rightFoot = -1;
                    for (int y = minY; y <= minY + 7; y++) for (int x = minX; x <= maxX; x++) if (pixels[y * 256 + x].a != 0)
                    { leftFoot = Math.Min(leftFoot, x); rightFoot = Math.Max(rightFoot, x); }
                    float footX = (leftFoot + rightFoot + 1) * .5f;
                    int w = maxX - minX + 1, h = maxY - minY + 1; widths[index] = Mathf.Clamp(Mathf.RoundToInt((rightFoot - leftFoot + 1) * .55f), 14, 22);
                    rects.Add(new SpriteRect { name = Names[index], rect = new Rect(minX, minY, w, h), alignment = SpriteAlignment.Custom,
                        pivot = new Vector2((footX - minX) / w, 0), spriteID = old.TryGetValue(Names[index], out var id) ? id : GUID.Generate() });
                }
                provider.SetSpriteRects(rects.ToArray()); provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
                provider.Apply(); importer.SaveAndReimport();
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/IDGS/Vegetacion_Unlit.mat");
            var sprites = AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>().ToDictionary(s => s.name);
            var stage = GameObject.Find("IDGS_Prueba_Vegetacion").transform;
            for (int i = 0; i < 4; i++)
            {
                var root = new GameObject(Names[i], typeof(SortingGroup), typeof(IDGSVegetationYSort), typeof(BoxCollider2D));
                var visual = new GameObject("Arbol_Completo", typeof(SpriteRenderer)); visual.transform.SetParent(root.transform, false);
                var renderer = visual.GetComponent<SpriteRenderer>(); renderer.sprite = sprites[Names[i]]; renderer.sharedMaterial = material; renderer.spriteSortPoint = SpriteSortPoint.Pivot;
                var collider = root.GetComponent<BoxCollider2D>(); collider.size = new Vector2(widths[i] / 32f, 10f / 32); collider.offset = new Vector2(0, 5f / 32);
                GameObject prefab;
                try { prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath + "/" + Names[i] + ".prefab"); }
                finally { UnityEngine.Object.DestroyImmediate(root); }
                var instance = stage.Find(Names[i]);
                if (instance == null) { var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab); go.transform.SetParent(stage, false); instance = go.transform; }
                instance.position = new Vector3(-6 + i * 4, 1.25f, 0); instance.GetComponent<IDGSVegetationYSort>().Refresh();
                Label(stage, "Grande_Etiqueta_" + i, new[] { "1 · GRANDE REDONDO", "2 · ESTRATIFICADO", "3 · IRREGULAR", "4 · DORADO" }[i], new Vector3(-6 + i * 4, 5.1f, 0));
            }
            var small = new[] { "Arbol_Redondo", "Arbol_Frondoso", "Arbol_Amarillo" };
            for (int i = 0; i < 3; i++) stage.Find(small[i]).position = new Vector3(-4 + i * 4, -1.75f, 0);
            var labels = new[] { "1 · REDONDO", "2 · FRONDOSO", "3 · AMARILLO", "4 · ARBUSTO VERDE", "5 · ARBUSTO DORADO", "6 · HIERBAS / PLANTAS" };
            var newLabels = new[] { "5 · PEQUEÑO REDONDO", "6 · PEQUEÑO FRONDOSO", "7 · PEQUEÑO AMARILLO", "8 · ARBUSTO VERDE", "9 · ARBUSTO DORADO", "10 · HIERBAS / PLANTAS" };
            for (int i = 0; i < labels.Length; i++)
            {
                var item = stage.Cast<Transform>().First(t => t.name == labels[i]); item.position = new Vector3(-4 + i % 3 * 4, i < 3 ? .25f : -4.55f, 0); item.GetComponent<TMP_Text>().text = newLabels[i];
            }
            foreach (string name in new[] { "Arbusto_Verde", "Arbusto_Dorado", "Hierbas_Y_Plantas" })
            { var t = stage.Find(name); t.position = new Vector3(t.position.x, -4, 0); }
            var player = UnityEngine.Object.FindFirstObjectByType<IDGSVegetationTestPlayer>(); player.transform.position = new Vector3(-2, -2.75f, 0);
            Camera.main.transform.position = new Vector3(0, 0, -10); Camera.main.orthographicSize = 7.03125f;
            stage.Find("Informacion_Prueba/Titulo/Texto").GetComponent<TMP_Text>().text = "IDGS · ÁRBOLES GRANDES Y PEQUEÑOS · ESCALA NATIVA";
            ExpandGround(stage);
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }

        private static void Label(Transform parent, string name, string text, Vector3 position)
        {
            var t = parent.Find(name);
            if (t == null) { var go = new GameObject(name, typeof(TextMeshPro)); go.transform.SetParent(parent, false); t = go.transform; }
            t.position = position; var tmp = t.GetComponent<TextMeshPro>(); tmp.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/PixeloidSans SDF.asset");
            tmp.text = text; tmp.fontSize = 1.8f; tmp.color = new Color32(233, 216, 166, 255); tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(3.9f, .5f); tmp.GetComponent<MeshRenderer>().sortingOrder = 20000;
        }
        private static void ExpandGround(Transform stage)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Data/IDGS/PruebaVegetacion/Suelo_Comparacion_Grandes.asset");
            if (mesh == null) { mesh = new Mesh(); AssetDatabase.CreateAsset(mesh, "Assets/Data/IDGS/PruebaVegetacion/Suelo_Comparacion_Grandes.asset"); }
            var vertices = new List<Vector3>(); var a = new List<int>(); var b = new List<int>();
            for (int y = -6; y < 6; y++) for (int x = -9; x < 9; x++)
            {
                int n = vertices.Count; vertices.Add(new Vector3(x, y, .2f)); vertices.Add(new Vector3(x, y + 1, .2f)); vertices.Add(new Vector3(x + 1, y + 1, .2f)); vertices.Add(new Vector3(x + 1, y, .2f));
                (((x + y) & 1) == 0 ? a : b).AddRange(new[] { n, n + 1, n + 2, n + 2, n + 3, n });
            }
            mesh.Clear(); mesh.SetVertices(vertices); mesh.subMeshCount = 2; mesh.SetTriangles(a, 0); mesh.SetTriangles(b, 1); mesh.RecalculateNormals(); mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh);
            stage.Find("Suelo_Provisional_1u_32px").GetComponent<MeshFilter>().sharedMesh = mesh;
            foreach (var c in stage.GetComponentsInChildren<BoxCollider2D>().Where(c => c.name == "Limite_Prueba"))
                if (c.size.x < c.size.y) { c.transform.position = new Vector3(Mathf.Sign(c.transform.position.x) * 9.125f, 0, 0); c.size = new Vector2(.25f, 12.5f); }
                else { c.transform.position = new Vector3(0, Mathf.Sign(c.transform.position.y) * 6.125f, 0); c.size = new Vector2(18.5f, .25f); }
        }
    }
}
#endif
