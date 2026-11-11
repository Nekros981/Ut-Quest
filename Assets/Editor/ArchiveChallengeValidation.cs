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
    /// <summary>Explicit Editor-only regression suite. Never runs in the player or automatically.</summary>
    public static class ArchiveChallengeValidation
    {
        private static IEnumerator steps;
        private static int frame;
        public static string Status { get; private set; } = "Not started";
        private static readonly List<string> results = new List<string>();
        private static ArchiveChallenge archive;
        private static RectoriaDialogueInteractor interactor;
        private static RectoriaPlayerTest player;
        private static readonly string[] OriginalFolders =
        {
            "Roja · 2006", "Azul · 2007", "Verde · 2008", "Amarilla · 2007",
            "Azul · 2008", "Naranja · 2006", "Morada · 2007", "Amarilla · 2008", "Azul · 2006"
        };

        public static void Begin()
        {
            if (!Application.isPlaying || steps != null) throw new InvalidOperationException("Requires Play Mode and no active validation.");
            archive = UnityEngine.Object.FindFirstObjectByType<ArchiveChallenge>();
            interactor = UnityEngine.Object.FindFirstObjectByType<RectoriaDialogueInteractor>();
            player = interactor.player;
            results.Clear(); Status = "Running"; frame = -1;
            Application.runInBackground = true; EditorApplication.isPaused = false;
            steps = Validate(); EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!Application.isPlaying) { Finish("FAILED: Play Mode ended during validation"); return; }
            if (Time.frameCount == frame) return;
            frame = Time.frameCount;
            try { if (!steps.MoveNext()) Finish("PASS"); }
            catch (Exception error) { Finish("FAILED: " + error); }
        }

        private static void Finish(string status)
        {
            EditorApplication.update -= Tick; steps = null; Status = status;
            Directory.CreateDirectory("tmp/archive-validation");
            File.WriteAllText("tmp/archive-validation/results.txt", string.Join("\n", results) + "\n" + status);
        }

        private static void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException(name);
            results.Add("PASS: " + name);
        }

        private static RectoriaScenePoint Face(string id, bool toward = true)
        {
            var point = interactor.points.Single(p => p.pointId == id);
            player.transform.position = player.map.CellCenter(point.approachCell);
            var delta = point.cell - point.approachCell;
            player.facing = toward ? new Vector2Int(delta.x, delta.y) : new Vector2Int(-delta.x, -delta.y);
            return point;
        }

        private static IEnumerable<object> FinishDialogue()
        {
            int safety = 0;
            while (archive.session.IsOpen)
            {
                yield return null;
                Check(archive.session.Advance(), "Dialogue advances once per frame");
                if (++safety > 20) throw new InvalidOperationException("Dialogue did not end");
            }
            yield return null;
        }

        private static void Select(int index)
        {
            while (archive.SelectedIndex % 3 < index % 3) archive.MoveSelection(Vector2Int.right);
            while (archive.SelectedIndex % 3 > index % 3) archive.MoveSelection(Vector2Int.left);
            while (archive.SelectedIndex / 3 < index / 3) archive.MoveSelection(Vector2Int.down);
            while (archive.SelectedIndex / 3 > index / 3) archive.MoveSelection(Vector2Int.up);
        }

        private static IEnumerable<object> Press(Key key)
        {
            if (Keyboard.current == null) throw new InvalidOperationException("Keyboard required for input validation.");
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(key));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            yield return null; yield return null;
        }

        private static IEnumerator Validate()
        {
            Check(archive.table != null && archive.table.IsValid, "Production folder table is valid; no synthetic fixture");
            Check(archive.table.folders.Select(f => f.Label).SequenceEqual(OriginalFolders), "All nine original page-7 options in exact order");
            archive.mission.StartNewGame();
            Check(!archive.TryOpen(), "Cannot open puzzle before enrollment");
            yield return null;
            Face("don_chuy");
            foreach (var step in Press(Key.E)) yield return step;
            Check(archive.session.IsOpen, "E talks to adjacent Don Chuy through actual keyboard input");
            Check(archive.session.CurrentDialogueId == archive.prerequisiteDialogue.dialogueId, "Don Chuy sends player to Rectora first");
            foreach (var step in FinishDialogue()) yield return step;
            Face("rectora"); Check(interactor.TryInteract(), "Existing Rectora welcome starts");
            foreach (var step in FinishDialogue()) yield return step;
            Check(archive.mission.IsActive, "Actual welcome completion activates enrollment");
            Face("nota_1"); Check(!interactor.TryInteract(), "Notes require Don Chuy introduction");
            Face("don_chuy", false); Check(!interactor.TryInteract(), "Wrong facing cannot talk to Don Chuy");
            Face("don_chuy"); player.transform.position += new Vector3(.1f, 0, 0);
            Check(!interactor.TryInteract(), "Mid-step interaction rejected");
            Face("don_chuy"); Check(interactor.TryInteract(), "Don Chuy introduction starts");
            archive.session.Abort();
            Check(!archive.session.HasCompleted(ArchiveChallenge.IntroId), "Aborted introduction not completed");
            yield return null;
            Check(interactor.TryInteract(), "Introduction can restart after abort");
            foreach (var step in FinishDialogue()) yield return step;
            Check(archive.session.HasCompleted(ArchiveChallenge.IntroId), "Don Chuy introduction persisted");
            Check(!archive.TryOpen(), "Puzzle locked before all four notes");

            int count = 0;
            foreach (int i in new[] { 3, 1, 4, 2 })
            {
                Face("nota_" + i, false); Check(!interactor.TryInteract(), "Note " + i + " requires facing");
                Face("nota_" + i);
                foreach (var step in Press(Key.E)) yield return step;
                Check(archive.session.IsOpen, "E opens note " + i + " through shared dialogue");
                Check(archive.session.view.bodyLabel.text == ArchiveChallenge.Clues[i - 1], "Exact clue text " + i);
                Check(archive.CluesRead == count, "Opening note alone does not record completion");
                foreach (var step in FinishDialogue()) yield return step;
                Check(archive.CluesRead == ++count, "Read note increases persistent clue count");
                if (count == 2)
                {
                    Face("don_chuy"); Check(interactor.TryInteract(), "Can ask Don Chuy while clues missing");
                    Check(archive.session.CurrentDialogueId == archive.missingCluesDialogue.dialogueId, "Missing clues block folders");
                    foreach (var step in FinishDialogue()) yield return step;
                }
            }
            Face("nota_1"); Check(interactor.TryInteract(), "Notes can be reread");
            foreach (var step in FinishDialogue()) yield return step;
            Check(archive.CluesRead == 4, "Reread does not duplicate clues");

            Face("don_chuy"); Check(interactor.TryInteract(), "Four notes unlock Don Chuy ready dialogue");
            foreach (var step in FinishDialogue()) yield return step;
            Check(archive.IsOpen && !player.enabled, "Ready dialogue opens puzzle and suspends movement");
            Check(archive.AttemptsRemaining == 3, "Round starts with three attempts");
            Check(Enumerable.Range(0, 9).Select(i => archive.FolderAt(i).Label).SequenceEqual(OriginalFolders), "Initial grid preserves the script order");
            Check(!archive.rewardBanner.gameObject.activeSelf, "No reward hint before solving");
            for (int i = 0; i < 9; i++)
            {
                Select(i);
                Check(archive.folderIcons[i].color == archive.FolderAt(i).Tint
                    && archive.folderIcons[i].rectTransform.sizeDelta == archive.folderIcons[0].rectTransform.sizeDelta
                    && archive.folderLabels[i].font == archive.folderLabels[0].font
                    && archive.folderLabels[i].fontSize == archive.folderLabels[0].fontSize
                    && archive.folderLabels[i].color == archive.folderLabels[0].color
                    && archive.folderFrames.Where((f, index) => index != i).All(f => f.color == archive.folderFrames[(i + 1) % 9].color),
                    "Folder " + i + " uses identical visual rules regardless of answer");
            }
            Check(archive.folderIcons[1].color == archive.folderIcons[4].color && archive.folderIcons[4].color == archive.folderIcons[8].color,
                "Blue 2006, 2007 and 2008 share the exact same tint");
            Select(0);
            yield return null; yield return null;
            Capture("initial-grid.png");
            Check(!interactor.TryInteract(), "NPC interaction blocked during puzzle");
            archive.MoveSelection(Vector2Int.left); archive.MoveSelection(Vector2Int.up);
            Check(archive.SelectedIndex == 0, "Grid selection clamps at top-left");
            foreach (var step in Press(Key.RightArrow)) yield return step;
            Check(archive.SelectedIndex == 1, "Right arrow moves actual keyboard selection");
            foreach (var step in Press(Key.DownArrow)) yield return step;
            Check(archive.SelectedIndex == 4, "Down arrow moves actual keyboard selection");
            foreach (var step in Press(Key.LeftArrow)) yield return step;
            Check(archive.SelectedIndex == 3, "Left arrow moves actual keyboard selection");
            foreach (var step in Press(Key.UpArrow)) yield return step;
            Check(archive.SelectedIndex == 0, "Up arrow moves actual keyboard selection");
            Select(8); archive.MoveSelection(Vector2Int.right); archive.MoveSelection(Vector2Int.down);
            Check(archive.SelectedIndex == 8, "Grid selection clamps at bottom-right");
            var before = Enumerable.Range(0, 9).Select(i => archive.FolderAt(i).Label).ToArray();
            int wrong = Enumerable.Range(0, 9).First(i => !archive.FolderAt(i).IsAnswer); Select(wrong);
            Check(!archive.ConfirmSelection() && archive.AttemptsRemaining == 2, "First wrong answer consumes one attempt");
            Check(!archive.ConfirmSelection() && archive.AttemptsRemaining == 1, "Second wrong answer consumes one attempt");
            Check(!archive.ConfirmSelection() && archive.AttemptsRemaining == 3 && archive.Round == 2, "Third wrong answer starts fresh round");
            var after = Enumerable.Range(0, 9).Select(i => archive.FolderAt(i).Label).ToArray();
            Check(!before.SequenceEqual(after) && before.OrderBy(x => x).SequenceEqual(after.OrderBy(x => x)), "Retry rearranges exactly the same nine options");
            Check(archive.CluesRead == 4 && archive.mission.DocumentCount == 0, "Failed rounds preserve clues and give no document");
            Select(Enumerable.Range(0, 9).Single(i => archive.FolderAt(i).IsAnswer));
            foreach (var step in Press(Key.Enter)) yield return step;
            Check(archive.IsSolved, "Enter confirms blue 2008 through actual keyboard input");
            Check(archive.RewardGrantedThisRun && archive.rewardBanner.gameObject.activeSelf, "First victory shows reward animation");
            Check(archive.mission.DocumentCount == 1 && archive.mission.counterLabel.text == "Documentos: 1/3", "Reward uses enrollment API and updates HUD");
            Check(!archive.ConfirmSelection() && archive.mission.DocumentCount == 1, "Repeated confirm cannot award twice");
            yield return null; yield return null;
            Capture("first-reward.png");
            archive.Close(); Check(player.enabled && !archive.panel.activeSelf, "Closing puzzle restores movement and hides modal");
            yield return null;
            Face("don_chuy"); Check(interactor.TryInteract(), "Completed Archive can be replayed through Don Chuy");
            foreach (var step in FinishDialogue()) yield return step;
            Check(archive.IsOpen, "Replay opens puzzle");
            Check(Enumerable.Range(0, 9).Select(i => archive.FolderAt(i).Label).SequenceEqual(OriginalFolders), "Replay starts in original order again");
            Select(Enumerable.Range(0, 9).Single(i => archive.FolderAt(i).IsAnswer));
            Check(archive.ConfirmSelection() && !archive.RewardGrantedThisRun && !archive.rewardBanner.gameObject.activeSelf, "Replay succeeds without reward or animation");
            Check(archive.mission.DocumentCount == 1, "Replay does not duplicate acta");
            Check(archive.session.CaptureCompletedIds().Count(id => id == EnrollmentMission.BirthEvent) == 1,
                "Persistent registry contains exactly one acta reward event");
            var completedProgress = archive.session.CaptureCompletedIds();
            archive.Close(); yield return null;
            Check(archive.mission.RestoreProgress() && archive.CluesRead == 4 && archive.mission.DocumentCount == 1, "Save restores clues and document together");
            Check(archive.TryOpen(), "Can reopen after restore");
            yield return null;
            foreach (var step in Press(Key.Escape)) yield return step;
            Check(!archive.IsOpen && player.enabled && archive.CluesRead == 4, "Escape cancels puzzle, restores controls and keeps clues");
            Check(archive.TryOpen(), "Can reopen after cancellation");
            archive.mission.StartNewGame();
            Check(!archive.IsOpen && player.enabled && archive.CluesRead == 0 && !archive.CanPlay, "New Game closes puzzle, releases controls and clears clues");
            // Restore the actual completed run for the independent Play Mode restart check.
            archive.session.RestoreCompletedIds(completedProgress);
        }

        private static void Capture(string name)
        {
            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            var view = Resources.FindObjectsOfTypeAll(type).FirstOrDefault();
            if (view == null) { results.Add("NOTICE: No Game View for capture"); return; }
            var field = type.GetField("m_RenderTexture", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var rt = field?.GetValue(view) as RenderTexture;
            if (rt == null) { results.Add("NOTICE: No Game View render texture for capture"); return; }
            var old = RenderTexture.active;
            var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = rt; texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); texture.Apply();
                if (SystemInfo.graphicsUVStartsAtTop)
                {
                    var pixels = texture.GetPixels32(); var flipped = new Color32[pixels.Length];
                    for (int y = 0; y < rt.height; y++) Array.Copy(pixels, y * rt.width, flipped, (rt.height - 1 - y) * rt.width, rt.width);
                    texture.SetPixels32(flipped); texture.Apply();
                }
                Directory.CreateDirectory("tmp/archive-validation"); File.WriteAllBytes("tmp/archive-validation/" + name, texture.EncodeToPNG());
            }
            finally { RenderTexture.active = old; UnityEngine.Object.DestroyImmediate(texture); }
        }
    }
}
#endif
