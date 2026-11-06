#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class CampusQuestRectoriaOfficeArtV1
{
    private const string SourceScene = "Assets/Scenes/Rectoria_Tutorial_Blockout.unity";
    private const string PreviewScene = "Assets/Scenes/Rectoria_Tutorial_Arte_v01.unity";
    private const string ArtRoot = "Assets/Sprites/Enviroment/Rectoria/ArtV1";
    private const string TileRoot = ArtRoot + "/Tiles";

    [MenuItem("Campus Quest/Rectoria/3 - Ambientar oficina Rectora (copia)")]
    public static void CreateOfficePreview()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Campus Quest", "Deten Play Mode antes de ambientar.", "OK");
            return;
        }
        if (!File.Exists(SourceScene))
        {
            EditorUtility.DisplayDialog("Campus Quest", "No encuentro la escena base:\n" + SourceScene, "OK");
            return;
        }
        if (File.Exists(PreviewScene))
        {
            EditorUtility.DisplayDialog("Campus Quest", "La copia de arte ya existe y no la voy a sobrescribir:\n" + PreviewScene, "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // Always clone saved base; do not modify it.
        Scene scene = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
        if (!EditorSceneManager.SaveScene(scene, PreviewScene, true))
        {
            EditorUtility.DisplayDialog("Campus Quest", "No pude crear la copia de la escena.", "OK");
            return;
        }
        scene = EditorSceneManager.OpenScene(PreviewScene, OpenSceneMode.Single);
        Transform root = GameObject.Find("Rectoria_Blockout")?.transform;
        Transform grid = root != null ? root.Find("Grid") : null;
        if (grid == null)
        {
            Debug.LogError("Campus Quest: escena sin Rectoria_Blockout/Grid. No hubo modificaciones a la base.");
            return;
        }
        Tilemap floor = grid.Find("Floor_Tilemap")?.GetComponent<Tilemap>();
        Tilemap walls = grid.Find("Walls_Tilemap")?.GetComponent<Tilemap>();
        Tilemap collisions = grid.Find("Collision_Tilemap")?.GetComponent<Tilemap>();
        if (floor == null || walls == null || collisions == null)
        {
            Debug.LogError("Campus Quest: faltan Tilemaps en la copia; no se modifico la base.");
            return;
        }
        if (!floor.HasTile(new Vector3Int(21, 15, 0)))
        {
            EditorUtility.DisplayDialog("Campus Quest", "La copia parece tener Tilemaps vacios. Ejecuta primero Reparar Tilemaps en la escena original, guarda y vuelve a intentar.", "OK");
            return;
        }

        TileBase artFloor = EnsureTile("Piso_Oficina", Tile.ColliderType.None);
        TileBase artWall = EnsureTile("Muro_Oficina", Tile.ColliderType.Grid);
        TileBase artWood = EnsureTile("Madera_Provisional", Tile.ColliderType.Grid);

        Undo.RegisterCompleteObjectUndo(floor, "Ambientar piso Rectora");
        Undo.RegisterCompleteObjectUndo(walls, "Ambientar muros Rectora");
        Undo.RegisterCompleteObjectUndo(collisions, "Ambientar muebles provisionales");
        int countFloor=0, countWall=0, countWood=0;
        // Rectora room: visible art changes only, not grid dimensions or movement logic.
        for (int y=12; y<=19; y++)
        for (int x=16; x<=27; x++)
        {
            var p=new Vector3Int(x,y,0);
            if (floor.HasTile(p)) { floor.SetTile(p,artFloor); countFloor++; }
            if (collisions.HasTile(p)) { collisions.SetTile(p,artWood); countWood++; }
        }
        // Only the walls surrounding the Rectora's room, without changing solid layout.
        for (int y=11; y<=20; y++)
        for (int x=15; x<=28; x++)
        {
            var p=new Vector3Int(x,y,0);
            if (walls.HasTile(p)) { walls.SetTile(p,artWall); countWall++; }
        }
        SetUnlit(floor); SetUnlit(walls); SetUnlit(collisions);
        EditorUtility.SetDirty(floor); EditorUtility.SetDirty(walls); EditorUtility.SetDirty(collisions);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
        {
            Debug.LogError("Campus Quest: no se pudo guardar la escena de arte.");
            return;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"Campus Quest - Arte Rectora: pisos {countFloor}, muros {countWall}, muebles provisionales {countWood}. Escena: {PreviewScene}");
        EditorUtility.DisplayDialog("Campus Quest - Oficina de la Rectora", $"Copia creada: {PreviewScene}\nPisos: {countFloor}\nMuros: {countWall}\nMuebles provisionales: {countWood}\n\nLa escena base y el movimiento de Mateo no se modificaron.", "OK");
    }

    private static TileBase EnsureTile(string name, Tile.ColliderType collider)
    {
        string texturePath = ArtRoot + "/" + name + ".png";
        string tilePath = TileRoot + "/" + name + ".asset";
        if (!File.Exists(texturePath)) throw new Exception("Falta archivo de arte: " + texturePath);
        var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null) { AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport); importer=AssetImporter.GetAtPath(texturePath) as TextureImporter; }
        if (importer == null) throw new Exception("No puedo importar " + texturePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32; // 32 pixels from original atlas = 1 Unity cell. Mateo untouched.
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
        if (sprite == null) throw new Exception("No se encontro Sprite: " + texturePath);
        if (!Directory.Exists(TileRoot)) Directory.CreateDirectory(TileRoot);
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
        if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile,tilePath); }
        tile.sprite=sprite;
        tile.colliderType=collider;
        tile.color=Color.white;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static void SetUnlit(Tilemap map)
    {
        var renderer = map.GetComponent<TilemapRenderer>();
        if (renderer == null) return;
        Shader shader=Shader.Find("Sprites/Default");
        if (shader == null) return;
        const string matPath=ArtRoot+"/Rectoria_Unlit_Arte.mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null) { mat=new Material(shader); AssetDatabase.CreateAsset(mat,matPath); }
        renderer.sharedMaterial=mat;
        EditorUtility.SetDirty(renderer);
    }
}
#endif
