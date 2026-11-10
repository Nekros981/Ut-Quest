#if UNITY_EDITOR
using System;
using CampusQuest.Dialogue;
using CampusQuest.Rectoria;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CampusQuest.EditorTools
{
    public static class EnrollmentMissionTools
    {
        [MenuItem("Campus Quest/Rectoria/6 - Configurar misión de inscripción")]
        public static void Configure()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/Scenes/Rectoria_Tutorial_Arte_v01.unity" || scene.isDirty)
                throw new InvalidOperationException("Abre la escena de arte guardada y fuera de Play Mode.");
            var session = UnityEngine.Object.FindFirstObjectByType<DialogueSession>();
            if (session == null) throw new InvalidOperationException("Falta DialogueSession.");
            var mission = session.GetComponent<EnrollmentMission>();
            if (mission != null) return;
            mission = Undo.AddComponent<EnrollmentMission>(session.gameObject);
            mission.session = session;
            var root = new GameObject("Inscripcion_HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Undo.RegisterCreatedObjectUndo(root, "Crear HUD de inscripción");
            root.transform.SetParent(session.transform, false);
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 30; canvas.pixelPerfect = true;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            mission.hud = root;
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(root.transform, false);
            var rect = panel.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-24, -70); rect.sizeDelta = new Vector2(426, 190);
            panel.GetComponent<Image>().color = new Color32(233, 216, 166, 255); panel.GetComponent<Image>().raycastTarget = false;
            var bg = new GameObject("Fondo", typeof(RectTransform), typeof(Image)); bg.transform.SetParent(panel.transform, false);
            var br = bg.GetComponent<RectTransform>(); br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one; br.offsetMin = new Vector2(3, 3); br.offsetMax = new Vector2(-3, -3);
            bg.GetComponent<Image>().color = new Color32(27, 32, 45, 245); bg.GetComponent<Image>().raycastTarget = false;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/PixeloidSans SDF.asset");
            mission.objectiveLabel = Label(bg.transform, font, "Objetivo", 12, 40, 18);
            mission.counterLabel = Label(bg.transform, font, "Documentos", 55, 26, 22);
            mission.documentLabels = new TMP_Text[3];
            for (int i = 0; i < 3; i++) mission.documentLabels[i] = Label(bg.transform, font, "Documento_" + i, 89 + i * 28, 26, 18);
            mission.RefreshHud();
            EditorUtility.SetDirty(mission);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }

        private static TMP_Text Label(Transform parent, TMP_FontAsset font, string name, float top, float height, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f, 1);
            rect.offsetMin = new Vector2(15, -top - height); rect.offsetMax = new Vector2(-15, -top);
            var text = go.GetComponent<TMP_Text>(); text.font = font; text.fontSize = size; text.color = new Color32(233, 216, 166, 255); text.richText = false; text.raycastTarget = false;
            return text;
        }

        private static EnrollmentMission Live()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Solo disponible en Play Mode.");
            return UnityEngine.Object.FindFirstObjectByType<EnrollmentMission>();
        }
        [MenuItem("Campus Quest/Desarrollo/Inscripción/Obtener acta")]
        public static void Birth() { Live().TryAwardDocument(EnrollmentDocument.BirthCertificate); }
        [MenuItem("Campus Quest/Desarrollo/Inscripción/Obtener credencial")]
        public static void Card() { Live().TryAwardDocument(EnrollmentDocument.StudentCard); }
        [MenuItem("Campus Quest/Desarrollo/Inscripción/Obtener comprobante")]
        public static void Payment() { Live().TryAwardDocument(EnrollmentDocument.PaymentReceipt); }
        [MenuItem("Campus Quest/Desarrollo/Inscripción/Nueva partida")]
        public static void NewGame() { Live().StartNewGame(); }
        [MenuItem("Campus Quest/Desarrollo/Inscripción/Restaurar guardado")]
        public static void Restore() { Live().RestoreProgress(); }
    }
}
#endif
