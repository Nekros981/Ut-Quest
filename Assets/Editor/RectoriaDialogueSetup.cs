#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using CampusQuest.Dialogue;
using CampusQuest.Rectoria;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CampusQuest.EditorTools
{
    public static class RectoriaDialogueSetup
    {
        public const string WelcomePath = "Assets/Data/Dialogs/Rectoria/Rectora_Bienvenida.asset";
        private static readonly Color Gold = new Color32(233, 216, 166, 255);
        private static readonly Color Dark = new Color32(27, 32, 45, 255);
        private static TMP_FontAsset font;

        [MenuItem("Campus Quest/Rectoria/5 - Conectar dialogo inicial de la Rectora")]
        public static void Configure()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != RectoriaSceneStageSetup.ScenePath || scene.isDirty)
                throw new InvalidOperationException("Abre y guarda la escena de arte de Rectoría, fuera de Play Mode.");
            var stage = GameObject.Find("Rectoria_Escenario");
            var player = UnityEngine.Object.FindFirstObjectByType<RectoriaPlayerTest>();
            var guide = UnityEngine.Object.FindFirstObjectByType<RectoriaSceneGuide>();
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/PixeloidSans SDF.asset");
            if (stage == null || player == null || guide == null || font == null)
                throw new InvalidOperationException("Faltan el escenario, el jugador, la guía o Pixeloid Sans.");
            var rector = stage.GetComponentsInChildren<RectoriaScenePoint>().Single(p => p.pointId == "rectora");

            var definition = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(WelcomePath);
            if (definition == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(WelcomePath));
                definition = ScriptableObject.CreateInstance<DialogueDefinition>();
                definition.dialogueId = RectoriaDialogueInteractor.WelcomeId;
                definition.defaultSpeaker = "Rectora";
                // Verbatim spoken lines, GUION RECTORIA.pdf, page 5; stage directions are excluded.
                definition.lines = new[]
                {
                    new DialogueLine { text = "¡Bienvenido a la Universidad Tecnológica! Qué gusto verte por aquí." },
                    new DialogueLine { text = "Antes de entrar a tu carrera necesito que completes tu inscripción." },
                    new DialogueLine { text = "Debes reunir 3 documentos: tu acta de nacimiento, tu credencial de estudiante y tu comprobante de pago." },
                    new DialogueLine { text = "El Archivo está a la izquierda, Credencialización a la derecha y la Caja al fondo." },
                    new DialogueLine { text = "Cuando los tengas todos, el guardia te dejará pasar a los bloques. ¡Mucha suerte!" }
                };
                AssetDatabase.CreateAsset(definition, WelcomePath);
            }
            if (!definition.IsValid) throw new InvalidOperationException("El diálogo no es válido.");

            Undo.SetCurrentGroupName("Conectar diálogo de Rectoría");
            int group = Undo.GetCurrentGroup();
            Undo.RecordObject(rector, "Asignar bienvenida de Rectora");
            rector.dialogue = definition;
            var root = Child(stage.transform, "Dialogos");
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Configurar diálogo compartido");
            var session = Get<DialogueSession>(root);
            var context = Get<DialoguePlayerContext>(root);
            var view = Get<DialogueView>(root);
            var interactor = Get<RectoriaDialogueInteractor>(root);
            session.playerContext = context; session.view = view;
            session.suspendWhileOpen = new Behaviour[] { player, guide };
            session.hideWhileOpen = new[] { guide.contextLabel.gameObject };
            interactor.player = player; interactor.session = session; interactor.points = guide.points;
            interactor.inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/Settings/InputSystem_Actions.inputactions");
            BuildView(root, view);
            foreach (var component in new UnityEngine.Object[] { rector, session, context, view, interactor }) EditorUtility.SetDirty(component);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("No se guardó la escena.");
            Undo.CollapseUndoOperations(group);
        }

        private static void BuildView(Transform parent, DialogueView view)
        {
            var root = Child(parent, "Dialogo_UI", true);
            var canvas = Get<Canvas>(root); root = canvas.transform;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 50; canvas.pixelPerfect = true;
            var scaler = Get<CanvasScaler>(root);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;

            var frame = Child(root, "Caja_De_Dialogo", true);
            var frameRect = (RectTransform)frame;
            frameRect.anchorMin = new Vector2(0, 0); frameRect.anchorMax = new Vector2(1, 0);
            frameRect.pivot = new Vector2(.5f, 0); frameRect.sizeDelta = new Vector2(-48, 232); frameRect.anchoredPosition = new Vector2(0, 16);
            var frameImage = Get<Image>(frame); frameImage.color = Gold;
            view.panel = Get<CanvasGroup>(frame);
            var inside = Child(frame, "Fondo", true); Stretch((RectTransform)inside, 5, 5, 5, 5); Get<Image>(inside).color = Dark;

            var portraitFrame = Child(frame, "Retrato_Marco", true);
            Fixed((RectTransform)portraitFrame, new Vector2(24, 40), new Vector2(134, 158));
            Get<Image>(portraitFrame).color = Gold;
            var portraitArea = Child(portraitFrame, "Retrato_Fondo", true); Stretch((RectTransform)portraitArea, 3, 3, 3, 3);
            Get<Image>(portraitArea).color = new Color32(42, 44, 56, 255);
            var portraitImage = Child(portraitArea, "Imagen", true); Stretch((RectTransform)portraitImage, 6, 6, 6, 6);
            view.portraitImage = Get<Image>(portraitImage); view.portraitImage.preserveAspect = true; view.portraitImage.raycastTarget = false;
            view.portraitPlaceholder = Label(portraitArea, "Retrato_Provisional", 17, Gold, TextAlignmentOptions.Center);
            Stretch(view.portraitPlaceholder.rectTransform, 6, 6, 6, 6);

            view.speakerLabel = Label(frame, "Nombre_NPC", 28, Gold, TextAlignmentOptions.Left);
            Stretch(view.speakerLabel.rectTransform, 184, 30, 174, 24);
            view.bodyLabel = Label(frame, "Texto", 26, Color.white, TextAlignmentOptions.TopLeft);
            Stretch(view.bodyLabel.rectTransform, 184, 30, 60, 66);
            view.bodyLabel.textWrappingMode = TextWrappingModes.Normal;
            view.bodyLabel.enableAutoSizing = true; view.bodyLabel.fontSizeMin = 18; view.bodyLabel.fontSizeMax = 26;
            var help = Label(frame, "Avanzar", 19, Gold, TextAlignmentOptions.BottomLeft);
            Stretch(help.rectTransform, 184, 160, 22, 174); help.text = "E / Enter · Continuar";
            view.pageLabel = Label(frame, "Pagina", 19, Gold, TextAlignmentOptions.BottomRight);
            Stretch(view.pageLabel.rectTransform, 184, 30, 22, 174);

            var prompt = Child(root, "Aviso_Interaccion", true);
            var rect = (RectTransform)prompt; rect.anchorMin = new Vector2(.5f, 0); rect.anchorMax = new Vector2(.5f, 0);
            rect.pivot = new Vector2(.5f, 0); rect.anchoredPosition = new Vector2(0, 80); rect.sizeDelta = new Vector2(480, 44);
            Get<Image>(prompt).color = Dark;
            view.interactionHint = Label(prompt, "Texto", 21, Gold, TextAlignmentOptions.Center);
            Stretch(view.interactionHint.rectTransform, 10, 10, 3, 3); view.promptRoot = prompt.gameObject;
            view.SetVisible(false); view.SetPrompt(string.Empty);
            view.ShowLine("Rectora", string.Empty, null, 1, 5);
        }

        private static Transform Child(Transform parent, string name, bool rect = false)
        {
            var found = parent.Find(name); if (found != null) return found;
            var go = rect ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Crear " + name); go.transform.SetParent(parent, false); return go.transform;
        }

        private static T Get<T>(Transform root) where T : Component
        {
            var result = root.GetComponent<T>(); return result != null ? result : Undo.AddComponent<T>(root.gameObject);
        }

        private static TextMeshProUGUI Label(Transform parent, string name, float size, Color color, TextAlignmentOptions alignment)
        {
            var root = Child(parent, name, true); var label = Get<TextMeshProUGUI>(root);
            label.font = font; label.fontSize = size; label.color = color; label.alignment = alignment;
            label.raycastTarget = false; label.richText = false; label.textWrappingMode = TextWrappingModes.NoWrap;
            return label;
        }

        private static void Fixed(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = Vector2.zero; rect.pivot = Vector2.zero;
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }

        // Bottom and top margins, respectively; all boxes are anchored to the parent rectangle.
        private static void Stretch(RectTransform rect, float left, float right, float bottom, float top)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(-right, -top);
        }
    }
}
#endif
