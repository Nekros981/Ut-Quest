#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace CampusQuest.EditorTools
{
    public static class RectoriaTilemapRepair
    {
        private const string MapPath = "Assets/Scripts/Minigames/Rectoria/Maps/Mapa_exploracion.txt";
        private const string GeneratedRoot = "Assets/Sprites/Enviroment/Rectoria";
        private const int Width = 44;
        private const int Height = 46;

        [MenuItem("Campus Quest/Rectoria/2 - Reparar Tilemaps vacios")]
        public static void Repair()
        {
            var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(MapPath);
            if (textAsset == null)
            {
                EditorUtility.DisplayDialog("Campus Quest", "No se encontro " + MapPath + ". Instala primero el generador original.", "Aceptar");
                return;
            }
            var root = GameObject.Find("Rectoria_Blockout");
            if (root == null || root.transform.Find("Grid") == null)
            {
                EditorUtility.DisplayDialog("Campus Quest", "Abre la escena Rectoria_Tutorial_Blockout antes de ejecutar la reparacion.", "Aceptar");
                return;
            }
            var grid = root.transform.Find("Grid");
            Tilemap floor = FindTilemap(grid, "Floor_Tilemap");
            Tilemap walls = FindTilemap(grid, "Walls_Tilemap");
            Tilemap obstacles = FindTilemap(grid, "Collision_Tilemap");
            Tilemap doors = FindTilemap(grid, "ClosedDoors_Tilemap");
            Tilemap markers = FindTilemap(grid, "Markers_Tilemap");
            if (floor == null || walls == null || obstacles == null || doors == null || markers == null)
            {
                EditorUtility.DisplayDialog("Campus Quest", "Falta al menos un Tilemap de la escena original; no se realizo ningun cambio.", "Aceptar");
                return;
            }

            char[,] cells;
            try { cells = Parse(textAsset.text); }
            catch (Exception ex)
            {
                Debug.LogError("Campus Quest: mapa invalido: " + ex.Message);
                return;
            }

            var targets = new[] {floor, walls, obstacles, doors, markers};
            foreach (var target in targets) Undo.RegisterCompleteObjectUndo(target, "Reparar Tilemaps Rectoría");

            Tile floorTile = EnsureTile("Piso", new Color32(206, 203, 175, 255));
            Tile wallTile = EnsureTile("Muro", new Color32(77, 83, 100, 255));
            Tile obstacleTile = EnsureTile("Obstaculo", new Color32(147, 101, 69, 255));
            Tile doorTile = EnsureTile("Puerta_Cerrada", new Color32(199, 60, 72, 255));
            Tile markerTile = EnsureTile("Indicador", new Color32(81, 175, 199, 255));

            int[] counts = new int[5];
            // Only repopulate tilemaps. Preserve all scene actors, positions and camera.
            foreach (var target in targets) target.ClearAllTiles();
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                char c = cells[x, y];
                if (c == ' ') continue;
                var pos = new Vector3Int(x, y, 0);
                if (c != '#') { floor.SetTile(pos, floorTile); counts[0]++; }
                if (c == '#') { walls.SetTile(pos, wallTile); counts[1]++; }
                else if (c == 'X' || c == 'N') { obstacles.SetTile(pos, obstacleTile); counts[2]++; }
                else if (c == 'D') { doors.SetTile(pos, doorTile); counts[3]++; }
                else if (c == 'i' || c == 'F' || c == 'S' || c == 'M') { markers.SetTile(pos, markerTile); counts[4]++; }
            }

            var unlitShader = Shader.Find("Sprites/Default");
            if (unlitShader != null)
            {
                string matPath = GeneratedRoot + "/Rectoria_Unlit.mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (material == null)
                {
                    material = new Material(unlitShader);
                    AssetDatabase.CreateAsset(material, matPath);
                }
                foreach (var target in targets)
                {
                    var renderer = target.GetComponent<TilemapRenderer>();
                    if (renderer != null) renderer.sharedMaterial = material;
                }
            }

            int[] verified = new int[5];
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                var p = new Vector3Int(x, y, 0);
                for (int i = 0; i < targets.Length; i++) if (targets[i].HasTile(p)) verified[i]++;
            }

            for (int i = 0; i < targets.Length; i++) EditorUtility.SetDirty(targets[i]);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            bool saved = EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            string report = "Piso: " + verified[0] + ", Muros: " + verified[1] + ", Obstaculos: " + verified[2] +
                            ", Puertas: " + verified[3] + ", Marcadores: " + verified[4] + ". Guardado: " + saved;
            Debug.Log("Campus Quest: reparacion de Tilemaps: " + report);
            EditorUtility.DisplayDialog("Campus Quest - Reparacion", report + "\n\nSi el piso sigue sin verse, selecciona Floor_Tilemap y revisa su Inspector.", "Aceptar");
        }

        private static Tilemap FindTilemap(Transform parent, string name)
        {
            var child = parent.Find(name);
            return child ? child.GetComponent<Tilemap>() : null;
        }

        private static Tile EnsureTile(string name, Color32 color)
        {
            string tilePath = GeneratedRoot + "/Tiles/" + name + ".asset";
            string texturePath = GeneratedRoot + "/Textures/" + name + ".png";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            if (sprite == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(texturePath));
                var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                Color[] pixels = new Color[256];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
                tex.SetPixels(pixels); tex.Apply();
                File.WriteAllBytes(texturePath, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
                var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spritePixelsPerUnit = 16;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            }
            if (sprite == null) throw new Exception("No se pudo cargar sprite de " + name);
            if (tile == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(tilePath));
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, tilePath);
            }
            tile.sprite = sprite;
            tile.color = Color.white;
            tile.colliderType = name == "Piso" || name == "Indicador" ? Tile.ColliderType.None : Tile.ColliderType.Grid;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static char[,] Parse(string input)
        {
            var cells = new char[Width, Height];
            var found = new bool[Height];
            foreach (string line in input.Replace("\r", "").Split('\n'))
            {
                if (line.Length != 48 || line[2] != ' ' || line[3] != ' ') continue;
                if (!int.TryParse(line.Substring(0, 2), out int y) || y < 0 || y >= Height) continue;
                if (found[y]) throw new Exception("Fila duplicada " + y);
                found[y] = true;
                for (int x = 0; x < Width; x++)
                {
                    char c = line[x + 4];
                    if (" #.XNiDFMS/".IndexOf(c) < 0) throw new Exception("Simbolo invalido " + c);
                    cells[x, y] = c;
                }
            }
            for (int y = 0; y < Height; y++) if (!found[y]) throw new Exception("Falta fila " + y);
            return cells;
        }
    }
}
#endif
