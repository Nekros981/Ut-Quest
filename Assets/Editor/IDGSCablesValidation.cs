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
    public static class IDGSCablesValidation
    {
        private const string Folder = "tmp/idgs-cables";
        public static string Status { get; private set; } = "Not started";
        private static IEnumerator steps;
        private static int frame;
        private static Keyboard keyboard, previousKeyboard;
        private static Mouse mouse, previousMouse;
        private static bool previousBackground;
        private static int changes;
        private static readonly List<string> results = new List<string>();
        private static IDGSCablesChallenge Challenge => UnityEngine.Object.FindFirstObjectByType<IDGSCablesChallenge>();
        private static IDGSPlayerMotor Player => UnityEngine.Object.FindFirstObjectByType<IDGSPlayerMotor>();
        private static IDGSInteractionController Interactor => UnityEngine.Object.FindFirstObjectByType<IDGSInteractionController>();

        public static string Prepare()
        {
            string prepared = IDGSInteriorValidation.Prepare();
            Directory.CreateDirectory(Folder);
            File.Copy("tmp/idgs-interiors/validation-context.json", Folder + "/profile-backup.json", true);
            return prepared;
        }
        private static void Check(bool valid, string message)
        {
            if (!valid) throw new InvalidOperationException(message);
            results.Add("PASS " + message);
        }
        public static void Begin()
        {
            if (!Application.isPlaying || steps != null || !GameProgress.StorageKey.StartsWith("CampusQuest.Test.IDGSInteriors.", StringComparison.Ordinal))
                throw new InvalidOperationException("Requires isolated Docencia Play Mode.");
            GameProgress.EnsureLoaded();
            previousKeyboard = Keyboard.current; previousMouse = Mouse.current;
            keyboard = InputSystem.AddDevice<Keyboard>("CablesKeyboard");
            mouse = InputSystem.AddDevice<Mouse>("CablesMouse");
            previousBackground = Application.runInBackground; Application.runInBackground = true;
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Show();
            results.Clear(); changes = 0; frame = -1; Status = "Running";
            GameProgress.Changed += CountChange;
            steps = Validate(); EditorApplication.update += Tick;
        }
        private static void CountChange() { changes++; }
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
            GameProgress.Changed -= CountChange;
            KeyInput();
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (previousMouse != null && previousMouse.added) previousMouse.MakeCurrent();
            if (previousKeyboard != null && previousKeyboard.added) previousKeyboard.MakeCurrent();
            Application.runInBackground = previousBackground;
            File.WriteAllLines(Folder + "/results.txt", results.Append(status));
        }
        private static void KeyInput(params Key[] keys)
        {
            if (keyboard == null || !keyboard.added) return;
            keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); InputSystem.Update();
            if (Player != null && Player.enabled) Player.SendMessage("Update");
        }
        private static void MouseInput(Vector2 position, bool pressed)
        {
            mouse.MakeCurrent();
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left, pressed));
            InputSystem.Update();
            if (EventSystem.current != null && EventSystem.current.currentInputModule != null) EventSystem.current.currentInputModule.Process();
        }
        private static Vector2 Screen(RectTransform rect) => RectTransformUtility.WorldToScreenPoint(Challenge.canvas.worldCamera, rect.position);
        private static IEnumerable<object> Walk(Vector2Int goal)
        {
            float deadline = Time.realtimeSinceStartup + 12;
            while ((Player.Cell != goal || !Player.IsAtRest) && Time.realtimeSinceStartup < deadline)
            {
                Vector2Int delta = goal - Player.Cell;
                KeyInput(delta.x != 0 ? (delta.x > 0 ? Key.D : Key.A) : (delta.y > 0 ? Key.W : Key.S));
                yield return null;
            }
            KeyInput(); yield return null;
            Check(Player.Cell == goal && Player.IsAtRest, "Keyboard grid movement reaches " + goal);
        }
        private static IEnumerable<object> ApproachStation()
        {
            foreach (var step in Walk(new Vector2Int(8, 6))) yield return step;
            foreach (var step in Walk(new Vector2Int(3, 6))) yield return step;
            foreach (var step in Walk(new Vector2Int(3, 7))) yield return step;
            KeyInput(Key.W); yield return null; KeyInput(); yield return null;
        }
        private static IEnumerable<object> Open(Key key)
        {
            KeyInput(); yield return null;
            KeyInput(key); Interactor.SendMessage("Update"); yield return null;
            KeyInput(); yield return null;
            Check(Challenge.IsOpen, key + " opens the west station panel");
            Check(Enumerable.Range(0, 4).All(i => Challenge.ColorAtRight(i) != i), "All right connectors are shuffled away from their original rows");
        }
        private static IEnumerable<object> Drag(int cable, int slot, bool expectCorrect)
        {
            var source = Challenge.leftSockets[cable].transform.parent.GetComponent<RectTransform>();
            Vector2 start = Screen(source), end = Screen(Challenge.rightTargets[slot]);
            MouseInput(start, false); yield return null;
            MouseInput(start, true); yield return null;
            for (int i = 1; i <= 8; i++)
            {
                MouseInput(Vector2.Lerp(start, end, i / 8f), true);
                yield return null;
                if (i == 4) Check(Challenge.DraggingIndex == cable, "Mouse drag follows cable " + cable);
            }
            MouseInput(end, false); yield return null; yield return null;
            Check(Challenge.DraggingIndex == -1 && Challenge.IsConnected(cable) == expectCorrect,
                expectCorrect ? "Matching connector locks cable " + cable : "Wrong connector returns cable " + cable + " to its origin");
        }
        private static IEnumerable<object> Click(RectTransform rect)
        {
            Vector2 position = Screen(rect);
            MouseInput(position, false); yield return null;
            MouseInput(position, true); yield return null;
            MouseInput(position, false); yield return null; yield return null;
        }
        private static IEnumerable<object> Travel(IDGSInteractionPoint point)
        {
            foreach (var step in Walk(point.approachCell)) yield return step;
            KeyInput(point.cell.y > point.approachCell.y ? Key.W : Key.S); yield return null;
            KeyInput(); yield return null;
            string target = point.destinationScene;
            KeyInput(Key.Enter); Interactor.SendMessage("Update");
            float deadline = Time.realtimeSinceStartup + 20;
            while ((SceneManager.GetActiveScene().name != target || IDGSSceneTravel.IsChanging) && Time.realtimeSinceStartup < deadline) yield return null;
            Check(SceneManager.GetActiveScene().name == target, "Actual portal transition reaches " + target);
            KeyInput(); yield return null; yield return null;
        }
        private static IEnumerator Validate()
        {
            Check(SceneManager.GetActiveScene().name == "IDGS_Docencia_Interior", "Started in the real Docencia scene");
            Check(!Challenge.HasCompleted, "Isolated fixture starts with the exact cables event pending");
            Check(!Challenge.TryOpen(), "Opening from the arrival cell is rejected");
            foreach (var step in ApproachStation()) yield return step;
            foreach (var step in Open(Key.E)) yield return step;
            Vector2 position = Player.transform.position;
            for (int i = 0; i < 12; i++) { KeyInput(Key.W, Key.LeftShift); yield return null; }
            KeyInput();
            Check(Vector2.Distance(Player.transform.position, position) < .001f && !Player.enabled && !Interactor.enabled, "Mateo cannot move or run while cables are open");
            Check(!Interactor.TryInteract(), "Other station interactions are blocked");
            foreach (var step in Drag(0, Challenge.RightSlotFor(1), false)) yield return step;
            Check(Challenge.ConnectedCount == 0 && !GameProgress.HasEvent(IDGSCablesChallenge.EventId), "Incorrect drop grants no progress");
            foreach (var step in Drag(0, Challenge.RightSlotFor(0), true)) yield return step;
            Check(Challenge.ConnectedCount == 1 && !Challenge.IsSolved, "One cable is insufficient");
            KeyInput(Key.Escape); Challenge.SendMessage("Update"); yield return null; KeyInput(); yield return null;
            Check(!Challenge.IsOpen && Player.enabled && Interactor.enabled && changes == 0, "Escape restores controls and leaves an incomplete activity pending");
            foreach (var step in Open(Key.Enter)) yield return step;
            Check(Challenge.ConnectedCount == 0, "Reopening resets the incomplete board");
            for (int cable = 0; cable < 3; cable++)
                foreach (var step in Drag(cable, Challenge.RightSlotFor(cable), true)) yield return step;
            Check(Challenge.ConnectedCount == 3 && !Challenge.IsSolved && !GameProgress.HasEvent(IDGSCablesChallenge.EventId), "Three cables cannot win or register the event");
            IDGSVegetationValidation.Capture(Camera.main, Folder + "/cables.png", 1280, 720);
            yield return null; Canvas.ForceUpdateCanvases(); yield return null;
            foreach (var step in Drag(3, Challenge.RightSlotFor(3), true)) yield return step;
            Check(Challenge.IsSolved && Challenge.ConnectedCount == 4 && Challenge.RewardGrantedThisRun, "All four cables finish the activity for the first time");
            Check(Challenge.status.text == "¡Conexión restablecida!", "Success message is shown");
            Check(changes == 1 && GameProgress.CaptureCompletedIds().Count(id => id == IDGSCablesChallenge.EventId) == 1, "Exact event is stored once with one progress notification");
            IDGSVegetationValidation.Capture(Camera.main, Folder + "/restablecida.png", 1280, 720);
            yield return null; Canvas.ForceUpdateCanvases(); yield return null;
            string completedJson = PlayerPrefs.GetString(GameProgress.StorageKey);
            foreach (var step in Click(Challenge.finishButton.GetComponent<RectTransform>())) yield return step;
            Check(!Challenge.IsOpen && Player.enabled && Challenge.stationState.text.Contains("COMPLETADO"), "Return button restores the room and its completed station marker");
            foreach (var step in Walk(new Vector2Int(3, 6))) yield return step;
            foreach (var step in Walk(new Vector2Int(8, 6))) yield return step;
            foreach (var step in Travel(Interactor.points.Single(p => p.kind == IDGSPointKind.Exit))) yield return step;
            foreach (var step in Travel(Interactor.points.Single(p => p.pointId == "idgs.docencia"))) yield return step;
            Check(Challenge.HasCompleted && Challenge.stationState.text.Contains("COMPLETADO"), "Completion marker restores after leaving Docencia and returning");
            Check(PlayerPrefs.GetString(GameProgress.StorageKey) == completedJson, "Scene transitions preserve the saved activity exactly");
            foreach (var step in ApproachStation()) yield return step;
            foreach (var step in Open(Key.E)) yield return step;
            for (int cable = 0; cable < 4; cable++)
                foreach (var step in Drag(cable, Challenge.RightSlotFor(cable), true)) yield return step;
            Check(Challenge.IsSolved && !Challenge.RewardGrantedThisRun && changes == 1, "Repeat play succeeds without another reward or progress notification");
            Check(PlayerPrefs.GetString(GameProgress.StorageKey) == completedJson, "Repeat victory does not rewrite or duplicate the saved reward");
            foreach (var step in Click(Challenge.finishButton.GetComponent<RectTransform>())) yield return step;
            Check(!EditorUtility.scriptCompilationFailed, "Project compiles");
            File.WriteAllText(Folder + "/completed-test-save.json", completedJson);
        }
        public static string VerifyRestart()
        {
            if (!Application.isPlaying || !GameProgress.StorageKey.StartsWith("CampusQuest.Test.IDGSInteriors.", StringComparison.Ordinal))
                throw new InvalidOperationException("Isolated Play Mode required.");
            bool valid = Challenge.HasCompleted && Challenge.stationState.text.Contains("COMPLETADO")
                && PlayerPrefs.GetString(GameProgress.StorageKey) == File.ReadAllText(Folder + "/completed-test-save.json");
            if (!valid) throw new InvalidOperationException("Restart did not restore activity.");
            File.AppendAllText(Folder + "/results.txt", "PASS Real Play Mode restart restores activity and station marker\n");
            return "Real Play Mode restart restored the completed activity";
        }
    }
}
#endif
