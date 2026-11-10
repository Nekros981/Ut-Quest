#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CampusQuest.Dialogue;
using CampusQuest.Rectoria;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CampusQuest.EditorTools
{
    /// <summary>Explicit Editor-only regression suite; back up the existing save first.</summary>
    public static class CashierChallengeValidation
    {
        public static string Status { get; private set; } = "Not started";
        private static IEnumerator steps;
        private static int frame;
        private static readonly List<string> results = new List<string>();
        private static CashierChallenge cashier;
        private static RectoriaDialogueInteractor interactor;
        private static RectoriaPlayerTest player;
        private static RectoriaMinimap minimap;
        private static readonly int[] Amounts = { 300, 450, 600, 700, 850, 1000 };
        [Serializable]
        private sealed class ProgressBackup
        {
            public bool hadSave;
            public string progressJson;
        }

        public static void Begin()
        {
            if (!Application.isPlaying || steps != null) throw new InvalidOperationException("Requires Play Mode and no active suite.");
            cashier = UnityEngine.Object.FindFirstObjectByType<CashierChallenge>();
            interactor = UnityEngine.Object.FindFirstObjectByType<RectoriaDialogueInteractor>(); player = interactor.player;
            minimap = UnityEngine.Object.FindFirstObjectByType<RectoriaMinimap>();
            results.Clear(); Status = "Running"; frame = -1;
            // A session-only backup is lost if Unity closes. Keep a durable copy before testing.
            Directory.CreateDirectory("tmp/cashier-validation");
            string backupPath = "tmp/cashier-validation/progress-before-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff") + ".json";
            File.WriteAllText(backupPath, JsonUtility.ToJson(new ProgressBackup
            {
                hadSave = PlayerPrefs.HasKey(EnrollmentMission.SaveKey),
                progressJson = PlayerPrefs.GetString(EnrollmentMission.SaveKey, "")
            }, true));
            results.Add("BACKUP: " + backupPath);
            Application.runInBackground = true; EditorApplication.isPaused = false;
            steps = Validate(); EditorApplication.update += Tick;
        }
        private static void Tick()
        {
            if (!Application.isPlaying) { Finish("FAILED: Play Mode ended"); return; }
            if (frame == Time.frameCount) return; frame = Time.frameCount;
            try { if (!steps.MoveNext()) Finish("PASS"); }
            catch (Exception error) { Finish("FAILED: " + error); }
        }
        private static void Finish(string status)
        {
            EditorApplication.update -= Tick; steps = null; Status = status;
            if (Keyboard.current != null) InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            Directory.CreateDirectory("tmp/cashier-validation");
            File.WriteAllText("tmp/cashier-validation/results.txt", string.Join("\n", results) + "\n" + status);
        }
        private static void Check(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message); results.Add("PASS: " + message);
        }
        private static RectoriaScenePoint Face(string id)
        {
            var p = interactor.points.Single(x => x.pointId == id);
            player.transform.position = player.map.CellCenter(p.approachCell);
            var d = p.cell - p.approachCell; player.facing = new Vector2Int(d.x, d.y); return p;
        }
        private static IEnumerable<object> Press(params Key[] keys)
        {
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(keys)); yield return null; yield return null;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); yield return null; yield return null;
        }
        private static IEnumerable<object> Click(RectTransform target)
        {
            if (Mouse.current == null) throw new InvalidOperationException("Mouse required for UI validation.");
            var point = RectTransformUtility.WorldToScreenPoint(null, target.position);
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = point }.WithButton(MouseButton.Left));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = point }); yield return null; yield return null;
        }
        private static IEnumerable<object> Dialogue()
        {
            int safety = 0;
            while (cashier.session.IsOpen)
            {
                yield return null; Check(cashier.session.Advance(), "Dialogue advances normally");
                if (++safety > 20) throw new InvalidOperationException("Dialogue stuck");
            }
            yield return null;
        }

        private static IEnumerator Validate()
        {
            Check(Enumerable.Range(0, 6).Select(ReceiptSelection.AmountAt).SequenceEqual(Amounts), "Exactly six original unique receipt amounts");
            var model = new ReceiptSelection(); int correct = 0;
            for (int mask = 0; mask < 64; mask++)
            {
                model.Clear(); int expected = 0;
                for (int i = 0; i < 6; i++) if ((mask & (1 << i)) != 0) { model.Toggle(i); expected += Amounts[i]; }
                Check(model.Total == expected && model.IsExact == (expected == 1850), "Receipt model subset " + mask + " totals " + expected);
                if (model.IsExact) correct++;
            }
            Check(correct == 2, "All 64 subsets contain exactly two valid combinations");
            model.Clear(); model.Toggle(0); model.Toggle(0);
            Check(model.Total == 0 && !model.Toggle(-1) && !model.Toggle(6), "Receipts deselect and invalid indices are rejected");

            cashier.mission.StartNewGame(); yield return null;
            Check(!cashier.TryOpen(), "Caja locked before enrollment");
            Face("beto"); player.facing = -player.facing; Check(!interactor.TryInteract(), "Beto requires facing");
            Face("beto"); foreach (var step in Press(Key.E)) yield return step;
            Check(cashier.session.CurrentDialogueId == cashier.prerequisiteDialogue.dialogueId, "Beto sends player to Rectora before enrollment");
            foreach (var step in Dialogue()) yield return step;
            Face("rectora"); Check(interactor.TryInteract(), "Existing welcome starts");
            foreach (var step in Dialogue()) yield return step;
            Check(cashier.mission.IsActive, "Real Rectora welcome activates enrollment");
            Face("recibos"); foreach (var step in Press(Key.Enter)) yield return step;
            Check(cashier.session.CurrentDialogueId == cashier.talkFirstDialogue.dialogueId && !cashier.IsOpen, "Table requires completed Beto introduction");
            foreach (var step in Dialogue()) yield return step;
            Face("beto"); foreach (var step in Press(Key.Enter)) yield return step;
            Check(cashier.session.CurrentDialogueId == CashierChallenge.IntroId
                && cashier.session.view.bodyLabel.text.Contains("$1,850"), "Beto explains the exact enrollment price");
            cashier.session.Abort();
            Check(!cashier.session.HasCompleted(CashierChallenge.IntroId) && !cashier.CanStart, "Aborted Beto dialogue does not unlock puzzle");
            yield return null; Check(interactor.TryInteract(), "Beto can restart after abort");
            foreach (var step in Dialogue()) yield return step;
            Check(cashier.CanStart && !cashier.mission.HasDocument(EnrollmentDocument.BirthCertificate)
                && !cashier.mission.HasDocument(EnrollmentDocument.StudentCard), "Caja unlocks without either other document");
            Face("recibos"); player.facing = -player.facing; Check(!interactor.TryInteract(), "Table requires adjacent facing");
            Face("recibos"); player.transform.position += Vector3.right * .1f;
            Check(!interactor.TryInteract(), "Cannot use table halfway through a step");
            Face("recibos"); foreach (var step in Press(Key.E)) yield return step;
            Check(cashier.IsOpen && !cashier.IsSolved && cashier.Selection.Total == 0, "E at table opens empty receipt selection");
            Check(!player.enabled && minimap.visibility.alpha == 0 && interactor.AvailableSpeaker() == null, "Caja suspends world controls and hides minimap");
            Vector3 position = player.transform.position;
            foreach (var step in Press(Key.LeftShift, Key.D)) yield return step;
            Check(player.transform.position == position, "Shift movement cannot move Mateo while Caja is open");
            Check(cashier.differenceLabel.text == "Faltan: $1,850", "Empty selection reports full missing amount");
            foreach (var step in Press(Key.E)) yield return step;
            Check(cashier.Selection.IsSelected(0) && cashier.Selection.Total == 300, "E selects first receipt once");
            foreach (var step in Press(Key.Enter)) yield return step;
            Check(!cashier.Selection.IsSelected(0) && cashier.Selection.Total == 0, "Enter deselects the same receipt");
            foreach (var step in Press(Key.Space)) yield return step;
            Check(cashier.Selection.Total == 300, "Space also toggles selected receipt");
            foreach (var step in Press(Key.RightArrow)) yield return step;
            Check(cashier.SelectedIndex == 1, "Right arrow chooses second receipt");
            foreach (var step in Press(Key.DownArrow)) yield return step;
            Check(cashier.SelectedIndex == 4, "Down arrow chooses next row");
            foreach (var step in Press(Key.DownArrow)) yield return step;
            Check(cashier.SelectedIndex == 6, "Down from last row focuses Confirmar");
            foreach (var step in Press(Key.Enter)) yield return step;
            Check(!cashier.IsSolved && cashier.Selection.Total == 300 && cashier.statusLabel.text.Contains("no coincide")
                && cashier.mission.DocumentCount == 0, "Incorrect confirm keeps selection, shows error and grants no document");
            foreach (var step in Press(Key.R)) yield return step;
            Check(cashier.Selection.Total == 0, "R clears selection without penalty");
            for (int i = 0; i < 6; i++) cashier.ToggleReceipt(i);
            Check(cashier.Selection.Total == 3900 && cashier.differenceLabel.text == "Te excedes por: $2,050", "Overpayment shows exact excess");
            Check(!cashier.ConfirmPayment() && cashier.Selection.Total == 3900, "Overpayment rejected without resetting choices");
            foreach (var step in Click(cashier.clearControl)) yield return step;
            Check(cashier.Selection.Total == 0, "Mouse clear control empties selection");
            foreach (var step in Click(cashier.receiptFrames[4].rectTransform)) yield return step;
            Check(cashier.Selection.Total == 850, "Mouse selects receipt");
            foreach (var step in Click(cashier.receiptFrames[4].rectTransform)) yield return step;
            Check(cashier.Selection.Total == 0, "Mouse deselects receipt");
            foreach (var step in Click(cashier.receiptFrames[4].rectTransform)) yield return step;
            foreach (var step in Click(cashier.receiptFrames[5].rectTransform)) yield return step;
            Check(cashier.Selection.Total == 1850 && !cashier.IsSolved && cashier.mission.DocumentCount == 0,
                "Exact total waits for explicit confirmation");
            yield return null; Capture("selection.png");
            foreach (var step in Click(cashier.confirmFrame.rectTransform)) yield return step;
            Check(cashier.IsSolved && cashier.RewardGrantedThisRun && cashier.stamp.gameObject.activeSelf, "850 + 1000 grants first payment receipt and stamp");
            float stampEnd = Time.realtimeSinceStartup + .4f;
            while (Time.realtimeSinceStartup < stampEnd) yield return null;
            Check(cashier.stampVisibility.alpha == 1 && Vector3.Distance(cashier.stamp.localScale, Vector3.one) < .001f, "Stamp finishes its short animation");
            Check(cashier.mission.DocumentCount == 1 && cashier.mission.counterLabel.text == "Documentos: 1/3"
                && cashier.session.HasCompleted(EnrollmentMission.PaymentEvent), "Payment is first document; HUD and persistent registry update");
            Check(!cashier.ConfirmPayment() && !cashier.ToggleReceipt(0), "Solved screen cannot grant again or alter selected receipts");
            Capture("success-stamp.png");
            foreach (var step in Press(Key.Enter)) yield return step;
            Check(cashier.session.IsOpen && cashier.session.view.bodyLabel.text == "¡Todo en orden! Bienvenido a la UT", "Beto delivers exact success line through shared dialogue");
            foreach (var step in Dialogue()) yield return step;
            Check(player.enabled && minimap.visibility.alpha == 1, "Success dialogue restores world controls and minimap");
            Face("recibos"); foreach (var step in Press(Key.Enter)) yield return step;
            Check(cashier.IsOpen && cashier.Selection.Total == 0, "Replay is available from normal table interaction");
            cashier.ToggleReceipt(0); cashier.ToggleReceipt(3); cashier.ToggleReceipt(4);
            foreach (var step in Press(Key.C)) yield return step;
            Check(cashier.IsSolved && !cashier.RewardGrantedThisRun && !cashier.stamp.gameObject.activeSelf
                && cashier.mission.DocumentCount == 1, "300 + 700 + 850 succeeds on replay without duplicate reward or stamp");
            cashier.Close(false); yield return null;

            correct = 0;
            for (int mask = 0; mask < 64; mask++)
            {
                Check(cashier.TryOpen(), "Component subset " + mask + " opens");
                int expected = 0;
                for (int i = 0; i < 6; i++) if ((mask & (1 << i)) != 0) { cashier.ToggleReceipt(i); expected += Amounts[i]; }
                bool result = cashier.ConfirmPayment();
                Check(result == (expected == 1850) && cashier.Selection.Total == expected, "Component subset " + mask + " exact-sum result");
                if (result) correct++;
                Check(!cashier.RewardGrantedThisRun && !cashier.stamp.gameObject.activeSelf && cashier.mission.DocumentCount == 1,
                    "Subset " + mask + " cannot duplicate reward or animation");
                cashier.Close(false); yield return null;
            }
            Check(correct == 2 && cashier.session.CaptureCompletedIds().Count(id => id == EnrollmentMission.PaymentEvent) == 1,
                "Exactly two winning subsets and one persistent payment event");
            Check(cashier.mission.RestoreProgress() && cashier.CanStart && cashier.mission.DocumentCount == 1, "Restore retains Beto introduction and payment document");
            Check(cashier.TryOpen(), "Can reopen after restore"); yield return null;
            foreach (var step in Press(Key.Escape)) yield return step;
            Check(!cashier.IsOpen && player.enabled && minimap.visibility.alpha == 1, "Escape closes puzzle and restores minimap");
            var completed = cashier.session.CaptureCompletedIds();
            Check(cashier.TryOpen(), "Open before new-game reset"); cashier.mission.StartNewGame(); yield return null;
            Check(!cashier.IsOpen && !cashier.CanStart && player.enabled && !cashier.mission.IsActive && minimap.visibility.alpha == 1,
                "New Game during Caja clears progress and releases controls");
            cashier.session.RestoreCompletedIds(completed);
            cashier.mission.TryAwardDocument(EnrollmentDocument.BirthCertificate);
            cashier.mission.TryAwardDocument(EnrollmentDocument.StudentCard);
            Check(cashier.mission.DocumentCount == 3 && cashier.mission.objectiveLabel.text == "Dirígete a la Puerta de Bloques",
                "All three documents switch existing objective to Blocks gate");
            cashier.session.RestoreCompletedIds(completed);
            Check(!EditorUtility.scriptCompilationFailed, "No compilation errors");
        }

        private static void Capture(string name)
        {
            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            var view = Resources.FindObjectsOfTypeAll(type).FirstOrDefault();
            var field = type.GetField("m_RenderTexture", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var rt = view == null ? null : field?.GetValue(view) as RenderTexture; if (rt == null) return;
            var previous = RenderTexture.active; var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = rt; texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); texture.Apply();
                if (SystemInfo.graphicsUVStartsAtTop)
                {
                    var pixels = texture.GetPixels32(); var flipped = new Color32[pixels.Length];
                    for (int y = 0; y < rt.height; y++) Array.Copy(pixels, y * rt.width, flipped, (rt.height - 1 - y) * rt.width, rt.width);
                    texture.SetPixels32(flipped); texture.Apply();
                }
                Directory.CreateDirectory("tmp/cashier-validation"); File.WriteAllBytes("tmp/cashier-validation/" + name, texture.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(texture); }
        }
    }
}
#endif
