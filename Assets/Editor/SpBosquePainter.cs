#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
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
        const string MixelGroundSheet =
            "Assets/Sprites/Enviroment/Sp/Top-Down RPG 32x32 by Mixel v1.7/Nature v1.5/Topdown RPG 32x32 - Ground Tileset 1.2.PNG";
        const string PixelLabForestSheet = "Assets/Sprites/Enviroment/Sp/PixelLab/SP_Bosque.png";
        const string BunkerRetroSheet = "Assets/Sprites/Enviroment/Sp/PixelLab/objetos-bunker-retro.png";
        const string CampoMilitarSheet = "Assets/Sprites/Enviroment/Sp/PixelLab/CompoMilitar.png";
        const string ZoneBunkerSheet = "Assets/Sprites/Enviroment/Sp/PixelLab/Zone_Bunker.png";
        const string HatchSheet = "Assets/Sprites/Enviroment/Sp/PixelLab/Escotilla_Bunker.png";
        const string FireVfxRoot = "Assets/Sprites/Enviroment/Sp/Free Pixel Art Explosions/PNG/Fire";
        const string SmokeVfxRoot = "Assets/Sprites/Enviroment/Sp/Free Pixel Art Explosions/PNG/Smoke";
        const string VfxParticleMaterialPath = "Assets/Sprites/Enviroment/Sp/Materials/SP_VfxSpriteUnlit.mat";
        const string PixelLabTilesRoot = "Assets/Sprites/Enviroment/Sp/Tiles/PixelLab";
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
        const int RouteMapH = 80;
        const int NorthRouteStartY = 80;
        const int FullMapH = 112;
        const int NorthPathLeft = 14;
        const int NorthPathRight = 21;
        const int NorthPathEndY = 94;
        const int FieldStartY = 95;
        const int FieldLeft = 8;
        const int FieldRight = 39;
        const float FieldLaneLeft = 13.5f;
        const float FieldLaneRight = 22.5f;
        const int PlazaStartY = 112;
        const int BunkerMapH = 132;
        const int PlazaTopY = 118;
        const int PlazaBottomY = 131;
        const int PlazaLeft = 10;
        const int PlazaRight = 25;
        const int PlazaGateLeft = 16;
        const int PlazaGateRight = 17;

        static readonly string[] MarkerNames =
        {
            "SP_Puerta", "SP_Spawn_Sur", "SP_Secuaz_1", "SP_Secuaz_2", "SP_Tolete",
            "SP_Claro_1", "SP_Emboscada_2", "SP_Bunker", "SP_Campo", "SP_Escotilla"
        };

        [MenuItem("Campus Quest/SP/Pintar bosque exterior")]
        public static void PaintFromMenu() => Paint();

        [MenuItem("Campus Quest/SP/Cambiar piso del bosque")]
        public static void RepaintGroundFromMenu() => RepaintGroundOnly();

        [MenuItem("Campus Quest/SP/Agregar recorrido del bosque")]
        public static void PaintRouteExtensionFromMenu() => PaintRouteExtension();

        [MenuItem("Campus Quest/SP/Pintar campo destruido")]
        public static void PaintDestroyedCampNorthFromMenu() => PaintDestroyedCampNorth();

        [MenuItem("Campus Quest/SP/Pintar plaza del búnker")]
        public static void PaintBunkerPlazaFromMenu() => PaintBunkerPlazaNorth();

        [MenuItem("Campus Quest/SP/Cambiar estilo del bosque")]
        public static void ApplyPixelLabStyleFromMenu() => ApplyPixelLabForestStyle();

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

        public static void RepaintGroundOnly()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var ground = FindGroundTilemap();
            if (ground == null)
            {
                Debug.LogError("Campus Quest: no hay Tilemap_Ground en Sp_Bosque.");
                return;
            }

            EnsureTileFolder();
            var tiles = BuildMixelGroundPalette();
            var path = BuildPathMask();

            for (int y = 0; y < MapH; y++)
            for (int x = 0; x < MapW; x++)
            {
                var cell = new Vector3Int(x, y, 0);
                ground.SetTile(cell, path[x, y]
                    ? SelectPathGroundTile(tiles, path, x, y)
                    : SelectGrassGroundTile(tiles, x, y));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Campus Quest: Tilemap_Ground actualizado con tileset Mixel.");
        }

        public static void PaintRouteExtension()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var ground = FindGroundTilemap();
            var walls = FindTilemap("Tilemap_Walls");
            var canopy = FindTilemap("Tilemap_Canopy");
            if (ground == null || walls == null || canopy == null)
            {
                Debug.LogError("Campus Quest: falta el Grid con capas de tilemap en Sp_Bosque.");
                return;
            }

            var trunk = AssetDatabase.LoadAssetAtPath<Tile>(TilesRoot + "/SP_Trunk.asset");
            var canopyTile = AssetDatabase.LoadAssetAtPath<Tile>(TilesRoot + "/SP_Canopy.asset");
            if (trunk == null || canopyTile == null)
            {
                Debug.LogError("Campus Quest: faltan SP_Trunk o SP_Canopy.");
                return;
            }

            EnsureTileFolder();
            var mixel = BuildMixelGroundPalette();
            var path = BuildExtensionPathMask();

            for (int y = MapH; y < RouteMapH; y++)
            for (int x = 0; x < MapW; x++)
            {
                var cell = new Vector3Int(x, y, 0);
                ground.SetTile(cell, path[x, y]
                    ? SelectPathGroundTile(mixel, path, x, y)
                    : SelectGrassGroundTile(mixel, x, y));

                if (IsExtensionTree(x, y, path))
                {
                    walls.SetTile(cell, trunk);
                    canopy.SetTile(cell, canopyTile);
                }
            }

            var markers = GameObject.Find("SP_MapMarkers");
            if (markers != null)
            {
                PlaceMarker(markers.transform, "SP_Claro_1", new Vector3(12.5f, 43.5f, 0f));
                PlaceMarker(markers.transform, "SP_Emboscada_2", new Vector3(15.5f, 70.5f, 0f));
                PlaceMarker(markers.transform, "SP_Bunker", new Vector3(16.5f, 76.5f, 0f));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Campus Quest: recorrido agregado encima de la entrada (" + MapW + "x" + RouteMapH + ").");
        }

        public static void PaintDestroyedCampNorth()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var ground = FindGroundTilemap();
            var walls = FindTilemap("Tilemap_Walls");
            var canopy = FindTilemap("Tilemap_Canopy");
            var decorations = FindTilemap("Tilemap_Decorations");
            if (ground == null || walls == null || canopy == null || decorations == null)
            {
                Debug.LogError("Campus Quest: faltan capas de tilemap en Sp_Bosque.");
                return;
            }

            var trunk = AssetDatabase.LoadAssetAtPath<Tile>(TilesRoot + "/SP_Trunk.asset");
            if (trunk == null)
            {
                Debug.LogError("Campus Quest: falta SP_Trunk.");
                return;
            }

            EnsureTileFolder();
            EnsurePixelLabTileFolder();
            var forest = BuildPixelLabPalette();

            for (int y = NorthRouteStartY; y < FullMapH; y++)
            for (int x = 0; x < MapW; x++)
            {
                var cell = new Vector3Int(x, y, 0);
                bool onPath = IsNorthPathCell(x, y);
                bool inField = IsFieldCell(x, y);

                if (onPath || inField)
                    ground.SetTile(cell, forest.Dirt);
                else
                    ground.SetTile(cell, forest.GrassVariants[(x * 3 + y * 7) & 3]);

                decorations.SetTile(cell, null);

                walls.SetTile(cell, null);
                canopy.SetTile(cell, null);
                if (IsNorthEdgeTree(x, y, onPath, inField))
                {
                    walls.SetTile(cell, trunk);
                    canopy.SetTile(cell, forest.Pines[(x + y) % forest.Pines.Length]);
                }
            }

            var markers = GameObject.Find("SP_MapMarkers");
            if (markers != null)
                PlaceMarker(markers.transform, "SP_Campo", new Vector3(17.5f, 103.5f, 0f));

            EnsureCampoMilitarProps();
            EnsureFieldFireSmokeColumns();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Campus Quest: campo destruido al norte (camino libre x "
                + FieldLaneLeft + "-" + FieldLaneRight + ", " + MapW + "x" + FullMapH + ").");
        }

        public static string PlazaLayoutVersion() => "ordered-v3";

        const float PlazaCenterX = 18f;
        const float PlazaCenterY = 125f;

        public static void PaintBunkerPlazaNorth()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var ground = FindGroundTilemap();
            var walls = FindTilemap("Tilemap_Walls");
            var canopy = FindTilemap("Tilemap_Canopy");
            var decorations = FindTilemap("Tilemap_Decorations");
            if (ground == null || walls == null || canopy == null || decorations == null)
            {
                Debug.LogError("Campus Quest: faltan capas de tilemap en Sp_Bosque.");
                return;
            }

            var trunk = AssetDatabase.LoadAssetAtPath<Tile>(TilesRoot + "/SP_Trunk.asset");
            if (trunk == null)
            {
                Debug.LogError("Campus Quest: falta SP_Trunk.");
                return;
            }

            EnsureTileFolder();
            var forest = BuildPixelLabPalette();

            for (int y = PlazaStartY; y < BunkerMapH; y++)
            for (int x = 0; x < MapW; x++)
            {
                var cell = new Vector3Int(x, y, 0);
                bool onPath = IsPlazaPathCell(x, y);
                bool inPlaza = IsPlazaSlabCell(x, y);

                if (onPath)
                    ground.SetTile(cell, forest.Dirt);
                else if (inPlaza)
                    ground.SetTile(cell, forest.Dirt);
                else
                    ground.SetTile(cell, forest.GrassVariants[(x * 3 + y * 7) & 3]);

                decorations.SetTile(cell, null);

                walls.SetTile(cell, null);
                canopy.SetTile(cell, null);
                if (!onPath && !inPlaza && (x <= 6 || x >= MapW - 7 || (x + y) % 4 == 0))
                {
                    walls.SetTile(cell, trunk);
                    canopy.SetTile(cell, forest.Pines[(x + y) % forest.Pines.Length]);
                }
            }

            var markers = GameObject.Find("SP_MapMarkers");
            if (markers != null)
                PlaceMarker(markers.transform, "SP_Escotilla", new Vector3(PlazaCenterX, PlazaCenterY, 0f));

            EnsureBunkerPlazaVisuals();
            EnsureBunkerPlazaFires();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Campus Quest: plaza del búnker (filas " + PlazaStartY + "-" + (BunkerMapH - 1) + ").");
        }

        static bool IsPlazaPathCell(int x, int y) =>
            y >= PlazaStartY && y < BunkerMapH && x >= NorthPathLeft && x <= NorthPathRight;

        static bool IsPlazaSlabCell(int x, int y) =>
            y >= PlazaTopY && y <= PlazaBottomY && x >= PlazaLeft && x <= PlazaRight;

        static bool IsPlazaBorderCell(int x, int y)
        {
            if (!IsPlazaSlabCell(x, y))
                return false;
            return x == PlazaLeft || x == PlazaRight || y == PlazaTopY || y == PlazaBottomY;
        }

        static bool IsPlazaSouthGateAnchor(int x, int y) =>
            y == PlazaTopY && x == PlazaGateLeft;

        static bool IsPlazaCornerConcreteAnchor(int x, int y) =>
            (x == PlazaLeft || x == PlazaRight - 1) && (y == PlazaTopY || y == PlazaBottomY - 1);

        static Dictionary<int, Sprite> LoadZoneBunkerSprites()
        {
            return LoadSprites(ZoneBunkerSheet)
                .Select(s => new { Sprite = s, Index = ParseZoneBunkerIndex(s.name) })
                .Where(p => p.Index >= 0)
                .ToDictionary(p => p.Index, p => p.Sprite);
        }

        static int ParseZoneBunkerIndex(string spriteName)
        {
            const string suffix = "Zone_Bunker_";
            int i = spriteName.LastIndexOf(suffix, StringComparison.Ordinal);
            if (i < 0)
                return -1;
            return int.TryParse(spriteName.Substring(i + suffix.Length), out var index) ? index : -1;
        }

        static void EnsureBunkerPlazaVisuals()
        {
            var existing = GameObject.Find("SP_BunkerPlaza");
            if (existing != null)
                Undo.DestroyObjectImmediate(existing);

            var zone = LoadZoneBunkerSprites();
            Sprite Require(int index)
            {
                if (!zone.TryGetValue(index, out var sprite))
                    throw new InvalidOperationException("Zone_Bunker sin sprite _" + index);
                return sprite;
            }

            var root = new GameObject("SP_BunkerPlaza");
            Undo.RegisterCreatedObjectUndo(root, "SP_BunkerPlaza");

            var slabClean = Require(0);
            var slabCorner = Require(1);
            var meshFence = Require(12);

            for (int y = PlazaTopY; y <= PlazaBottomY - 1; y += 2)
            for (int x = PlazaLeft; x <= PlazaRight - 1; x += 2)
            {
                var sprite = IsPlazaCornerConcreteAnchor(x, y) ? slabCorner : slabClean;
                PlacePlazaSprite(root.transform, sprite, x, y, 5);
            }

            var fenceAnchors = new HashSet<Vector2Int>();
            int fenceBottom = PlazaBottomY - 1;
            int fenceRight = PlazaRight - 1;
            for (int x = PlazaLeft; x <= fenceRight; x += 2)
            {
                if (!IsPlazaSouthGateAnchor(x, PlazaTopY))
                    fenceAnchors.Add(new Vector2Int(x, PlazaTopY));
                fenceAnchors.Add(new Vector2Int(x, fenceBottom));
            }

            for (int y = PlazaTopY; y <= fenceBottom; y += 2)
            {
                fenceAnchors.Add(new Vector2Int(PlazaLeft, y));
                fenceAnchors.Add(new Vector2Int(fenceRight, y));
            }

            foreach (var cell in fenceAnchors)
                PlacePlazaFenceSegment(root.transform, meshFence, cell.x, cell.y);

            var hatch = LoadSprites(HatchSheet).FirstOrDefault(s => s.name == "Escotilla_Bunker_0");
            if (hatch != null)
                PlacePlazaSpriteAtCenter(root.transform, hatch, PlazaCenterX, PlazaCenterY, 20);

            if (zone.TryGetValue(5, out var panel))
                PlacePlazaSpriteAtCenter(root.transform, panel, 20.5f, PlazaCenterY, 19);

            var campo = LoadSprites(CampoMilitarSheet)
                .Select(s => new { Sprite = s, Index = ParseCompoMilitarIndex(s.name) })
                .Where(p => p.Index >= 0)
                .ToDictionary(p => p.Index, p => p.Sprite);

            void Tire(int index, float centerX, float centerY)
            {
                if (!campo.TryGetValue(index, out var sprite))
                    return;
                var go = new GameObject("CompoMilitar_tire_" + index);
                go.transform.SetParent(root.transform, false);
                go.transform.position = new Vector3(centerX, centerY, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = 9;
                var delta = new Vector3(centerX, centerY, 0f) - sr.bounds.center;
                go.transform.position += delta;
            }

            const int tireStack = 26;
            Tire(tireStack, 13f, 121f);
            Tire(tireStack, 23f, 121f);
            Tire(tireStack, 13f, 129f);
            Tire(tireStack, 23f, 129f);
        }

        static void PlacePlazaSprite(Transform parent, Sprite sprite, int tileX, int tileY, int sortingOrder)
        {
            var go = new GameObject(sprite.name + "_" + tileX + "_" + tileY);
            Undo.RegisterCreatedObjectUndo(go, go.name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            AlignSpriteBottomLeft(go.transform, sr, tileX, tileY);
        }

        static void PlacePlazaSpriteAtCenter(Transform parent, Sprite sprite, float centerX, float centerY, int sortingOrder)
        {
            var go = new GameObject(sprite.name);
            Undo.RegisterCreatedObjectUndo(go, go.name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(centerX, centerY, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            var delta = new Vector3(centerX, centerY, 0f) - sr.bounds.center;
            go.transform.position += delta;
        }

        static void PlacePlazaFenceSegment(Transform parent, Sprite sprite, int tileX, int tileY)
        {
            var go = new GameObject("fence_" + tileX + "_" + tileY);
            Undo.RegisterCreatedObjectUndo(go, go.name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 8;
            AlignSpriteBottomLeft(go.transform, sr, tileX, tileY);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = sprite.bounds.size;
            box.offset = sprite.bounds.center;
        }

        static void AlignSpriteBottomLeft(Transform transform, SpriteRenderer renderer, float tileX, float tileY)
        {
            transform.position = new Vector3(tileX, tileY, 0f);
            var delta = new Vector3(tileX, tileY, 0f) - renderer.bounds.min;
            transform.position += delta;
        }

        static void EnsureBunkerPlazaFires()
        {
            var existing = GameObject.Find("SP_BunkerPlazaFires");
            if (existing != null)
                Undo.DestroyObjectImmediate(existing);

            var fireFrames = LoadExplosionFrames(FireVfxRoot, "Fire");
            var smokeFrames = LoadExplosionFrames(SmokeVfxRoot, "Smoke");
            var material = EnsureVfxParticleMaterial();
            if (fireFrames.Length == 0 || smokeFrames.Length == 0 || material == null)
                return;

            var root = new GameObject("SP_BunkerPlazaFires");
            Undo.RegisterCreatedObjectUndo(root, "SP_BunkerPlazaFires");

            var column = new GameObject("SP_PlazaFireSmoke_SE");
            Undo.RegisterCreatedObjectUndo(column, column.name);
            column.transform.SetParent(root.transform, false);
            column.transform.position = new Vector3(23f, 121f, 0f);

            var fireGo = new GameObject("Fire");
            fireGo.transform.SetParent(column.transform, false);
            ConfigureSpriteLoopParticles(fireGo, fireFrames, material, new Color(1f, 0.55f, 0.15f, 1f),
                startLifetime: 0.55f, startSpeed: 0.9f, startSize: 0.45f, rate: 14f, rise: 1.6f, sortingOrder: 25);

            var smokeGo = new GameObject("Smoke");
            smokeGo.transform.SetParent(column.transform, false);
            smokeGo.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            ConfigureSpriteLoopParticles(smokeGo, smokeFrames, material, new Color(0.38f, 0.38f, 0.4f, 0.78f),
                startLifetime: 2.9f, startSpeed: 0.3f, startSize: 1.15f, rate: 8f, rise: 0.7f, sortingOrder: 27);
        }

        public static void ApplyPixelLabForestStyle()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var ground = FindGroundTilemap();
            var canopy = FindTilemap("Tilemap_Canopy");
            if (ground == null || canopy == null)
            {
                Debug.LogError("Campus Quest: faltan Tilemap_Ground o Tilemap_Canopy en Sp_Bosque.");
                return;
            }

            EnsureTileFolder();
            EnsurePixelLabTileFolder();
            var palette = BuildPixelLabPalette();
            var mixel = LoadMixelTileSet();
            var legacyCanopy = AssetDatabase.LoadAssetAtPath<Tile>(TilesRoot + "/SP_Canopy.asset");

            int groundSwaps = 0;
            foreach (var pos in ground.cellBounds.allPositionsWithin)
            {
                var current = ground.GetTile(pos);
                if (current == null)
                    continue;

                if (current == mixel.GrassA || current == mixel.GrassB)
                {
                    ground.SetTile(pos, palette.GrassVariants[(pos.x * 3 + pos.y * 7) & 3]);
                    groundSwaps++;
                }
                else if (mixel.PathAndDirt.Contains(current))
                {
                    ground.SetTile(pos, palette.Dirt);
                    groundSwaps++;
                }
            }

            int canopySwaps = 0;
            foreach (var pos in canopy.cellBounds.allPositionsWithin)
            {
                var current = canopy.GetTile(pos);
                if (current == null)
                    continue;
                if (current != legacyCanopy && !palette.Pines.Contains(current))
                    continue;

                canopy.SetTile(pos, palette.Pines[(pos.x + pos.y) % palette.Pines.Length]);
                canopySwaps++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Campus Quest: estilo PixelLab aplicado (suelo " + groundSwaps + ", copas " + canopySwaps + ").");
        }

        static void EnsurePixelLabTileFolder()
        {
            if (!AssetDatabase.IsValidFolder(TilesRoot + "/PixelLab"))
                AssetDatabase.CreateFolder(TilesRoot, "PixelLab");
        }

        struct PixelLabPalette
        {
            public Tile Dirt;
            public Tile[] GrassVariants;
            public Tile[] Pines;
        }

        struct MixelTileSet
        {
            public Tile GrassA;
            public Tile GrassB;
            public HashSet<Tile> PathAndDirt;
        }

        static MixelTileSet LoadMixelTileSet()
        {
            var pathNames = new[]
            {
                "SP_Mixel_Dirt_A", "SP_Mixel_Dirt_B", "SP_Mixel_Dirt_C", "SP_Mixel_Dirt_D",
                "SP_Mixel_Path_N", "SP_Mixel_Path_S", "SP_Mixel_Path_E", "SP_Mixel_Path_W",
                "SP_Mixel_Path_NE", "SP_Mixel_Path_NW", "SP_Mixel_Path_SE", "SP_Mixel_Path_SW"
            };
            var set = new HashSet<Tile>();
            foreach (var name in pathNames)
            {
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(TilesRoot + "/" + name + ".asset");
                if (tile != null)
                    set.Add(tile);
            }

            return new MixelTileSet
            {
                GrassA = AssetDatabase.LoadAssetAtPath<Tile>(TilesRoot + "/SP_Mixel_Grass_A.asset"),
                GrassB = AssetDatabase.LoadAssetAtPath<Tile>(TilesRoot + "/SP_Mixel_Grass_B.asset"),
                PathAndDirt = set
            };
        }

        struct BunkerFieldPalette
        {
            public Tile[] Floors;
            public Tile[] Debris;
            public Tile Hatch;
            public Tile Barrel;
        }

        static BunkerFieldPalette BuildBunkerFieldPalette()
        {
            var byIndex = LoadSprites(BunkerRetroSheet)
                .Select(s => new { Sprite = s, Index = ParseBunkerSpriteIndex(s.name) })
                .Where(p => p.Index >= 0)
                .ToDictionary(p => p.Index, p => p.Sprite);
            Sprite Require(int index)
            {
                if (!byIndex.TryGetValue(index, out var sprite))
                    throw new InvalidOperationException("Bunker retro sin sprite _" + index);
                return TrimSpriteTopWhiteGuide(sprite);
            }

            Tile Floor(int index) => EnsureTile(Require(index), "SP_Bunker_Floor_" + index);
            return new BunkerFieldPalette
            {
                Floors = new[] { 0, 1, 2, 6, 14, 16, 33, 34 }.Select(Floor).ToArray(),
                Debris = new[] { 18, 24, 35, 52 }.Select(Floor).ToArray(),
                Hatch = EnsureTile(Require(4), "SP_Bunker_Hatch"),
                Barrel = EnsureTile(Require(25), "SP_Bunker_Barrel")
            };
        }

        static int ParseBunkerSpriteIndex(string spriteName)
        {
            const string suffix = "objetos-bunker-retro_";
            int i = spriteName.LastIndexOf(suffix, StringComparison.Ordinal);
            if (i < 0)
                return -1;
            return int.TryParse(spriteName.Substring(i + suffix.Length), out var index) ? index : -1;
        }

        static Sprite TrimSpriteTopWhiteGuide(Sprite source)
        {
            if (source == null)
                return source;
            var tex = source.texture;
            if (tex == null)
                return source;

            string cacheKey = "SP_BunkerTrim_" + source.GetInstanceID();
            var cached = AssetDatabase.LoadAssetAtPath<Sprite>(PixelLabTilesRoot + "/" + cacheKey + ".png");
            if (cached != null)
                return cached;

            var readable = MakeReadable(tex);
            try
            {
                var rect = source.textureRect;
                int x0 = Mathf.RoundToInt(rect.x);
                int y0 = Mathf.RoundToInt(rect.y);
                int w = Mathf.RoundToInt(rect.width);
                int h = Mathf.RoundToInt(rect.height);
                if (w <= 2 || h <= 2)
                    return source;

                int whiteTopRows = 0;
                for (int row = h - 1; row >= 0 && whiteTopRows < 2; row--)
                {
                    int white = 0;
                    int samples = 0;
                    for (int col = 0; col < w; col++)
                    {
                        var c = readable.GetPixel(x0 + col, y0 + row);
                        if (c.a < 0.1f)
                            continue;
                        samples++;
                        if (c.r > 0.92f && c.g > 0.92f && c.b > 0.92f)
                            white++;
                    }

                    if (samples > 0 && white >= samples * 0.7f)
                        whiteTopRows++;
                    else
                        break;
                }

                if (whiteTopRows == 0)
                    return source;

                int trimH = h - whiteTopRows;
                var pixels = readable.GetPixels(x0, y0, w, trimH);
                var slice = new Texture2D(w, trimH, TextureFormat.RGBA32, false);
                slice.SetPixels(pixels);
                slice.Apply();

                string path = PixelLabTilesRoot + "/" + cacheKey + ".png";
                File.WriteAllBytes(path, slice.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(slice);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 32f;
                    importer.filterMode = FilterMode.Point;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                }

                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(readable);
            }
        }

        static bool IsNorthPathCell(int x, int y) =>
            y >= NorthRouteStartY && y <= NorthPathEndY && x >= NorthPathLeft && x <= NorthPathRight;

        static bool IsFieldCell(int x, int y) =>
            y >= FieldStartY && y < FullMapH && x >= FieldLeft && x <= FieldRight;

        static bool IsNorthEdgeTree(int x, int y, bool onPath, bool inField)
        {
            if (onPath)
                return false;
            if (x == 0 || x == MapW - 1)
                return true;
            if (inField)
                return x <= FieldLeft - 1 || x >= FieldRight + 1;
            if (x >= NorthPathLeft - 2 && x <= NorthPathRight + 2)
                return false;
            if (x <= 5 || x >= MapW - 6)
                return (x * 5 + y * 11) % 5 != 0;
            return false;
        }

        static readonly int[] CampoMilitarExcludedIndices =
        {
            7, 11, 15, 38, 40, 41
        };

        static void EnsureCampoMilitarProps()
        {
            var existing = GameObject.Find("SP_CampoMilitar");
            if (existing != null)
                Undo.DestroyObjectImmediate(existing);

            var byIndex = LoadSprites(CampoMilitarSheet)
                .Select(s => new { Sprite = s, Index = ParseCompoMilitarIndex(s.name) })
                .Where(p => p.Index >= 0 && !CampoMilitarExcludedIndices.Contains(p.Index))
                .Where(p => p.Sprite.rect.width >= 16 && p.Sprite.rect.height >= 16)
                .ToDictionary(p => p.Index, p => p.Sprite);

            if (byIndex.Count == 0)
            {
                Debug.LogWarning("Campus Quest: CompoMilitar sin sprites utilizables.");
                return;
            }

            var placementsCore = new (int index, float wx, float wy, int order)[]
            {
                // Boca del sendero: alambre, letreros, sacos y restos a cada lado (sin camino)
                (8, 9f, 96.5f, 9),
                (14, 37f, 96f, 9),
                (23, 11.5f, 97f, 8),
                (31, 28f, 96.5f, 7),
                (17, 30f, 97f, 7),
                (29, 33f, 96f, 7),
                (5, 10f, 96.5f, 10),
                (6, 12f, 96.5f, 10),
                (36, 35f, 96.5f, 8),
                (35, 37f, 97f, 8),

                // Foco oeste: tienda _0 + sacos, cajas, escombro, hoguera _32
                (0, 10f, 109f, 12),
                (5, 9.5f, 108f, 10),
                (6, 11.5f, 108.5f, 10),
                (24, 10f, 107.5f, 8),
                (25, 11f, 106.5f, 7),
                (33, 9f, 110.5f, 8),
                (35, 12f, 109f, 8),
                (32, 10.5f, 109.8f, 17),
                (43, 9.5f, 107f, 18),

                // Zona entrenamiento oeste (ruinas sueltas, no en camino)
                (4, 11f, 101f, 11),
                (9, 10f, 104f, 8),
                (12, 10.5f, 102f, 8),
                (16, 11.5f, 105f, 8),
                (20, 9.5f, 103f, 7),
                (22, 11f, 107f, 8),
                (34, 10.5f, 104f, 8),
                (39, 11f, 101f, 8),

                // Foco este: tienda en llamas _2 + barriles, ceniza, llama _42
                (2, 26f, 108f, 13),
                (28, 25f, 107f, 9),
                (30, 27f, 106.5f, 7),
                (24, 25.5f, 109.5f, 8),
                (39, 26.5f, 110f, 8),
                (44, 25f, 107.5f, 18),
                (42, 26f, 108.8f, 18),

                // Foco torre _3 y caseta _4: alambre, blancos, restos
                (3, 35f, 107f, 14),
                (4, 33f, 101f, 11),
                (8, 37f, 103f, 9),
                (14, 36f, 104.5f, 9),
                (9, 34f, 100f, 8),
                (13, 30f, 105f, 8),
                (36, 35f, 100.5f, 8),
                (37, 34f, 109f, 8),
                (26, 33f, 102f, 7),
                (27, 36f, 108f, 7),

                // Campo este central (pocas piezas, pegadas a estructuras)
                (19, 28f, 104f, 8),
                (22, 34f, 109f, 8),
                (31, 25f, 109f, 7),
                (29, 30f, 111f, 10),

                // Foco sureste: junto al camino al entrar (y 96–101, este del sendero)
                (2, 27.5f, 99f, 13),
                (4, 30.5f, 98.5f, 11),
                (5, 25f, 97.5f, 10),
                (5, 28f, 96.8f, 10),
                (6, 31f, 97f, 10),
                (6, 26.5f, 98.5f, 10),
                (24, 29f, 98f, 8),
                (25, 27f, 99.5f, 7),
                (26, 32f, 97.5f, 7),
                (27, 25.5f, 100f, 7),
                (28, 30f, 96.5f, 9),
                (33, 27.5f, 100.5f, 8),
                (35, 29.5f, 99f, 8),
                (39, 26f, 98f, 8),
                (42, 27.5f, 99.2f, 18),
                (43, 30.5f, 98.8f, 18),
                (44, 28.5f, 97f, 18)
            };

            var placements = placementsCore.Concat(BuildCampoSideBandPlacements()).ToArray();

            var root = new GameObject("SP_CampoMilitar");
            Undo.RegisterCreatedObjectUndo(root, "SP_CampoMilitar");

            foreach (var (index, wx, wy, order) in placements)
            {
                if (!byIndex.TryGetValue(index, out var sprite))
                    continue;

                string goName = "CompoMilitar_" + index;
                var existingChild = root.transform.Find(goName);
                if (existingChild != null)
                {
                    int suffix = 1;
                    while (root.transform.Find(goName + "_" + suffix) != null)
                        suffix++;
                    goName = goName + "_" + suffix;
                }

                var go = new GameObject(goName);
                Undo.RegisterCreatedObjectUndo(go, goName);
                go.transform.SetParent(root.transform, false);
                float footX = NudgeCampoMilitarFootX(sprite, wx, wy);
                go.transform.position = CampoMilitarSpritePosition(sprite, footX, wy);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = order;
            }
        }

        static (int index, float wx, float wy, int order)[] BuildCampoSideBandPlacements()
        {
            var list = new List<(int index, float wx, float wy, int order)>();
            int[] west = { 5, 6, 24, 25, 33, 34, 35, 39, 9, 12, 16, 20, 22 };
            int[] east = { 24, 25, 26, 27, 28, 30, 31, 33, 35, 36, 37, 19, 22 };
            int[] westFire = { 32, 42, 43, 44 };
            int[] eastFire = { 42, 43, 44 };

            for (int y = 97; y <= 111; y += 2)
            {
                float westX = 9.5f + (y % 3) * 0.6f;
                list.Add((west[(y / 2) % west.Length], westX, y, 8));
                list.Add((west[(y / 2 + 3) % west.Length], westX + 1.2f, y + 0.4f, 7));
                list.Add((west[(y / 2 + 6) % west.Length], 11f, y + 0.2f, 8));
                if (y % 4 == 1)
                    list.Add((westFire[(y / 2) % westFire.Length], westX + 0.3f, y + 0.5f, 17));

                float eastX = 26f + (y % 4) * 1.1f;
                list.Add((east[(y / 2) % east.Length], eastX, y, 8));
                list.Add((east[(y / 2 + 4) % east.Length], eastX + 2f, y + 0.3f, 7));
                list.Add((east[(y / 2 + 2) % east.Length], eastX + 4.5f, y + 0.1f, 8));
                if (y % 3 == 0)
                    list.Add((eastFire[(y / 2) % eastFire.Length], eastX + 1f, y + 0.6f, 18));
            }

            for (int y = 96; y <= 111; y += 5)
            {
                list.Add((8, 9f, y + 0.5f, 9));
                list.Add((14, 37f, y + 0.5f, 9));
            }

            return list.ToArray();
        }

        static int ParseCompoMilitarIndex(string spriteName)
        {
            const string suffix = "CompoMilitar_";
            int i = spriteName.LastIndexOf(suffix, StringComparison.Ordinal);
            if (i < 0)
                return -1;
            return int.TryParse(spriteName.Substring(i + suffix.Length), out var index) ? index : -1;
        }

        static Vector3 CampoMilitarSpritePosition(Sprite sprite, float footCenterX, float footY)
        {
            float halfWidth = sprite.bounds.extents.x;
            return new Vector3(footCenterX - halfWidth, footY, 0f);
        }

        static float NudgeCampoMilitarFootX(Sprite sprite, float footCenterX, float footY)
        {
            if (footY < FieldStartY)
                return footCenterX;

            float half = sprite.bounds.extents.x;
            float left = footCenterX - half;
            float right = footCenterX + half;
            if (right <= FieldLaneLeft || left >= FieldLaneRight)
                return footCenterX;

            const float pad = 0.2f;
            float laneCenter = (FieldLaneLeft + FieldLaneRight) * 0.5f;
            float nudged = footCenterX <= laneCenter
                ? FieldLaneLeft - half - pad
                : FieldLaneRight + half + pad;

            float minX = FieldLeft + half + 0.25f;
            float maxX = FieldRight + 1f - half - 0.25f;
            return Mathf.Clamp(nudged, minX, maxX);
        }

        static Vector3 NudgeCampoFirePosition(Vector3 position)
        {
            if (position.y < FieldStartY)
                return position;
            if (position.x > FieldLaneLeft && position.x < FieldLaneRight)
            {
                float laneCenter = (FieldLaneLeft + FieldLaneRight) * 0.5f;
                float x = position.x <= laneCenter ? FieldLaneLeft - 1.2f : FieldLaneRight + 1.2f;
                return new Vector3(x, position.y, position.z);
            }

            return position;
        }

        static void EnsureFieldFireSmokeColumns()
        {
            var existingRoot = GameObject.Find("SP_FieldFires");
            if (existingRoot != null)
                Undo.DestroyObjectImmediate(existingRoot);

            var fireFrames = LoadExplosionFrames(FireVfxRoot, "Fire");
            var smokeFrames = LoadExplosionFrames(SmokeVfxRoot, "Smoke");
            var material = EnsureVfxParticleMaterial();
            if (fireFrames.Length == 0 || smokeFrames.Length == 0)
            {
                Debug.LogWarning("Campus Quest: no hay sprites de fuego o humo.");
                return;
            }

            var root = new GameObject("SP_FieldFires");
            Undo.RegisterCreatedObjectUndo(root, "SP_FieldFires");

            var positions = new[]
            {
                new Vector3(10.5f, 109.8f, 0f),
                new Vector3(11f, 103.5f, 0f),
                new Vector3(10f, 106.5f, 0f),
                new Vector3(11.5f, 101f, 0f),
                new Vector3(26f, 108.8f, 0f),
                new Vector3(27f, 106.5f, 0f),
                new Vector3(33f, 101f, 0f),
                new Vector3(35f, 107f, 0f),
                new Vector3(32f, 105f, 0f),
                new Vector3(34f, 100.5f, 0f),
                new Vector3(30.5f, 98.5f, 0f),
                new Vector3(10f, 98f, 0f),
                new Vector3(11f, 102f, 0f),
                new Vector3(10.5f, 105f, 0f),
                new Vector3(11f, 110f, 0f),
                new Vector3(27f, 100f, 0f),
                new Vector3(29f, 103f, 0f),
                new Vector3(31f, 106f, 0f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                var column = new GameObject("SP_FireSmoke_" + (i + 1));
                Undo.RegisterCreatedObjectUndo(column, column.name);
                column.transform.SetParent(root.transform, false);
                column.transform.position = NudgeCampoFirePosition(positions[i]);

                var fireGo = new GameObject("Fire");
                fireGo.transform.SetParent(column.transform, false);
                fireGo.transform.localPosition = Vector3.zero;
                ConfigureSpriteLoopParticles(fireGo, fireFrames, material, new Color(1f, 0.55f, 0.15f, 1f),
                    startLifetime: 0.55f, startSpeed: 0.9f, startSize: 0.45f, rate: 14f, rise: 1.6f, sortingOrder: 25);

                var smokeGo = new GameObject("Smoke");
                smokeGo.transform.SetParent(column.transform, false);
                smokeGo.transform.localPosition = new Vector3(0f, 0.85f, 0f);
                ConfigureSpriteLoopParticles(smokeGo, smokeFrames, material, new Color(0.38f, 0.38f, 0.4f, 0.78f),
                    startLifetime: 2.9f, startSpeed: 0.3f, startSize: 1.15f, rate: 8f, rise: 0.7f, sortingOrder: 27);
            }
        }

        static Sprite[] LoadExplosionFrames(string folder, string prefix)
        {
            var frames = new List<Sprite>();
            for (int i = 1; i <= 6; i++)
            {
                var sheet = LoadSprites(folder + "/" + prefix + i + ".png");
                var primary = sheet.FirstOrDefault(s => s.name == prefix + i + "_0")
                    ?? sheet.Where(s => s.rect.width >= 40 && s.rect.height >= 40)
                        .OrderByDescending(s => s.rect.width * s.rect.height)
                        .FirstOrDefault();
                if (primary != null)
                    frames.Add(primary);
            }

            return frames.ToArray();
        }

        static Material EnsureVfxParticleMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(VfxParticleMaterialPath);
            if (existing != null)
                return existing;

            EnsureMaterialFolder();
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                ?? Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogWarning("Campus Quest: no hay shader URP 2D para partículas.");
                return null;
            }

            var material = new Material(shader) { name = "SP_VfxSpriteUnlit" };
            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 0f);
            AssetDatabase.CreateAsset(material, VfxParticleMaterialPath);
            return material;
        }

        static void EnsureMaterialFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Sprites/Enviroment/Sp/Materials"))
                AssetDatabase.CreateFolder("Assets/Sprites/Enviroment/Sp", "Materials");
        }

        static void ConfigureSpriteLoopParticles(
            GameObject go,
            Sprite[] frames,
            Material material,
            Color tint,
            float startLifetime,
            float startSpeed,
            float startSize,
            float rate,
            float rise,
            int sortingOrder)
        {
            var ps = go.AddComponent<ParticleSystem>();
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = sortingOrder;

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = startLifetime;
            main.startSpeed = startSpeed;
            main.startSize = startSize;
            main.startColor = tint;
            main.maxParticles = 48;

            var emission = ps.emission;
            emission.rateOverTime = rate;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.12f;

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.y = new ParticleSystem.MinMaxCurve(rise);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(tint, 0f),
                    new GradientColorKey(tint, 0.7f),
                    new GradientColorKey(tint, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(Mathf.Clamp01(tint.a), 0f),
                    new GradientAlphaKey(Mathf.Clamp01(tint.a * 0.75f), 0.6f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            var tex = ps.textureSheetAnimation;
            tex.enabled = true;
            tex.mode = ParticleSystemAnimationMode.Sprites;
            tex.fps = 12f;
            for (int i = 0; i < frames.Length; i++)
                tex.AddSprite(frames[i]);
        }

        static PixelLabPalette BuildPixelLabPalette()
        {
            var bySpriteName = LoadSprites(PixelLabForestSheet)
                .ToDictionary(s => s.name, s => s, StringComparer.Ordinal);
            Sprite RequireSprite(string name)
            {
                if (!bySpriteName.TryGetValue(name, out var sprite))
                    throw new InvalidOperationException("PixelLab: falta sprite " + name);
                return sprite;
            }

            return new PixelLabPalette
            {
                Dirt = EnsureTile(RequireSprite("SP_Bosque_19"), "SP_Pixel_Dirt"),
                GrassVariants = new[]
                {
                    EnsureTile(RequireSprite("SP_Bosque_0"), "SP_Pixel_Grass_0"),
                    EnsureTile(RequireSprite("SP_Bosque_2"), "SP_Pixel_Grass_1"),
                    EnsureTile(RequireSprite("SP_Bosque_3"), "SP_Pixel_Grass_2"),
                    EnsureTile(RequireSprite("SP_Bosque_9"), "SP_Pixel_Grass_3")
                },
                Pines = new[]
                {
                    EnsureTile(EnsurePixelLabPineSprite("SP_Pixel_Pine_0", 177, 176, 31, 79), "SP_Pixel_Pine_Tile_0"),
                    EnsureTile(EnsurePixelLabPineSprite("SP_Pixel_Pine_1", 225, 193, 30, 62), "SP_Pixel_Pine_Tile_1"),
                    EnsureTile(EnsurePixelLabPineSprite("SP_Pixel_Pine_2", 225, 97, 30, 62), "SP_Pixel_Pine_Tile_2")
                }
            };
        }

        static Sprite EnsurePixelLabPineSprite(string fileName, int x, int y, int width, int height)
        {
            string pngPath = PixelLabTilesRoot + "/" + fileName + ".png";
            string metaPath = pngPath + ".meta";

            if (!File.Exists(pngPath) || !File.Exists(metaPath) || new FileInfo(pngPath).Length == 0)
            {
                var source = AssetDatabase.LoadAssetAtPath<Texture2D>(PixelLabForestSheet);
                if (source == null)
                    throw new InvalidOperationException("No se encuentra " + PixelLabForestSheet);

                var readable = MakeReadable(source);
                try
                {
                    var pixels = readable.GetPixels(x, y, width, height);
                    var slice = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    slice.SetPixels(pixels);
                    slice.Apply();
                    File.WriteAllBytes(pngPath, slice.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(slice);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(readable);
                }

                AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);
            }

            ConfigurePixelLabPineImporter(pngPath);
            return AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
        }

        static void ConfigurePixelLabPineImporter(string pngPath)
        {
            var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
            if (importer == null)
                return;

            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                dirty = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                dirty = true;
            }
            if (importer.spritePixelsPerUnit != 32f)
            {
                importer.spritePixelsPerUnit = 32f;
                dirty = true;
            }
            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                dirty = true;
            }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                dirty = true;
            }
            if (importer.alphaIsTransparency != true)
            {
                importer.alphaIsTransparency = true;
                dirty = true;
            }

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (settings.spriteAlignment != (int)SpriteAlignment.Custom
                || settings.spritePivot != new Vector2(0.5f, 0f)
                || Math.Abs(settings.spritePixelsPerUnit - 32f) > 0.01f)
            {
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(0.5f, 0f);
                settings.spritePixelsPerUnit = 32;
                importer.SetTextureSettings(settings);
                dirty = true;
            }

            if (dirty)
                importer.SaveAndReimport();
        }

        static Texture2D MakeReadable(Texture2D source)
        {
            var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, rt);
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            copy.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            return copy;
        }

        static bool[,] BuildExtensionPathMask()
        {
            var path = new bool[MapW, RouteMapH];
            int pathLeft = PathCenterX - PathHalfWidth;
            int pathRight = PathCenterX + PathHalfWidth - 1;
            FillPathRect(path, pathLeft, pathRight, 0, MapH - 1);
            FillPathRect(path, pathLeft, pathRight, MapH, 42);
            FillPathRect(path, 6, 27, 36, 43);
            FillPathRect(path, 2, 22, 34, 52);
            FillPathRect(path, 8, 15, 48, 70);
            FillPathRect(path, 4, 26, 66, 74);
            FillPathRect(path, 10, 22, 74, RouteMapH - 1);
            return path;
        }

        static void FillPathRect(bool[,] path, int xMin, int xMax, int yMin, int yMax)
        {
            int width = path.GetLength(0);
            int height = path.GetLength(1);
            for (int y = yMin; y <= yMax; y++)
            for (int x = xMin; x <= xMax; x++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                    path[x, y] = true;
            }
        }

        static bool IsExtensionTree(int x, int y, bool[,] path)
        {
            if (path[x, y])
                return false;
            if (x == 0 || x == MapW - 1 || y == RouteMapH - 1)
                return true;
            if ((x * 5 + y * 11) % 5 == 0)
                return false;
            return (x + y) % 2 == 0;
        }

        static Tilemap FindGroundTilemap()
        {
            foreach (var tilemap in UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            {
                if (tilemap != null && tilemap.name == "Tilemap_Ground")
                    return tilemap;
            }
            return null;
        }

        static Tilemap FindTilemap(string name)
        {
            foreach (var tilemap in UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            {
                if (tilemap != null && tilemap.name == name)
                    return tilemap;
            }
            return null;
        }

        struct MixelGroundPalette
        {
            public Tile GrassA;
            public Tile GrassB;
            public Tile DirtA;
            public Tile DirtB;
            public Tile DirtC;
            public Tile DirtD;
            public Tile EdgeNorth;
            public Tile EdgeSouth;
            public Tile EdgeWest;
            public Tile EdgeEast;
            public Tile CornerNorthWest;
            public Tile CornerNorthEast;
            public Tile CornerSouthWest;
            public Tile CornerSouthEast;
        }

        static MixelGroundPalette BuildMixelGroundPalette()
        {
            var byIndex = LoadSprites(MixelGroundSheet)
                .Select(s => new { Sprite = s, Index = ParseGroundSpriteIndex(s.name) })
                .Where(p => p.Index >= 0)
                .ToDictionary(p => p.Index, p => p.Sprite);

            Tile Require(int index, string assetName)
            {
                if (!byIndex.TryGetValue(index, out var sprite))
                    throw new InvalidOperationException(
                        "Ground Tileset 1.2 sin sprite _" + index + " (" + assetName + ").");
                return EnsureTile(sprite, assetName);
            }

            return new MixelGroundPalette
            {
                GrassA = Require(11, "SP_Mixel_Grass_A"),
                GrassB = Require(23, "SP_Mixel_Grass_B"),
                DirtA = Require(98, "SP_Mixel_Dirt_A"),
                DirtB = Require(99, "SP_Mixel_Dirt_B"),
                DirtC = Require(110, "SP_Mixel_Dirt_C"),
                DirtD = Require(124, "SP_Mixel_Dirt_D"),
                EdgeNorth = Require(72, "SP_Mixel_Path_N"),
                EdgeSouth = Require(84, "SP_Mixel_Path_S"),
                EdgeWest = Require(76, "SP_Mixel_Path_W"),
                EdgeEast = Require(77, "SP_Mixel_Path_E"),
                CornerNorthWest = Require(96, "SP_Mixel_Path_NW"),
                CornerNorthEast = Require(97, "SP_Mixel_Path_NE"),
                CornerSouthWest = Require(108, "SP_Mixel_Path_SW"),
                CornerSouthEast = Require(109, "SP_Mixel_Path_SE")
            };
        }

        static int ParseGroundSpriteIndex(string spriteName)
        {
            const string suffix = "Ground Tileset 1.2_";
            int i = spriteName.LastIndexOf(suffix, StringComparison.Ordinal);
            if (i < 0)
                return -1;
            return int.TryParse(spriteName.Substring(i + suffix.Length), out var index) ? index : -1;
        }

        static bool IsPathCell(bool[,] path, int x, int y)
        {
            int width = path.GetLength(0);
            int height = path.GetLength(1);
            if (x < 0 || x >= width || y < 0 || y >= height)
                return true;
            return path[x, y];
        }

        static Tile SelectGrassGroundTile(MixelGroundPalette palette, int x, int y) =>
            (x * 3 + y * 7) % 11 == 0 ? palette.GrassB : palette.GrassA;

        static Tile SelectPathGroundTile(MixelGroundPalette palette, bool[,] path, int x, int y)
        {
            bool north = IsPathCell(path, x, y + 1);
            bool south = IsPathCell(path, x, y - 1);
            bool west = IsPathCell(path, x - 1, y);
            bool east = IsPathCell(path, x + 1, y);

            if (!north && !west)
                return palette.CornerNorthWest;
            if (!north && !east)
                return palette.CornerNorthEast;
            if (!south && !west)
                return palette.CornerSouthWest;
            if (!south && !east)
                return palette.CornerSouthEast;
            if (!west)
                return palette.EdgeWest;
            if (!east)
                return palette.EdgeEast;
            if (!north)
                return palette.EdgeNorth;
            if (!south)
                return palette.EdgeSouth;

            return SelectDirtFillTile(palette, x, y);
        }

        static Tile SelectDirtFillTile(MixelGroundPalette palette, int x, int y)
        {
            switch ((x * 5 + y * 11) % 4)
            {
                case 0: return palette.DirtA;
                case 1: return palette.DirtB;
                case 2: return palette.DirtC;
                default: return palette.DirtD;
            }
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
