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
    public static class IDGSServerValidation
    {
        const string Folder = "tmp/idgs-server";
        public static string Status { get; private set; } = "Not started";
        static IEnumerator steps;
        static readonly List<string> results = new List<string>();
        static Keyboard keyboard, priorKeyboard;
        static Mouse mouse, priorMouse;
        static InputSettings priorInputSettings, temporaryInputSettings;
        static string priorSettingsPath, priorSettingsJson;
        static bool priorBackground;
        static int frame, changes;
        static IDGSServerChallenge Server => UnityEngine.Object.FindFirstObjectByType<IDGSServerChallenge>();
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
            keyboard = InputSystem.AddDevice<Keyboard>("ServerValidationKeyboard"); mouse = InputSystem.AddDevice<Mouse>("ServerValidationMouse");
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
        static IEnumerable<object> Drag(RectTransform from, RectTransform to, Canvas canvas)
        {
            Canvas.ForceUpdateCanvases(); Vector2 a = Screen(from, canvas), b = Screen(to, canvas);
            Pointer(a, false); yield return null; Pointer(a, true); yield return null;
            for (int i = 1; i <= 8; i++) { Pointer(Vector2.Lerp(a, b, i / 8f), true); yield return null; }
            Pointer(b, false); yield return null; yield return null;
        }
        static IEnumerable<object> Walk(Vector2Int target)
        {
            float end = Time.realtimeSinceStartup + 45;
            while ((Player.Cell != target || !Player.IsAtRest) && Time.realtimeSinceStartup < end)
            {
                Vector2Int d = target - Player.Cell;
                if (d == Vector2Int.zero) Keys(); else Keys(d.x != 0 ? (d.x > 0 ? Key.D : Key.A) : (d.y > 0 ? Key.W : Key.S));
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
        static IEnumerable<object> Load(string name)
        {
            var load = SceneManager.LoadSceneAsync(name); float end = Time.realtimeSinceStartup + 30;
            while (!load.isDone && Time.realtimeSinceStartup < end) yield return null;
            foreach (var s in Frames(5)) yield return s;
            Check(load.isDone && SceneManager.GetActiveScene().name == name, "Loads " + name);
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
        static IEnumerable<object> WaitPosition(float low, float high)
        {
            float end = Time.realtimeSinceStartup + 10;
            while ((!Server.CanCalibrate || Server.IndicatorPosition < low || Server.IndicatorPosition > high) && Time.realtimeSinceStartup < end) yield return null;
            Check(Server.CanCalibrate && Server.IndicatorPosition >= low && Server.IndicatorPosition <= high, "Moving indicator reaches requested timing window");
        }
        static IEnumerable<object> SolveServer(bool errors)
        {
            for (int module = 0; module < 3; module++)
            {
                Check((IDGSServerChallenge.ZoneEnd - IDGSServerChallenge.ZoneStart) / IDGSServerChallenge.ModuleSpeed(module) > .7f, "Module " + (module + 1) + " has over 700 ms to hit");
                if (module > 0) Check(IDGSServerChallenge.ModuleSpeed(module) > IDGSServerChallenge.ModuleSpeed(module - 1), "Next module is slightly faster");
                if (errors)
                {
                    foreach (var s in WaitPosition(module == 1 ? .91f : .12f, module == 1 ? .95f : .18f)) yield return s;
                    foreach (var s in Press(Key.Space)) yield return s;
                    Check(Server.Phase == IDGSServerPhase.Retry && Server.CalibratedCount == module, "Miss resets only module " + (module + 1));
                    Check(!Server.CalibrateButton.interactable && !Server.TryCalibrate(), "Feedback ignores additional attempts");
                }
                foreach (var s in WaitPosition(.63f, .69f)) yield return s;
                Check(!Server.Status.text.Contains("Fuera de rango"), "Retry message clears before the next active attempt");
                if (module == 2 && errors)
                {
                    IDGSVegetationValidation.Capture(Camera.main, Folder + "/calibrar-servidor.png", 1280, 720);
                    yield return null; Canvas.ForceUpdateCanvases(); yield return null;
                    // Capture can pause the game; wait for a fresh safe window before input.
                    foreach (var s in WaitPosition(.63f, .69f)) yield return s;
                }
                if (module == 1) foreach (var s in Click(Server.CalibrateButton.GetComponent<RectTransform>(), Server.canvas)) yield return s;
                else foreach (var s in Press(Key.Space)) yield return s;
                Check(Server.CalibratedCount == module + 1, "Correct " + (module == 1 ? "mouse click" : "Space") + " calibrates module " + (module + 1));
                if (module < 2) Check(!Server.IsSolved, "All three modules are required");
            }
            Check(Server.IsSolved && Server.Status.text == "¡Servidor calibrado!", "All modules activate the server and show success");
        }
        static IEnumerable<object> RegressionFiles()
        {
            foreach (var s in Approach(3, 7)) yield return s;
            foreach (var s in Press(Key.Enter)) yield return s;
            var c = UnityEngine.Object.FindFirstObjectByType<IDGSFilesChallenge>();
            Check(c.IsOpen && !Player.enabled, "Files still opens and blocks movement after integration");
            Vector2 origin = c.Files[0].anchoredPosition;
            foreach (var s in Drag(c.Files[0], c.Folders[1], c.canvas)) yield return s;
            Check(c.SortedCount == 0 && c.Files[0].anchoredPosition == origin, "Files still rejects a wrong category");
            for (int i = 0; i < 6; i++) foreach (var s in Drag(c.Files[i], c.Folders[i / 2], c.canvas)) yield return s;
            Check(c.IsSolved && c.RewardGrantedThisRun && GameProgress.HasEvent(IDGSFilesChallenge.EventId), "Files regression completes all six and saves");
            foreach (var s in Click(c.FinishButton.GetComponent<RectTransform>(), c.canvas)) yield return s;
            Check(Hud.CompletedCount == 2, "Files and server are counted independently");
        }
        static IEnumerable<object> RegressionDocencia()
        {
            foreach (var s in Load("IDGS_Docencia_Interior")) yield return s;
            foreach (var s in Approach(3, 7)) yield return s;
            foreach (var s in Press(Key.E)) yield return s;
            var cables = UnityEngine.Object.FindFirstObjectByType<IDGSCablesChallenge>();
            Check(cables.IsOpen && !Player.enabled, "Cables still opens from west station");
            var handles = UnityEngine.Object.FindObjectsByType<IDGSCableHandle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var s in Drag(handles.Single(h => h.cableIndex == 0).GetComponent<RectTransform>(), cables.rightTargets[(cables.RightSlotFor(0) + 1) % 4], cables.canvas)) yield return s;
            Check(cables.ConnectedCount == 0, "Cables rejects mismatched connections");
            for (int i = 0; i < 4; i++) foreach (var s in Drag(handles.Single(h => h.cableIndex == i).GetComponent<RectTransform>(), cables.rightTargets[cables.RightSlotFor(i)], cables.canvas)) yield return s;
            Check(cables.IsSolved && GameProgress.HasEvent(IDGSCablesChallenge.EventId), "Cables regression completes four real mouse drags");
            foreach (var s in Click(cables.finishButton.GetComponent<RectTransform>(), cables.canvas)) yield return s;
            foreach (var s in Approach(8, 8)) yield return s;
            foreach (var s in Press(Key.Enter)) yield return s;
            var circuit = UnityEngine.Object.FindFirstObjectByType<IDGSCircuitChallenge>();
            Check(circuit.IsOpen && !Player.enabled && !circuit.IsSolved, "Circuit still opens an unsolved board");
            for (int i = 0; i < 9 && !circuit.IsSolved; i++)
            {
                int turns = 0;
                while (!circuit.IsSolved && circuit.Board.MaskAt(i) != circuit.Board.SolutionMaskForValidation(i) && turns++ < 4)
                    foreach (var s in Click(circuit.tiles[i].rectTransform, circuit.canvas)) yield return s;
            }
            Check(circuit.IsSolved && GameProgress.HasEvent(IDGSCircuitChallenge.EventId), "Circuit regression rotates pieces through actual mouse clicks and saves");
            foreach (var s in Click(circuit.finishButton.GetComponent<RectTransform>(), circuit.canvas)) yield return s;
            foreach (var s in Approach(12, 7)) yield return s;
            foreach (var s in Press(Key.E)) yield return s;
            var memory = UnityEngine.Object.FindFirstObjectByType<IDGSMemoryChallenge>();
            Check(memory.IsOpen && !Player.enabled, "Memory still opens from east station");
            for (int round = 1; round <= 5; round++)
            {
                float end = Time.realtimeSinceStartup + 15;
                while (!memory.CanAcceptInput && Time.realtimeSinceStartup < end) yield return null;
                Check(memory.CanAcceptInput && memory.Round == round, "Memory plays and unlocks round " + round);
                for (int i = 0; i < round; i++)
                {
                    int color = memory.SequenceStepForValidation(i);
                    if (i % 2 == 0) foreach (var s in Press(new[] { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 }[color])) yield return s;
                    else foreach (var s in Click(memory.pads[color].GetComponent<RectTransform>(), memory.canvas)) yield return s;
                    Check(memory.InputIndex == i + 1, "Memory receives each expected keyboard/mouse answer " + (i + 1) + " in round " + round);
                }
                Check(round == 5 ? memory.IsSolved : memory.Phase == IDGSMemoryPhase.RoundPassed, "Memory accepts round " + round + " with real keyboard and mouse");
            }
            Check(GameProgress.HasEvent(IDGSMemoryChallenge.EventId), "Memory regression completes all five rounds and saves");
            foreach (var s in Click(memory.finishButton.GetComponent<RectTransform>(), memory.canvas)) yield return s;
            Check(Hud.CompletedCount == 5, "All five implemented activities contribute to HUD 5/6");
        }
        static IEnumerator Validate()
        {
            var baseline = GameProgress.CaptureCompletedIds().Where(id => !IDGSProgressHud.EventIds.Contains(id)).ToArray();
            GameProgress.RestoreCompletedIds(baseline); changes = 0;
            Check(Hud.CompletedCount == 0 && !Server.TryOpen(), "Server starts independently of the other activities and cannot open remotely");
            Check(IDGSServerChallenge.IsInZone(IDGSServerChallenge.ZoneStart) && IDGSServerChallenge.IsInZone(IDGSServerChallenge.ZoneEnd) && !IDGSServerChallenge.IsInZone(IDGSServerChallenge.ZoneStart - .001f) && !IDGSServerChallenge.IsInZone(IDGSServerChallenge.ZoneEnd + .001f), "Green zone accepts both endpoints and rejects positions outside");
            foreach (var s in Approach(8, 8)) yield return s;
            foreach (var s in Press(Key.S)) yield return s;
            Check(!Server.TryOpen(), "Wrong facing cannot activate central station");
            foreach (var s in Walk(new Vector2Int(8, 8))) yield return s;
            foreach (var s in Press(Key.W)) yield return s;
            foreach (var s in Press(Key.E)) yield return s;
            Check(Server.IsOpen && Server.CalibratedCount == 0 && !Server.IsSolved, "E opens central-north server station with three pending modules");
            Check(!Player.enabled && !Interactor.enabled && !Server.gameplayHud.activeSelf && !Hud, "Modal blocks walking, other interactions and HUD");
            Vector2Int before = Player.Cell; Keys(Key.D, Key.LeftShift); foreach (var s in Frames(15)) yield return s; Keys(); yield return null;
            Check(Player.Cell == before && !GameProgress.HasEvent(IDGSServerChallenge.EventId), "Shift and movement cannot move Mateo or complete server");
            foreach (var s in SolveServer(true)) yield return s;
            Check(Server.RewardGrantedThisRun && changes == 1 && GameProgress.CaptureCompletedIds().Count(id => id == IDGSServerChallenge.EventId) == 1, "Server registers exact idgs.cmd.servidor once");
            IDGSVegetationValidation.Capture(Camera.main, Folder + "/servidor-listo.png", 1280, 720); yield return null;
            foreach (var s in Click(Server.FinishButton.GetComponent<RectTransform>(), Server.canvas)) yield return s;
            Check(!Server.IsOpen && Player.enabled && Interactor.enabled && Hud.CompletedCount == 1 && Server.stationState.text.Contains("COMPLETADO"), "Closing restores controls, marks station and updates HUD 1/6");
            foreach (var s in RegressionFiles()) yield return s;
            foreach (var s in RegressionDocencia()) yield return s;
            foreach (var s in Load("IDGS_CMD_Interior")) yield return s;
            Check(Server.HasCompleted && Server.stationState.text.Contains("COMPLETADO") && Hud.CompletedCount == 5, "Scene reload restores all five events and server completion");
            foreach (var s in Approach(8, 8)) yield return s;
            string saved = PlayerPrefs.GetString(GameProgress.StorageKey); int count = changes;
            foreach (var s in Press(Key.Enter)) yield return s;
            Check(Server.IsOpen && Server.CalibratedCount == 0, "Enter opens replay from zero modules");
            foreach (var s in SolveServer(false)) yield return s;
            Check(!Server.RewardGrantedThisRun && changes == count && saved == PlayerPrefs.GetString(GameProgress.StorageKey), "Replay completes without extra event, reward or save rewrite");
            foreach (var s in Press(Key.Escape)) yield return s;
            foreach (var s in Walk(new Vector2Int(8, 6))) yield return s;
            foreach (var s in Travel(Interactor.points.Single(p => p.kind == IDGSPointKind.Exit))) yield return s;
            Check(Hud.CompletedCount == 5, "Exterior preserves HUD 5/6");
            foreach (var s in Travel(Interactor.points.Single(p => p.pointId == "idgs.cmd"))) yield return s;
            Check(Hud.CompletedCount == 5 && Server.HasCompleted, "Reentering CMD preserves server and all other activities");
            Check(baseline.All(GameProgress.HasEvent) && GameProgress.PlayerName == "Prueba IDGS", "Tutorial documents, narrative and player name are preserved");
            IDGSVegetationValidation.Capture(Camera.main, Folder + "/hud-cinco.png", 1280, 720); yield return null;
            File.WriteAllText(Folder + "/completed-test-save.json", PlayerPrefs.GetString(GameProgress.StorageKey));
            Check(!EditorUtility.scriptCompilationFailed, "Project compiles without errors");
        }
        public static string VerifyRestart()
        {
            if (!Application.isPlaying || !GameProgress.StorageKey.StartsWith("CampusQuest.Test.IDGSInteriors.", StringComparison.Ordinal)) throw new InvalidOperationException("Isolated Play Mode required.");
            if (!Server.HasCompleted || !Server.stationState.text.Contains("COMPLETADO") || Hud.CompletedCount != 5 || PlayerPrefs.GetString(GameProgress.StorageKey) != File.ReadAllText(Folder + "/completed-test-save.json")) throw new InvalidOperationException("Progress did not restore.");
            File.AppendAllText(Folder + "/results.txt", "PASS Real Play Mode restart restores all five activities and HUD\n");
            return "Server, previous activities and HUD restored after Play Mode restart";
        }
    }
}
#endif
