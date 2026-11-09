#if UNITY_EDITOR
using System;
using System.Linq;
using CampusQuest.Dialogue;
using CampusQuest.Rectoria;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CampusQuest.EditorTools
{
    public static class ArchiveChallengeSetup
    {
        public const string FolderTablePath = "Assets/Data/Dialogs/Rectoria/Archivo_Carpetas.asset";
        private const string Data = "Assets/Data/Dialogs/Rectoria/";
        private static readonly Color Gold = new Color32(233, 216, 166, 255);
        private static readonly Color Dark = new Color32(27, 32, 45, 255);
        private static TMP_FontAsset font;

        [MenuItem("Campus Quest/Rectoria/7 - Conectar Archivo con Don Chuy")]
        public static void Configure()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != RectoriaSceneStageSetup.ScenePath || scene.isDirty)
                throw new InvalidOperationException("Abre la escena de arte guardada fuera de Play Mode.");
            var session = UnityEngine.Object.FindFirstObjectByType<DialogueSession>();
            var interactor = UnityEngine.Object.FindFirstObjectByType<RectoriaDialogueInteractor>();
            if (session == null || interactor == null || session.GetComponent<EnrollmentMission>() == null)
                throw new InvalidOperationException("Falta el sistema de conversación o inscripción.");
            var existing = session.GetComponent<ArchiveChallenge>();
            if (existing != null)
            {
                foreach (var icon in existing.folderIcons)
                    if (icon != null && icon.GetComponent<CanvasRenderer>() == null) Undo.AddComponent<CanvasRenderer>(icon.gameObject);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                return;
            }
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/PixeloidSans SDF.asset");
            if (font == null) throw new InvalidOperationException("Falta Pixeloid Sans.");
            var archive = Undo.AddComponent<ArchiveChallenge>(session.gameObject);
            archive.session = session; archive.mission = session.GetComponent<EnrollmentMission>();
            archive.table = AssetDatabase.LoadAssetAtPath<ArchiveFolderTable>(FolderTablePath);
            if (archive.table == null)
            {
                archive.table = ScriptableObject.CreateInstance<ArchiveFolderTable>();
                AssetDatabase.CreateAsset(archive.table, FolderTablePath);
            }
            archive.prerequisiteDialogue = Dialogue("Archivo_Primero_Rectora", "rectoria.archive.visit_rector_first", "Don Chuy", "Primero habla con la Rectora para comenzar tu inscripción. Después te ayudo a buscar tu acta.");
            archive.introDialogue = Dialogue("DonChuy_Introduccion", ArchiveChallenge.IntroId, "Don Chuy",
                "¡Bienvenido al Archivo! Tengo todas las carpetas revueltas y necesito tu ayuda para encontrar tu acta de nacimiento.",
                "Dejé cuatro notas por la oficina. Acércate a cada una, mírala y presiona E o Enter para leerla.",
                "Cuando hayas leído las cuatro pistas, vuelve conmigo. Juntos encontraremos la carpeta correcta.");
            archive.missingCluesDialogue = Dialogue("DonChuy_Faltan_Notas", "rectoria.archive.missing_clues", "Don Chuy", "Todavía faltan pistas. Lee las cuatro notas de la oficina y vuelve conmigo para revisar las carpetas.");
            archive.readyDialogue = Dialogue("DonChuy_Abrir_Carpetas", ArchiveChallenge.ReadyId, "Don Chuy", "¡Ya tienes las cuatro pistas! Revisa el color y el año de cada carpeta.", "Tienes tres intentos. Si no la encuentras, reorganizaré las carpetas para que vuelvas a intentarlo.");
            archive.missingTableDialogue = Dialogue("Archivo_Tabla_Pendiente", "rectoria.archive.table_pending", "Don Chuy", "Las carpetas aún no están disponibles. Tus cuatro pistas quedan guardadas.");
            var points = UnityEngine.Object.FindObjectsByType<RectoriaScenePoint>(FindObjectsSortMode.None);
            var chuy = points.Single(p => p.pointId == "don_chuy");
            Undo.RecordObject(chuy, "Conectar diálogo de Don Chuy"); chuy.dialogue = archive.introDialogue; EditorUtility.SetDirty(chuy);
            for (int i = 1; i <= 4; i++)
            {
                var point = points.Single(p => p.pointId == "nota_" + i);
                Undo.RecordObject(point, "Conectar lectura de nota");
                point.dialogue = Dialogue("Archivo_Nota_" + i, ArchiveChallenge.ClueId(i), "Nota " + i + " · Archivo", ArchiveChallenge.Clues[i - 1]);
                EditorUtility.SetDirty(point);
            }
            Undo.RecordObject(interactor, "Conectar interacciones de Archivo"); interactor.archive = archive;
            BuildView(archive);
            EditorUtility.SetDirty(interactor); EditorUtility.SetDirty(archive);
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("No se pudo guardar la escena.");
        }

        private static DialogueDefinition Dialogue(string name, string id, string speaker, params string[] lines)
        {
            var asset = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(Data + name + ".asset");
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<DialogueDefinition>();
            asset.dialogueId = id; asset.defaultSpeaker = speaker;
            if (speaker == "Don Chuy") asset.defaultPortrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Characters/Rectoria/don chuy.png");
            asset.lines = lines.Select(t => new DialogueLine { text = t }).ToArray();
            AssetDatabase.CreateAsset(asset, Data + name + ".asset");
            return asset;
        }

        private static void BuildView(ArchiveChallenge archive)
        {
            var root = new GameObject("Archivo_Acertijo_UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Undo.RegisterCreatedObjectUndo(root, "Crear interfaz del Archivo"); root.transform.SetParent(archive.transform, false);
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 80; canvas.pixelPerfect = true;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            var shade = Box(root.transform, "Modal", Vector2.zero, new Vector2(1280, 720), new Color32(8, 12, 20, 235));
            shade.anchorMin = Vector2.zero; shade.anchorMax = Vector2.one; shade.sizeDelta = Vector2.zero;
            archive.panel = shade.gameObject;
            var border = Box(shade, "Marco", Vector2.zero, new Vector2(1120, 640), Gold);
            var body = Box(border, "Fondo", Vector2.zero, new Vector2(1112, 632), Dark);
            Text(body, "Titulo", "ARCHIVO · DON CHUY", new Vector2(0, 276), new Vector2(1050, 38), 30, Gold);
            archive.statusLabel = Text(body, "Estado", "", new Vector2(0, 225), new Vector2(1050, 48), 20, Color.white);
            archive.clueLabel = Text(body, "Pistas", "", new Vector2(-341, -5), new Vector2(330, 370), 19, Gold);
            archive.clueLabel.alignment = TextAlignmentOptions.TopLeft;
            archive.folderLabels = new TMP_Text[9]; archive.folderFrames = new Image[9]; archive.folderIcons = new Graphic[9];
            for (int i = 0; i < 9; i++)
            {
                var frame = Box(body, "Carpeta_" + i, new Vector2(-64 + i % 3 * 220, 115 - i / 3 * 139), new Vector2(206, 125), Gold);
                archive.folderFrames[i] = frame.GetComponent<Image>();
                var tile = Box(frame, "Fondo", Vector2.zero, new Vector2(200, 119), new Color32(39, 46, 61, 255));
                var icon = new GameObject("Carpeta_Pixel_Provisional", typeof(RectTransform), typeof(ArchiveFolderGraphic)); icon.transform.SetParent(tile, false);
                var ir = icon.GetComponent<RectTransform>(); ir.anchoredPosition = new Vector2(0, 16); ir.sizeDelta = new Vector2(91, 62);
                archive.folderIcons[i] = icon.GetComponent<ArchiveFolderGraphic>(); archive.folderIcons[i].raycastTarget = false;
                archive.folderLabels[i] = Text(tile, "Color_Año", "", new Vector2(0, -35), new Vector2(190, 29), 20, Color.white);
            }
            archive.instructionsLabel = Text(body, "Controles", "", new Vector2(0, -271), new Vector2(1050, 57), 19, Gold);
            var reward = Box(body, "Recompensa", new Vector2(150, -87), new Vector2(620, 65), Gold);
            Box(reward, "Fondo", Vector2.zero, new Vector2(614, 59), Dark);
            archive.rewardBanner = reward;
            archive.rewardLabel = Text(reward, "Acta", "ACTA DE NACIMIENTO CONSEGUIDA", Vector2.zero, new Vector2(600, 55), 22, Gold);
            reward.gameObject.SetActive(false); shade.gameObject.SetActive(false);
        }

        private static RectTransform Box(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchoredPosition = position; rect.sizeDelta = size;
            var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
            return rect;
        }
        private static TMP_Text Text(Transform parent, string name, string value, Vector2 position, Vector2 size, float fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchoredPosition = position; rect.sizeDelta = size;
            var text = go.GetComponent<TMP_Text>(); text.font = font; text.fontSize = fontSize; text.color = color;
            text.text = value; text.alignment = TextAlignmentOptions.Center; text.richText = false; text.raycastTarget = false;
            return text;
        }
    }
}
#endif
