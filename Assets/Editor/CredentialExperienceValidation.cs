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
using UnityEngine.UI;

namespace CampusQuest.EditorTools
{
    /// <summary>Explicit Editor-only integration checks. Back up the save before running.</summary>
    public static class CredentialExperienceValidation
    {
        public static string Status { get; private set; } = "Not started";
        private static IEnumerator steps;
        private static int frame;
        private static readonly List<string> results = new List<string>();
        private static CredentialChallenge c;
        private static RectoriaMinimap minimap;
        private static RectoriaDialogueInteractor interactor;
        private static RectoriaPlayerTest player;
        private static Vector3Int corridor;

        public static void Begin()
        {
            if (!Application.isPlaying || steps != null) throw new InvalidOperationException("Requires Play Mode, no active suite.");
            c = UnityEngine.Object.FindFirstObjectByType<CredentialChallenge>();
            minimap = UnityEngine.Object.FindFirstObjectByType<RectoriaMinimap>();
            interactor = UnityEngine.Object.FindFirstObjectByType<RectoriaDialogueInteractor>(); player = interactor.player;
            Status = "Running"; results.Clear(); frame = -1; Application.runInBackground = true;
            EditorApplication.isPaused = false; steps = Validate(); EditorApplication.update += Tick;
        }
        private static void Tick()
        {
            if (!Application.isPlaying) { Finish("FAILED: Play Mode ended"); return; }
            if (frame == Time.frameCount) return; frame = Time.frameCount;
            try { if (!steps.MoveNext()) Finish("PASS"); }
            catch (Exception e) { Finish("FAILED: " + e); }
        }
        private static void Finish(string status)
        {
            EditorApplication.update -= Tick; steps = null; Status = status;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            Directory.CreateDirectory("tmp/credential-validation");
            File.WriteAllText("tmp/credential-validation/results.txt", string.Join("\n", results) + "\n" + status);
        }
        private static void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException(name); results.Add("PASS: " + name);
        }
        private static IEnumerable<object> Wait(float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end) yield return null;
        }
        private static IEnumerable<object> Press(params Key[] keys)
        {
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(keys));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            yield return null; yield return null;
        }
        private static IEnumerable<object> Dialogue()
        {
            int guard = 0;
            while (c.session.IsOpen)
            {
                yield return null; Check(c.session.Advance(), "Dialogue advances");
                if (++guard > 20) throw new Exception("Dialogue stuck");
            }
            yield return null;
        }
        private static void Face(string id)
        {
            var p = interactor.points.Single(x => x.pointId == id);
            player.transform.position = player.map.CellCenter(p.approachCell);
            var delta = p.cell - p.approachCell; player.facing = new Vector2Int(delta.x, delta.y);
        }
        private static void Select(int index)
        {
            while (c.SelectedIndex % 3 < index % 3) c.MoveSelection(Vector2Int.right);
            while (c.SelectedIndex % 3 > index % 3) c.MoveSelection(Vector2Int.left);
            while (c.SelectedIndex / 3 < index / 3) c.MoveSelection(Vector2Int.down);
            while (c.SelectedIndex / 3 > index / 3) c.MoveSelection(Vector2Int.up);
        }

        private static List<int> Solve(int[] board)
        {
            string start = string.Concat(board.Select(i => (char)('0' + i)));
            const string goal = "123456780";
            var queue = new Queue<string>(); queue.Enqueue(start);
            var parents = new Dictionary<string, (string parent, int move)> { [start] = (null, -1) };
            while (queue.Count > 0)
            {
                string state = queue.Dequeue();
                if (state == goal)
                {
                    var moves = new List<int>();
                    while (parents[state].parent != null) { moves.Add(parents[state].move); state = parents[state].parent; }
                    moves.Reverse(); return moves;
                }
                int blank = state.IndexOf('0');
                for (int i = 0; i < 9; i++)
                {
                    if (!SlidingCredentialBoard.Adjacent(i, blank)) continue;
                    var chars = state.ToCharArray(); chars[blank] = chars[i]; chars[i] = '0'; string next = new string(chars);
                    if (!parents.ContainsKey(next)) { parents[next] = (state, i); queue.Enqueue(next); }
                }
            }
            throw new Exception("Unsolvable board");
        }

        private static IEnumerable<object> Photo()
        {
            player.transform.position = player.map.CellCenter(c.photoMark.cell) + Vector3.right * .15f;
            yield return null; yield return null;
            player.transform.position = player.map.CellCenter(c.photoMark.cell);
            float end = Time.realtimeSinceStartup + 2.5f;
            while (c.Stage == CredentialStage.Waiting && Time.realtimeSinceStartup < end) yield return null;
            Check(c.Stage == CredentialStage.Countdown && !player.enabled, "1.5 seconds stillness starts locked capture");
            Vector3 position = player.transform.position;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.RightShift, Key.RightArrow));
            var seen = new HashSet<int>(); end = Time.realtimeSinceStartup + 5;
            bool sawFlash = false;
            while (c.Stage != CredentialStage.Puzzle && Time.realtimeSinceStartup < end)
            {
                if (c.Stage == CredentialStage.Countdown) seen.Add(c.CountdownNumber);
                if (c.Stage == CredentialStage.Flash) sawFlash = c.flash.gameObject.activeSelf;
                Check((player.transform.position - position).sqrMagnitude < .00001f, "Shift/arrows cannot move Mateo during capture");
                yield return null;
            }
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); yield return null; yield return null;
            Check(seen.SetEquals(new[] { 3, 2, 1 }) && sawFlash, "Capture shows 3, 2, 1 and camera flash");
            Check(c.Stage == CredentialStage.Puzzle && c.session.HasCompleted(CredentialChallenge.PhotoId), "Photo opens puzzle and persists photo event");
            Check(minimap.visibility.alpha == 0 && !player.enabled && interactor.AvailableSpeaker() == null, "Puzzle hides minimap and blocks world input");
        }

        private static IEnumerable<object> Travel(Key? shift, Key direction, string name)
        {
            player.transform.position = player.map.CellCenter(corridor); yield return null; yield return null;
            var keys = shift.HasValue ? new[] { direction, shift.Value } : new[] { direction };
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(keys));
            Vector3 previous = player.transform.position; Vector3 start = previous;
            var speeds = new List<float>(); float end = Time.realtimeSinceStartup + 2;
            while ((player.transform.position - start).magnitude < 3.2f && Time.realtimeSinceStartup < end)
            {
                yield return null;
                float distance = Vector3.Distance(previous, player.transform.position);
                if (distance > .0001f && Time.deltaTime > 0) speeds.Add(distance / Time.deltaTime);
                previous = player.transform.position;
                Check(Vector2.Distance(minimap.playerDot.anchoredPosition, minimap.PositionForWorld(player.transform.position)) < .02f,
                    "Minimap tracks interpolated " + name);
            }
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            foreach (var step in Wait(.4f)) yield return step;
            speeds.Sort(); float measured = speeds.Count > 0 ? speeds[speeds.Count / 2] : 0;
            float expected = shift.HasValue && shift.Value != Key.Z ? 1 / player.runSecondsPerCell : 1 / player.walkSecondsPerCell;
            Check(Mathf.Abs(measured - expected) < .5f, name + " speed=" + measured.ToString("0.00") + " expected=" + expected.ToString("0.00"));
            Vector3 stopped = player.transform.position;
            foreach (var step in Wait(.2f)) yield return step;
            Check(Vector3.Distance(stopped, player.transform.position) < .00001f, "Release stops " + name + " at cell center");
        }

        private static IEnumerable<object> ShiftRelease()
        {
            player.transform.position = player.map.CellCenter(corridor); yield return null; yield return null;
            Vector3 start = player.transform.position;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.LeftShift, Key.D));
            float end = Time.realtimeSinceStartup + 2;
            while (Vector3.Distance(start, player.transform.position) < 1.2f && Time.realtimeSinceStartup < end) yield return null;
            Check(Vector3.Distance(start, player.transform.position) >= 1.2f, "Shift starts running while D held");
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.D));
            foreach (var step in Wait(.3f)) yield return step;
            Vector3 previous = player.transform.position; var speeds = new List<float>(); end = Time.realtimeSinceStartup + .6f;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null; float distance = Vector3.Distance(previous, player.transform.position);
                if (distance > .0001f) speeds.Add(distance / Time.deltaTime); previous = player.transform.position;
            }
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            foreach (var step in Wait(.4f)) yield return step;
            speeds.Sort(); Check(speeds.Count > 0 && Mathf.Abs(speeds[speeds.Count / 2] - 1 / player.walkSecondsPerCell) < .5f,
                "Releasing Shift while D remains held returns to walk speed at next cell");
        }

        private static IEnumerator Validate()
        {
            c.mission.StartNewGame();
            for (int seed = 0; seed < 500; seed++)
            {
                var board = new SlidingCredentialBoard(seed); var values = board.Capture();
                int inversions = 0;
                for (int i = 0; i < 9; i++) for (int j = i + 1; j < 9; j++) if (values[i] != 0 && values[j] != 0 && values[i] > values[j]) inversions++;
                if (inversions % 2 != 0 || board.IsSolved || values.Distinct().Count() != 9 || board.ScrambleLength < 10 || board.ScrambleLength > 12)
                    throw new Exception("Invalid scramble seed " + seed);
                int adjacent = Enumerable.Range(0, 9).First(i => SlidingCredentialBoard.Adjacent(i, board.Blank));
                if (!board.TryMove(adjacent) || board.Moves != 1) throw new Exception("Legal move failed");
                board.Restart(); if (!values.SequenceEqual(board.Capture()) || board.Moves != 0) throw new Exception("Restart failed");
            }
            Check(true, "500 seeds: valid permutations, solvable parity, ~10 legal moves, restart restores board");
            int painted = 0;
            for (int y = 0; y < 46; y++) for (int x = 0; x < 44; x++)
            {
                var cell = new Vector3Int(x, y, 0); var kind = minimap.KindAt(cell);
                if ((kind == MinimapCell.Walkable) != player.map.CanEnter(cell)) throw new Exception("Minimap walkability mismatch " + cell);
                if (player.map.walls.HasTile(cell) && kind != MinimapCell.Wall) throw new Exception("Minimap wall mismatch");
                if (kind != MinimapCell.Empty) painted++;
            }
            Check(true, "All 2024 minimap cells agree with logical walkability and walls");
            Check(!minimap.raycastTarget && !minimap.visibility.blocksRaycasts
                && minimap.visibility.GetComponentsInChildren<Graphic>(true).All(g => !g.raycastTarget), "Minimap consumes no mouse input");
            Check(((RectTransform)minimap.transform.parent).anchoredPosition == Vector2.zero, "Minimap background is centered inside gold border");
            bool found = false;
            for (int y = 2; y < 44 && !found; y++) for (int x = 1; x < 35 && !found; x++)
                if (Enumerable.Range(0, 8).All(i => player.map.CanEnter(new Vector3Int(x + i, y, 0)))) { corridor = new Vector3Int(x, y, 0); found = true; }
            Check(found, "Found existing open corridor for movement tests");
            foreach (var step in Travel(null, Key.D, "walking")) yield return step;
            foreach (var step in Travel(Key.LeftShift, Key.D, "left Shift + WASD")) yield return step;
            foreach (var step in Travel(Key.RightShift, Key.RightArrow, "right Shift + arrows")) yield return step;
            foreach (var step in Travel(Key.Z, Key.D, "legacy Z uses walking")) yield return step;
            foreach (var step in Travel(null, Key.RightArrow, "walking after Shift release")) yield return step;
            foreach (var step in ShiftRelease()) yield return step;
            Face("lic_mony"); foreach (var step in Press(Key.E)) yield return step;
            Check(c.session.CurrentDialogueId == c.prerequisiteDialogue.dialogueId, "Mony requires enrollment only, not Archive");
            foreach (var step in Dialogue()) yield return step;
            c.session.RegisterEvent(RectoriaDialogueInteractor.WelcomeId);
            Face("lic_mony"); player.facing = -player.facing; Check(!interactor.TryInteract(), "Mony requires facing");
            Face("lic_mony"); foreach (var step in Press(Key.Enter)) yield return step;
            Check(c.session.CurrentDialogueId == CredentialChallenge.IntroId && c.session.view.bodyLabel.text == "Para tu credencial necesito tu foto. Párate sobre la marca del piso", "Mony first exact script line");
            Vector3 dialogPosition = player.transform.position;
            foreach (var step in Press(Key.LeftShift, Key.D)) yield return step;
            Check(player.transform.position == dialogPosition && minimap.visibility.alpha == 1, "Shift cannot move during dialogue; minimap remains visible");
            Check(c.session.Advance(), "Advance to second Mony line"); yield return null;
            Check(c.session.view.bodyLabel.text == "Cuando salga la foto, la máquina te pedirá armar el marco. Se desacomodó", "Mony second exact script line");
            foreach (var step in Dialogue()) yield return step;
            player.transform.position = player.map.CellCenter(c.photoMark.cell);
            foreach (var step in Wait(.65f)) yield return step;
            Check(c.Stage == CredentialStage.Waiting && c.StillSeconds > .2f && player.enabled, "Before 1.5 seconds player can still leave mark");
            player.transform.position += Vector3.right * .1f; yield return null; yield return null;
            Check(c.StillSeconds == 0, "Moving resets photo stillness timer");
            foreach (var step in Photo()) yield return step;
            Check(!c.Board.IsSolved && c.Board.Moves == 0, "Puzzle starts shuffled with zero player moves");
            Select(c.Board.Blank); Check(!c.MoveSelected() && c.Board.Moves == 0, "Blank/non-adjacent move cannot move");
            int invalid = Enumerable.Range(0, 9).First(i => i != c.Board.Blank && !SlidingCredentialBoard.Adjacent(i, c.Board.Blank));
            Select(invalid); Check(!c.MoveSelected(), "Non-adjacent piece rejected");
            var initial = c.Board.Capture();
            int legal = Enumerable.Range(0, 9).First(i => SlidingCredentialBoard.Adjacent(i, c.Board.Blank));
            Select(legal); foreach (var step in Press(Key.E)) yield return step;
            Check(c.Board.Moves == 1, "E moves adjacent piece and increments counter");
            foreach (var step in Press(Key.R)) yield return step;
            Check(c.Board.Moves == 0 && initial.SequenceEqual(c.Board.Capture()), "R restarts without penalty");
            if (Mouse.current != null)
            {
                Select(legal);
                Vector2 point = RectTransformUtility.WorldToScreenPoint(null, c.frames[legal].transform.position);
                InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = point }.WithButton(MouseButton.Left)); yield return null; yield return null;
                InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = point }); yield return null; yield return null;
                Check(c.Board.Moves == 1, "Mouse click moves adjacent tile"); c.RestartPuzzle();
            }
            yield return null; Capture("credential-puzzle.png");
            foreach (int index in Solve(c.Board.Capture())) { Select(index); Check(c.MoveSelected(), "Solver applies legal adjacent move"); yield return null; }
            Check(c.Stage == CredentialStage.Success && c.RewardGrantedThisRun && c.rewardLabel.gameObject.activeSelf, "First solve grants credential and reward animation");
            Check(c.mission.HasDocument(EnrollmentDocument.StudentCard) && !c.mission.HasDocument(EnrollmentDocument.BirthCertificate)
                && c.mission.counterLabel.text == "Documentos: 1/3", "Credential can be first document, HUD updates and save records it");
            foreach (var step in Press(Key.Enter)) yield return step;
            Check(c.session.IsOpen && c.session.view.bodyLabel.text == "¡Listo! Ya eres oficialmente estudiante", "Mony speaks exact success line through shared dialogue");
            foreach (var step in Dialogue()) yield return step;
            foreach (var step in Wait(1.7f)) yield return step;
            Check(!c.IsBusy && minimap.visibility.alpha == 1, "No unwanted automatic recapture; minimap restored");
            foreach (var step in Photo()) yield return step;
            foreach (int index in Solve(c.Board.Capture())) { Select(index); c.MoveSelected(); yield return null; }
            Check(c.Stage == CredentialStage.Success && !c.RewardGrantedThisRun && !c.rewardLabel.gameObject.activeSelf && c.mission.DocumentCount == 1, "Repeat solves with success only, no second credential or animation");
            c.Close(true); foreach (var step in Dialogue()) yield return step;
            var completed = c.session.CaptureCompletedIds();
            c.session.RegisterEvent(ArchiveChallenge.IntroId);
            for (int i = 1; i <= 4; i++) c.session.RegisterEvent(ArchiveChallenge.ClueId(i));
            Check(c.archive.TryOpen(), "Existing Archive still opens"); yield return null; yield return null;
            Check(minimap.visibility.alpha == 0, "Minimap hides during Archive puzzle too");
            c.archive.Close(); yield return null; yield return null;
            Check(minimap.visibility.alpha == 1, "Archive close restores minimap");
            c.mission.StartNewGame(); yield return null;
            Check(!c.IsBusy && player.enabled && !c.mission.IsActive, "New Game clears credential flow");
            c.session.RestoreCompletedIds(completed); Face("lic_mony"); yield return null;
            Capture("minimap-hud.png");
            Check(!EditorUtility.scriptCompilationFailed, "No compilation errors");
        }

        private static void Capture(string filename)
        {
            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            var view = Resources.FindObjectsOfTypeAll(type).FirstOrDefault();
            var field = type.GetField("m_RenderTexture", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var rt = view == null ? null : field?.GetValue(view) as RenderTexture;
            if (rt == null) return;
            var prior = RenderTexture.active; var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = rt; texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); texture.Apply();
                if (SystemInfo.graphicsUVStartsAtTop)
                {
                    var pixels = texture.GetPixels32(); var flipped = new Color32[pixels.Length];
                    for (int y = 0; y < rt.height; y++) Array.Copy(pixels, y * rt.width, flipped, (rt.height - 1 - y) * rt.width, rt.width);
                    texture.SetPixels32(flipped); texture.Apply();
                }
                Directory.CreateDirectory("tmp/credential-validation"); File.WriteAllBytes("tmp/credential-validation/" + filename, texture.EncodeToPNG());
            }
            finally { RenderTexture.active = prior; UnityEngine.Object.DestroyImmediate(texture); }
        }
    }
}
#endif
