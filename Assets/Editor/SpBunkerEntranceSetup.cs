#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class SpBunkerEntranceSetup
{
    const string ScenePath = "Assets/Scenes/Sp_Bunker.unity";
    const string TilesRoot = "Assets/Sprites/Enviroment/Sp/Tiles";
    const string BunkerRetroSheet = "Assets/Sprites/Enviroment/Sp/PixelLab/objetos-bunker-retro.png";
    const string BunkerPropsSheet = "Assets/Sprites/Enviroment/Sp/PixelLab/Sprites_Bunker01.png";
    const string CellBarsSheet = "Assets/Sprites/Enviroment/Sp/PixelLab/Celdas.png";
    const string DoorName = "Puerta_Bunker";
    const string DecorRootName = "SP_BunkerEntrance_Decor";
    const string CellShadeRootName = "SP_BunkerEntrance_Shade";
    const string ArrivalName = "CampusQuest_BunkerArrival";
    const string MateoPrefabPath = "Assets/Prefabs/Characters/Mateo/Mateo_Provisional.prefab";
    const string BunkerMusicClipPath = "Assets/Audio/Musica/Iron_Decree.wav";
    const string MixerPath = "Assets/Audio/MainMixer.mixer";
    const string BunkerMusicObjectName = "SP_BunkerMusic";

    const int MapW = 17;
    const int MapH = 14;
    const int PathLeft = 7;
    const int PathRight = 9;
    const int CellLeftMin = 1;
    const int CellLeftMax = 5;
    const int CellRightMin = 11;
    const int CellRightMax = 15;
    const int MidDividerY = 7;

    const int WallSpriteIndex = 5;
    const int RedLightIndex = 36;
    const float LeftBarX = 6.5f;
    const float RightBarX = 10.5f;

    static readonly int[] PathFloorIndices = { 0, 1, 2, 3, 6 };
    static readonly int[] CellFloorIndices = { 0, 1, 2 };
    static readonly float[] BottomCellBarY = { 1.5f, 2.5f, 3.5f, 4.5f, 5.5f, 6.5f };
    static readonly float[] TopCellBarY = { 8.5f, 9.5f, 10.5f, 11.5f, 12.5f };

    static Sprite cachedCellShadeSprite;

    [MenuItem("Campus Quest/SP/Pintar entrada del búnker")]
    public static void PaintFromMenu()
    {
        EnsureSceneFile();
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var oldGrid = GameObject.Find("Grid");
        if (oldGrid != null)
            Undo.DestroyObjectImmediate(oldGrid);

        var oldDecor = GameObject.Find(DecorRootName);
        if (oldDecor != null)
            Undo.DestroyObjectImmediate(oldDecor);

        var oldShade = GameObject.Find(CellShadeRootName);
        if (oldShade != null)
            Undo.DestroyObjectImmediate(oldShade);

        var oldDoor = GameObject.Find(DoorName);
        if (oldDoor != null)
            Undo.DestroyObjectImmediate(oldDoor);

        var oldHud = GameObject.Find("CampusQuest_HUD");
        if (oldHud != null)
            Undo.DestroyObjectImmediate(oldHud);

        var oldMinimapCam = GameObject.Find("MinimapCamera");
        if (oldMinimapCam != null)
            Undo.DestroyObjectImmediate(oldMinimapCam);

        var oldArrival = GameObject.Find(ArrivalName);
        if (oldArrival != null)
            Undo.DestroyObjectImmediate(oldArrival);

        var oldMusic = GameObject.Find(BunkerMusicObjectName);
        if (oldMusic != null)
            Undo.DestroyObjectImmediate(oldMusic);

        var oldMateo = GameObject.Find("Mateo_Provisional");
        if (oldMateo != null)
            Undo.DestroyObjectImmediate(oldMateo);

        Directory.CreateDirectory(TilesRoot);
        var palette = BuildPalette();
        var controller = SpBunkerDoorSetup.EnsureAnimationAssets();

        var gridGo = new GameObject("Grid");
        Undo.RegisterCreatedObjectUndo(gridGo, "Sp_Bunker entrada");
        var grid = gridGo.AddComponent<Grid>();
        grid.cellSize = Vector3.one;

        var ground = NewTilemap(gridGo.transform, "Tilemap_Ground", 0, false);
        var walls = NewTilemap(gridGo.transform, "Tilemap_Walls", 2, true);
        ConfigureWallComposite(walls.gameObject);

        PaintTiles(ground, walls, palette);

        var shadeRoot = new GameObject(CellShadeRootName);
        Undo.RegisterCreatedObjectUndo(shadeRoot, CellShadeRootName);
        PlaceCellShadeOverlays(shadeRoot.transform);

        var decorRoot = new GameObject(DecorRootName);
        Undo.RegisterCreatedObjectUndo(decorRoot, DecorRootName);
        PlaceCellDecor(decorRoot.transform, palette);

        WireDoor(controller, palette.ClosedDoorSprite);

        var mateo = EnsureMateoSpawn();
        SpBunkerHudSetup.EnsurePlayHud(mateo != null ? mateo.transform : null);
        EnsureBunkerArrival();
        ConfigureCamera(mateo);
        EnsureGlobalLight();
        EnsureBunkerMusic();
        SpBunkerBuildSettings.EnsureSpBunkerInBuild();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = gridGo;
        Debug.Log("[Campus Quest] Sp_Bunker: entrada del búnker pintada.");
    }

    static void EnsureSceneFile()
    {
        if (File.Exists(ScenePath))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    struct EntrancePalette
    {
        public Tile[] PathFloors;
        public Tile[] CellFloors;
        public Tile Wall;
        public Sprite RedLight;
        public Sprite PipeHorizontal;
        public Sprite PipeElbow;
        public Sprite PipeVertical;
        public Sprite ClosedDoorSprite;
    }

    struct CellLayout
    {
        public string Id;
        public Vector3 Center;
        public float BackWallX;
        public bool IsLeft;
    }

    static EntrancePalette BuildPalette()
    {
        var retro = LoadSprites(BunkerRetroSheet)
            .Select(s => new { Sprite = s, Index = ParseRetroIndex(s.name) })
            .Where(p => p.Index >= 0)
            .ToDictionary(p => p.Index, p => p.Sprite);

        Sprite RequireRetro(int index)
        {
            if (!retro.TryGetValue(index, out var sprite))
                throw new InvalidOperationException("objetos-bunker-retro sin _" + index);
            return sprite;
        }

        Tile Floor(int index) => EnsureTile(RequireRetro(index), "SP_Bunker_Floor_" + index);

        Sprite RequireProp(string spriteName)
        {
            var sprite = AssetDatabase.LoadAllAssetsAtPath(BunkerPropsSheet).OfType<Sprite>()
                .FirstOrDefault(s => s.name == spriteName);
            if (sprite == null)
                throw new InvalidOperationException("Sprites_Bunker01 sin " + spriteName);
            return sprite;
        }

        var closedDoor = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Enviroment/Sp/PixelLab/Open_Door.png")
            .OfType<Sprite>()
            .First(s => s.name == "Open_Door_0");

        return new EntrancePalette
        {
            PathFloors = PathFloorIndices.Select(Floor).ToArray(),
            CellFloors = CellFloorIndices.Select(Floor).ToArray(),
            Wall = EnsureTile(RequireRetro(WallSpriteIndex), "SP_Bunker_Wall_" + WallSpriteIndex, Tile.ColliderType.Sprite),
            RedLight = RequireRetro(RedLightIndex),
            PipeHorizontal = RequireProp("Sprites_Bunker01_0"),
            PipeElbow = RequireProp("Sprites_Bunker01_16"),
            PipeVertical = RequireProp("Sprites_Bunker01_2"),
            ClosedDoorSprite = closedDoor
        };
    }

    static void PaintTiles(Tilemap ground, Tilemap walls, EntrancePalette palette)
    {
        for (int y = 0; y < MapH; y++)
        for (int x = 0; x < MapW; x++)
        {
            var cell = new Vector3Int(x, y, 0);
            bool onPath = x >= PathLeft && x <= PathRight;
            bool northDoorGap = y == MapH - 1 && onPath;

            if (IsPerimeterWall(x, y) && !northDoorGap)
                walls.SetTile(cell, palette.Wall);
            else if (y == MidDividerY && !onPath)
                walls.SetTile(cell, palette.Wall);
            else if (IsCellBarWall(x, y))
                walls.SetTile(cell, palette.Wall);
            else
                walls.SetTile(cell, null);

            if (onPath)
            {
                int pick = (x * 3 + y * 7) % palette.PathFloors.Length;
                ground.SetTile(cell, palette.PathFloors[pick]);
                ground.SetColor(cell, Color.white);
            }
            else if (IsLeftCell(x, y) || IsRightCell(x, y))
            {
                int pick = (x * 5 + y * 11) % palette.CellFloors.Length;
                ground.SetTile(cell, palette.CellFloors[pick]);
                ground.SetColor(cell, new Color(0.45f, 0.45f, 0.48f, 1f));
            }
            else
            {
                ground.SetTile(cell, null);
                ground.SetColor(cell, Color.white);
            }
        }
    }

    static bool IsPerimeterWall(int x, int y) =>
        x == 0 || x == MapW - 1 || y == 0 || y == MapH - 1;

    static bool IsLeftCell(int x, int y) =>
        x >= CellLeftMin && x <= CellLeftMax && ((y >= 1 && y < MidDividerY) || (y > MidDividerY && y < MapH - 1));

    static bool IsRightCell(int x, int y) =>
        x >= CellRightMin && x <= CellRightMax && ((y >= 1 && y < MidDividerY) || (y > MidDividerY && y < MapH - 1));

    static bool IsCellBarWall(int x, int y)
    {
        if (x != CellLeftMax + 1 && x != CellRightMin - 1)
            return false;
        if (y <= 0 || y >= MapH - 1 || y == MidDividerY)
            return false;
        return true;
    }

    static void PlaceCellShadeOverlays(Transform root)
    {
        var sprite = EnsureCellShadeSprite();
        foreach (var cell in BuildCellLayouts())
        {
            var go = new GameObject("Shade_" + cell.Id);
            Undo.RegisterCreatedObjectUndo(go, go.name);
            go.transform.SetParent(root, false);
            go.transform.position = cell.Center;
            go.transform.localScale = new Vector3(5f, 6f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0f, 0f, 0f, 0.82f);
            sr.sortingOrder = 1;
        }
    }

    static CellLayout[] BuildCellLayouts() =>
        new[]
        {
            new CellLayout { Id = "L_Bottom", Center = new Vector3(3f, 3.5f, 0f), BackWallX = 1.35f, IsLeft = true },
            new CellLayout { Id = "L_Top", Center = new Vector3(3f, 10.5f, 0f), BackWallX = 1.35f, IsLeft = true },
            new CellLayout { Id = "R_Bottom", Center = new Vector3(13f, 3.5f, 0f), BackWallX = 14.65f, IsLeft = false },
            new CellLayout { Id = "R_Top", Center = new Vector3(13f, 10.5f, 0f), BackWallX = 14.65f, IsLeft = false }
        };

    static void PlaceCellDecor(Transform root, EntrancePalette palette)
    {
        PlaceVerticalCellBars(root);

        foreach (var cell in BuildCellLayouts())
        {
            float pipeY = cell.Center.y + 2.15f;
            float lightY = cell.Center.y + 2.35f;
            float pipeMidX = cell.IsLeft ? cell.BackWallX + 1.65f : cell.BackWallX - 1.65f;

            if (palette.PipeHorizontal != null)
            {
                var pipe = SpawnSprite(root, palette.PipeHorizontal, new Vector3(pipeMidX, pipeY, 0f), 4, "PipeH_" + cell.Id);
                pipe.transform.localScale = new Vector3(cell.IsLeft ? 1f : -1f, 1f, 1f);
            }

            if (palette.PipeElbow != null)
            {
                float elbowX = cell.IsLeft ? cell.BackWallX + 0.55f : cell.BackWallX - 0.55f;
                var elbow = SpawnSprite(root, palette.PipeElbow, new Vector3(elbowX, pipeY - 0.85f, 0f), 4, "PipeElbow_" + cell.Id);
                if (!cell.IsLeft)
                    elbow.transform.localScale = new Vector3(-1f, 1f, 1f);
            }

            if (palette.RedLight != null)
            {
                var lightGo = SpawnSprite(root, palette.RedLight, new Vector3(cell.BackWallX, lightY, 0f), 5, "BunkerLight_" + cell.Id);
                SpawnPointLight(lightGo.transform, new Vector3(0.55f, -0.15f, 0f));
            }
        }
    }

    static Sprite LoadCellBarSprite() =>
        AssetDatabase.LoadAllAssetsAtPath(CellBarsSheet).OfType<Sprite>()
            .FirstOrDefault(s => s.name == "Celdas_1");

    static void PlaceVerticalCellBars(Transform root)
    {
        var barSprite = LoadCellBarSprite();
        if (barSprite == null)
            throw new InvalidOperationException("Celdas.png sin sprite Celdas_1");

        foreach (var cell in BuildCellLayouts())
        {
            float barX = cell.IsLeft ? LeftBarX : RightBarX;
            bool topCell = cell.Id.EndsWith("Top", StringComparison.Ordinal);
            var ys = topCell ? TopCellBarY : BottomCellBarY;
            for (int i = 0; i < ys.Length; i++)
                SpawnCellBarSegment(root, barSprite, new Vector3(barX, ys[i], 0f), "Bar_" + cell.Id + "_" + i, cell.IsLeft);
        }
    }

    static void SpawnCellBarSegment(Transform parent, Sprite sprite, Vector3 pos, string name, bool flipTowardCorridor)
    {
        var go = SpawnSprite(parent, sprite, pos, 3, name);
        if (flipTowardCorridor)
            go.transform.localScale = new Vector3(-1f, 1f, 1f);
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1f, 1f);
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

    static void WireDoor(RuntimeAnimatorController controller, Sprite closedSprite)
    {
        var doorGo = new GameObject(DoorName);
        Undo.RegisterCreatedObjectUndo(doorGo, DoorName);
        doorGo.transform.position = new Vector3(8f, MapH - 0.55f, 0f);

        var sr = doorGo.AddComponent<SpriteRenderer>();
        sr.sprite = closedSprite;
        sr.sortingOrder = 6;

        var animator = doorGo.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        var blockerGo = new GameObject("Pasaje_Bloqueo");
        blockerGo.transform.SetParent(doorGo.transform, false);
        blockerGo.transform.localPosition = new Vector3(0f, -1.65f, 0f);
        var box = blockerGo.AddComponent<BoxCollider2D>();
        box.size = new Vector2(2.9f, 0.45f);

        var door = doorGo.AddComponent<CampusQuestBunkerDoor>();
        var so = new SerializedObject(door);
        so.FindProperty("spriteRenderer").objectReferenceValue = sr;
        so.FindProperty("animator").objectReferenceValue = animator;
        so.FindProperty("passageBlocker").objectReferenceValue = box;
        so.ApplyModifiedPropertiesWithoutUndo();

        var interactGo = new GameObject("Puerta_Interact");
        Undo.RegisterCreatedObjectUndo(interactGo, "Puerta_Interact");
        interactGo.transform.SetParent(doorGo.transform, false);
        interactGo.transform.localPosition = new Vector3(0f, -2.15f, 0f);
        var interact = interactGo.AddComponent<CampusQuestBunkerDoorInteract>();
        var interactSo = new SerializedObject(interact);
        interactSo.FindProperty("door").objectReferenceValue = door;
        interactSo.ApplyModifiedPropertiesWithoutUndo();
    }

    static GameObject EnsureMateoSpawn()
    {
        const string mateoName = "Mateo_Provisional";
        var existing = GameObject.Find(mateoName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MateoPrefabPath);
        GameObject mateo;
        if (prefab != null)
            mateo = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        else
        {
            mateo = new GameObject(mateoName);
            Undo.RegisterCreatedObjectUndo(mateo, mateoName);
        }

        mateo.name = mateoName;
        mateo.transform.position = new Vector3(8f, 1.5f, 0f);
        SpBosqueCollisionSetup.EnsureMateoPhysics(mateo);
        return mateo;
    }

    static void EnsureBunkerArrival()
    {
        var go = GameObject.Find(ArrivalName);
        if (go == null)
        {
            go = new GameObject(ArrivalName);
            Undo.RegisterCreatedObjectUndo(go, ArrivalName);
        }

        if (go.GetComponent<SpBunkerArrival>() == null)
            Undo.AddComponent<SpBunkerArrival>(go);
    }

    static void ConfigureCamera(GameObject mateo)
    {
        var cam = Camera.main;
        if (cam == null)
            return;

        cam.orthographic = true;
        cam.orthographicSize = 7f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.02f, 0.03f, 1f);

        var follow = cam.GetComponent<SpCameraFollow>();
        if (follow == null)
            follow = Undo.AddComponent<SpCameraFollow>(cam.gameObject);
        if (mateo != null)
        {
            var so = new SerializedObject(follow);
            so.FindProperty("target").objectReferenceValue = mateo.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        cam.transform.position = new Vector3(8f, 6.5f, -10f);
    }

    static void EnsureBunkerMusic()
    {
        var go = GameObject.Find(BunkerMusicObjectName);
        if (go == null)
        {
            go = new GameObject(BunkerMusicObjectName);
            Undo.RegisterCreatedObjectUndo(go, BunkerMusicObjectName);
        }

        var source = go.GetComponent<AudioSource>();
        if (source == null)
            source = Undo.AddComponent<AudioSource>(go);

        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(BunkerMusicClipPath);
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = true;
        source.spatialBlend = 0f;
        source.volume = 0.65f;

        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        if (mixer != null)
        {
            foreach (var group in mixer.FindMatchingGroups("Music"))
            {
                if (group != null && group.name == "Music")
                {
                    source.outputAudioMixerGroup = group;
                    break;
                }
            }
        }
    }

    static void EnsureGlobalLight()
    {
        var light = UnityEngine.Object.FindFirstObjectByType<Light2D>();
        if (light == null || light.lightType != Light2D.LightType.Global)
        {
            foreach (var existing in UnityEngine.Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None))
            {
                if (existing.lightType == Light2D.LightType.Global)
                {
                    light = existing;
                    break;
                }
            }
        }

        if (light == null)
        {
            var go = new GameObject("Global Light 2D");
            Undo.RegisterCreatedObjectUndo(go, "Global Light 2D");
            light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
        }

        light.intensity = 0.35f;
        light.color = new Color(0.72f, 0.76f, 0.88f, 1f);
    }

    static Sprite EnsureCellShadeSprite()
    {
        if (cachedCellShadeSprite != null)
            return cachedCellShadeSprite;

        const string path = "Assets/Sprites/Enviroment/Sp/Tiles/SP_Bunker_CellShade.png";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null)
        {
            cachedCellShadeSprite = existing;
            return cachedCellShadeSprite;
        }

        var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        var fill = new Color32(255, 255, 255, 255);
        var pixels = Enumerable.Repeat(fill, 32 * 32).ToArray();
        tex.SetPixels32(pixels);
        tex.Apply();

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? TilesRoot);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }

        cachedCellShadeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        return cachedCellShadeSprite;
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

    static int ParseRetroIndex(string spriteName)
    {
        const string suffix = "objetos-bunker-retro_";
        int i = spriteName.LastIndexOf(suffix, StringComparison.Ordinal);
        if (i < 0)
            return -1;
        return int.TryParse(spriteName.Substring(i + suffix.Length), out var index) ? index : -1;
    }

    static Sprite[] LoadSprites(string texturePath) =>
        AssetDatabase.LoadAllAssetsAtPath(texturePath).OfType<Sprite>().ToArray();

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
        {
            tile.sprite = sprite;
            tile.colliderType = collider;
            EditorUtility.SetDirty(tile);
        }

        return tile;
    }
}
#endif
