using System.Collections.Generic;
using CampusQuest.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CampusQuest.Rectoria
{
    [DefaultExecutionOrder(-80)]
    public sealed class CashierChallenge : MonoBehaviour
    {
        public const string IntroId = "rectoria.cashier.beto_intro";
        public DialogueSession session;
        public EnrollmentMission mission;
        public ArchiveChallenge archive;
        public CredentialChallenge credential;
        public DialogueDefinition introDialogue;
        public DialogueDefinition prerequisiteDialogue;
        public DialogueDefinition talkFirstDialogue;
        public DialogueDefinition successDialogue;
        public GameObject panel;
        public TMP_Text statusLabel;
        public TMP_Text totalLabel;
        public TMP_Text differenceLabel;
        public TMP_Text controlsLabel;
        public TMP_Text[] receiptLabels;
        public Image[] receiptFrames;
        public Image confirmFrame;
        public RectTransform clearControl;
        public RectTransform stamp;
        public CanvasGroup stampVisibility;
        public bool IsOpen { get; private set; }
        public bool IsSolved { get; private set; }
        public bool RewardGrantedThisRun { get; private set; }
        public int SelectedIndex { get; private set; }
        public ReceiptSelection Selection { get; private set; } = new ReceiptSelection();
        public bool CanStart => mission != null && mission.IsActive && session.HasCompleted(IntroId);
        public bool ConsumedInputThisFrame => lastInputFrame == Time.frameCount;
        private int lastInputFrame = -1;
        private float stampSeconds;
        private readonly Dictionary<Behaviour, bool> behaviours = new Dictionary<Behaviour, bool>();
        private readonly Dictionary<GameObject, bool> objects = new Dictionary<GameObject, bool>();
        private static readonly Color Gold = new Color32(233, 216, 166, 255);
        private static readonly Color Muted = new Color32(67, 73, 91, 255);

        private void Awake() { panel.SetActive(false); stamp.gameObject.SetActive(false); }
        private void OnEnable() { if (session != null) session.ProgressChanged += OnProgressChanged; }
        private void OnDisable() { if (session != null) session.ProgressChanged -= OnProgressChanged; Close(false); }
        private void OnProgressChanged() { if (IsOpen && !CanStart) Close(false); }
        public DialogueDefinition DialogueForBeto() => mission != null && mission.IsActive ? introDialogue : prerequisiteDialogue;
        public DialogueDefinition DialogueForTable() => mission != null && mission.IsActive ? talkFirstDialogue : prerequisiteDialogue;

        public bool TryOpen()
        {
            if (!isActiveAndEnabled || IsOpen || !CanStart || DialogueSession.IsAnyOpen
                || (archive != null && archive.IsOpen) || (credential != null && credential.IsBusy)) return false;
            IsOpen = true; IsSolved = false; RewardGrantedThisRun = false; SelectedIndex = 0;
            lastInputFrame = Time.frameCount; Selection.Clear(); stampSeconds = 0;
            behaviours.Clear(); objects.Clear();
            foreach (var behaviour in session.suspendWhileOpen)
            {
                if (behaviour == null || behaviour == this || behaviour == session || behaviours.ContainsKey(behaviour)) continue;
                behaviours.Add(behaviour, behaviour.enabled); behaviour.enabled = false;
            }
            foreach (var go in session.hideWhileOpen)
            {
                if (go == null || go == gameObject || objects.ContainsKey(go)) continue;
                objects.Add(go, go.activeSelf); go.SetActive(false);
            }
            session.view.SetPrompt(string.Empty); panel.SetActive(true); stamp.gameObject.SetActive(false);
            Refresh("Elige los recibos que sumen exactamente $1,850 y confirma el pago.");
            return true;
        }

        public bool ToggleReceipt(int index)
        {
            if (!IsOpen || IsSolved || !Selection.Toggle(index)) return false;
            Refresh("Puedes seleccionar o quitar cada recibo. Confirma cuando el total sea exacto."); return true;
        }
        public void ClearSelection()
        {
            if (!IsOpen || IsSolved) return;
            Selection.Clear(); Refresh("Selección vacía. Puedes volver a elegir tus recibos.");
        }
        public bool ConfirmPayment()
        {
            if (!IsOpen || IsSolved || !CanStart) return false;
            if (!Selection.IsExact)
            {
                Refresh("El total no coincide con $1,850. Ajusta los recibos y vuelve a confirmar."); return false;
            }
            IsSolved = true;
            RewardGrantedThisRun = mission.TryAwardDocument(EnrollmentDocument.PaymentReceipt);
            stampSeconds = 0; stamp.gameObject.SetActive(RewardGrantedThisRun);
            stampVisibility.alpha = 0; stamp.localScale = Vector3.one * 1.4f;
            Refresh("Cajero Beto: ¡Todo en orden! Bienvenido a la UT");
            return true;
        }

        public bool MoveSelection(Vector2Int direction)
        {
            if (!IsOpen || IsSolved || Mathf.Abs(direction.x) + Mathf.Abs(direction.y) != 1) return false;
            if (SelectedIndex == ReceiptSelection.Count)
            {
                if (direction.y > 0) SelectedIndex = 3;
            }
            else if (direction.y < 0 && SelectedIndex >= 3) SelectedIndex = ReceiptSelection.Count;
            else SelectedIndex = Mathf.Clamp(SelectedIndex / 3 - direction.y, 0, 1) * 3 + Mathf.Clamp(SelectedIndex % 3 + direction.x, 0, 2);
            Refresh(null); return true;
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (IsSolved && RewardGrantedThisRun)
            {
                stampSeconds += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(stampSeconds / .28f);
                stamp.localScale = Vector3.one * Mathf.Lerp(1.4f, 1, t);
                stamp.anchoredPosition = new Vector2(300, -159 + (1 - t) * 26);
                stampVisibility.alpha = Mathf.Clamp01(t * 3);
            }
            if (ConsumedInputThisFrame) return;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.escapeKey.wasPressedThisFrame) { Close(IsSolved); return; }
                if (DialogueSession.ConfirmPressedThisFrame())
                {
                    lastInputFrame = Time.frameCount;
                    if (IsSolved) Close(true);
                    else if (SelectedIndex == ReceiptSelection.Count) ConfirmPayment(); else ToggleReceipt(SelectedIndex);
                    return;
                }
                if (!IsSolved)
                {
                    if (keyboard.cKey.wasPressedThisFrame) { ConfirmPayment(); return; }
                    if (keyboard.spaceKey.wasPressedThisFrame && SelectedIndex < ReceiptSelection.Count) { ToggleReceipt(SelectedIndex); return; }
                    if (keyboard.rKey.wasPressedThisFrame) ClearSelection();
                    else if (keyboard.tabKey.wasPressedThisFrame) { SelectedIndex = (SelectedIndex + 1) % 7; Refresh(null); }
                    else if (keyboard.leftArrowKey.wasPressedThisFrame) MoveSelection(Vector2Int.left);
                    else if (keyboard.rightArrowKey.wasPressedThisFrame) MoveSelection(Vector2Int.right);
                    else if (keyboard.upArrowKey.wasPressedThisFrame) MoveSelection(Vector2Int.up);
                    else if (keyboard.downArrowKey.wasPressedThisFrame) MoveSelection(Vector2Int.down);
                }
            }
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            Vector2 position = mouse.position.ReadValue();
            if (RectTransformUtility.RectangleContainsScreenPoint(confirmFrame.rectTransform, position))
            { if (IsSolved) Close(true); else { SelectedIndex = ReceiptSelection.Count; ConfirmPayment(); } return; }
            if (IsSolved) return;
            if (RectTransformUtility.RectangleContainsScreenPoint(clearControl, position)) { ClearSelection(); return; }
            for (int i = 0; i < ReceiptSelection.Count; i++)
                if (RectTransformUtility.RectangleContainsScreenPoint(receiptFrames[i].rectTransform, position))
                { SelectedIndex = i; ToggleReceipt(i); return; }
        }

        private void Refresh(string message)
        {
            if (message != null) statusLabel.text = message;
            totalLabel.text = "Total: $" + Selection.Total.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " / $1,850";
            int difference = ReceiptSelection.Target - Selection.Total;
            differenceLabel.text = difference == 0 ? "Importe exacto" : difference > 0
                ? "Faltan: $" + difference.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)
                : "Te excedes por: $" + (-difference).ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            differenceLabel.color = difference < 0 ? new Color32(231, 148, 129, 255) : Gold;
            controlsLabel.text = IsSolved ? "E / Enter o clic en Continuar · Hablar con Beto"
                : "Flechas / Tab · Elegir   E / Enter / Espacio o clic · Marcar\nC o Confirmar · Revisar pago   R / Limpiar · Vaciar   Esc · Volver";
            for (int i = 0; i < ReceiptSelection.Count; i++)
            {
                receiptLabels[i].text = (Selection.IsSelected(i) ? "[X]" : "[ ]") + " RECIBO\n$" + ReceiptSelection.AmountAt(i).ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
                receiptLabels[i].color = Selection.IsSelected(i) ? Gold : new Color32(228, 225, 211, 255);
                receiptFrames[i].color = SelectedIndex == i ? Gold : Muted;
            }
            confirmFrame.color = SelectedIndex == ReceiptSelection.Count || IsSolved ? Gold : Muted;
            confirmFrame.GetComponentInChildren<TMP_Text>().text = IsSolved ? "Continuar" : "Confirmar";
        }

        public void Close(bool speak)
        {
            if (!IsOpen) return;
            bool solved = IsSolved; IsOpen = false; lastInputFrame = Time.frameCount;
            panel.SetActive(false); stamp.gameObject.SetActive(false);
            foreach (var state in behaviours) if (state.Key != null) state.Key.enabled = state.Value;
            foreach (var state in objects) if (state.Key != null) state.Key.SetActive(state.Value);
            behaviours.Clear(); objects.Clear();
            if (speak && solved && successDialogue != null) session.TryBegin(successDialogue);
        }
    }
}
