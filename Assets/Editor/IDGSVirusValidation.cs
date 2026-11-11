#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CampusQuest.Dialogue;
using CampusQuest.IDGS;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace CampusQuest.EditorTools
{
    // Editor-only input driver. All gameplay actions go through normal UI/keyboard processing.
    public static class IDGSVirusValidation
    {
        const string Folder = "tmp/idgs-virus";
        public static string Status { get; private set; } = "Not started";
        static IEnumerator steps;
        static readonly List<string> results = new List<string>();
        static Keyboard keyboard, priorKeyboard;
        static Mouse mouse, priorMouse;
        static InputSettings priorInputSettings, temporaryInputSettings;
        static string priorSettingsPath, priorSettingsJson;
        static bool priorBackground;
        static int frame, changes;
        static IDGSVirusChallenge Virus => UnityEngine.Object.FindFirstObjectByType<IDGSVirusChallenge>();
        static IDGSPlayerMotor Player => UnityEngine.Object.FindFirstObjectByType<IDGSPlayerMotor>();
        static IDGSInteractionController Interactor => UnityEngine.Object.FindFirstObjectByType<IDGSInteractionController>();
        static IDGSProgressHud Hud => UnityEngine.Object.FindFirstObjectByType<IDGSProgressHud>();
        public static string Prepare()
        {
            if (!string.IsNullOrEmpty(SessionState.GetString(GameProgress.EditorTestStorageSetting, ""))) throw new InvalidOperationException("Existing temporary profile must be cleaned first.");
            var result = IDGSInteriorValidation.Prepare();
            Directory.CreateDirectory(Folder); File.Copy("tmp/idgs-interiors/validation-context.json", Folder + "/profile-backup.json", true);
            return result;
        }
        static void Check(bool valid, string label)
        {
            if (!valid) throw new InvalidOperationException(label);
            results.Add("PASS " + label); File.WriteAllLines(Folder + "/results.txt", results);
        }
        public static void Begin()
        {
            if (!Application.isPlaying || steps != null || !GameProgress.StorageKey.StartsWith("CampusQuest.Test.IDGSInteriors.", StringComparison.Ordinal)) throw new InvalidOperationException("Isolated Play Mode required.");
            priorKeyboard = Keyboard.current; priorMouse = Mouse.current;
            priorInputSettings = InputSystem.settings;
            priorSettingsPath = AssetDatabase.GetAssetPath(priorInputSettings);
            priorSettingsJson = JsonUtility.ToJson(priorInputSettings);
            temporaryInputSettings = UnityEngine.Object.Instantiate(priorInputSettings);
            temporaryInputSettings.hideFlags = HideFlags.HideAndDontSave;
            temporaryInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            temporaryInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings = temporaryInputSettings;
            keyboard = InputSystem.AddDevice<Keyboard>("VirusValidationKeyboard"); mouse = InputSystem.AddDevice<Mouse>("VirusValidationMouse");
            priorBackground = Application.runInBackground; Application.runInBackground = true;
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Show();
            results.Clear(); changes = 0; frame = -1; Status = "Running";
            GameProgress.Changed += Changed; steps = Validate(); EditorApplication.update += Tick;
        }
        static void Changed() { changes++; }
        static void Tick()
        {
            try
            {
                if (!Application.isPlaying) throw new InvalidOperationException("Play Mode ended early.");
                EditorApplication.QueuePlayerLoopUpdate(); if (frame == Time.frameCount) return; frame = Time.frameCount;
                if (!steps.MoveNext()) Finish("PASS");
            }
            catch (Exception e) { Finish("FAILED " + e); }
        }
        static void Finish(string result)
        {
            EditorApplication.update -= Tick; GameProgress.Changed -= Changed; steps = null; Status = result;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            if (priorKeyboard != null && priorKeyboard.added) priorKeyboard.MakeCurrent();
            if (priorMouse != null && priorMouse.added) priorMouse.MakeCurrent();
            // Scene loading may unload the original native asset while the clone is active.
            var restored = priorInputSettings;
            if (restored == null && !string.IsNullOrEmpty(priorSettingsPath)) restored = AssetDatabase.LoadAssetAtPath<InputSettings>(priorSettingsPath);
            if (restored == null) { restored = ScriptableObject.CreateInstance<InputSettings>(); JsonUtility.FromJsonOverwrite(priorSettingsJson, restored); }
            InputSystem.settings = restored;
            if (temporaryInputSettings != null) UnityEngine.Object.DestroyImmediate(temporaryInputSettings);
            Application.runInBackground = priorBackground; File.WriteAllLines(Folder + "/results.txt", results.Append(result));
        }
        static IEnumerable<object> Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static void Keys(params Key[] keys) { keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); }
        static IEnumerable<object> Press(Key key)
        {
            Keys(key); yield return null; yield return null; Keys(); yield return null; yield return null;
        }
        static void Pointer(Vector2 p, bool down)
        {
            mouse.MakeCurrent(); InputSystem.QueueStateEvent(mouse, new MouseState { position = p }.WithButton(MouseButton.Left, down));
        }
        static Vector2 Screen(RectTransform rect, Canvas canvas) => RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, rect.position);
        static IEnumerable<object> Click(RectTransform rect, Canvas canvas)
        {
            Canvas.ForceUpdateCanvases(); Vector2 p = Screen(rect, canvas);
            Pointer(p, false); yield return null; yield return null;
            Pointer(p, true); yield return null; yield return null;
            Pointer(p, false); yield return null; yield return null; yield return null;
        }
        static IEnumerable<object> Walk(Vector2Int target)
        {
            float end = Time.realtimeSinceStartup + 45;
            while ((Player.Cell != target || !Player.IsAtRest) && Time.realtimeSinceStartup < end)
            {
                Vector2Int d = target - Player.Cell;
                // Release between grid steps so a slow Editor frame cannot overshoot the waypoint.
                if (Player.IsMoving || d == Vector2Int.zero) Keys(); else Keys(d.x != 0 ? (d.x > 0 ? Key.D : Key.A) : (d.y > 0 ? Key.W : Key.S));
                // Synchronize held movement with physics during Editor-driven stepping.
                // Edge-triggered puzzle inputs keep the ordinary PlayerLoop path.
                InputSystem.Update(); Player.SendMessage("Update");
                yield return null;
            }
            Keys(); yield return null; yield return null;
            Check(Player.Cell == target && Player.IsAtRest, "Normal movement reaches " + target);
        }
        static IEnumerable<object> Approach(int x, int y)
        {
            foreach (var s in Walk(new Vector2Int(Player.Cell.x, 6))) yield return s;
            foreach (var s in Walk(new Vector2Int(x, 6))) yield return s;
            foreach (var s in Walk(new Vector2Int(x, y))) yield return s;
            foreach (var s in Press(Key.W)) yield return s;
        }
        static IEnumerable<object> Travel(IDGSInteractionPoint point)
        {
            foreach (var s in Walk(point.approachCell)) yield return s;
            foreach (var s in Press(point.cell.y > point.approachCell.y ? Key.W : Key.S)) yield return s;
            string target = point.destinationScene;
            foreach (var s in Press(Key.Enter)) yield return s;
            float end = Time.realtimeSinceStartup + 30;
            while ((SceneManager.GetActiveScene().name != target || IDGSSceneTravel.IsChanging) && Time.realtimeSinceStartup < end) yield return null;
            foreach (var s in Frames(5)) yield return s;
            Check(SceneManager.GetActiveScene().name == target, "Door reaches " + target);
        }
        static int IndependentMask(int index)
        {
            int mask = 0;
            for (int j = 0; j < 16; j++)
                if (Math.Abs(j % 4 - index % 4) + Math.Abs(j / 4 - index / 4) <= 1) mask |= 1 << j;
            return mask;
        }
        public static string ModelChecks()
        {
            Directory.CreateDirectory(Folder);
            var masks = Enumerable.Range(0, 16).Select(IndependentMask).ToArray();
            var distances = Enumerable.Repeat(-1, 65536).ToArray(); var queue = new Queue<int>();
            distances[0] = 0; queue.Enqueue(0);
            while (queue.Count > 0)
            {
                int state = queue.Dequeue();
                foreach (int mask in masks)
                {
                    int next = state ^ mask;
                    if (distances[next] >= 0) continue;
                    distances[next] = distances[state] + 1; queue.Enqueue(next);
                }
            }
            var generated = new HashSet<int>();
            for (int seed = 0; seed < 2000; seed++)
            {
                var board = new IDGSVirusBoard(seed); int initial = board.InitialMask;
                int[] witness = board.ScrambleForValidation();
                if (board.IsSolved || distances[initial] < 4 || distances[initial] > 6 || witness.Length < 4 || witness.Length > 6 || witness.Distinct().Count() != witness.Length)
                    throw new InvalidOperationException("Solved, trivial or invalid scramble at seed " + seed);
                generated.Add(initial);
                int independentState = 0; foreach (int i in witness) independentState ^= masks[i];
                if (independentState != initial) throw new InvalidOperationException("Scramble does not reconstruct initial board");
                foreach (int index in witness.Reverse()) board.Toggle(index);
                if (!board.IsSolved) throw new InvalidOperationException("Generated board cannot be restored");
                board.Reset();
                if (board.StateMask != initial || board.Moves != 0) throw new InvalidOperationException("Reset changed original");
                for (int index = 0; index < 16; index++)
                {
                    board.Reset(); board.Toggle(index);
                    if ((board.StateMask ^ initial) != masks[index]) throw new InvalidOperationException("Diagonal, missing neighbor or wraparound at " + index);
                    board.Toggle(index);
                    if (board.StateMask != initial || board.Moves != 2) throw new InvalidOperationException("Toggle is not reversible");
                }
                int saved = board.StateMask, moves = board.Moves;
                if (board.Toggle(-1) || board.Toggle(16) || saved != board.StateMask || board.Moves != moves) throw new InvalidOperationException("Invalid index mutated board");
            }
            if (generated.Count < 100 || distances[masks[0] ^ masks[3] ^ masks[12] ^ masks[15]] < 4) throw new InvalidOperationException("Insufficient variety or trivial fallback");
            string report = "PASS 2000 seeds: independent breadth-first search proves all generated boards need 4–6 moves; original 4–6 distinct legal toggles reconstruct and solve them. All 16 cell masks affect only Manhattan-distance <= 1, with no diagonals or edge wraparound. Reset, double toggles and invalid indices verified. Unique initial states=" + generated.Count;
            File.WriteAllText(Folder + "/generation-results.txt", report); return report;
        }
        static IEnumerable<object> SelectCell(int index)
        {
            int guard = 0;
            while (Virus.SelectedIndex != index && guard++ < 10)
            {
                int current = Virus.SelectedIndex;
                var key = current % 4 != index % 4 ? (current % 4 < index % 4 ? Key.RightArrow : Key.LeftArrow) : (current / 4 < index / 4 ? Key.DownArrow : Key.UpArrow);
                foreach (var s in Press(key)) yield return s;
            }
            Check(Virus.SelectedIndex == index, "Arrow selection reaches cell " + index);
        }
        static IEnumerable<object> Solve()
        {
            int[] solution = Virus.Board.ScrambleForValidation().Reverse().ToArray();
            for (int i = 0; i < solution.Length; i++)
            {
                int index = solution[i];
                if (i % 2 == 0) foreach (var s in Click(Virus.Cells[index].GetComponent<RectTransform>(), Virus.canvas)) yield return s;
                else
                {
                    foreach (var s in SelectCell(index)) yield return s;
                    foreach (var s in Press(i % 4 == 1 ? Key.E : Key.Enter)) yield return s;
                }
                Check(Virus.Board.Moves == i + 1, "Each mouse/key pulse counts exactly once");
                if (i < solution.Length - 1) Check(!Virus.IsSolved, "Infected fragments prevent early victory");
            }
            Check(Virus.Board.IsSolved && Virus.IsSolved && Virus.Status.text == "¡Virus eliminado!", "All 16 cells clean triggers victory");
        }
        static IEnumerable<object> FinishSealIfOpen()
        {
            foreach(var s in Frames(5))yield return s;
            var seal=UnityEngine.Object.FindFirstObjectByType<IDGSCompletion>();
            if(seal==null||!seal.IsOpen)yield break;
            float end=Time.realtimeSinceStartup+15;
            while(!seal.AnimationFinished&&Time.realtimeSinceStartup<end)yield return null;
            Check(seal.AnimationFinished,"Final review animation finishes after exiting CMD");
            foreach(var s in Click(seal.ContinueButton.GetComponent<RectTransform>(),seal.canvas))yield return s;
        }
        static IEnumerator Validate()
        {
            var previous = GameProgress.CaptureCompletedIds();
            var otherFive = IDGSProgressHud.EventIds.Where(id => id != IDGSVirusChallenge.EventId).ToArray();
            Check(otherFive.All(GameProgress.HasEvent), "Previous five minigames were completed by regression run");
            var narrative = previous.Where(id => !IDGSProgressHud.EventIds.Contains(id)).ToArray();
            GameProgress.RestoreCompletedIds(narrative); changes = 0;
            Check(Hud.CompletedCount == 0 && !Virus.TryOpen() && !Virus.HasCompleted, "Virus can start independently and cannot be opened remotely");
            foreach (var s in Approach(12, 7)) yield return s;
            Player.PlaceAtCell(new Vector2Int(12, 7), Vector2Int.left);
            Check(!Virus.TryOpen(), "Correct adjacent cell with wrong facing cannot open virus");
            foreach (var s in Press(Key.W)) yield return s;
            string untouched = PlayerPrefs.GetString(GameProgress.StorageKey);
            foreach (var s in Press(Key.E)) yield return s;
            Check(Virus.IsOpen && Virus.station.pointId == "idgs.cmd.virus" && Virus.Board.Moves == 0 && !Virus.Board.IsSolved, "E opens unsolved 4x4 board from east station");
            Check(!Player.enabled && !Interactor.enabled && !Virus.gameplayHud.activeSelf && !Hud, "Shared modal suspends movement, other interactions and HUD");
            Check(!UnityEngine.Object.FindFirstObjectByType<IDGSFilesChallenge>().TryOpen() && !UnityEngine.Object.FindFirstObjectByType<IDGSServerChallenge>().TryOpen(), "Other CMD activities cannot open over virus");
            Vector2Int cell = Player.Cell;
            Keys(Key.A, Key.LeftShift); foreach (var s in Frames(12)) yield return s; Keys(); yield return null;
            Check(Player.Cell == cell, "Walk and sprint inputs cannot move Mateo");
            int initial = Virus.Board.InitialMask;
            foreach (var s in Click(Virus.Cells[5].GetComponent<RectTransform>(), Virus.canvas)) yield return s;
            int expected = initial ^ IndependentMask(5);
            Check(Virus.Board.StateMask == expected && Virus.Board.Moves == 1 && Virus.SelectedIndex == 5, "Mouse pulse changes center and four orthogonal neighbors");
            foreach (int diagonal in new[] { 0, 2, 8, 10 }) Check(((initial ^ Virus.Board.StateMask) & (1 << diagonal)) == 0, "Diagonal " + diagonal + " stays unchanged");
            foreach (var s in SelectCell(0)) yield return s;
            foreach (var s in Press(Key.E)) yield return s;
            Check(Virus.Board.StateMask == (expected ^ IndependentMask(0)) && Virus.Board.Moves == 2, "E changes corner with only its two valid neighbors");
            foreach (var s in Press(Key.Enter)) yield return s;
            Check(Virus.Board.StateMask == expected && Virus.Board.Moves == 3, "Enter repeats the selected pulse reversibly");
            foreach (var s in Press(Key.R)) yield return s;
            Check(Virus.Board.StateMask == initial && Virus.Board.InitialMask == initial && Virus.Board.Moves == 0, "R restores exactly the original board and zero moves");
            foreach (var s in Press(Key.LeftArrow)) yield return s;
            foreach (var s in Press(Key.UpArrow)) yield return s;
            Check(Virus.SelectedIndex == 0 && Virus.Board.Moves == 0, "Top and left selection boundaries clamp without pulsing");
            foreach (var s in SelectCell(15)) yield return s;
            foreach (var s in Press(Key.RightArrow)) yield return s;
            foreach (var s in Press(Key.DownArrow)) yield return s;
            Check(Virus.SelectedIndex == 15, "Bottom and right selection boundaries clamp");
            foreach (var s in Click(Virus.Cells[1].GetComponent<RectTransform>(), Virus.canvas)) yield return s;
            Check(Virus.Board.StateMask == (initial ^ IndependentMask(1)), "Top edge pulse has no row wrap or diagonal");
            foreach (var s in Click(Virus.ResetButton.GetComponent<RectTransform>(), Virus.canvas)) yield return s;
            Check(Virus.Board.StateMask == initial && Virus.Board.Moves == 0, "Mouse reset restores the same original board without penalty");
            foreach (var s in Press(Key.Escape)) yield return s;
            Check(!Virus.IsOpen && !GameProgress.HasEvent(IDGSVirusChallenge.EventId) && untouched == PlayerPrefs.GetString(GameProgress.StorageKey), "Canceling before victory neither awards nor saves partial moves");
            foreach (var s in Press(Key.Enter)) yield return s;
            Check(Virus.IsOpen && Virus.Board.Moves == 0, "Enter reopens east station with a fresh unsolved board");
            IDGSVegetationValidation.Capture(Camera.main, Folder + "/elimina-virus.png", 1280, 720); yield return null; Canvas.ForceUpdateCanvases(); yield return null;
            foreach (var s in Solve()) yield return s;
            Check(Virus.RewardGrantedThisRun && changes == 1 && GameProgress.CaptureCompletedIds().Count(id => id == IDGSVirusChallenge.EventId) == 1, "Exact idgs.cmd.virus event registered and saved once");
            int solvedMask = Virus.Board.StateMask, solvedMoves = Virus.Board.Moves;
            Check(!Virus.TryPulse(0), "Completed board rejects extra pulses"); Virus.ResetBoard();
            Check(Virus.Board.StateMask == solvedMask && Virus.Board.Moves == solvedMoves, "Completed board cannot reset into another reward");
            IDGSVegetationValidation.Capture(Camera.main, Folder + "/virus-eliminado.png", 1280, 720); yield return null;
            foreach (var s in Click(Virus.FinishButton.GetComponent<RectTransform>(), Virus.canvas)) yield return s;
            Check(Player.enabled && Interactor.enabled && Hud.CompletedCount == 1 && Virus.stationState.text.Contains("COMPLETADO"), "Closing restores controls, HUD 1/6 and completed station");
            // Restore the actual results of the preceding regression, only inside this test slot.
            GameProgress.RestoreCompletedIds(previous.Concat(new[] { IDGSVirusChallenge.EventId }));
            Check(Hud.CompletedCount == 6 && Hud.Label.text == "Sistemas restaurados: 6/6", "All six independently registered events contribute to HUD");
            string saved = PlayerPrefs.GetString(GameProgress.StorageKey); int eventChanges = changes;
            foreach (var s in Press(Key.E)) yield return s;
            Check(Virus.IsOpen && !Virus.IsSolved && Virus.Board.Moves == 0, "Completed station allows a fresh replay");
            foreach (var s in Solve()) yield return s;
            Check(!Virus.RewardGrantedThisRun && changes == eventChanges && saved == PlayerPrefs.GetString(GameProgress.StorageKey), "Replay wins without duplicated event, reward or JSON rewrite");
            foreach (var s in Press(Key.Escape)) yield return s;
            foreach (var s in Walk(new Vector2Int(12, 6))) yield return s;
            foreach (var s in Walk(new Vector2Int(8, 6))) yield return s;
            foreach (var s in Travel(Interactor.points.Single(p => p.kind == IDGSPointKind.Exit))) yield return s;
            foreach (var s in FinishSealIfOpen()) yield return s;
            Check(Hud.CompletedCount == 6, "Exterior restores HUD 6/6 from shared progress");
            foreach (var s in Travel(Interactor.points.Single(p => p.pointId == "idgs.cmd"))) yield return s;
            Check(Virus.HasCompleted && Virus.stationState.text.Contains("COMPLETADO") && Hud.CompletedCount == 6, "Reentering CMD restores virus and all previous activities");
            Check(GameProgress.HasEvent(IDGSCompletion.SealEvent) && GameProgress.HasEvent(IDGSCompletion.LevelEvent), "Completed exit review persists the seal and level completion");
            Check(narrative.All(GameProgress.HasEvent) && GameProgress.PlayerName == "Prueba IDGS", "Documents, narrative and custom name remain intact");
            Check(PlayerPrefs.HasKey(GameProgress.StorageKey + ".backup"), "Shared save maintains its backup in the temporary slot");
            IDGSVegetationValidation.Capture(Camera.main, Folder + "/hud-seis.png", 1280, 720); yield return null;
            File.WriteAllText(Folder + "/completed-test-save.json", PlayerPrefs.GetString(GameProgress.StorageKey));
            Check(!EditorUtility.scriptCompilationFailed, "Project compiles without errors");
        }
        public static string VerifyRestart()
        {
            if (!Application.isPlaying || !GameProgress.StorageKey.StartsWith("CampusQuest.Test.IDGSInteriors.", StringComparison.Ordinal)) throw new InvalidOperationException("Isolated Play Mode required.");
            if (!Virus.HasCompleted || !Virus.stationState.text.Contains("COMPLETADO") || Hud.CompletedCount != 6 || PlayerPrefs.GetString(GameProgress.StorageKey) != File.ReadAllText(Folder + "/completed-test-save.json")) throw new InvalidOperationException("Progress did not restore.");
            File.AppendAllText(Folder + "/results.txt", "PASS Real Play Mode restart restores all six activities and HUD\n");
            return "All six activities and HUD restored after Play Mode restart";
        }
    }
}
#endif
