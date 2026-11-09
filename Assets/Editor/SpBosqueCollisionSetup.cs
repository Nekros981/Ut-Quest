#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class SpBosqueCollisionSetup
{
    const string ScenePath = "Assets/Scenes/Sp_Bosque.unity";
    const string SouthWallTilePath = "Assets/Sprites/Enviroment/Sp/Tiles/SP_SouthWall.asset";
    const string MateoName = "Mateo_Provisional";

    const int AmbushLeft = 1;
    const int AmbushRight = 19;
    const int AmbushBottomY = 34;
    const int AmbushTopY = 52;

    [MenuItem("Campus Quest/SP/Poner colisiones de prueba")]
    public static void ApplyCollisions()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var wallsGo = GameObject.Find("Tilemap_Walls");
        var canopyGo = GameObject.Find("Tilemap_Canopy");
        if (wallsGo != null)
        {
            EnsureWallComposite(wallsGo);
            var walls = wallsGo.GetComponent<Tilemap>();
            var canopy = canopyGo != null ? canopyGo.GetComponent<Tilemap>() : null;
            if (walls != null && canopy != null)
            {
                var cleared = ClearAmbushOverfillRect(canopy, walls);
                Debug.Log($"Campus Quest: claro emboscada restaurado — {cleared.pines} pinos y {cleared.walls} muros quitados en x {AmbushLeft}-{AmbushRight}, y {AmbushBottomY}-{AmbushTopY}.");
            }
        }
        else
            Debug.LogWarning("Campus Quest: no se encontró Tilemap_Walls.");

        EnsureSpritePropColliders("SP_CampoMilitar");
        EnsureSpritePropColliders("SP_BunkerPlaza", go => go.name.StartsWith("CompoMilitar_tire_"));

        var mateo = GameObject.Find(MateoName);
        if (mateo != null)
            EnsureMateoPhysics(mateo);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Campus Quest: colisiones de prueba (pared solo en pinos, props y Mateo).");
    }

    public static (int pines, int walls) ClearAmbushOverfillRect(Tilemap canopy, Tilemap walls)
    {
        int pinesCleared = 0;
        int wallsCleared = 0;
        for (int y = AmbushBottomY; y <= AmbushTopY; y++)
        for (int x = AmbushLeft; x <= AmbushRight; x++)
        {
            var cell = new Vector3Int(x, y, 0);
            if (canopy.GetTile(cell) != null)
            {
                canopy.SetTile(cell, null);
                pinesCleared++;
            }

            if (walls.GetTile(cell) != null)
            {
                walls.SetTile(cell, null);
                wallsCleared++;
            }
        }

        return (pinesCleared, wallsCleared);
    }

    public static (int placed, int cleared) SyncWallsToCanopy(Tilemap walls, Tilemap canopy)
    {
        var southWall = AssetDatabase.LoadAssetAtPath<Tile>(SouthWallTilePath);
        if (southWall == null)
        {
            Debug.LogError("Campus Quest: falta " + SouthWallTilePath);
            return (0, 0);
        }

        int placed = 0;
        int cleared = 0;
        var bounds = EncapsulateBounds(walls.cellBounds, canopy.cellBounds);
        foreach (var pos in bounds.allPositionsWithin)
        {
            bool hasPine = canopy.GetTile(pos) != null;
            var existing = walls.GetTile(pos);

            if (hasPine)
            {
                if (existing != southWall)
                {
                    walls.SetTile(pos, southWall);
                    placed++;
                }
            }
            else if (existing != null)
            {
                walls.SetTile(pos, null);
                cleared++;
            }
        }

        return (placed, cleared);
    }

    static BoundsInt EncapsulateBounds(BoundsInt a, BoundsInt b)
    {
        int xMin = Mathf.Min(a.xMin, b.xMin);
        int yMin = Mathf.Min(a.yMin, b.yMin);
        int xMax = Mathf.Max(a.xMax, b.xMax);
        int yMax = Mathf.Max(a.yMax, b.yMax);
        return new BoundsInt(xMin, yMin, 0, xMax - xMin, yMax - yMin, 1);
    }

    public static void EnsureMateoPhysics(GameObject mateo)
    {
        var body = mateo.GetComponent<Rigidbody2D>();
        if (body == null)
            body = Undo.AddComponent<Rigidbody2D>(mateo);

        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        var circle = mateo.GetComponent<CircleCollider2D>();
        if (circle == null)
            circle = Undo.AddComponent<CircleCollider2D>(mateo);
        circle.radius = 0.28f;
        circle.offset = new Vector2(0f, -0.12f);

        if (mateo.GetComponent<SpTopDownTestMover>() == null)
            Undo.AddComponent<SpTopDownTestMover>(mateo);
    }

    static void EnsureWallComposite(GameObject wallsGo)
    {
        var body = wallsGo.GetComponent<Rigidbody2D>();
        if (body == null)
            body = Undo.AddComponent<Rigidbody2D>(wallsGo);
        body.bodyType = RigidbodyType2D.Static;

        if (wallsGo.GetComponent<CompositeCollider2D>() == null)
            Undo.AddComponent<CompositeCollider2D>(wallsGo);

        var tileCollider = wallsGo.GetComponent<TilemapCollider2D>();
        if (tileCollider != null)
            tileCollider.usedByComposite = true;
    }

    static void EnsureSpritePropColliders(string rootName, System.Func<GameObject, bool> filter = null)
    {
        var root = GameObject.Find(rootName);
        if (root == null)
            return;

        foreach (Transform child in root.transform)
        {
            if (filter != null && !filter(child.gameObject))
                continue;

            var sr = child.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null)
                continue;

            SpBosqueFootColliderUtil.EnsureFootBox(child.gameObject, sr.sprite);
        }
    }
}
#endif
