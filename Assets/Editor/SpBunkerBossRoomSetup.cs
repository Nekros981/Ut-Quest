#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

public static class SpBunkerBossRoomSetup
{
    const string ScenePath = "Assets/Scenes/Sp_Bunker.unity";
    const string TilesRoot = "Assets/Sprites/Enviroment/Sp/Tiles";
    const string BunkerRetroSheet = "Assets/Sprites/Enviroment/Sp/PixelLab/objetos-bunker-retro.png";
    const string DecorRootName = "SP_BunkerJefe_Decor";
    const string BossSpawnName = "SP_JefeSpawn";

    const int MapW = 17;
    const int EntranceH = 14;
    const int RoomH = 14;
    const int RoomBottom = EntranceH;
    const int RoomTop = EntranceH + RoomH - 1;
    const int WallSpriteIndex = 5;
    const string PlateFloorTilePath = "Assets/Sprites/Enviroment/Sp/Tiles/PixelLab/Floor_Bunker/Floor_Bunker/Tiles_Bunker_22.asset";
    const int RedLightIndex = 36;

    [MenuItem("Campus Quest/SP/Extender sala del jefe")]
    public static void ExpandFromMenu()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var ground = FindTilemap("Tilemap_Ground");
        var walls = FindTilemap("Tilemap_Walls");
        if (ground == null || walls == null)
            throw new InvalidOperationException("Sp_Bunker no tiene Tilemap_Ground o Tilemap_Walls.");

        ClearExtension(ground, walls);
        DestroyIfExists(DecorRootName);
        DestroyIfExists(BossSpawnName);

        Directory.CreateDirectory(TilesRoot);
        var retro = LoadRetroSprites();
        PaintExtension(ground, walls, retro);

