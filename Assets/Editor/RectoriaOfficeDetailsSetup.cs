#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CampusQuest.Rectoria;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CampusQuest.EditorTools
{
    /// <summary>Appearance only. Existing Tilemaps and interaction anchors remain authoritative.</summary>
    public static class RectoriaOfficeDetailsSetup
    {
        private const string Art = "Assets/Sprites/Enviroment/Rectoria/OfficeTilesetAI/";
        private const string Prefabs = "Assets/Prefabs/Enviroment/Rectoria/OfficeDetails/";
        [Serializable] private sealed class Selection { public string name; public string sheet; public int x; public int top; public int width; public int height; }
        [Serializable] private sealed class SelectionList { public Selection[] sprites; }
        private static RectoriaGridMap map;
        private static Transform owner;
        private static Transform furniture;
        private static Transform covers;
        private static Sprite floor;
        private static Material material;
        private static readonly HashSet<Vector3Int> covered = new HashSet<Vector3Int>();
        private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

        [MenuItem("Campus Quest/Rectoria/6 - Ambientar con Office Tileset")]
        public static void Apply()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != RectoriaSceneStageSetup.ScenePath || scene.isDirty)
                throw new InvalidOperationException("Abre y guarda Rectoría Arte fuera de Play Mode.");
            map = UnityEngine.Object.FindFirstObjectByType<RectoriaGridMap>();
            var stage = GameObject.Find("Rectoria_Escenario");
            if (map == null || stage == null) throw new InvalidOperationException("Falta el escenario existente.");
            var selection = JsonUtility.FromJson<SelectionList>("{\"sprites\":" + File.ReadAllText(Art + "SpriteSelection.json").TrimStart('\uFEFF') + "}").sprites;
            if (selection == null || selection.Length == 0 || selection.Any(s => s == null || string.IsNullOrWhiteSpace(s.name)
                || (s.sheet != "A" && s.sheet != "B") || s.width <= 0 || s.height <= 0)
                || selection.Select(s => s.name).Distinct().Count() != selection.Length)
                throw new InvalidOperationException("El catálogo de recortes no es válido; no se modificó ningún importador.");
            Import(selection);
            material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Sprites/Enviroment/Rectoria/Rectoria_Unlit.mat");
            floor = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Enviroment/Rectoria/ArtV1/Piso_Oficina.png");
            if (floor == null || material == null) throw new InvalidOperationException("Faltan el piso o material originales.");
            Directory.CreateDirectory(Prefabs);
            foreach (var pair in sprites) EnsurePrefab(pair.Key, pair.Value);

            Undo.SetCurrentGroupName("Ambientar Rectoría con Office Tileset");
            int group = Undo.GetCurrentGroup();
            owner = Child(stage.transform, "Office_Details");
            Undo.RegisterFullObjectHierarchyUndo(owner.gameObject, "Actualizar detalles de oficina");
            furniture = Child(owner, "Mobiliario"); covers = Child(owner, "Suelo_Bajo_Mobiliario"); covered.Clear();

            // Reception: plants and waiting chairs only on already blocked cells.
            Place("Entrada_Planta_Izq", "tall_plant", 16, 1, 1, 1, .9f, 1.7f);
            Place("Entrada_Planta_Der", "tall_plant", 27, 1, 1, 1, .9f, 1.7f);
            foreach (int x in new[] { 16, 27 }) foreach (int y in new[] { 4, 5 })
                Place("Espera_" + x + "_" + y, "office_chair", x, y, 1, 1, .8f, 1.2f);
            Place("Recibidor_Agua", "water_cooler", 16, 7, 1, 1, .85f, 1.8f);
            Place("Recibidor_Planta", "round_plant", 27, 7, 1, 1, .85f, 1.3f);

            // Rector's office: the four-by-two desk retains its original blocked footprint.
            Place("Rectora_Escritorio", "executive_desk", 19, 16, 4, 2, 4f, 2.25f);
            Place("Rectora_Monitor", "monitor", 20, 17, 1, 1, .7f, .75f, false, .1f, 12);
            Place("Rectora_Planta_Izq", "round_plant", 16, 12, 1, 1, .85f, 1.3f);
            Place("Rectora_Planta_Der", "tall_plant", 27, 12, 1, 1, .9f, 1.7f);
            foreach (int x in new[] { 16, 27 })
            {
                Place("Rectora_Archivo_" + x, "filing_cabinet", x, 14, 1, 2, 1f, 2f);
                Place("Rectora_Librero_" + x, "bookcase", x, 16, 1, 2, 1f, 2f);
                Place("Rectora_Planta_Fondo_" + x, "round_plant", x, 19, 1, 1, .85f, 1.3f);
            }
            Place("Rectora_Archivo_Lateral", "filing_cabinet", 18, 18, 1, 2, 1f, 2f);

            // Archive: four note anchors remain exactly where they were.
            Place("Archivo_Mostrador", "executive_desk", 4, 28, 4, 2, 4f, 2.25f);
            Place("Archivo_Librero_Izq", "bookcase", 10, 30, 2, 2, 2f, 3.3f);
            Place("Archivo_Librero_Der", "bookcase", 12, 30, 2, 2, 2f, 3.3f);
            foreach (int x in new[] { 2, 3 }) foreach (int y in new[] { 24, 26 })
                Place("Archivo_Archivador_" + x + "_" + y, "filing_cabinet", x, y, 1, 2, 1f, 2f);
            foreach (int x in new[] { 10, 11 }) foreach (int y in new[] { 23, 25 })
                Place("Archivo_Archivador_" + x + "_" + y, "filing_cabinet", x, y, 1, 2, 1f, 2f);
            Place("Archivo_Pizarron", "noticeboard", 5, 22, 3, 2, 3f, 1.8f, true, .15f);

            // Credential office: keep the camera, photo X and NPC untouched.
            Place("Credenciales_Escritorio", "executive_desk", 32, 28, 4, 2, 4f, 2.25f);
            Place("Credenciales_Monitor", "monitor", 33, 29, 1, 1, .75f, .8f, false, .1f, 12);
            Place("Credenciales_Impresora", "printer", 38, 26, 1, 1, .95f, 1.05f);
            Place("Credenciales_Archivo_Lateral", "filing_cabinet", 28, 28, 1, 2, 1f, 2f);
            Place("Credenciales_Planta_Izq", "tall_plant", 28, 31, 1, 1, .9f, 1.7f);
            Place("Credenciales_Planta_Entrada", "round_plant", 41, 22, 1, 1, .85f, 1.3f);
            foreach (int x in new[] { 40, 41 })
            {
                Place("Credenciales_Archivador_" + x, "filing_cabinet", x, 29, 1, 2, 1f, 2f);
                Place("Credenciales_Planta_" + x, "round_plant", x, 31, 1, 1, .85f, 1.3f);
            }

            // Cashier: leave receipt interaction and the six receipt indicators in place.
            Place("Caja_Mostrador", "executive_desk", 4, 38, 4, 2, 4f, 2.25f);
            Place("Caja_Monitor", "monitor", 5, 39, 1, 1, .75f, .8f, false, .1f, 12);
            Place("Caja_Impresora", "printer", 2, 34, 1, 1, .95f, 1.05f);
            foreach (int x in new[] { 2, 3 })
            {
                Place("Caja_Archivo_" + x, "filing_cabinet", x, 39, 1, 2, 1f, 2f);
                Place("Caja_Planta_" + x, "round_plant", x, 41, 1, 1, .85f, 1.3f);
            }
            Place("Caja_Planta_Fondo", "tall_plant", 13, 41, 1, 1, .9f, 1.7f);
            Place("Caja_Mesa_Recibos", "receipt_table", 9, 35, 4, 2, 4f, 2.3f);
            var receiptAnchor = stage.GetComponentsInChildren<RectoriaScenePoint>().Single(p => p.pointId == "recibos");
            for (int i = 0; i < 6; i++)
            {
                var paper = receiptAnchor.transform.Find("Recibo_" + i);
                if (paper != null) { Undo.RecordObject(paper, "Alinear recibo sobre la mesa"); paper.localPosition = new Vector3(-.29f + i % 3 * .29f, -.17f + i / 3 * .3f + 1.15f, 0); }
            }
            Place("Pasillo_Copiadora", "printer", 20, 22, 2, 2, 2f, 2f);

            // Wall decorations occupy no new floor cells.
            Wall("Rectora_Ventana_Izq", "window", 16, 20, 2, 2f, 1.2f);
            Wall("Rectora_Ventana_Der", "window", 25, 20, 2, 2f, 1.2f);
            Wall("Archivo_Ventana", "window", 3, 32, 2, 2f, 1.2f);
            Wall("Credenciales_Ventana", "window", 30, 32, 2, 2f, 1.2f);
            Wall("Caja_Ventana", "window", 10, 42, 2, 2f, 1.2f);
            Wall("Recibidor_Reloj", "wall_clock", 25, 8, 1, .6f, .65f);
            Wall("Archivo_Reloj", "wall_clock", 8, 32, 1, .6f, .65f);

            // Visual underlay hides the old generic NPC furniture tile without touching that tile.
            foreach (var npc in stage.GetComponentsInChildren<RectoriaScenePoint>().Where(p => p.kind == RectoriaPointKind.Npc)) Cover(npc.cell);
            var oldDecor = stage.transform.Find("Ambientacion_Provisional");
            if (oldDecor != null)
                foreach (var name in new[] { "Planta_Recibidor_Izq", "Planta_Recibidor_Der", "Planta_Rectora" })
                { var old = oldDecor.Find(name); if (old != null) { Undo.RecordObject(old.gameObject, "Sustituir planta provisional"); old.gameObject.SetActive(false); } }
            var result = RectoriaSceneStageSetup.Validate();
            if (owner.GetComponentsInChildren<Collider2D>(true).Length != 0) throw new InvalidOperationException("No debe haber nuevos colliders.");
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("No se pudo guardar Rectoría.");
            Undo.CollapseUndoOperations(group);
            Debug.Log("Office Tileset: " + furniture.childCount + " detalles, " + covered.Count + " bases visuales. " + result);
        }

        private static void Import(Selection[] selection)
        {
            sprites.Clear();
            foreach (var sheet in new[] { "A", "B" })
            {
                string path = Art + "OfficeObjects_" + sheet + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Importa primero " + path);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = 16; importer.filterMode = FilterMode.Point; importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true; importer.maxTextureSize = 2048; importer.npotScale = TextureImporterNPOTScale.None;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
                var factory = new SpriteDataProviderFactories(); factory.Init();
                var provider = factory.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
                var old = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var rects = selection.Where(s => s.sheet == sheet).Select(s => new SpriteRect
                {
                    name = s.name, rect = new Rect(s.x, texture.height - s.top - s.height, s.width, s.height),
                    pivot = new Vector2(.5f, 0), alignment = SpriteAlignment.Custom,
                    spriteID = old.TryGetValue(s.name, out var id) ? id : GUID.Generate()
                }).ToArray();
                provider.SetSpriteRects(rects);
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
                provider.Apply(); importer.SaveAndReimport();
                foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()) sprites.Add(sprite.name, sprite);
            }
            if (sprites.Count != selection.Length) throw new InvalidOperationException("No se importaron todos los sprites seleccionados.");
        }

        private static void EnsurePrefab(string name, Sprite sprite)
        {
            string path = Prefabs + name + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                var existing = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var component = existing.GetComponent<SpriteRenderer>();
                    if (component == null) throw new InvalidOperationException("Prefab de mobiliario inesperado: " + path);
                    component.sprite = sprite; component.sharedMaterial = material;
                    PrefabUtility.SaveAsPrefabAsset(existing, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(existing); }
                return;
            }
            var go = new GameObject(name); var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite; renderer.sharedMaterial = material; renderer.sortingOrder = 8;
            PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go); // Only the temporary object just created here.
        }

        private static void Place(string name, string spriteName, int x, int y, int w, int h, float width, float maxHeight, bool mask = true, float yOffset = 0, int order = 8)
        {
            for (int yy = y; yy < y + h; yy++) for (int xx = x; xx < x + w; xx++)
                if (!map.obstacles.HasTile(new Vector3Int(xx, yy, 0))) throw new InvalidOperationException("El objeto ocuparía una casilla libre: " + name + " " + xx + "," + yy);
            var root = Spawn(name, spriteName, x + w * .5f, y + yOffset, width, maxHeight, order);
            if (mask) for (int yy = y; yy < y + h; yy++) for (int xx = x; xx < x + w; xx++) Cover(new Vector3Int(xx, yy, 0));
            if (spriteName == "noticeboard")
            {
                var backing = Child(root, "Fondo_Pizarron");
                var r = backing.GetComponent<SpriteRenderer>(); if (r == null) r = Undo.AddComponent<SpriteRenderer>(backing.gameObject);
                r.sprite = floor; r.color = new Color32(36, 42, 47, 255); r.sharedMaterial = material; r.sortingOrder = order - 1;
                backing.localScale = new Vector3(sprites[spriteName].rect.width / 16f * .94f, sprites[spriteName].rect.height / 16f * .84f, 1);
                backing.localPosition = new Vector3(0, sprites[spriteName].rect.height / 32f, 0);
            }
        }

        private static void Wall(string name, string sprite, int x, int y, int w, float width, float maxHeight)
        {
            for (int xx = x; xx < x + w; xx++) if (!map.walls.HasTile(new Vector3Int(xx, y, 0))) throw new InvalidOperationException("Pared inválida: " + name);
            Spawn(name, sprite, x + w * .5f, y, width, maxHeight, 8);
        }

        private static Transform Spawn(string name, string spriteName, float x, float y, float width, float maxHeight, int order)
        {
            var root = furniture.Find(name);
            if (root == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + spriteName + ".prefab");
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, furniture); go.name = name;
                Undo.RegisterCreatedObjectUndo(go, "Colocar " + name); root = go.transform;
            }
            var sprite = sprites[spriteName];
            float scale = Mathf.Min(width / (sprite.rect.width / 16f), maxHeight / (sprite.rect.height / 16f));
            Undo.RecordObject(root, "Ajustar mobiliario");
            root.localScale = new Vector3(scale, scale, 1); root.position = new Vector3(x, y, .5f);
            var renderer = root.GetComponent<SpriteRenderer>();
            Undo.RecordObject(renderer, "Ajustar sprite de mobiliario");
            renderer.sprite = sprite; renderer.sortingOrder = order; renderer.sharedMaterial = material;
            PrefabUtility.RecordPrefabInstancePropertyModifications(root.gameObject);
            PrefabUtility.RecordPrefabInstancePropertyModifications(root);
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            return root;
        }

        private static void Cover(Vector3Int cell)
        {
            if (!map.obstacles.HasTile(cell) || !map.floor.HasTile(cell)) throw new InvalidOperationException("Base visual fuera del mobiliario: " + cell);
            covered.Add(cell);
            var root = Child(covers, "Base_" + cell.x + "_" + cell.y); root.position = map.CellCenter(cell);
            var renderer = root.GetComponent<SpriteRenderer>(); if (renderer == null) renderer = Undo.AddComponent<SpriteRenderer>(root.gameObject);
            renderer.sprite = floor; renderer.color = Color.white; renderer.sharedMaterial = material; renderer.sortingOrder = 1;
        }

        private static Transform Child(Transform parent, string name)
        {
            var old = parent.Find(name); if (old != null) return old;
            var go = new GameObject(name); Undo.RegisterCreatedObjectUndo(go, "Crear " + name); go.transform.SetParent(parent, false); return go.transform;
        }
    }
}
#endif
