#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using CampusQuest.Rectoria;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace CampusQuest.EditorTools
{
    /// <summary>Idempotent scene dressing over the existing layout. Does not regenerate the map.</summary>
    public static class RectoriaSceneStageSetup
    {
        public const string ScenePath = "Assets/Scenes/Rectoria_Tutorial_Arte_v01.unity";
        private const string Art = "Assets/Sprites/Enviroment/Rectoria/";
        private static readonly Color Gold = new Color32(233, 216, 166, 255);
        private static readonly Color Panel = new Color32(27, 32, 45, 245);
        private static Sprite square;
        private static TMP_FontAsset font;
        private static RectoriaGridMap map;

        [MenuItem("Campus Quest/Rectoria/4 - Preparar escenario jugable (sin regenerar mapa)")]
        public static void Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Detén Play Mode primero.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != ScenePath) throw new InvalidOperationException("Abre " + ScenePath + ". La escena base no se modifica.");
            if (scene.isDirty) throw new InvalidOperationException("Guarda los cambios de la escena antes de preparar el escenario.");
            map = UnityEngine.Object.FindFirstObjectByType<RectoriaGridMap>();
            var player = UnityEngine.Object.FindFirstObjectByType<RectoriaPlayerTest>();
            square = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Textures/Piso.png");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/PixeloidSans SDF.asset");
            var floor = AssetDatabase.LoadAssetAtPath<TileBase>(Art + "ArtV1/Tiles/Piso_Oficina.asset");
            var wall = AssetDatabase.LoadAssetAtPath<TileBase>(Art + "ArtV1/Tiles/Muro_Oficina.asset");
            var wood = AssetDatabase.LoadAssetAtPath<TileBase>(Art + "ArtV1/Tiles/Madera_Provisional.asset");
            if (map == null || player == null || square == null || font == null || floor == null || wall == null || wood == null)
                throw new InvalidOperationException("Faltan recursos existentes de Rectoría, Mateo o Pixeloid Sans.");

            Undo.SetCurrentGroupName("Preparar escenario Rectoría");
            int undo = Undo.GetCurrentGroup();
            var stage = Child(null, "Rectoria_Escenario");
            Undo.RegisterFullObjectHierarchyUndo(stage.gameObject, "Actualizar escenario Rectoría");
            var npcs = Child(stage, "NPCs_Provisionales");
            var anchors = Child(stage, "Puntos_De_Tramite");
            var signs = Child(stage, "Senaletica");
            var decor = Child(stage, "Ambientacion_Provisional");

            // Replace appearance only: every occupied coordinate, grid transform and collider remains.
            PaintExisting(map.floor, floor);
            PaintExisting(map.walls, wall);
            PaintExisting(map.obstacles, wood);
            foreach (var tilemap in new[] { map.floor, map.walls, map.obstacles })
            {
                var renderer = tilemap.GetComponent<TilemapRenderer>();
                Undo.RecordObject(renderer, "Material de Rectoría");
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Art + "ArtV1/Rectoria_Unlit_Arte.mat");
            }
            var oldMarkers = map.transform.Find("NPCs_Marcadores_NoImplementados");
            if (oldMarkers != null) { Undo.RecordObject(oldMarkers.gameObject, "Ocultar rotulos de prueba"); oldMarkers.gameObject.SetActive(false); }
            var markersMap = map.grid.transform.Find("Markers_Tilemap");
            if (markersMap != null)
            {
                var renderer = markersMap.GetComponent<TilemapRenderer>();
                Undo.RecordObject(renderer, "Ocultar marcadores de prueba"); renderer.enabled = false;
            }
            var oldPlayerSquare = player.GetComponent<SpriteRenderer>();
            if (oldPlayerSquare != null) { Undo.RecordObject(oldPlayerSquare, "Ocultar cuadrado provisional de Mateo"); oldPlayerSquare.enabled = false; }

            Npc(npcs, "rectora", "Rectora", "Oficina central", 22, 18, 23, 18, new Color32(148, 104, 173, 255));
            Npc(npcs, "don_chuy", "Don Chuy", "Archivo", 7, 30, 8, 30, new Color32(113, 150, 116, 255));
            Npc(npcs, "lic_mony", "Lic. Mony", "Credencialización", 35, 30, 36, 30, new Color32(100, 157, 186, 255));
            Npc(npcs, "beto", "Cajero Beto", "Caja", 7, 40, 8, 40, new Color32(189, 143, 87, 255));
            Npc(npcs, "rocky", "Guardia Rocky", "Puerta de Bloques", 25, 39, 24, 39, new Color32(105, 120, 160, 255));

            Note(anchors, "nota_1", "Nota 1 · Pizarrón", 5, 23, 5, 24, "La carpeta no es roja ni verde.");
            Note(anchors, "nota_2", "Nota 2 · Escritorio", 10, 24, 9, 24, "Es del año 2007 o 2008.");
            Note(anchors, "nota_3", "Nota 3 · Archivero", 3, 25, 4, 25, "No es amarilla ni morada.");
            Note(anchors, "nota_4", "Nota 4 · Estante", 11, 30, 11, 29, "Es la más reciente de las que quedan.");

            var photo = Point(anchors, "foto", "Marca de fotografía", RectoriaPointKind.PhotoMark, 34, 26, 34, 26);
            var cross = Shape(photo.transform, "Marca_X_1", Vector2.zero, new Vector2(.8f, .07f), Gold, 10);
            cross.localRotation = Quaternion.Euler(0, 0, 45);
            cross = Shape(photo.transform, "Marca_X_2", Vector2.zero, new Vector2(.8f, .07f), Gold, 10);
            cross.localRotation = Quaternion.Euler(0, 0, -45);
            WorldText(photo.transform, "Etiqueta", "FOTO", new Vector2(0, -.55f), 1.7f, 2.5f, Gold);
            // Camera is a decorative provisional, placed on the existing furniture four cells north of X.
            var camera = Child(decor, "Camara_Fotografica_PROVISIONAL");
            camera.position = map.CellCenter(new Vector3Int(34, 30, 0));
            Shape(camera, "Cuerpo", Vector2.zero, new Vector2(.55f, .35f), Panel, 35);
            Shape(camera, "Lente", new Vector2(0, -.18f), new Vector2(.2f, .13f), Gold, 36);
            Shape(camera, "Soporte", new Vector2(0, -.36f), new Vector2(.08f, .32f), Panel, 34);

            var receipts = Point(anchors, "recibos", "Mesa de recibos", RectoriaPointKind.ReceiptsTable, 10, 35, 10, 34);
            for (int i = 0; i < 6; i++)
                Shape(receipts.transform, "Recibo_" + i, new Vector2(-.29f + i % 3 * .29f, -.17f + i / 3 * .3f), new Vector2(.18f, .22f), new Color32(248, 241, 211, 255), 15);
            receipts.designNote = "Preparado para recibos de $300, $450, $600, $700, $850 y $1,000. El puzle todavía no está implementado.";

            var office = Point(anchors, "tramites", "Oficina de Trámites", RectoriaPointKind.LockedOffice, 19, 39, 20, 39);
            office.designNote = "Solo personal autorizado. El Burócrata se presenta aquí en la futura cinemática; no hay combate.";
            var gate = Point(anchors, "bloques", "Puerta de Bloques", RectoriaPointKind.BlocksGate, 22, 42, 22, 41);
            gate.designNote = "Conservar cerrada hasta implementar la revisión de los tres documentos por Rocky.";

            Sign(signs, "Recibidor", "RECTORÍA", 21.5f, 7.9f, 5.8f);
            Sign(signs, "Rectora", "RECTORA", 21.5f, 19.7f, 4.5f);
            Sign(signs, "Archivo", "ARCHIVO", 7.6f, 31.6f, 5.2f);
            Sign(signs, "Credencializacion", "CREDENCIALIZACIÓN", 35.1f, 31.6f, 8.8f);
            Sign(signs, "Caja", "CAJA", 7.7f, 41.6f, 4.4f);
            Sign(signs, "Tramites", "TRÁMITES\nSolo personal", 17.7f, 40.6f, 3.3f);
            Sign(signs, "Bloques", "BLOQUES", 23f, 44.1f, 3.5f);
            Sign(signs, "Cruce", "< ARCHIVO     CREDENCIALIZACIÓN >", 22f, 28f, 6f, .22f);

            // Decorations sit entirely inside existing obstacles: they never consume walkable cells.
            Plant(decor, "Planta_Recibidor_Izq", 16, 1);
            Plant(decor, "Planta_Recibidor_Der", 27, 1);
            Plant(decor, "Planta_Rectora", 16, 12);
            var rug = Child(decor, "Alfombra_Recibidor");
            rug.position = new Vector3(21.5f, 5f, .5f);
            Shape(rug, "Tejido", Vector2.zero, new Vector2(3.2f, 2.8f), new Color32(95, 58, 64, 170), -2);
            Shape(rug, "Borde_Superior", new Vector2(0, 1.3f), new Vector2(2.95f, .055f), Gold, -1);
            Shape(rug, "Borde_Inferior", new Vector2(0, -1.3f), new Vector2(2.95f, .055f), Gold, -1);

            var guide = stage.GetComponent<RectoriaSceneGuide>();
            if (guide == null) guide = Undo.AddComponent<RectoriaSceneGuide>(stage.gameObject);
            guide.player = player;
            guide.points = stage.GetComponentsInChildren<RectoriaScenePoint>();
            BuildHud(stage, guide);
            EditorUtility.SetDirty(guide);
            EditorSceneManager.MarkSceneDirty(scene);
            Validate(stage);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("No se pudo guardar Rectoría.");
            Undo.CollapseUndoOperations(undo);
            Debug.Log("Rectoría: escenario preparado con 5 NPC provisionales, 4 notas, foto, recibos y accesos. Sin modificar layout, escala ni lógica de movimiento.");
        }

        private static void PaintExisting(Tilemap target, TileBase art)
        {
            Undo.RegisterCompleteObjectUndo(target, "Actualizar apariencia de " + target.name);
            var occupied = new List<Vector3Int>();
            foreach (var p in target.cellBounds.allPositionsWithin) if (target.HasTile(p)) occupied.Add(p);
            var positions = occupied.ToArray();
            target.SetTiles(positions, Enumerable.Repeat(art, positions.Length).ToArray());
            foreach (var p in positions) { target.SetTileFlags(p, TileFlags.None); target.SetColor(p, Color.white); }
            EditorUtility.SetDirty(target);
        }

        private static RectoriaScenePoint Point(Transform parent, string id, string name, RectoriaPointKind kind, int x, int y, int ax, int ay)
        {
            var root = Child(parent, id);
            root.position = map.CellCenter(new Vector3Int(x, y, 0));
            var point = root.GetComponent<RectoriaScenePoint>();
            if (point == null) point = Undo.AddComponent<RectoriaScenePoint>(root.gameObject);
            point.pointId = id; point.displayName = name; point.kind = kind;
            point.cell = new Vector3Int(x, y, 0); point.approachCell = new Vector3Int(ax, ay, 0);
            point.provisionalVisual = true;
            EditorUtility.SetDirty(point);
            return point;
        }

        private static void Npc(Transform parent, string id, string name, string office, int x, int y, int ax, int ay, Color clothes)
        {
            var point = Point(parent, id, name, RectoriaPointKind.Npc, x, y, ax, ay);
            point.designNote = office + ". Figura geométrica provisional; sustituir por arte del equipo. Diálogo pendiente.";
            // Keep the occupied NPC tile for logical and physical collisions; hide only its furniture graphic.
            if (!map.obstacles.HasTile(point.cell)) throw new InvalidOperationException("El NPC debe ocupar su casilla original: " + id);
            map.obstacles.SetColor(point.cell, new Color(1, 1, 1, 0));
            Shape(point.transform, "Sombra", new Vector2(0, -.32f), new Vector2(.72f, .2f), new Color(0, 0, 0, .25f), 28);
            Shape(point.transform, "Cuerpo", new Vector2(0, -.08f), new Vector2(.48f, .43f), clothes, 30);
            Shape(point.transform, "Pie_Izq", new Vector2(-.14f, -.35f), new Vector2(.16f, .15f), Panel, 31);
            Shape(point.transform, "Pie_Der", new Vector2(.14f, -.35f), new Vector2(.16f, .15f), Panel, 31);
            Shape(point.transform, "Cabeza", new Vector2(0, .28f), new Vector2(.38f, .32f), new Color32(244, 200, 157, 255), 32);
            Shape(point.transform, "Cabello", new Vector2(0, .45f), new Vector2(.4f, .1f), Panel, 33);
            WorldText(point.transform, "Nombre", name, new Vector2(0, .8f), 3.2f, 4.5f, Color.white);
            WorldText(point.transform, "Aviso_Provisional", "PROVISIONAL", new Vector2(0, -.62f), 1.6f, 4f, Gold);
        }

        private static void Note(Transform parent, string id, string name, int x, int y, int ax, int ay, string text)
        {
            var note = Point(parent, id, name, RectoriaPointKind.Clue, x, y, ax, ay);
            note.designNote = text;
            Shape(note.transform, "Papel", Vector2.zero, new Vector2(.36f, .43f), new Color32(255, 230, 123, 255), 20);
            for (int i = 0; i < 3; i++) Shape(note.transform, "Linea_" + i, new Vector2(0, .1f - i * .1f), new Vector2(.23f, .025f), Panel, 21);
            WorldText(note.transform, "Numero", id.Substring(id.Length - 1), new Vector2(.29f, .12f), 1.3f, 1f, Gold);
        }

        private static void Plant(Transform parent, string name, int x, int y)
        {
            if (!map.obstacles.HasTile(new Vector3Int(x, y, 0))) return;
            var root = Child(parent, name); root.position = map.CellCenter(new Vector3Int(x, y, 0));
            Shape(root, "Maceta", new Vector2(0, -.15f), new Vector2(.4f, .35f), new Color32(163, 103, 69, 255), 20);
            Shape(root, "Hojas_1", new Vector2(-.1f, .16f), new Vector2(.37f, .38f), new Color32(88, 155, 96, 255), 21);
            Shape(root, "Hojas_2", new Vector2(.12f, .27f), new Vector2(.3f, .4f), new Color32(119, 181, 111, 255), 22);
        }

        private static Transform Child(Transform parent, string name)
        {
            var found = parent != null ? parent.Find(name) : GameObject.Find(name)?.transform;
            if (found != null) return found;
            var go = new GameObject(name); Undo.RegisterCreatedObjectUndo(go, "Crear " + name);
            go.transform.SetParent(parent, false); return go.transform;
        }

        private static Transform Shape(Transform parent, string name, Vector2 offset, Vector2 size, Color color, int order)
        {
            var root = Child(parent, name); root.localPosition = new Vector3(offset.x, offset.y, 0); root.localScale = new Vector3(size.x, size.y, 1);
            var sr = root.GetComponent<SpriteRenderer>(); if (sr == null) sr = Undo.AddComponent<SpriteRenderer>(root.gameObject);
            sr.sprite = square; sr.color = color; sr.sortingOrder = order;
            sr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Art + "Rectoria_Unlit.mat");
            return root;
        }

        private static void Sign(Transform parent, string name, string text, float x, float y, float width, float height = .48f)
        {
            var root = Child(parent, name); root.position = new Vector3(x, y, .5f);
            Shape(root, "Placa", Vector2.zero, new Vector2(width, height * (text.Contains("\n") ? 2 : 1)), Panel, 39);
            WorldText(root, "Texto", text, Vector2.zero, text.Length > 22 ? 2f : 3.2f, width, Gold);
        }

        private static void WorldText(Transform parent, string name, string text, Vector2 offset, float size, float width, Color color)
        {
            var root = Child(parent, name); root.localPosition = new Vector3(offset.x, offset.y, -.01f);
            var tmp = root.GetComponent<TextMeshPro>(); if (tmp == null) tmp = Undo.AddComponent<TextMeshPro>(root.gameObject);
            tmp.font = font; tmp.fontSize = size; tmp.text = text; tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center; tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.rectTransform.sizeDelta = new Vector2(width, 1.5f);
            tmp.GetComponent<MeshRenderer>().sortingOrder = 45;
        }

        private static void BuildHud(Transform stage, RectoriaSceneGuide guide)
        {
            var root = Child(stage, "Orientacion_UI");
            var canvas = root.GetComponent<Canvas>(); if (canvas == null) canvas = Undo.AddComponent<Canvas>(root.gameObject);
            root = canvas.transform;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            var scaler = root.GetComponent<CanvasScaler>(); if (scaler == null) scaler = Undo.AddComponent<CanvasScaler>(root.gameObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            HudPanel(root, "Fondo_Zona", true);
            HudPanel(root, "Fondo_Controles", false);
            guide.areaLabel = HudText(root, "Zona", "RECTORÍA / RECIBIDOR", true, 22);
            guide.contextLabel = HudText(root, "Controles_Y_Contexto", "WASD / Flechas: caminar     Z: correr", false, 20);
        }

        private static void HudPanel(Transform parent, string name, bool top)
        {
            var root = Child(parent, name); var image = root.GetComponent<Image>();
            if (image == null) image = Undo.AddComponent<Image>(root.gameObject);
            image.color = Panel; image.raycastTarget = false;
            var rect = image.rectTransform; rect.anchorMin = new Vector2(0, top ? 1 : 0); rect.anchorMax = new Vector2(1, top ? 1 : 0);
            rect.pivot = new Vector2(.5f, top ? 1 : 0); rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(0, 68);
            rect.SetAsFirstSibling();
        }

        private static TextMeshProUGUI HudText(Transform parent, string name, string text, bool top, float size)
        {
            var root = Child(parent, name); var tmp = root.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = Undo.AddComponent<TextMeshProUGUI>(root.gameObject);
            var rect = tmp.rectTransform; rect.anchorMin = new Vector2(0, top ? 1 : 0); rect.anchorMax = new Vector2(1, top ? 1 : 0);
            rect.pivot = new Vector2(.5f, top ? 1 : 0); rect.anchoredPosition = new Vector2(0, top ? -16 : 16); rect.sizeDelta = new Vector2(-48, 40);
            tmp.font = font; tmp.fontSize = size; tmp.color = Gold; tmp.text = text; tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false; tmp.textWrappingMode = TextWrappingModes.NoWrap;
            return tmp;
        }

        public static string Validate(Transform stage = null)
        {
            if (map == null) map = UnityEngine.Object.FindFirstObjectByType<RectoriaGridMap>();
            if (stage == null) stage = GameObject.Find("Rectoria_Escenario")?.transform;
            var player = UnityEngine.Object.FindFirstObjectByType<RectoriaPlayerTest>();
            if (map == null || stage == null || player == null) throw new InvalidOperationException("Escenario incompleto.");
            var start = map.grid.WorldToCell(player.transform.position);
            var seen = new HashSet<Vector3Int> { start }; var queue = new Queue<Vector3Int>(); queue.Enqueue(start);
            var directions = new[] { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
            while (queue.Count > 0) { var p = queue.Dequeue(); foreach (var d in directions) { var n = p + d; if (map.CanEnter(n) && seen.Add(n)) queue.Enqueue(n); } }
            var points = stage.GetComponentsInChildren<RectoriaScenePoint>();
            if (points.Length != 13 || points.Select(p => p.pointId).Distinct().Count() != points.Length) throw new InvalidOperationException("Se esperaban 13 puntos únicos.");
            foreach (var point in points)
            {
                if (!seen.Contains(point.approachCell)) throw new InvalidOperationException("Punto inaccesible: " + point.pointId);
                var delta = point.cell - point.approachCell;
                if (point.kind != RectoriaPointKind.PhotoMark && Mathf.Abs(delta.x) + Mathf.Abs(delta.y) != 1)
                    throw new InvalidOperationException("Interacción no adyacente: " + point.pointId);
                if (point.kind == RectoriaPointKind.Npc && map.CanEnter(point.cell)) throw new InvalidOperationException("NPC sin bloqueo lógico: " + point.pointId);
            }
            return "13 puntos accesibles; 5 NPCs bloquean su casilla; " + seen.Count + " casillas transitables desde la entrada. Puertas cerradas.";
        }
    }
}
#endif