        var decorRoot = new GameObject(DecorRootName);
        Undo.RegisterCreatedObjectUndo(decorRoot, DecorRootName);
        PlaceBorderDecor(decorRoot.transform, retro);
        EnsureBossSpawnMarker();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Campus Quest] Sp_Bunker: sala del jefe extendida hacia arriba.");
    }

    static Tilemap FindTilemap(string name)
    {
        var grid = GameObject.Find("Grid");
        if (grid == null)
            return null;
        foreach (var map in grid.GetComponentsInChildren<Tilemap>(true))
        {
            if (map.gameObject.name == name)
                return map;
        }
        return null;
    }

    static void ClearExtension(Tilemap ground, Tilemap walls)
    {
        for (int y = RoomBottom; y <= RoomTop; y++)
        for (int x = 0; x < MapW; x++)
        {
            var cell = new Vector3Int(x, y, 0);
            ground.SetTile(cell, null);
            walls.SetTile(cell, null);
        }
    }

    static void DestroyIfExists(string name)
    {
        var go = GameObject.Find(name);
        if (go != null)
            Undo.DestroyObjectImmediate(go);
    }

    static Dictionary<int, Sprite> LoadRetroSprites() =>
        AssetDatabase.LoadAllAssetsAtPath(BunkerRetroSheet).OfType<Sprite>()
            .Select(s => new { Sprite = s, Index = ParseRetroIndex(s.name) })
            .Where(p => p.Index >= 0)
            .ToDictionary(p => p.Index, p => p.Sprite);

    static void PaintExtension(Tilemap ground, Tilemap walls, Dictionary<int, Sprite> retro)
    {
        Sprite Require(int index)
        {
            if (!retro.TryGetValue(index, out var sprite))
                throw new InvalidOperationException("objetos-bunker-retro sin _" + index);
            return sprite;
        }

        var wall = EnsureTile(Require(WallSpriteIndex), "SP_BossRoom_Wall_" + WallSpriteIndex, Tile.ColliderType.Sprite);
        var plate = AssetDatabase.LoadAssetAtPath<Tile>(PlateFloorTilePath);
        if (plate == null)
            throw new InvalidOperationException("No está el piso pintado: " + PlateFloorTilePath);

        for (int y = RoomBottom; y <= RoomTop; y++)
        for (int x = 0; x < MapW; x++)
        {
            var cell = new Vector3Int(x, y, 0);
            bool side = x == 0 || x == MapW - 1;
            bool north = y == RoomTop;

            if (side || north)
            {
                walls.SetTile(cell, wall);
                ground.SetTile(cell, null);
                continue;
            }

            walls.SetTile(cell, null);
            ground.SetTile(cell, plate);
            ground.SetColor(cell, Color.white);
        }
    }

    static void PlaceBorderDecor(Transform root, Dictionary<int, Sprite> retro)
    {
        Sprite S(int i) => retro.TryGetValue(i, out var s) ? s : null;
        float Y(int localY) => RoomBottom + localY + 0.5f;

        SpawnPipe(root, S(8), 3.5f, Y(12), false, "PipeN_L");
        SpawnPipe(root, S(8), 8.5f, Y(12), false, "PipeN_C");
        SpawnPipe(root, S(8), 13.5f, Y(12), false, "PipeN_R");
        SpawnPipe(root, S(11), 5.5f, Y(12), false, "PipeN_JoinL");
        SpawnPipe(root, S(14), 10.5f, Y(12), false, "PipeN_JoinR");
        SpawnPipe(root, S(15), 6.5f, Y(11), false, "ValveL");
        SpawnPipe(root, S(15), 11.5f, Y(11), true, "ValveR");

        for (int localY = 5; localY <= 10; localY += 2)
        {
            SpawnPipe(root, S(10), 1.5f, Y(localY), false, "PipeV_L_" + localY);
            SpawnPipe(root, S(10), 15.5f, Y(localY), true, "PipeV_R_" + localY);
        }

        SpawnPipe(root, S(9), 2.5f, Y(11), false, "Elbow_L");
        SpawnPipe(root, S(9), 14.5f, Y(11), true, "Elbow_R");

        SpawnProp(root, S(24), 1.5f, Y(4), "Crate_L1");
        SpawnProp(root, S(25), 2.5f, Y(5), "Crate_L2");
        SpawnProp(root, S(26), 1.5f, Y(2), "Crate_L3");
        SpawnProp(root, S(27), 15.5f, Y(4), "Barrel_R1");
        SpawnProp(root, S(28), 14.5f, Y(5), "Barrel_R2");
        SpawnProp(root, S(29), 15.5f, Y(2), "Tank_R");
        SpawnProp(root, S(23), 2.5f, Y(2), "Sacks_L");
        SpawnProp(root, S(31), 14.5f, Y(2), "Rubble_R");

        SpawnProp(root, S(32), 1.5f, Y(7), "Console_L");
        SpawnProp(root, S(33), 1.5f, Y(9), "Cabinet_L");
        SpawnProp(root, S(34), 15.5f, Y(7), "Server_R");
        SpawnProp(root, S(48), 15.5f, Y(9), "Lockers_R");
        SpawnProp(root, S(56), 2.5f, Y(11), "Shelf_LT");
        SpawnProp(root, S(56), 14.5f, Y(11), "Shelf_RT");

        if (S(RedLightIndex) != null)
        {
            var lightL = SpawnSprite(root, S(RedLightIndex), new Vector3(4.5f, Y(12), 0f), 5, "BossLight_L");
            SpawnPointLight(lightL.transform, new Vector3(0.35f, -0.25f, 0f));
            var lightR = SpawnSprite(root, S(RedLightIndex), new Vector3(12.5f, Y(12), 0f), 5, "BossLight_R");
            SpawnPointLight(lightR.transform, new Vector3(-0.35f, -0.25f, 0f));
        }
    }

    static void SpawnPipe(Transform root, Sprite sprite, float x, float y, bool flipX, string name)
    {
        if (sprite == null)
            return;
        var go = SpawnSprite(root, sprite, new Vector3(x, y, 0f), 3, name);
        if (flipX)
            go.transform.localScale = new Vector3(-1f, 1f, 1f);
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.9f, 0.35f);
    }

    static void SpawnProp(Transform root, Sprite sprite, float x, float y, string name)
    {
        if (sprite == null)
            return;
        var go = SpawnSprite(root, sprite, new Vector3(x, y, 0f), 3, name);
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.8f, 0.8f);
    }

    static void EnsureBossSpawnMarker()
    {
        var go = new GameObject(BossSpawnName);
        Undo.RegisterCreatedObjectUndo(go, BossSpawnName);
        go.transform.position = new Vector3(8f, RoomBottom + 9f, 0f);
    }

    static void SpawnPointLight(Transform parent, Vector3 localPos)
    {
        var go = new GameObject("RedGlow");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = new Color(1f, 0.22f, 0.18f, 1f);
        light.intensity = 0.55f;
        light.pointLightInnerRadius = 0.1f;
        light.pointLightOuterRadius = 2.4f;
        light.falloffIntensity = 0.65f;
    }

    static GameObject SpawnSprite(Transform parent, Sprite sprite, Vector3 pos, int order, string name)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return go;
    }

    static int ParseRetroIndex(string spriteName)
    {
        const string suffix = "objetos-bunker-retro_";
        int i = spriteName.LastIndexOf(suffix, StringComparison.Ordinal);
        if (i < 0)
            return -1;
        return int.TryParse(spriteName.Substring(i + suffix.Length), out var index) ? index : -1;
    }

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
#endif
