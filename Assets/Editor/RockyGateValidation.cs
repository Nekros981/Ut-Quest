#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CampusQuest.Dialogue;
using CampusQuest.Rectoria;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Tilemaps;

namespace CampusQuest.EditorTools
{
    /// <summary>Isolated test slot, durable current-profile backup, and read-only integrity checks.</summary>
    public static class RockyGateValidation
    {
        [Serializable] private sealed class Context
        {
            public bool hadSave;
            public string progressJson;
            public string sha256;
            public string testKey;
        }
        private static string DirectoryPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../tmp/rocky-validation"));
        private static string ContextPath => Path.Combine(DirectoryPath, "test-context.json");
        private static Context context;
        public static string Status { get; private set; } = "Not started";
        private static IEnumerator steps;
        private static int lastFrame;
        private static string report;
        private static readonly List<string> results = new List<string>();
        private static RockyGateReview gate;
        private static RectoriaDialogueInteractor interactor;
        private static RectoriaPlayerTest player;
        private static TilemapCollider2D collider;
        private static Keyboard keyboard;
        private static int completions;
        private static readonly List<string> signals = new List<string>();

        private static string Hash(bool exists, string json)
        {
            using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(exists + "\n" + json))).Replace("-", "");
        }
        private static Context ReadContext() => JsonUtility.FromJson<Context>(File.ReadAllText(ContextPath));
        public static string Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Prepare before Play Mode.");
            Directory.CreateDirectory(DirectoryPath);
            bool exists = PlayerPrefs.HasKey(EnrollmentMission.SaveKey);
            string json = PlayerPrefs.GetString(EnrollmentMission.SaveKey, "");
            string checkpoint = Path.Combine(DirectoryPath, "current-progress-preserved.json");
            if (File.Exists(checkpoint) && File.ReadAllText(checkpoint) != json)
                throw new InvalidOperationException("Current profile differs from the first backup; refusing to overwrite either.");
            if (File.Exists(ContextPath))
            {
                context = ReadContext(); VerifyCurrentProfile();
            }
            else
            {
                context = new Context { hadSave = exists, progressJson = json, sha256 = Hash(exists, json), testKey = "CampusQuest.Test.Rocky." + Guid.NewGuid().ToString("N") };
                File.WriteAllText(ContextPath, JsonUtility.ToJson(context, true));
            }
            var disk = ReadContext();
            if (disk.sha256 != Hash(disk.hadSave, disk.progressJson) || disk.progressJson != json || disk.hadSave != exists)
                throw new InvalidOperationException("Durable backup read-back validation failed.");
            SessionState.SetString(EnrollmentMission.EditorTestStorageSetting, context.testKey);
            return "Durable backup verified; isolated key=" + context.testKey + "; current profile SHA256=" + context.sha256;
        }
        public static string VerifyCurrentProfile()
        {
            var data = context ?? ReadContext();
            bool exists = PlayerPrefs.HasKey(EnrollmentMission.SaveKey);
            string json = PlayerPrefs.GetString(EnrollmentMission.SaveKey, "");
            if (data.hadSave != exists || data.progressJson != json || data.sha256 != Hash(exists, json))
                throw new InvalidOperationException("Current user profile changed. No automatic overwrite is allowed.");
            return "Current user profile is byte-for-byte unchanged: " + data.sha256;
        }
        private static void Guard()
        {
            if (gate.mission.ProgressStorageKey != context.testKey || !context.testKey.StartsWith("CampusQuest.Test.Rocky.", StringComparison.Ordinal)
                || context.testKey == EnrollmentMission.SaveKey) throw new InvalidOperationException("Tests require their isolated progress slot.");
            VerifyCurrentProfile();
        }
        public static void Begin(bool restoredRun = false)
        {
            if (!Application.isPlaying || steps != null) throw new InvalidOperationException("Requires Play Mode and no active suite.");
            context = ReadContext(); gate = UnityEngine.Object.FindFirstObjectByType<RockyGateReview>();
            interactor = UnityEngine.Object.FindFirstObjectByType<RectoriaDialogueInteractor>(); player = interactor.player;
            collider = gate.map.doors.GetComponent<TilemapCollider2D>(); Guard();
            results.Clear(); signals.Clear(); completions = 0; Status = "Running"; lastFrame = -1;
            keyboard = InputSystem.AddDevice<Keyboard>("RockyValidationKeyboard"); keyboard.MakeCurrent();
            gate.ReviewCompleted += CountCompletion; gate.onReviewCompleted.AddListener(CountSignal);
            report = restoredRun ? "reload-results.txt" : "results.txt";
            Application.runInBackground = true; EditorApplication.isPaused = false;
            steps = restoredRun ? ValidateReload() : Validate(); EditorApplication.update += Tick;
        }
        private static void CountCompletion() { completions++; }
        private static void CountSignal(string id) { signals.Add(id); }
        private static void Tick()
        {
            if (!Application.isPlaying) { Finish("FAILED: Play Mode ended"); return; }
            if (Time.frameCount == lastFrame) return; lastFrame = Time.frameCount;
            try { Guard(); if (!steps.MoveNext()) Finish("PASS"); }
            catch (Exception error) { Finish("FAILED: " + error); }
        }
        private static void Finish(string status)
        {
            EditorApplication.update -= Tick; steps = null; Status = status;
            if (gate != null) { gate.ReviewCompleted -= CountCompletion; gate.onReviewCompleted.RemoveListener(CountSignal); }
            if (keyboard != null && keyboard.added) { InputSystem.ResetDevice(keyboard); InputSystem.RemoveDevice(keyboard); }
            File.WriteAllText(Path.Combine(DirectoryPath, report), string.Join("\n", results) + "\n" + status);
        }
        private static void Check(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message); results.Add("PASS: " + message);
        }
        private static IEnumerable<object> Wait(float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end) yield return null;
        }
        private static void FaceRocky()
        {
            var p = interactor.points.Single(x => x.pointId == "rocky");
            player.transform.position = player.map.CellCenter(p.approachCell);
            var d = p.cell - p.approachCell; player.facing = new Vector2Int(d.x, d.y);
        }
        private static void InteractWithKey(Key key)
        {
            keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); InputSystem.Update();
            interactor.SendMessage("Update", SendMessageOptions.RequireReceiver);
            InputSystem.ResetDevice(keyboard);
        }
        private static IEnumerable<object> FinishDialogue()
        {
            int safety = 0;
            while (gate.session.IsOpen)
            {
                yield return null; Check(gate.session.Advance(), "Shared dialogue advances normally");
                if (++safety > 10) throw new InvalidOperationException("Dialogue did not close.");
            }
            yield return null;
        }
        private static void SetDocuments(int mask)
        {
            Guard(); gate.mission.StartNewGame(); gate.session.RegisterEvent(RectoriaDialogueInteractor.WelcomeId);
            for (int i = 0; i < 3; i++) if ((mask & (1 << i)) != 0) gate.mission.TryAwardDocument((EnrollmentDocument)i);
        }
        private static bool GateBlocked() => gate.gateCells.All(c => !gate.map.CanEnter(c) && gate.map.doors.HasTile(c) && collider.OverlapPoint(gate.map.CellCenter(c)));
        private static bool GatePassable() => gate.gateCells.All(c => gate.map.CanEnter(c) && !gate.map.doors.HasTile(c) && !collider.OverlapPoint(gate.map.CellCenter(c)));

        private static IEnumerator Validate()
        {
            Check(gate.mission.ProgressStorageKey != EnrollmentMission.SaveKey, "All mutations target the isolated temporary slot");
            Check(ReadContext().sha256 == Hash(context.hadSave, context.progressJson), "Current-profile durable backup round trip is valid");
            var originalDoors = new Dictionary<Vector3Int, TileBase>();
            foreach (var cell in gate.map.doors.cellBounds.allPositionsWithin)
                if (gate.map.doors.HasTile(cell)) originalDoors.Add(cell, gate.map.doors.GetTile(cell));
            Check(gate.gateCells.Length == 2 && GateBlocked(), "Existing two-cell gate is physically and logically blocked");
            for (int mask = 0; mask < 7; mask++)
            {
                SetDocuments(mask); yield return null; FaceRocky();
                InteractWithKey(mask % 2 == 0 ? Key.E : Key.Enter);
                Check(gate.session.IsOpen && gate.session.view.bodyLabel.text == "Aún te faltan documentos. Revisa la barra de arriba", "Mask " + mask + " gets exact missing-document dialogue");
                foreach (var step in FinishDialogue()) yield return step;
                Check(!gate.IsOpen && !gate.IsOpening && GateBlocked() && player.enabled, "Mask " + mask + " leaves gate closed and allows return to offices");
                Check(completions == 0 && signals.Count == 0, "Incomplete documents emit no completion signal");
            }
            var start = gate.gateCells[0] + Vector3Int.down;
            player.transform.position = gate.map.CellCenter(start);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); InputSystem.Update();
            foreach (var step in Wait(.4f)) yield return step;
            InputSystem.ResetDevice(keyboard);
            Check(Vector3.Distance(player.transform.position, gate.map.CellCenter(start)) < .001f, "Actual Mateo controller cannot cross closed gate");

            SetDocuments(7); yield return null; FaceRocky();
            player.facing = -player.facing; Check(!interactor.TryInteract(), "Rocky requires facing");
            FaceRocky(); player.transform.position += Vector3.right * .1f;
            Check(!interactor.TryInteract(), "Rocky cannot be used midway through a step");
            FaceRocky(); InteractWithKey(Key.E);
            Check(gate.session.view.bodyLabel.text == "A ver... acta, credencial y comprobante" && !gate.IsOpening, "Three documents show first exact review line");
            gate.session.Abort(); yield return null;
            Check(!gate.IsOpen && !gate.session.HasCompleted(RockyGateReview.ReviewId), "Aborting review grants no authorization");
            InteractWithKey(Key.Enter); yield return null; Check(gate.session.Advance(), "Advance to final review line");
            Check(gate.session.view.bodyLabel.text == "Todo en regla. Adelante" && GateBlocked() && !gate.IsOpening, "Gate waits until final spoken line is completed");
            foreach (var step in FinishDialogue()) yield return step;
            Check(gate.IsOpening && !player.enabled && interactor.AvailableSpeaker() == null, "Final completion starts sequence and blocks world input");
            var seen = new HashSet<int> { gate.LitSlots }; Vector3 position = player.transform.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftShift, Key.W)); InputSystem.Update();
            float deadline = Time.realtimeSinceStartup + 5;
            while (gate.IsOpening && Time.realtimeSinceStartup < deadline)
            {
                seen.Add(gate.LitSlots);
                if (player.transform.position != position || player.enabled) throw new Exception("Movement escaped the opening sequence.");
                Check(gate.gateCells.All(c => !gate.map.CanEnter(c)), "Door remains logically blocked until opening completes");
                yield return null;
            }
            InputSystem.ResetDevice(keyboard); seen.Add(gate.LitSlots);
            Check(gate.IsOpen && !gate.IsOpening && GatePassable(), "Sequence opens original tiles and rebuilds physical collision");
            Check(seen.SetEquals(new[] { 0, 1, 2, 3 }), "All three lock slots illuminate in sequence");
            Check(player.enabled && !gate.lockRoot.activeSelf, "Controls restored and unlocked lock hidden");
            Check(completions == 1 && signals.SequenceEqual(new[] { RockyGateReview.CompletionSignal }), "Exactly one completion signal is ready for later cinematics");
            Check(gate.gateCells.All(c => gate.minimap.KindAt(c) == MinimapCell.Walkable), "Minimap recognizes newly walkable gate cells");
            Check(originalDoors.Where(p => !gate.gateCells.Contains(p.Key)).All(p => gate.map.doors.GetTile(p.Key) == p.Value), "Every other closed door is unchanged");
            Check(gate.mission.counterLabel.text == "Documentos: 3/3" && gate.mission.objectiveLabel.text == "Puerta de Bloques abierta", "HUD retains documents and acknowledges opening");

            player.transform.position = gate.map.CellCenter(start);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); InputSystem.Update();
            bool crossed = false; deadline = Time.realtimeSinceStartup + 2;
            var farSide = gate.map.CellCenter(gate.gateCells[0] + Vector3Int.up);
            while (Time.realtimeSinceStartup < deadline)
            {
                if (player.transform.position.y >= farSide.y - .01f) { crossed = true; break; }
                yield return null;
            }
            InputSystem.ResetDevice(keyboard); foreach (var step in Wait(.35f)) yield return step;
            Check(crossed, "Actual Mateo grid movement crosses through opened gate to its far side");
            FaceRocky(); InteractWithKey(Key.E);
            Check(gate.session.CurrentDialogueId == gate.alreadyOpenDialogue.dialogueId && gate.session.view.pageLabel.text == "1 / 1", "Repeat Rocky interaction uses short already-open dialogue");
            foreach (var step in FinishDialogue()) yield return step;
            Check(!gate.IsOpening && completions == 1 && signals.Count == 1 && GatePassable(), "Repeat never animates, reviews all documents or emits again");
            Check(gate.mission.RestoreProgress() && GatePassable() && completions == 1, "Restore opens gate immediately without replaying completion");

            string completedSave = PlayerPrefs.GetString(context.testKey);
            File.WriteAllText(Path.Combine(DirectoryPath, "completed-isolated-save.json"), completedSave);
            SetDocuments(1); yield return null;
            string oneDocument = PlayerPrefs.GetString(context.testKey);
            string isolatedBackup = Path.Combine(DirectoryPath, "isolated-backup-roundtrip.json"); File.WriteAllText(isolatedBackup, oneDocument);
            gate.mission.StartNewGame(); PlayerPrefs.SetString(context.testKey, File.ReadAllText(isolatedBackup)); PlayerPrefs.Save();
            Check(gate.mission.RestoreProgress() && gate.mission.DocumentCount == 1 && GateBlocked(), "Disk backup restores known temporary progress only");
            SetDocuments(7); yield return null; FaceRocky(); Check(interactor.TryInteract(), "Can review after temporary new-game reset");
            foreach (var step in FinishDialogue()) yield return step;
            Check(gate.IsOpening, "Interrupted-sequence reset begins from closed gate");
            foreach (var step in Wait(.6f)) yield return step;
            gate.mission.StartNewGame(); yield return null;
            Check(!gate.IsOpening && !gate.IsOpen && GateBlocked() && player.enabled && !gate.mission.IsActive, "Temporary New Game during animation restores closed gate and controls");
            PlayerPrefs.SetString(context.testKey, completedSave); PlayerPrefs.Save(); gate.mission.RestoreProgress();
            Check(GatePassable() && completions == 1, "Loading completed temporary backup does not replay animation or signal");
            Check(originalDoors.Where(p => !gate.gateCells.Contains(p.Key)).All(p => gate.map.doors.GetTile(p.Key) == p.Value), "Other doors remain unchanged through resets and restoration");
            Check(VerifyCurrentProfile() != null, "Real user progress remains byte-for-byte unchanged after full suite");
            Check(!EditorUtility.scriptCompilationFailed, "No compilation errors");
        }

        private static IEnumerator ValidateReload()
        {
            Check(gate.IsOpen && GatePassable() && !gate.IsOpening && !gate.lockRoot.activeSelf, "New Play session restores open gate, collision and visuals immediately");
            Check(gate.mission.DocumentCount == 3 && gate.minimap.KindAt(gate.gateCells[0]) == MinimapCell.Walkable, "Reload restores document HUD and open minimap access");
            Check(completions == 0 && signals.Count == 0, "Loading never emits fresh review completion");
            FaceRocky(); InteractWithKey(Key.Enter);
            Check(gate.session.CurrentDialogueId == gate.alreadyOpenDialogue.dialogueId, "Restored Rocky uses short dialogue");
            foreach (var step in FinishDialogue()) yield return step;
            Check(GatePassable() && !gate.IsOpening && completions == 0 && signals.Count == 0, "Restored repeat does not review, animate or emit again");
            Check(VerifyCurrentProfile() != null, "User save is unchanged after isolated persistence test");
        }

        public static string Cleanup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Leave Play Mode before cleanup.");
            context = ReadContext(); string verification = VerifyCurrentProfile();
            if (!context.testKey.StartsWith("CampusQuest.Test.Rocky.", StringComparison.Ordinal)) throw new InvalidOperationException("Unexpected cleanup key.");
            PlayerPrefs.DeleteKey(context.testKey); PlayerPrefs.Save(); SessionState.EraseString(EnrollmentMission.EditorTestStorageSetting);
            return verification + "; only temporary test slot removed; disk backups retained.";
        }
    }
}
#endif
