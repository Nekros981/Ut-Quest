using CampusQuest.Dialogue;
using TMPro;
using UnityEngine;

namespace CampusQuest.Rectoria
{
    public enum EnrollmentDocument { BirthCertificate, StudentCard, PaymentReceipt }

    /// <summary>Mission state is derived entirely from the shared story-event registry.</summary>
    [DefaultExecutionOrder(-150)]
    public sealed class EnrollmentMission : MonoBehaviour
    {
        public const string SaveKey = GameProgress.SaveKey;
        public const string BirthEvent = "rectoria.document.birth_certificate";
        public const string CardEvent = "rectoria.document.student_card";
        public const string PaymentEvent = "rectoria.document.payment_receipt";
#if UNITY_EDITOR
        public const string EditorTestStorageSetting = GameProgress.EditorTestStorageSetting;
#endif
        /// <summary>Production always uses SaveKey. Editor tests may use a separate, named test slot.</summary>
        public string ProgressStorageKey => GameProgress.StorageKey;
        public DialogueSession session;
        public GameObject hud;
        public TMP_Text objectiveLabel;
        public TMP_Text counterLabel;
        public TMP_Text[] documentLabels;
        private static readonly string[] Names = { "Acta de nacimiento", "Credencial de estudiante", "Comprobante de pago" };

        public bool IsActive => session != null && session.HasCompleted(RectoriaDialogueInteractor.WelcomeId);
        public int DocumentCount => (HasDocument(EnrollmentDocument.BirthCertificate) ? 1 : 0)
            + (HasDocument(EnrollmentDocument.StudentCard) ? 1 : 0)
            + (HasDocument(EnrollmentDocument.PaymentReceipt) ? 1 : 0);
        public string Objective => session != null && session.HasCompleted(RockyGateReview.UnlockId)
            ? "Puerta de Bloques abierta"
            : DocumentCount == 3 ? "Dirígete a la Puerta de Bloques" : "Reúne tus documentos de inscripción";

        private static string EventId(EnrollmentDocument document)
        {
            switch (document)
            {
                case EnrollmentDocument.BirthCertificate: return BirthEvent;
                case EnrollmentDocument.StudentCard: return CardEvent;
                case EnrollmentDocument.PaymentReceipt: return PaymentEvent;
                default: return null;
            }
        }

        public bool HasDocument(EnrollmentDocument document) => IsActive && session.HasCompleted(EventId(document));

        /// <summary>Call only after a minigame succeeds; false means inactive, invalid or already awarded.</summary>
        public bool TryAwardDocument(EnrollmentDocument document)
        {
            return IsActive && EventId(document) != null && session.RegisterEvent(EventId(document));
        }

        public const string SceneCargaRectoria = "Carga_Rectoria";
        public const string SceneRectoriaPlay = "Rectoria_Tutorial_Arte_v01";

        public static bool IsRectoriaEntryScene(string sceneName)
        {
            return sceneName == SceneCargaRectoria || sceneName == SceneRectoriaPlay;
        }

        public static void ClearSavedProgress()
        {
            GameProgress.ResetLevel("rectoria");
        }

        private void Awake()
        {
            if (session == null) { Debug.LogError("EnrollmentMission requires a DialogueSession.", this); enabled = false; return; }
            GameProgress.EnsureLoaded();
        }

        private void OnEnable()
        {
            if (session == null) return;
            session.ProgressChanged += OnProgressChanged;
            RefreshHud();
        }

        private void OnDisable() { if (session != null) session.ProgressChanged -= OnProgressChanged; }

        private void OnProgressChanged()
        {
            RefreshHud();
        }

        public void SaveProgress()
        {
            GameProgress.Save();
        }

        public bool RestoreProgress()
        {
            if (session == null || session.IsOpen) return false;
            bool restored = GameProgress.Reload();
            RefreshHud();
            return restored;
        }

        /// <summary>Entry point for the future New Game screen. Leaves audio/video preferences intact.</summary>
        public void StartNewGame()
        {
            GameProgress.StartNewGame();
            RefreshHud();
        }

        public void RefreshHud()
        {
            if (hud != null) hud.SetActive(IsActive);
            if (objectiveLabel != null) objectiveLabel.text = Objective;
            if (counterLabel != null) counterLabel.text = "Documentos: " + DocumentCount + "/3";
            if (documentLabels == null) return;
            for (int i = 0; i < documentLabels.Length && i < Names.Length; i++)
            {
                if (documentLabels[i] == null) continue;
                bool obtained = HasDocument((EnrollmentDocument)i);
                documentLabels[i].text = (obtained ? "[X] " : "[ ] ") + Names[i];
                documentLabels[i].color = obtained ? new Color32(233, 216, 166, 255) : new Color32(193, 196, 205, 255);
            }
        }
    }
}
