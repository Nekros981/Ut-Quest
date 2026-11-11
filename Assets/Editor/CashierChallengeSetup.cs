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
    public static class CashierChallengeSetup
    {
        private static readonly Color Gold = new Color32(233, 216, 166, 255);
        private static readonly Color Dark = new Color32(27, 32, 45, 255);
        private static TMP_FontAsset font;

        [MenuItem("Campus Quest/Rectoria/9 - Conectar Caja con Cajero Beto")]
        public static void Configure()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty || scene.path != RectoriaSceneStageSetup.ScenePath)
                throw new InvalidOperationException("Abre la escena de arte guardada fuera de Play Mode.");
            var interactor = UnityEngine.Object.FindFirstObjectByType<RectoriaDialogueInteractor>();
            if (interactor == null || interactor.session == null) throw new InvalidOperationException("Falta la sesión de diálogos.");
            var session = interactor.session;
            if (session.GetComponent<CashierChallenge>() != null) return;
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/PixeloidSans SDF.asset");
            if (font == null) throw new InvalidOperationException("Falta Pixeloid Sans.");
            var cashier = Undo.AddComponent<CashierChallenge>(session.gameObject);
            cashier.session = session; cashier.mission = session.GetComponent<EnrollmentMission>();
            cashier.archive = session.GetComponent<ArchiveChallenge>(); cashier.credential = session.GetComponent<CredentialChallenge>();
            cashier.introDialogue = Dialogue("Beto_Recibos", CashierChallenge.IntroId,
                "Tu inscripción cuesta $1,850. Busca en la mesa los recibos que sumen exactamente esa cantidad.",
                "Hay un recibo de cada cantidad: $300, $450, $600, $700, $850 y $1,000. Acércate a la mesa y presiona E o Enter para comenzar.");
            cashier.successDialogue = Dialogue("Beto_Pago_Listo", "rectoria.cashier.beto_success", "¡Todo en orden! Bienvenido a la UT");
            cashier.prerequisiteDialogue = Dialogue("Beto_Primero_Rectora", "rectoria.cashier.visit_rector_first", "Primero habla con la Rectora para comenzar tu inscripción. Después revisamos tus recibos.");
            cashier.talkFirstDialogue = Dialogue("Caja_Hablar_Primero", "rectoria.cashier.talk_to_beto_first", "Habla primero con Cajero Beto. Él te explicará el pago de inscripción.");
            var beto = interactor.points.Single(p => p.pointId == "beto");
            Undo.RecordObject(beto, "Conectar diálogo de Beto"); beto.dialogue = cashier.introDialogue; EditorUtility.SetDirty(beto);
            Undo.RecordObject(interactor, "Conectar mesa de recibos"); interactor.cashier = cashier; EditorUtility.SetDirty(interactor);
            var minimap = UnityEngine.Object.FindFirstObjectByType<RectoriaMinimap>();
            if (minimap != null) { Undo.RecordObject(minimap, "Ocultar minimapa durante Caja"); minimap.cashier = cashier; EditorUtility.SetDirty(minimap); }
            BuildView(cashier);
            EditorUtility.SetDirty(cashier); AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("No se pudo guardar la escena.");
        }

        private static DialogueDefinition Dialogue(string name, string id, params string[] lines)
        {
            string path = "Assets/Data/Dialogs/Rectoria/" + name + ".asset";
            var d = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(path);
            if (d != null) return d;
            d = ScriptableObject.CreateInstance<DialogueDefinition>(); d.dialogueId = id; d.defaultSpeaker = "Cajero Beto";
            d.defaultPortrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Characters/Rectoria/cajerobeto.png");
            d.lines = lines.Select(t => new DialogueLine { text = t }).ToArray(); AssetDatabase.CreateAsset(d, path); return d;
        }

        private static void BuildView(CashierChallenge c)
        {
            var go = new GameObject("Caja_Recibos_UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Undo.RegisterCreatedObjectUndo(go, "Crear interfaz de Caja"); go.transform.SetParent(c.transform, false);
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 86; canvas.pixelPerfect = true;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            var shade = Box(go.transform, "Modal", Vector2.zero, Vector2.zero, new Color32(8, 12, 20, 245));
            shade.anchorMin = Vector2.zero; shade.anchorMax = Vector2.one; c.panel = shade.gameObject;
            var frame = Box(shade, "Marco", Vector2.zero, new Vector2(1120, 650), Gold);
            var body = Box(frame, "Fondo", Vector2.zero, new Vector2(1112, 642), Dark);
            Text(body, "Titulo", "CAJA · PAGO DE INSCRIPCIÓN", new Vector2(0, 280), new Vector2(1050, 48), 28, Gold);
            c.statusLabel = Text(body, "Estado", "", new Vector2(0, 224), new Vector2(1030, 62), 20, Color.white);
            c.receiptFrames = new Image[6]; c.receiptLabels = new TMP_Text[6];
            for (int i = 0; i < 6; i++)
            {
                var card = Box(body, "Recibo_" + i, new Vector2(-280 + i % 3 * 280, 112 - i / 3 * 146), new Vector2(248, 122), Gold);
                c.receiptFrames[i] = card.GetComponent<Image>();
                var inside = Box(card, "Papel", Vector2.zero, new Vector2(242, 116), new Color32(42, 48, 61, 255));
                Box(inside, "Linea_Superior", new Vector2(0, 43), new Vector2(205, 2), Gold);
                Box(inside, "Linea_Inferior", new Vector2(0, -43), new Vector2(205, 2), Gold);
                c.receiptLabels[i] = Text(inside, "Importe", "", Vector2.zero, new Vector2(230, 83), 25, Color.white);
            }
            c.totalLabel = Text(body, "Total", "", new Vector2(-160, -132), new Vector2(570, 38), 25, Gold);
            c.differenceLabel = Text(body, "Diferencia", "", new Vector2(-160, -170), new Vector2(570, 30), 20, Gold);
            var confirm = Box(body, "Confirmar", new Vector2(140, -220), new Vector2(248, 48), Gold);
            c.confirmFrame = confirm.GetComponent<Image>();
            Box(confirm, "Fondo", Vector2.zero, new Vector2(242, 42), Dark);
            Text(confirm, "Etiqueta", "Confirmar", Vector2.zero, new Vector2(235, 40), 22, Gold);
            c.clearControl = Box(body, "Limpiar", new Vector2(-140, -220), new Vector2(248, 48), new Color32(64, 65, 70, 255));
            Text(c.clearControl, "Etiqueta", "R · Limpiar", Vector2.zero, new Vector2(235, 40), 22, Gold);
            c.controlsLabel = Text(body, "Controles", "", new Vector2(0, -280), new Vector2(1070, 64), 18, Gold);
            c.stamp = Box(body, "Sello_Pagado", new Vector2(300, -159), new Vector2(248, 70), Gold);
            c.stamp.localEulerAngles = new Vector3(0, 0, -9);
            c.stampVisibility = c.stamp.gameObject.AddComponent<CanvasGroup>(); c.stampVisibility.blocksRaycasts = false; c.stampVisibility.interactable = false;
            Box(c.stamp, "Fondo", Vector2.zero, new Vector2(240, 62), Dark);
            Text(c.stamp, "Texto", "PAGADO", Vector2.zero, new Vector2(230, 60), 33, Gold);
            c.stamp.gameObject.SetActive(false); shade.gameObject.SetActive(false);
        }

        private static RectTransform Box(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>(); r.sizeDelta = size; r.anchoredPosition = position;
            var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false; return r;
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
