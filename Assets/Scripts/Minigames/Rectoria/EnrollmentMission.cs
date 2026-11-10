using System;
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
        public const string SaveKey = "CampusQuest.Progress.v1";
        public const string BirthEvent = "rectoria.document.birth_certificate";
        public const string CardEvent = "rectoria.document.student_card";
        public const string PaymentEvent = "rectoria.document.payment_receipt";
#if UNITY_EDITOR
        public const string EditorTestStorageSetting = "CampusQuest.Progress.EditorTestKey";
#endif
        /// <summary>Production always uses SaveKey. Editor tests may use a separate, named test slot.</summary>
        public string ProgressStorageKey
        {
            get
            {
#if UNITY_EDITOR
                string testKey = UnityEditor.SessionState.GetString(EditorTestStorageSetting, "");
                if (testKey.StartsWith("CampusQuest.Test.", StringComparison.Ordinal)) return testKey;
#endif
                return SaveKey;
            }
        }
        public DialogueSession session;
        public GameObject hud;
        public TMP_Text objectiveLabel;
        public TMP_Text counterLabel;
        public TMP_Text[] documentLabels;
        private bool restoring;
        private static readonly string[] Names = { "Acta de nacimiento", "Credencial de estudiante", "Comprobante de pago" };

        [Serializable] private sealed class SaveData
        {
            public int version = 1;
            public string[] events;
        }

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

        private void Awake()
        {
            if (session == null) { Debug.LogError("EnrollmentMission requires a DialogueSession.", this); enabled = false; return; }
            RestoreProgress();
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
            if (!restoring) SaveProgress();
        }

        public void SaveProgress()
        {
            if (session == null) return;
            PlayerPrefs.SetString(ProgressStorageKey, JsonUtility.ToJson(new SaveData { events = session.CaptureCompletedIds() }));
            PlayerPrefs.Save();
        }

        public bool RestoreProgress()
        {
            if (session == null || session.IsOpen) return false;
            string[] events = Array.Empty<string>();
            if (PlayerPrefs.HasKey(ProgressStorageKey))
            {
                try
                {
                    var data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(ProgressStorageKey));
                    if (data == null || data.version != 1 || data.events == null)
                        throw new FormatException("Unsupported or incomplete progress save.");
                    events = data.events;
                }
                catch (Exception error)
                {
                    Debug.LogWarning("No se pudo restaurar el progreso de inscripción: " + error.Message, this);
                    return false;
                }
            }
            restoring = true;
            try { session.RestoreCompletedIds(events); }
            finally { restoring = false; }
            RefreshHud();
            return true;
        }

        /// <summary>Entry point for the future New Game screen. Leaves audio/video preferences intact.</summary>
        public void StartNewGame()
        {
            session.Abort();
            session.RestoreCompletedIds(Array.Empty<string>());
            SaveProgress();
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
