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
    public static class CredentialAndMinimapSetup
    {
        private static readonly Color Gold = new Color32(233, 216, 166, 255);
        private static readonly Color Dark = new Color32(27, 32, 45, 255);
        private static TMP_FontAsset font;
        [MenuItem("Campus Quest/Rectoria/8 - Credencialización y minimapa")]
        public static void Configure()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty || scene.path != RectoriaSceneStageSetup.ScenePath)
                throw new InvalidOperationException("Abre la escena de arte guardada, fuera de Play Mode.");
            var session = UnityEngine.Object.FindFirstObjectByType<DialogueSession>();
            var interactor = UnityEngine.Object.FindFirstObjectByType<RectoriaDialogueInteractor>();
            if (session == null || interactor == null) throw new InvalidOperationException("Falta el sistema de diálogos.");
            if (session.GetComponent<CredentialChallenge>() != null) return;
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/PixeloidSans SDF.asset");
            var credential = Undo.AddComponent<CredentialChallenge>(session.gameObject);
            credential.session = session; credential.mission = session.GetComponent<EnrollmentMission>();
            credential.player = interactor.player; credential.archive = session.GetComponent<ArchiveChallenge>();
            credential.photoMark = interactor.points.Single(p => p.pointId == "foto");
            credential.introDialogue = Dialogue("Mony_Fotografia", CredentialChallenge.IntroId,
                "Para tu credencial necesito tu foto. Párate sobre la marca del piso",
                "Cuando salga la foto, la máquina te pedirá armar el marco. Se desacomodó");
            credential.successDialogue = Dialogue("Mony_Credencial_Lista", "rectoria.credential.mony_success", "¡Listo! Ya eres oficialmente estudiante");
            credential.prerequisiteDialogue = Dialogue("Mony_Primero_Rectora", "rectoria.credential.visit_rector_first", "Primero habla con la Rectora para comenzar tu inscripción. Después tomamos tu fotografía.");
            var mony = interactor.points.Single(p => p.pointId == "lic_mony");
            Undo.RecordObject(mony, "Conectar Lic. Mony"); mony.dialogue = credential.introDialogue; EditorUtility.SetDirty(mony);
            Undo.RecordObject(interactor, "Conectar Credencialización"); interactor.credential = credential; EditorUtility.SetDirty(interactor);
            BuildCredential(credential);
            BuildMinimap(session, interactor, credential);
            // The upper corners now have one panel each, leaving dialogue and context unobstructed.
            var documents = credential.mission.hud.transform.Find("Panel").GetComponent<RectTransform>();
            Undo.RecordObject(documents, "Separar documentos y minimapa");
            documents.anchorMin = documents.anchorMax = documents.pivot = new Vector2(0, 1);
            documents.anchoredPosition = new Vector2(24, -70); EditorUtility.SetDirty(documents);
            var guide = UnityEngine.Object.FindFirstObjectByType<RectoriaSceneGuide>();
            Undo.RecordObject(guide.contextLabel, "Actualizar control de carrera");
            guide.contextLabel.text = "WASD / Flechas: caminar     Shift: correr"; EditorUtility.SetDirty(guide.contextLabel);
            EditorUtility.SetDirty(credential); AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }

        private static DialogueDefinition Dialogue(string name, string id, params string[] lines)
        {
            string path = "Assets/Data/Dialogs/Rectoria/" + name + ".asset";
            var d = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(path);
            if (d != null) return d;
            d = ScriptableObject.CreateInstance<DialogueDefinition>(); d.dialogueId = id; d.defaultSpeaker = "Lic. Mony";
            d.defaultPortrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Characters/Rectoria/licmony.png");
            d.lines = lines.Select(t => new DialogueLine { text = t }).ToArray(); AssetDatabase.CreateAsset(d, path); return d;
        }

        private static Transform CanvasRoot(Transform parent, string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Undo.RegisterCreatedObjectUndo(go, "Crear interfaz de Rectoría"); go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = order; canvas.pixelPerfect = true;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f; return go.transform;
        }

        private static void BuildCredential(CredentialChallenge c)
        {
            var root = CanvasRoot(c.transform, "Credencializacion_UI", 85);
            var capture = Box(root, "Captura", new Vector2(0, 130), new Vector2(520, 150), Gold);
            Box(capture, "Fondo", Vector2.zero, new Vector2(514, 144), Dark);
            c.capturePanel = capture.gameObject;
            c.countdownLabel = Text(capture, "Cuenta_Regresiva", "", Vector2.zero, new Vector2(500, 130), 32, Gold);
            var shade = Box(root, "Rompecabezas", Vector2.zero, Vector2.zero, new Color32(8, 12, 20, 245));
            shade.anchorMin = Vector2.zero; shade.anchorMax = Vector2.one; c.puzzlePanel = shade.gameObject;
            var frame = Box(shade, "Marco", Vector2.zero, new Vector2(1120, 650), Gold);
            var body = Box(frame, "Fondo", Vector2.zero, new Vector2(1112, 642), Dark);
            c.statusLabel = Text(body, "Objetivo", "", new Vector2(0, 280), new Vector2(1060, 56), 25, Gold);
            c.tiles = new CredentialTileGraphic[9]; c.frames = new Image[9]; c.numbers = new TMP_Text[9];
            for (int i = 0; i < 9; i++)
            {
                var tileFrame = Box(body, "Pieza_" + i, new Vector2(-340 + i % 3 * 140, 140 - i / 3 * 140), new Vector2(134, 134), Gold);
                c.frames[i] = tileFrame.GetComponent<Image>();
                Box(tileFrame, "Vacio", Vector2.zero, new Vector2(128, 128), new Color32(38, 45, 60, 255));
                var go = new GameObject("Imagen", typeof(RectTransform), typeof(CredentialTileGraphic)); go.transform.SetParent(tileFrame, false);
                go.GetComponent<RectTransform>().sizeDelta = new Vector2(124, 124); c.tiles[i] = go.GetComponent<CredentialTileGraphic>(); c.tiles[i].raycastTarget = false;
                var numberBox = Box(tileFrame, "Numero_Fondo", new Vector2(48, -48), new Vector2(24, 24), Dark);
                c.numbers[i] = Text(numberBox, "Numero", "", Vector2.zero, new Vector2(24, 24), 16, Gold);
            }
            Text(body, "Referencia_Titulo", "CREDENCIAL DE ESTUDIANTE\nIMAGEN PROVISIONAL", new Vector2(280, 207), new Vector2(420, 64), 22, Gold);
            var reference = Box(body, "Referencia", new Vector2(280, 45), new Vector2(246, 246), Dark);
            for (int i = 0; i < 9; i++)
            {
                var go = new GameObject("Referencia_" + i, typeof(RectTransform), typeof(CredentialTileGraphic)); go.transform.SetParent(reference, false);
                var r = go.GetComponent<RectTransform>(); r.sizeDelta = new Vector2(80, 80); r.anchoredPosition = new Vector2(-80 + i % 3 * 80, 80 - i / 3 * 80);
                var graphic = go.GetComponent<CredentialTileGraphic>(); graphic.SetTile(i + 1); graphic.raycastTarget = false;
            }
            c.rewardLabel = Text(body, "Recompensa", "", new Vector2(280, -122), new Vector2(430, 72), 22, Gold);
            c.restartControl = Box(body, "Reiniciar", new Vector2(280, -195), new Vector2(210, 45), new Color32(64, 65, 70, 255));
            Text(c.restartControl, "Etiqueta", "R · Reiniciar", Vector2.zero, new Vector2(206, 40), 21, Gold);
            c.movesLabel = Text(body, "Controles", "", new Vector2(0, -275), new Vector2(1050, 86), 19, Gold);
            var flash = Box(root, "Flash", Vector2.zero, Vector2.zero, Color.white); flash.anchorMin = Vector2.zero; flash.anchorMax = Vector2.one;
            c.flash = flash.GetComponent<Image>();
            capture.gameObject.SetActive(false); shade.gameObject.SetActive(false); flash.gameObject.SetActive(false);
        }

        private static void BuildMinimap(DialogueSession session, RectoriaDialogueInteractor interactor, CredentialChallenge credential)
        {
            var root = CanvasRoot(session.transform, "Rectoria_Minimapa_UI", 35);
            var panel = Box(root, "Minimapa", Vector2.zero, new Vector2(188, 226), Gold);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1, 1); panel.anchoredPosition = new Vector2(-24, -70);
            var group = panel.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false;
            var inside = Box(panel, "Fondo", Vector2.zero, new Vector2(182, 220), Dark);
            Text(inside, "Titulo", "RECTORÍA", new Vector2(0, 94), new Vector2(178, 24), 17, Gold);
            var mapObject = new GameObject("Cuadricula_Logica", typeof(RectTransform), typeof(RectoriaMinimap)); mapObject.transform.SetParent(inside, false);
            var rect = mapObject.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(132, 138); rect.anchoredPosition = new Vector2(0, 5);
            var minimap = mapObject.GetComponent<RectoriaMinimap>(); minimap.raycastTarget = false; minimap.visibility = group;
            minimap.map = interactor.player.map; minimap.player = interactor.player; minimap.archive = credential.archive; minimap.credential = credential;
            minimap.destinations = new[] { "don_chuy", "lic_mony", "beto", "bloques" }.Select(id => interactor.points.Single(p => p.pointId == id)).ToArray();
            minimap.markers = new RectTransform[4]; var labels = new[] { "A", "C", "$", "B" };
            for (int i = 0; i < 4; i++) minimap.markers[i] = Text(rect, "Destino_" + labels[i], labels[i], Vector2.zero, new Vector2(15, 15), 13, Gold).rectTransform;
            minimap.playerDot = Box(rect, "Mateo", Vector2.zero, new Vector2(5, 5), new Color32(110, 231, 237, 255));
            Text(inside, "Leyenda", "A Archivo   C Credencial\n$ Caja   B Bloques\nPunto celeste · Tú", new Vector2(0, -85), new Vector2(178, 49), 12, Gold);
            minimap.SetVerticesDirty(); EditorUtility.SetDirty(minimap);
        }

        private static RectTransform Box(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>(); r.sizeDelta = size; r.anchoredPosition = position;
            go.GetComponent<Image>().color = color; go.GetComponent<Image>().raycastTarget = false; return r;
        }
        private static TMP_Text Text(Transform parent, string name, string value, Vector2 position, Vector2 size, float fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>(); r.sizeDelta = size; r.anchoredPosition = position;
            var t = go.GetComponent<TMP_Text>(); t.font = font; t.fontSize = fontSize; t.color = color; t.text = value;
            t.richText = false; t.raycastTarget = false; t.alignment = TextAlignmentOptions.Center; return t;
        }
    }
}
#endif
