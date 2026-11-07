#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using CampusQuest.Seguridad;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace CampusQuest.EditorTools
{
    public static class SpBosquePainter
    {
        const string ScenePath = "Assets/Scenes/Sp_Bosque.unity";
        const string TilesRoot = "Assets/Sprites/Enviroment/Sp/Tiles";
        const string PackRoot = "Assets/Sprites/Enviroment/Sp/Resurrected RPG 1.1";
        const string RainTexturePath = "Assets/Sprites/Enviroment/Sp/Lluvia/Rain effect/rain effect.png";
        const string RainClipPath = "Assets/Audio/SFX/rain-storm.wav";
        const string MixerPath = "Assets/Audio/MainMixer.mixer";

        const int MapW = 48;
        const int MapH = 32;

        const int ArenaLeft = 10;
        const int ArenaBottom = 6;
        const int ArenaWidth = 28;
        const int ArenaHeight = 18;

        const int DoorLeft = 21;
        const int DoorRight = 26;

        const int PathCenterX = 24;
        const int PathHalfWidth = 4;
        const int RainColumns = 8;
        const int RainRows = 5;

        static readonly string[] MarkerNames =
        {
            "SP_Puerta", "SP_Spawn_Sur", "SP_Secuaz_1", "SP_Secuaz_2", "SP_Tolete"
        };

        [MenuItem("Campus Quest/SP/Pintar bosque exterior")]
        public static void PaintFromMenu() => Paint();

        [MenuItem("Campus Quest/SP/Dejar 40 gotas de lluvia")]
        public static void RebuildRain()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            EnsureRainAtmosphere();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Campus Quest: lluvia de SP quedó en " + (RainColumns * RainRows) + " gotas.");
        }

        public static void Paint()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var oldGrid = GameObject.Find("Grid");
            if (oldGrid != null)
                Undo.DestroyObjectImmediate(oldGrid);

            EnsureTileFolder();
            var palette = BuildPalette();

            var gridGo = new GameObject("Grid");
            Undo.RegisterCreatedObjectUndo(gridGo, "Pintar bosque SP");
            var grid = gridGo.AddComponent<Grid>();
            grid.cellSize = Vector3.one;

            var ground = NewTilemap(gridGo.transform, "Tilemap_Ground", 0, false);
            var decorations = NewTilemap(gridGo.transform, "Tilemap_Decorations", 1, false);
            var walls = NewTilemap(gridGo.transform, "Tilemap_Walls", 2, true);
            var canopy = NewTilemap(gridGo.transform, "Tilemap_Canopy", 10, false);
            ConfigureWallComposite(walls.gameObject);

            var path = BuildPathMask();
            var forest = BuildForestMask(path);

            var grassTile = palette.Grass;
            var pathTile = palette.Path;
            var trunkTile = palette.Trunk;
            var canopyTile = palette.Canopy;
            var decorTiles = palette.Decor;

            for (int y = 0; y < MapH; y++)
            for (int x = 0; x < MapW; x++)
            {
                var cell = new Vector3Int(x, y, 0);

                if (path[x, y])
                {
                    ground.SetTile(cell, pathTile);
                    continue;
                }

                ground.SetTile(cell, grassTile);

                if (forest[x, y])
                {
                    walls.SetTile(cell, trunkTile);
                    canopy.SetTile(cell, canopyTile);
                }
                else if (IsInsideArena(x, y) && (x * 5 + y * 11) % 29 == 0 && decorTiles.Length > 0)
                {
                    decorations.SetTile(cell, decorTiles[(x + y) % decorTiles.Length]);
                }
            }

            EnsureMarkers();
            ConfigureCamera();
            EnsureGlobalLight();
            EnsureRainAtmosphere();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = gridGo;
            Debug.Log("Campus Quest: entrada amplia SP pintada (" + MapW + "x" + MapH + ").");
        }

        static bool IsInsideArena(int x, int y) =>
            x >= ArenaLeft && x < ArenaLeft + ArenaWidth
            && y >= ArenaBottom && y < ArenaBottom + ArenaHeight;

        static void EnsureTileFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Sprites/Enviroment/Sp"))
                AssetDatabase.CreateFolder("Assets/Sprites/Enviroment", "Sp");
            if (!AssetDatabase.IsValidFolder(TilesRoot))
                AssetDatabase.CreateFolder("Assets/Sprites/Enviroment/Sp", "Tiles");
        }

        static Palette BuildPalette()
        {
            var grassSprites = LoadSprites(PackRoot + "/Grass.png").Where(Is32Square).ToArray();
            var pathSprites = LoadSprites(PackRoot + "/Grass 2 layer.png")
                .Where(s => s.rect.width >= 24 && s.rect.height >= 14 && s.rect.height <= 40).ToArray();
            var trunkSprites = LoadSprites(PackRoot + "/Wall.png")
                .Where(s => s.rect.width <= 22 && s.rect.height >= 28).ToArray();
            var canopySprites = LoadSprites(PackRoot + "/Plants.png")
                .Where(s => s.name.StartsWith("Plants_") && s.rect.height >= 90).Take(5).ToArray();
            var decorSprites = LoadSprites(PackRoot + "/Props.png")
                .Where(s => s.rect.width <= 40 && s.rect.height <= 40).Take(6).ToArray();

            if (grassSprites.Length == 0) throw new InvalidOperationException("Grass.png sin tiles 32x32.");
            if (pathSprites.Length == 0) pathSprites = grassSprites;
            if (trunkSprites.Length == 0) trunkSprites = LoadSprites(PackRoot + "/Wall.png");
            if (canopySprites.Length == 0) canopySprites = LoadSprites(PackRoot + "/Plants.png");
            if (decorSprites.Length == 0) decorSprites = pathSprites;

            return new Palette
            {
                Grass = EnsureTile(grassSprites[0], "SP_Grass"),
                Path = EnsureTile(pathSprites[0], "SP_Path"),
                Trunk = EnsureTile(trunkSprites[0], "SP_Trunk", Tile.ColliderType.Sprite),
                Canopy = EnsureTile(canopySprites[0], "SP_Canopy"),
                Decor = decorSprites.Select((s, i) => EnsureTile(s, "SP_Decor_" + i)).ToArray()
            };
        }

        struct Palette
        {
            public Tile Grass;
            public Tile Path;
            public Tile Trunk;
            public Tile Canopy;
            public Tile[] Decor;
        }

        static bool[,] BuildPathMask()
        {
            var path = new bool[MapW, MapH];
            for (int y = 0; y < MapH; y++)
            for (int dx = -PathHalfWidth; dx < PathHalfWidth; dx++)
            {
                int x = PathCenterX + dx;
                if (x >= 0 && x < MapW)
                    path[x, y] = true;
            }
            return path;
        }

        static bool[,] BuildForestMask(bool[,] path)
        {
            var forest = new bool[MapW, MapH];
            for (int y = 0; y < MapH; y++)
            for (int x = 0; x < MapW; x++)
            {
                if (path[x, y])
                    continue;
                if (IsInsideArena(x, y))
                    continue;
                if (IsDoorGap(x, y))
                    continue;

                if (IsMapBorder(x, y))
                {
                    forest[x, y] = true;
                    continue;
                }

                if (IsAmbushTree(x, y))
                    forest[x, y] = true;
            }
            return forest;
        }

        static bool IsDoorGap(int x, int y) => y == 0 && x >= DoorLeft && x <= DoorRight;

        static bool IsMapBorder(int x, int y)
        {
            if (y == 0 && IsDoorGap(x, y))
                return false;
            return x == 0 || y == 0 || x == MapW - 1 || y == MapH - 1;
        }

        static bool IsAmbushTree(int x, int y)
        {
            if (y < 4 || y > MapH - 2)
                return false;

            bool leftBand = x < ArenaLeft;
            bool rightBand = x >= ArenaLeft + ArenaWidth;
            if (!leftBand && !rightBand)
                return false;

            if ((y + x) % 3 == 0)
                return false;

            int corridorEvery = 5;
            if (x % corridorEvery == 0 || y % corridorEvery == 0)
                return false;

            return (x + y) % 2 == 0;
        }

        static void ConfigureWallComposite(GameObject wallsGo)
        {
            var body = wallsGo.GetComponent<Rigidbody2D>();
            if (body == null)
                body = wallsGo.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;

            if (wallsGo.GetComponent<CompositeCollider2D>() == null)
                wallsGo.AddComponent<CompositeCollider2D>();

            var tileCollider = wallsGo.GetComponent<TilemapCollider2D>();
            if (tileCollider != null)
                tileCollider.usedByComposite = true;
        }

        static Tilemap NewTilemap(Transform parent, string name, int sortingOrder, bool collider)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;
            if (collider)
                go.AddComponent<TilemapCollider2D>();
            return go.GetComponent<Tilemap>();
        }

        static void EnsureMarkers()
        {
            var root = GameObject.Find("SP_MapMarkers");
            if (root == null)
            {
                root = new GameObject("SP_MapMarkers");
                Undo.RegisterCreatedObjectUndo(root, "Marcadores bosque SP");
            }

            var stale = new List<GameObject>();
            foreach (Transform child in root.transform)
            {
                if (!MarkerNames.Contains(child.name))
                    stale.Add(child.gameObject);
            }
            foreach (var go in stale)
                Undo.DestroyObjectImmediate(go);

            PlaceMarker(root.transform, "SP_Puerta", new Vector3(24.5f, 0.5f, 0f));
            PlaceMarker(root.transform, "SP_Spawn_Sur", new Vector3(24.5f, 1.5f, 0f));
            PlaceMarker(root.transform, "SP_Secuaz_1", new Vector3(20.5f, 5.5f, 0f));
            PlaceMarker(root.transform, "SP_Secuaz_2", new Vector3(28.5f, 5.5f, 0f));
            PlaceMarker(root.transform, "SP_Tolete", new Vector3(12.5f, 11.5f, 0f));
        }

        static void PlaceMarker(Transform parent, string name, Vector3 worldPos)
        {
            var existing = parent.Find(name);
            Transform t;
            if (existing != null)
            {
                t = existing;
            }
            else
            {
                var go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, name);
                t = go.transform;
                t.SetParent(parent);
            }

            t.position = worldPos;
        }

        static void ConfigureCamera()
        {
            var cam = Camera.main;
            if (cam == null)
                return;

            cam.orthographic = true;
            cam.orthographicSize = 18f;
            cam.transform.position = new Vector3(23.5f, 15.5f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.07f, 0.1f, 1f);
        }

        static void EnsureGlobalLight()
        {
            var light = UnityEngine.Object.FindFirstObjectByType<Light2D>();
            if (light == null)
            {
                var go = new GameObject("Global Light 2D");
                Undo.RegisterCreatedObjectUndo(go, "Global Light 2D");
                light = go.AddComponent<Light2D>();
                light.lightType = Light2D.LightType.Global;
            }

            light.intensity = 0.35f;
            light.color = new Color(0.72f, 0.78f, 0.95f, 1f);
        }

        static void EnsureRainAtmosphere()
        {
            var cam = Camera.main;
            if (cam == null)
                return;

            var atmosphere = GameObject.Find("SP_Atmosphere");
            if (atmosphere == null)
            {
                atmosphere = new GameObject("SP_Atmosphere");
                Undo.RegisterCreatedObjectUndo(atmosphere, "SP_Atmosphere");
            }

            EnsureRainAudio(atmosphere.transform);

            var previousOverlay = cam.transform.Find("RainOverlay");
            if (previousOverlay != null)
                UnityEngine.Object.DestroyImmediate(previousOverlay.gameObject);

            var overlayGo = new GameObject("RainOverlay");
            overlayGo.transform.SetParent(cam.transform, false);
            overlayGo.transform.localPosition = Vector3.zero;
            var overlayRoot = overlayGo.transform;

            var frames = LoadSprites(RainTexturePath)
                .Where(s => s.name.StartsWith("rain effect_", StringComparison.Ordinal))
                .OrderBy(s => s.name, StringComparer.Ordinal)
                .ToArray();
            if (frames.Length == 0)
            {
                Debug.LogWarning("Campus Quest: no hay sprites de lluvia en " + RainTexturePath);
                return;
            }

            float viewHeight = cam.orthographicSize * 2f;
            float viewWidth = viewHeight * cam.aspect;
            float stepX = viewWidth / RainColumns;
            float stepY = viewHeight / RainRows;
            var tileRenderers = new List<SpriteRenderer>(RainColumns * RainRows);
            float originX = -viewWidth * 0.5f;
            float originY = -viewHeight * 0.5f;

            for (int row = 0; row < RainRows; row++)
            for (int col = 0; col < RainColumns; col++)
            {
                var drop = new GameObject($"RainTile_{col}_{row}");
                drop.transform.SetParent(overlayRoot, false);
                drop.transform.localPosition = new Vector3(
                    originX + col * stepX + stepX * 0.5f,
                    originY + row * stepY + stepY * 0.5f,
                    0.05f);
                drop.transform.localScale = Vector3.one;

                var sr = drop.AddComponent<SpriteRenderer>();
                sr.sprite = frames[(col + row) % frames.Length];
                sr.sortingOrder = 30;
                sr.color = new Color(1f, 1f, 1f, 0.55f);
                tileRenderers.Add(sr);
            }

            var overlay = overlayRoot.gameObject.GetComponent<SpForestRainOverlay>();
            if (overlay == null)
                overlay = overlayRoot.gameObject.AddComponent<SpForestRainOverlay>();
            overlay.ConfigureGrid(frames, tileRenderers.ToArray(), 10f);
        }

        static void EnsureRainAudio(Transform parent)
        {
            var audioRoot = parent.Find("RainAudio");
            if (audioRoot == null)
            {
                var go = new GameObject("RainAudio");
                Undo.RegisterCreatedObjectUndo(go, "RainAudio");
                go.transform.SetParent(parent, false);
                audioRoot = go.transform;
            }

            var source = audioRoot.GetComponent<AudioSource>();
            if (source == null)
                source = audioRoot.gameObject.AddComponent<AudioSource>();

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(RainClipPath);
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = true;
            source.spatialBlend = 0f;
            source.volume = 0.65f;

            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (mixer != null)
            {
                foreach (var group in mixer.FindMatchingGroups("SFX"))
                {
                    if (group != null && group.name == "SFX")
                    {
                        source.outputAudioMixerGroup = group;
                        break;
                    }
                }
            }
        }

        static Sprite[] LoadSprites(string texturePath) =>
            AssetDatabase.LoadAllAssetsAtPath(texturePath).OfType<Sprite>().ToArray();

        static bool Is32Square(Sprite sprite) =>
            Mathf.RoundToInt(sprite.rect.width) == 32 && Mathf.RoundToInt(sprite.rect.height) == 32;

        static Tile EnsureTile(Sprite sprite, string assetName, Tile.ColliderType collider = Tile.ColliderType.None)
        {
            string path = TilesRoot + "/" + assetName + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (existing != null && existing.sprite == sprite && existing.colliderType == collider)
                return existing;

            var tile = existing != null ? existing : ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.color = Color.white;
            tile.colliderType = collider;
            if (existing == null)
                AssetDatabase.CreateAsset(tile, path);
            else
                EditorUtility.SetDirty(tile);
            return tile;
        }
    }
}
#endif
