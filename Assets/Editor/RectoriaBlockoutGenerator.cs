#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using CampusQuest.Rectoria;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace CampusQuest.EditorTools
{
    public static class RectoriaBlockoutGenerator
    {
        const string Root = "Assets/Sprites/Enviroment/Rectoria";
        const string ScenePath = "Assets/Scenes/Rectoria_Tutorial_Blockout.unity";
        const string MapPath = "Assets/Scripts/Minigames/Rectoria/Maps/Mapa_exploracion.txt";

        [MenuItem("Campus Quest/Rectoria/1 - Generar blockout v0.1")]
        public static void Generate()
        {
            TextAsset mapAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(MapPath);
            if (mapAsset == null)
            {
                EditorUtility.DisplayDialog("Campus Quest", "Falta " + MapPath + ". Instala el paquete completo.", "Aceptar");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null || File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("Campus Quest", "La escena ya existe. No se sobrescribirá: " + ScenePath, "Aceptar");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            char[,] data;
            try { data = ParseMap(mapAsset.text); }
            catch (Exception e) { Debug.LogError("Mapa de Rectoría inválido: " + e.Message); return; }

            EnsureFolders();
            var floorTile = CreateTile("Piso", new Color32(206, 203, 175, 255));
            var wallTile = CreateTile("Muro", new Color32(77, 83, 100, 255));
            var obstacleTile = CreateTile("Obstaculo", new Color32(147, 101, 69, 255));
            var doorTile = CreateTile("Puerta_Cerrada", new Color32(199, 60, 72, 255));
            var hintTile = CreateTile("Indicador", new Color32(81, 175, 199, 255));
            var characterSprite = CreateSprite("Mateo_Provisional", new Color32(60, 158, 108, 255));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("Rectoria_Blockout");
            var gridGo = new GameObject("Grid");
            gridGo.transform.SetParent(root.transform);
            var grid = gridGo.AddComponent<Grid>();
            grid.cellSize = Vector3.one;
            grid.cellLayout = GridLayout.CellLayout.Rectangle;
            GridCellSwizzleNoop(grid);

            var floor = NewMap(gridGo.transform, "Floor_Tilemap", -3, false);
            var walls = NewMap(gridGo.transform, "Walls_Tilemap", -1, true);
            var obstacles = NewMap(gridGo.transform, "Collision_Tilemap", 0, true);
            var doors = NewMap(gridGo.transform, "ClosedDoors_Tilemap", 1, true);
            var markers = NewMap(gridGo.transform, "Markers_Tilemap", -2, false);

            var logic = root.AddComponent<RectoriaGridMap>();
            logic.grid = grid;
            logic.floor = floor;
            logic.walls = walls;
            logic.obstacles = obstacles;
            logic.doors = doors;

            Vector3Int spawn = new Vector3Int(21, 2, 0);
            var markerCoords = new Dictionary<char, List<Vector3Int>>();
            const int h = 46, w = 44;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                char c = data[x, y];
                Vector3Int p = new Vector3Int(x, y, 0);
                if (c == ' ') continue;
                if (c != '#') floor.SetTile(p, floorTile);
                if (c == '#') walls.SetTile(p, wallTile);
                else if (c == 'X' || c == 'N') obstacles.SetTile(p, obstacleTile);
                else if (c == 'D') doors.SetTile(p, doorTile);
                else if (c == 'i' || c == 'F' || c == 'S' || c == 'M') markers.SetTile(p, hintTile);
                if ("NiFSM".IndexOf(c) >= 0)
                {
                    if (!markerCoords.ContainsKey(c)) markerCoords[c] = new List<Vector3Int>();
                    markerCoords[c].Add(p);
                }
                if (c == 'M') spawn = p;
            }
            var actor = new GameObject("Mateo_Provisional_REEMPLAZAR_SPRITE");
            actor.transform.SetParent(root.transform);
            actor.transform.position = logic.CellCenter(spawn);
            var sr = actor.AddComponent<SpriteRenderer>();
            sr.sprite = characterSprite;
            sr.sortingOrder = 20;
            var movement = actor.AddComponent<RectoriaPlayerTest>();
            movement.map = logic;

            var npcs = new GameObject("NPCs_Marcadores_NoImplementados");
            npcs.transform.SetParent(root.transform);
            foreach (Vector3Int p in Get(markerCoords, 'N')) CreateLabel(npcs.transform, "NPC en " + p.x + "," + p.y, logic.CellCenter(p), "NPC");
            foreach (Vector3Int p in Get(markerCoords, 'i')) CreateLabel(npcs.transform, "Interaccion " + p.x + "," + p.y, logic.CellCenter(p), "E");
            foreach (Vector3Int p in Get(markerCoords, 'F')) CreateLabel(npcs.transform, "Foto " + p.x + "," + p.y, logic.CellCenter(p), "FOTO");
            foreach (Vector3Int p in Get(markerCoords, 'S')) CreateLabel(npcs.transform, "Salida " + p.x + "," + p.y, logic.CellCenter(p), "SALIDA");

            var cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            cam.transform.position = actor.transform.position + new Vector3(0, 0, -10);
            var camera = cam.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.11f, 0.13f, 0.16f);
            cam.AddComponent<AudioListener>();
            var follow = cam.AddComponent<RectoriaCameraFollow>();
            follow.target = actor.transform;

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError("No se pudo guardar la escena en " + ScenePath);
                return;
            }
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = actor;
            Debug.Log("Campus Quest: escena creada en " + ScenePath + ". WASD/flechas caminan, Shift corre. Solo geometria de prueba: no hay misiones, dialogos ni sprites definitivos.");
            EditorUtility.DisplayDialog("Campus Quest", "Blockout creado en " + ScenePath + ". Pulsa Play para recorrerlo. Las puertas rojas permanecen bloqueadas hasta editarlas manualmente.", "Aceptar");
        }

        static void GridCellSwizzleNoop(Grid grid) { /* Unity default XYZ */ }

        static char[,] ParseMap(string text)
        {
            const int w = 44, h = 46;
            var map = new char[w, h];
            bool[] found = new bool[h];
            foreach (string raw in text.Replace("\r", "").Split('\n'))
            {
                if (raw.Length != 48 || raw[2] != ' ' || raw[3] != ' ') continue;
                if (!int.TryParse(raw.Substring(0, 2), out int y) || y < 0 || y >= h) continue;
                if (found[y]) throw new Exception("Fila duplicada: " + y);
                for (int x = 0; x < w; x++)
                {
                    char c = raw[x + 4];
                    if (" #.XNiDFMS/".IndexOf(c) < 0) throw new Exception("Símbolo inválido en " + x + "," + y + ": " + c);
                    map[x, y] = c;
                }
                found[y] = true;
            }
            for (int y = 0; y < h; y++) if (!found[y]) throw new Exception("Falta fila " + y);
            return map;
        }

        static IEnumerable<Vector3Int> Get(Dictionary<char, List<Vector3Int>> map, char c)
        {
            return map.TryGetValue(c, out var p) ? p : new List<Vector3Int>();
        }
        static Tilemap NewMap(Transform parent, string name, int order, bool collider)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = order;
            if (collider) go.AddComponent<TilemapCollider2D>();
            return go.GetComponent<Tilemap>();
        }
        static void CreateLabel(Transform parent, string name, Vector3 pos, string label)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos + new Vector3(0, 0, -0.2f);
            var text = go.AddComponent<TextMesh>();
            text.text = label;
            text.characterSize = .15f;
            text.fontSize = 35;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = Color.black;
            var render = go.GetComponent<MeshRenderer>();
            if (render != null) render.sortingOrder = 25;
        }
        static void EnsureFolders()
        {
            CreateFolder("Assets/Sprites/Enviroment", "Rectoria");
            CreateFolder(Root, "Tiles");
            CreateFolder(Root, "Textures");
            CreateFolder("Assets", "Scenes");
        }
        static void CreateFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
        }
        static Sprite CreateSprite(string name, Color color)
        {
            string path = name == "Mateo_Provisional"
                ? "Assets/Sprites/Characters/Mateo/Mateo_Provisional.png"
                : Root + "/Textures/" + name + ".png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                Color[] pixels = new Color[256];
                for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                    pixels[y * 16 + x] = name == "Piso" || name == "Muro"
                        ? color
                        : (x == 0 || y == 0 || x == 15 || y == 15 ? color * 0.72f : color);
                tex.SetPixels(pixels);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType != TextureImporterType.Sprite || importer.spritePixelsPerUnit != 16 || importer.filterMode != FilterMode.Point)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 16;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static Tile CreateTile(string name, Color color)
        {
            string path = Root + "/Tiles/" + name + ".asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile != null) return tile;
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = CreateSprite(name, color);
            tile.color = Color.white;
            tile.colliderType = Tile.ColliderType.Sprite;
            AssetDatabase.CreateAsset(tile, path);
            return tile;
        }
    }
}
#endif
