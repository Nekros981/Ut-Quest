#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using CampusQuest.Dialogue;
using CampusQuest.Rectoria;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CampusQuest.EditorTools
{
    public static class RockyGateSetup
    {
        private static readonly Color Gold = new Color32(233, 216, 166, 255);
        private static readonly Color Dark = new Color32(47, 51, 64, 255);
        [MenuItem("Campus Quest/Rectoria/10 - Guardia Rocky y puerta existente")]
        public static void Configure()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty || scene.path != RectoriaSceneStageSetup.ScenePath)
                throw new InvalidOperationException("Abre la escena de arte guardada fuera de Play Mode.");
            var interactor = UnityEngine.Object.FindFirstObjectByType<RectoriaDialogueInteractor>();
            if (interactor == null || interactor.session == null) throw new InvalidOperationException("Falta el sistema de diálogos.");
            if (interactor.session.GetComponent<RockyGateReview>() != null) return;
            var map = interactor.player.map;
            var gate = interactor.points.Single(p => p.pointId == "bloques");
            var cells = new List<Vector3Int>();
            for (var cell = gate.cell; map.doors.HasTile(cell) && cells.Count < 6; cell += Vector3Int.right) cells.Add(cell);
            if (cells.Count != 2 || map.doors.GetComponent<TilemapCollider2D>() == null || cells.Any(c => !map.floor.HasTile(c)))
                throw new InvalidOperationException("La puerta existente debe tener dos celdas bloqueadas, suelo y TilemapCollider2D.");
            var controller = Undo.AddComponent<RockyGateReview>(interactor.session.gameObject);
            controller.session = interactor.session; controller.mission = interactor.session.GetComponent<EnrollmentMission>();
            controller.player = interactor.player;
            controller.map = map; controller.minimap = UnityEngine.Object.FindFirstObjectByType<RectoriaMinimap>(); controller.gateCells = cells.ToArray();
            controller.missingDialogue = Dialogue("Rocky_Faltan_Documentos", "rectoria.rocky.missing_documents", "Aún te faltan documentos. Revisa la barra de arriba");
            controller.reviewDialogue = Dialogue("Rocky_Revisar_Documentos", RockyGateReview.ReviewId,
                "A ver... acta, credencial y comprobante", "Todo en regla. Adelante");
            controller.resumeDialogue = Dialogue("Rocky_Continuar_Revision", RockyGateReview.ResumeId, "Todo en regla. Adelante");
            controller.alreadyOpenDialogue = Dialogue("Rocky_Puerta_Abierta", "rectoria.rocky.already_open", "La Puerta de Bloques ya está abierta. Adelante.");
            var rocky = interactor.points.Single(p => p.pointId == "rocky");
            Undo.RecordObject(rocky, "Conectar revisión de Rocky"); rocky.dialogue = controller.reviewDialogue; EditorUtility.SetDirty(rocky);
            Undo.RecordObject(interactor, "Conectar Rocky"); interactor.rocky = controller; EditorUtility.SetDirty(interactor);
            BuildLockIndicators(controller, gate);
            EditorUtility.SetDirty(controller); AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("No se pudo guardar la escena.");
        }

        private static DialogueDefinition Dialogue(string name, string id, params string[] lines)
        {
            string path = "Assets/Data/Dialogs/Rectoria/" + name + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<DialogueDefinition>(); asset.dialogueId = id; asset.defaultSpeaker = "Guardia Rocky";
            asset.defaultPortrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Characters/Rectoria/guardiarocky.png");
            asset.lines = lines.Select(text => new DialogueLine { text = text }).ToArray(); AssetDatabase.CreateAsset(asset, path); return asset;
        }

        private static void BuildLockIndicators(RockyGateReview controller, RectoriaScenePoint gate)
        {
            // These are only the three lock slots. The existing door remains its original Tilemap.
            var root = new GameObject("Candado_Documentos"); Undo.RegisterCreatedObjectUndo(root, "Añadir ranuras al candado existente");
            root.transform.SetParent(gate.transform, false);
            root.transform.position = (controller.map.CellCenter(controller.gateCells[0]) + controller.map.CellCenter(controller.gateCells[1])) * .5f;
            controller.lockRoot = root; controller.slots = new SpriteRenderer[3]; controller.documents = new SpriteRenderer[3];
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Enviroment/Rectoria/Textures/Piso.png");
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Sprites/Enviroment/Rectoria/Rectoria_Unlit.mat");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/PixeloidSans SDF.asset");
            var names = new[] { "ACTA", "CRED.", "PAGO" };
            for (int i = 0; i < 3; i++)
            {
                var holder = new GameObject("Ranura_" + names[i]).transform; holder.SetParent(root.transform, false);
                holder.localPosition = new Vector3((i - 1) * .48f, 0, 0);
                Shape(holder, "Marco", Vector2.zero, new Vector2(.38f, .44f), sprite, material, Gold, 70);
                controller.slots[i] = Shape(holder, "Luz", Vector2.zero, new Vector2(.30f, .36f), sprite, material, Dark, 71);
                controller.documents[i] = Shape(root.transform, "Documento_" + names[i], new Vector2((i - 1) * .48f, 0), new Vector2(.17f, .24f), sprite, material, Gold, 72);
                controller.documents[i].gameObject.SetActive(false);
                var label = new GameObject("Etiqueta_" + names[i], typeof(TextMeshPro)); label.transform.SetParent(holder, false);
                label.transform.localPosition = new Vector3(0, .4f, -.01f);
                var text = label.GetComponent<TextMeshPro>(); text.font = font; text.fontSize = 1.1f; text.text = names[i]; text.color = Gold;
                text.alignment = TextAlignmentOptions.Center; text.textWrappingMode = TextWrappingModes.NoWrap;
                text.rectTransform.sizeDelta = new Vector2(.48f, .35f); text.GetComponent<MeshRenderer>().sortingOrder = 73;
            }
        }
        private static SpriteRenderer Shape(Transform parent, string name, Vector2 position, Vector2 size, Sprite sprite, Material material, Color color, int order)
        {
            var go = new GameObject(name, typeof(SpriteRenderer)); go.transform.SetParent(parent, false); go.transform.localPosition = position;
            go.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1);
            var renderer = go.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sharedMaterial = material; renderer.color = color; renderer.sortingOrder = order;
            return renderer;
        }
    }
}
#endif
