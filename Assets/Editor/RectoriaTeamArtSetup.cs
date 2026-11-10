#if UNITY_EDITOR
using System;
using System.Linq;
using CampusQuest.Rectoria;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace CampusQuest.EditorTools
{
    /// <summary>Visual overlays only: original logical Tilemaps remain authoritative.</summary>
    public static class RectoriaTeamArtSetup
    {
        const string Environment = "Assets/Sprites/Enviroment/Rectoria/TeamArt/";
        const string Characters = "Assets/Sprites/Characters/Rectoria/";

        static Sprite Import(string path, string name, Rect rect, float ppu)
        {
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            var factories = new SpriteDataProviderFactories(); factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var old = provider.GetSpriteRects().FirstOrDefault(s => s.name == name);
            var id = old == null ? GUID.Generate() : old.spriteID;
            var retained=provider.GetSpriteRects().Where(s=>s.name!=name).ToList();
            retained.Add(new SpriteRect { name=name, rect=rect, spriteID=id,
                alignment=SpriteAlignment.Custom, pivot=new Vector2(.5f,0) });
            provider.SetSpriteRects(retained.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(
                retained.Select(s=>new SpriteNameFileIdPair(s.name,s.spriteID)));
            provider.Apply(); importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single(s=>s.name==name);
        }

        static Transform Child(Transform parent, string name)
        {
            var t=parent.Find(name);
            if(t!=null) return t;
            var go=new GameObject(name); Undo.RegisterCreatedObjectUndo(go,"Arte Rectoría");
            go.transform.SetParent(parent,false); return go.transform;
        }
        static void Draw(Transform parent,string name,Sprite sprite,Vector3 position,Vector3 scale,int order)
        {
            var t=Child(parent,name); Undo.RecordObject(t,"Arte Rectoría");
            t.position=position; t.localScale=scale;
            var renderer=t.GetComponent<SpriteRenderer>();
            if(renderer==null) renderer=Undo.AddComponent<SpriteRenderer>(t.gameObject);
            Undo.RecordObject(renderer,"Arte Rectoría"); renderer.sprite=sprite; renderer.color=Color.white;
            renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Sprites/Enviroment/Rectoria/Rectoria_Unlit.mat");
            renderer.sortingOrder=order;
        }

        [MenuItem("Campus Quest/Rectoria/7 - Integrar arte del equipo")]
        public static string Apply()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!=RectoriaSceneStageSetup.ScenePath)
                throw new InvalidOperationException("Abre y guarda Rectoría Arte fuera de Play Mode.");
            var map=UnityEngine.Object.FindFirstObjectByType<RectoriaGridMap>();
            var stage=GameObject.Find("Rectoria_Escenario").transform;
            // Atlas coordinates are Unity bottom-left; source images are retained unchanged.
            // Exclude the transparent one-pixel gutter; retain PPU and cover exactly one cell.
            var wood=Import(Environment+"pisosyparedes.png","Madera_Oscura",new Rect(193,33,30,30),32);
            // A separate importer is unnecessary for the carpet: add a second sprite through the provider.
            var importer=(TextureImporter)AssetImporter.GetAtPath(Environment+"pisosyparedes.png");
            var factories=new SpriteDataProviderFactories(); factories.Init();
            var provider=factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
            var woodRect=provider.GetSpriteRects().Single(s=>s.name=="Madera_Oscura");
            var oldRug=provider.GetSpriteRects().FirstOrDefault(s=>s.name=="Alfombra_Clara");
            var rugId=oldRug==null?GUID.Generate():oldRug.spriteID;
            provider.SetSpriteRects(new[] {woodRect,new SpriteRect {name="Alfombra_Clara",rect=new Rect(32,96,32,32),spriteID=rugId,alignment=SpriteAlignment.Custom,pivot=new Vector2(.5f,0)}});
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(new[] {
                new SpriteNameFileIdPair(woodRect.name,woodRect.spriteID),new SpriteNameFileIdPair("Alfombra_Clara",rugId)});
            provider.Apply(); importer.SaveAndReimport();
            var all=AssetDatabase.LoadAllAssetsAtPath(Environment+"pisosyparedes.png").OfType<Sprite>().ToArray();
            wood=all.Single(s=>s.name=="Madera_Oscura"); var rug=all.Single(s=>s.name=="Alfombra_Clara");
            var root=Child(stage,"Arte_Equipo"); var floors=Child(root,"Piso_Visual");
            int count=0;
            foreach(var c in map.floor.cellBounds.allPositionsWithin)
            {
                if(!map.floor.HasTile(c)) continue;
                Draw(floors,"Piso_"+c.x+"_"+c.y,wood,map.CellCenter(c)+new Vector3(0,-.5f,0),new Vector3(32f/30f,32f/30f,1),2); count++;
            }
            Draw(root,"Alfombra_Recepcion",rug,new Vector3(21.5f,7,0),new Vector3(5,3,1),3);
            Draw(root,"Alfombra_Rectora",rug,new Vector3(21.5f,12,0),new Vector3(5,3,1),3);
            var ids=new[]{"rectora","don_chuy","lic_mony","beto","rocky"};
            var files=new[]{"rectora","don chuy","licmony","cajerobeto","guardiarocky"};
            for(int i=0;i<ids.Length;i++)
            {
                var sprite=Import(Characters+files[i]+".png",ids[i]+"_Frente",new Rect(i==4?160:0,224,32,32),32);
                var point=UnityEngine.Object.FindObjectsByType<RectoriaScenePoint>(FindObjectsSortMode.None).Single(p=>p.pointId==ids[i]);
                foreach(var name in new[]{"Sombra","Cuerpo","Pie_Izq","Pie_Der","Cabeza","Cabello","Aviso_Provisional"})
                {var old=point.transform.Find(name); if(old!=null){Undo.RecordObject(old.gameObject,"Ocultar provisional");old.gameObject.SetActive(false);}}
                Draw(point.transform,"Sprite_Equipo",sprite,map.CellCenter(point.cell)+new Vector3(0,-.5f,0),new Vector3(1.4f,1.4f,1),50);
                Undo.RecordObject(point,"Arte NPC"); point.provisionalVisual=false;
                point.designNote="Sprite suministrado por el equipo; pose frontal estática. Ancla y colisiones originales.";
                EditorUtility.SetDirty(point);
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            return count+" celdas con madera oscura, 2 alfombras y 5 NPCs. Tilemaps y lógica conservados.";
        }
    }
}
#endif
