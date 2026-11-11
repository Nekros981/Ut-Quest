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
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace CampusQuest.EditorTools
{
    public static class IDGSCircuitValidation
    {
        private const string Folder = "tmp/idgs-circuit";
        public static string Status { get; private set; } = "Not started";
        private static IEnumerator steps;
        private static Keyboard keyboard, previousKeyboard;
        private static Mouse mouse, previousMouse;
        private static bool previousBackground;
        private static int frame, changes;
        private static readonly List<string> results = new List<string>();
        private static IDGSCircuitChallenge Challenge => UnityEngine.Object.FindFirstObjectByType<IDGSCircuitChallenge>();
        private static IDGSPlayerMotor Player => UnityEngine.Object.FindFirstObjectByType<IDGSPlayerMotor>();
        private static IDGSInteractionController Interactor => UnityEngine.Object.FindFirstObjectByType<IDGSInteractionController>();

        public static string ModelChecks()
        {
            Directory.CreateDirectory(Folder);
            for (int seed = 0; seed < 2000; seed++)
            {
                var board = new IDGSCircuitBoard(seed);
                if (board.IsSolved) throw new InvalidOperationException("Generated solved board " + seed);
                for (int index = 0; index < 9; index++)
                {
                    int original = board.MaskAt(index);
                    int ports = Enumerable.Range(0, 4).Count(bit => (original & (1 << bit)) != 0);
                    if (ports != 2) throw new InvalidOperationException("Non straight/corner piece.");
                    for (int i = 0; i < 4; i++) board.Rotate(index);
                    if (board.MaskAt(index) != original) throw new InvalidOperationException("Four turns changed a piece.");
                    int turns = 0;
                    while (board.MaskAt(index) != board.SolutionMaskForValidation(index) && turns++ < 4) board.Rotate(index);
                }
                if (!board.IsSolved || board.PoweredCount >= 9) throw new InvalidOperationException("Unsolvable or all-pieces-only board " + seed);
                while ((board.MaskAt(IDGSCircuitBoard.Entry) & IDGSCircuitBoard.West) != 0) board.Rotate(IDGSCircuitBoard.Entry);
                if (board.IsSolved || board.PoweredCount != 0) throw new InvalidOperationException("Power crosses a closed input.");
            }
            File.WriteAllText(Folder + "/generation-results.txt", "PASS 2000 seeds start unsolved, use straight/corner pieces, restore their guaranteed solutions, and win without all nine pieces. Closed input cannot energize the board.");
            return "2000 generated boards verified with reversible solution witnesses";
        }
        public static string Prepare()
        {
            string result = IDGSInteriorValidation.Prepare();
            File.Copy("tmp/idgs-interiors/validation-context.json", Folder + "/profile-backup.json", true);
            string key = GameProgress.StorageKey;
            string fixture = PlayerPrefs.GetString(key).Replace("idgs.docencia.cables.completed", IDGSCablesChallenge.EventId);
            PlayerPrefs.SetString(key, fixture); PlayerPrefs.Save();
            return result;
        }
        private static void Check(bool valid, string message)
        {
            if (!valid) throw new InvalidOperationException(message);
            results.Add("PASS " + message);
        }
        public static void Begin()
        {
            if (!Application.isPlaying || steps != null || !GameProgress.StorageKey.StartsWith("CampusQuest.Test.IDGSInteriors.", StringComparison.Ordinal))
                throw new InvalidOperationException("Isolated Docencia Play Mode required.");
            GameProgress.EnsureLoaded();
            previousKeyboard = Keyboard.current; previousMouse = Mouse.current;
            keyboard = InputSystem.AddDevice<Keyboard>("CircuitKeyboard");
            mouse = InputSystem.AddDevice<Mouse>("CircuitMouse");
            previousBackground = Application.runInBackground; Application.runInBackground = true;
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Show();
            results.Clear(); frame = -1; changes = 0; Status = "Running";
            GameProgress.Changed += Changed;
            steps = Validate(); EditorApplication.update += Tick;
        }
        private static void Changed() { changes++; }
        private static void Tick()
        {
            try
            {
                if (!Application.isPlaying) throw new InvalidOperationException("Play Mode ended.");
                EditorApplication.QueuePlayerLoopUpdate();
                if (frame == Time.frameCount) return;
                frame = Time.frameCount;
                if (!steps.MoveNext()) Finish("PASS");
            }
            catch (Exception error) { Finish("FAILED " + error); }
        }
        private static void Finish(string status)
        {
            EditorApplication.update -= Tick; steps = null; Status = status;
            GameProgress.Changed -= Changed;
            Keys();
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (previousMouse != null && previousMouse.added) previousMouse.MakeCurrent();
            if (previousKeyboard != null && previousKeyboard.added) previousKeyboard.MakeCurrent();
            Application.runInBackground = previousBackground;
            File.WriteAllLines(Folder + "/results.txt", results.Append(status));
        }
        private static void Keys(params Key[] keys)
        {
            keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); InputSystem.Update();
            if (Player != null && Player.enabled) Player.SendMessage("Update");
        }
        private static void Pointer(Vector2 position, bool pressed)
        {
            mouse.MakeCurrent();
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left, pressed));
            InputSystem.Update();
            if (EventSystem.current != null && EventSystem.current.currentInputModule != null) EventSystem.current.currentInputModule.Process();
        }
        private static IEnumerable<object> Press(Key key)
        {
            Keys(key); Challenge.SendMessage("Update"); Keys(); yield return null; yield return null;
        }
        private static IEnumerable<object> Click(RectTransform target)
        {
            Canvas.ForceUpdateCanvases();
            Vector2 point = RectTransformUtility.WorldToScreenPoint(Challenge.canvas.worldCamera, target.position);
            Pointer(point, false); yield return null;
            Pointer(point, true); yield return null;
            Pointer(point, false); yield return null; yield return null;
        }
        private static IEnumerable<object> Walk(Vector2Int goal)
        {
            float end = Time.realtimeSinceStartup + 12;
            while ((Player.Cell != goal || !Player.IsAtRest) && Time.realtimeSinceStartup < end)
            {
                var delta = goal - Player.Cell;
                Keys(delta.x != 0 ? (delta.x > 0 ? Key.D : Key.A) : (delta.y > 0 ? Key.W : Key.S)); yield return null;
            }
            Keys(); yield return null;
            Check(Player.Cell == goal && Player.IsAtRest, "Grid movement reaches " + goal);
        }
        private static IEnumerable<object> Open(Key key)
        {
            Keys(Key.W); yield return null; Keys(); yield return null;
            Keys(key); Interactor.SendMessage("Update"); Keys(); yield return null; yield return null;
            Check(Challenge.IsOpen && !Challenge.Board.IsSolved, key + " opens an unsolved central station board");
        }
        private static IEnumerable<object> Select(int index)
        {
            while (Challenge.SelectedIndex % 3 != index % 3)
                foreach (var step in Press(Challenge.SelectedIndex % 3 < index % 3 ? Key.RightArrow : Key.LeftArrow)) yield return step;
            while (Challenge.SelectedIndex / 3 != index / 3)
                foreach (var step in Press(Challenge.SelectedIndex / 3 < index / 3 ? Key.DownArrow : Key.UpArrow)) yield return step;
        }
        private static IEnumerable<object> Solve()
        {
            // Keep the source for last; accept any earlier valid route as a legitimate solution.
            foreach (int index in new[] { 0, 1, 2, 4, 5, 6, 7, 8, 3 })
            {
                int attempts = 0;
                while (!Challenge.IsSolved && Challenge.Board.MaskAt(index) != Challenge.Board.SolutionMaskForValidation(index) && attempts++ < 4)
                {
                    if (index % 2 == 0)
                        foreach (var step in Click(Challenge.tiles[index].rectTransform)) yield return step;
                    else
                    {
                        foreach (var step in Select(index)) yield return step;
                        foreach (var step in Press(index % 3 == 0 ? Key.Enter : Key.E)) yield return step;
                    }
                }
            }
            Check(Challenge.IsSolved && Challenge.Board.IsSolved && Challenge.Board.PoweredCount < 9, "Continuous source-to-output route wins without all nine pieces");
        }
        private static IEnumerable<object> Travel(IDGSInteractionPoint point)
        {
            foreach (var step in Walk(point.approachCell)) yield return step;
            Keys(point.cell.y > point.approachCell.y ? Key.W : Key.S); yield return null; Keys(); yield return null;
            string target = point.destinationScene;
            Keys(Key.Enter); Interactor.SendMessage("Update");
            float end = Time.realtimeSinceStartup + 20;
            while ((SceneManager.GetActiveScene().name != target || IDGSSceneTravel.IsChanging) && Time.realtimeSinceStartup < end) yield return null;
            Check(SceneManager.GetActiveScene().name == target, "Portal reaches " + target);
            Keys(); yield return null; yield return null;
        }
        private static IEnumerator Validate()
        {
            Check(!Challenge.TryOpen() && !Challenge.HasCompleted, "Cannot open from arrival point; isolated circuit progress starts pending");
            foreach (var step in Walk(new Vector2Int(8, 8))) yield return step;
            foreach (var step in Open(Key.E)) yield return step;
            int retry = 0;
            while ((Challenge.Board.MaskAt(IDGSCircuitBoard.Entry) & IDGSCircuitBoard.West) != 0 && retry++ < 32)
            {
                foreach (var step in Press(Key.Escape)) yield return step;
                foreach (var step in Open(Key.Enter)) yield return step;
            }
            Check((Challenge.Board.MaskAt(IDGSCircuitBoard.Entry) & IDGSCircuitBoard.West) == 0, "Unpowered starting state available through normal reopening");
            var position = Player.transform.position;
            for (int i = 0; i < 12; i++) { Keys(Key.D, Key.LeftShift); yield return null; }
            Keys();
            Check(!Player.enabled && !Interactor.enabled && Vector2.Distance(position, Player.transform.position) < .001f, "Modal blocks walking, running and other interactions");
            Check(!UnityEngine.Object.FindFirstObjectByType<IDGSCablesChallenge>().TryOpen(), "Cables cannot open while circuit is active");
            int before = Challenge.Board.MaskAt(0), moves = Challenge.Moves;
            foreach (var step in Click(Challenge.tiles[0].rectTransform)) yield return step;
            Check(Challenge.Board.MaskAt(0) == IDGSCircuitBoard.Turn(before, 1) && Challenge.Moves == moves + 1 && Challenge.SelectedIndex == 0, "Mouse click selects and turns a piece exactly 90 degrees");
            foreach (var step in Select(4)) yield return step;
            Check(Challenge.SelectedIndex == 4, "Arrow keys select the central piece");
            before = Challenge.Board.MaskAt(4);
            foreach (var step in Press(Key.E)) yield return step;
            Check(Challenge.Board.MaskAt(4) == IDGSCircuitBoard.Turn(before, 1), "E turns the selected piece");
            before = Challenge.Board.MaskAt(4);
            foreach (var step in Press(Key.Enter)) yield return step;
            Check(Challenge.Board.MaskAt(4) == IDGSCircuitBoard.Turn(before, 1), "Enter turns the selected piece");
            Check(!Challenge.IsSolved && changes == 0 && !GameProgress.HasEvent(IDGSCircuitChallenge.EventId), "Disconnected pieces do not win or save progress");
            IDGSVegetationValidation.Capture(Camera.main, Folder + "/circuito.png", 1280, 720);
            yield return null; Canvas.ForceUpdateCanvases(); yield return null;
            foreach (var step in Press(Key.Escape)) yield return step;
            Check(Player.enabled && Interactor.enabled && !Challenge.IsOpen, "Escape restores normal controls");
            foreach (var step in Open(Key.Enter)) yield return step;
            foreach (var step in Solve()) yield return step;
            Check(Challenge.status.text == "¡Circuito reparado!" && Challenge.RewardGrantedThisRun, "First victory shows success and grants completion");
            Check(changes == 1 && GameProgress.CaptureCompletedIds().Count(id => id == IDGSCircuitChallenge.EventId) == 1, "Exact circuito event stored once");
            Check(GameProgress.HasEvent(IDGSCablesChallenge.EventId) && GameProgress.HasEvent("rectoria.document.payment_receipt") && GameProgress.PlayerName == "Prueba IDGS", "Existing cable completion, tutorial documents and name preserved");
            IDGSVegetationValidation.Capture(Camera.main, Folder + "/reparado.png", 1280, 720);
            yield return null; Canvas.ForceUpdateCanvases(); yield return null;
            string json = PlayerPrefs.GetString(GameProgress.StorageKey);
            foreach (var step in Click(Challenge.finishButton.GetComponent<RectTransform>())) yield return step;
            Check(!Challenge.IsOpen && Challenge.stationState.text.Contains("COMPLETADO"), "Station marker updates and return button restores movement");
            foreach (var step in Travel(Interactor.points.Single(p => p.kind == IDGSPointKind.Exit))) yield return step;
            foreach (var step in Travel(Interactor.points.Single(p => p.pointId == "idgs.docencia"))) yield return step;
            Check(Challenge.HasCompleted && Challenge.stationState.text.Contains("COMPLETADO"), "Station completion restores after leaving and returning");
            foreach (var step in Walk(new Vector2Int(8, 8))) yield return step;
            foreach (var step in Open(Key.E)) yield return step;
            foreach (var step in Solve()) yield return step;
            Check(!Challenge.RewardGrantedThisRun && changes == 1 && PlayerPrefs.GetString(GameProgress.StorageKey) == json, "Replay wins without duplicate reward or save rewrite");
            foreach (var step in Click(Challenge.finishButton.GetComponent<RectTransform>())) yield return step;
            Check(!EditorUtility.scriptCompilationFailed, "Project compiles");
            File.WriteAllText(Folder + "/completed-test-save.json", json);
        }
        public static string VerifyRestart()
        {
            if (!Application.isPlaying || !GameProgress.StorageKey.StartsWith("CampusQuest.Test.IDGSInteriors.", StringComparison.Ordinal)) throw new InvalidOperationException("Isolated Play Mode required.");
            if (!Challenge.HasCompleted || !Challenge.stationState.text.Contains("COMPLETADO") || PlayerPrefs.GetString(GameProgress.StorageKey) != File.ReadAllText(Folder + "/completed-test-save.json"))
                throw new InvalidOperationException("Circuit completion did not restore.");
            File.AppendAllText(Folder + "/results.txt", "PASS Real Play Mode restart restores circuit completion\n");
            return "Circuit completion restored after a real Play Mode restart";
        }
    }
}
#endif
