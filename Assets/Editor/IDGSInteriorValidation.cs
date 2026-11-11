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
    public static class IDGSInteriorValidation
    {
        [Serializable] private sealed class Context
        {
            public string testKey;
            public string originalTestKey;
            public string[] keys;
            public bool[] existed;
            public string[] values;
            public string[] optionKeys;
            public bool[] optionExisted;
            public string[] optionValues;
            public string fixture;
        }
        private const string Folder = "tmp/idgs-interiors";
        private const string ContextPath = Folder + "/validation-context.json";
        public static string Status { get; private set; } = "Not started";
        private static IEnumerator steps;
        private static Keyboard keyboard;
        private static Keyboard previousKeyboard;
        private static bool previousBackground;
        private static int frame;
        private static Context context;
        private static readonly List<string> results = new List<string>();
        private static IDGSPlayerMotor Player => UnityEngine.Object.FindFirstObjectByType<IDGSPlayerMotor>();
        private static IDGSInteractionController Interactor => UnityEngine.Object.FindFirstObjectByType<IDGSInteractionController>();

        public static string Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Prepare before Play Mode.");
            Directory.CreateDirectory(Folder);
            var keys = new[] { GameProgress.SaveKey, GameProgress.SaveKey + ".backup", GameProgress.SaveKey + ".legacy-backup", GameProgress.SaveKey + ".unreadable" };
            var options = new[] { "Options.MusicVolume", "Options.SfxVolume", "Options.LightIntensity", "Options.Fullscreen" };
            context = new Context {
                testKey = "CampusQuest.Test.IDGSInteriors." + Guid.NewGuid().ToString("N"),
                originalTestKey = SessionState.GetString(GameProgress.EditorTestStorageSetting, ""),
                keys = keys, existed = keys.Select(PlayerPrefs.HasKey).ToArray(),
                values = keys.Select(k => PlayerPrefs.GetString(k, "")).ToArray(),
                optionKeys = options, optionExisted = options.Select(PlayerPrefs.HasKey).ToArray(),
                optionValues = options.Select(OptionValue).ToArray(),
                fixture = JsonUtility.ToJson(new Fixture())
            };
            File.WriteAllText(ContextPath, JsonUtility.ToJson(context, true));
            SessionState.SetString(GameProgress.EditorTestStorageSetting, context.testKey);
            PlayerPrefs.SetString(context.testKey, context.fixture);
            PlayerPrefs.Save();
            return "Profile backed up; isolated slot=" + context.testKey;
        }
        [Serializable] private sealed class Fixture
        {
            public int version = 1;
            public string[] events = { "rectoria.rector_welcome", "rectoria.document.birth_certificate", "rectoria.document.student_card",
                "rectoria.document.payment_receipt", "rectoria.archive.note_1", "idgs.docencia.cables.completed", "idgs.cmd.files.completed", "story.test.keep_me" };
            public string playerName = "Prueba IDGS";
        }
        private static string OptionValue(string key) => key == "Options.Fullscreen"
            ? PlayerPrefs.GetInt(key).ToString()
            : PlayerPrefs.GetFloat(key).ToString("R", System.Globalization.CultureInfo.InvariantCulture);

        public static void Begin()
        {
            context = JsonUtility.FromJson<Context>(File.ReadAllText(ContextPath));
            if (!Application.isPlaying || steps != null || GameProgress.StorageKey != context.testKey)
                throw new InvalidOperationException("Run once in isolated Play Mode.");
            results.Clear(); frame = -1; Status = "Running";
            previousKeyboard = Keyboard.current;
            keyboard = InputSystem.AddDevice<Keyboard>("IDGSInteriorValidationKeyboard");
            previousBackground = Application.runInBackground;
            Application.runInBackground = true;
            EditorApplication.isPaused = false;
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Show();
            steps = Validate(); EditorApplication.update += Tick;
        }
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
            Hold();
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (previousKeyboard != null && previousKeyboard.added) previousKeyboard.MakeCurrent();
            Application.runInBackground = previousBackground;
            File.WriteAllLines(Folder + "/validation-results.txt", results.Append(status));
        }
        private static void Check(bool valid, string message)
        {
            if (!valid) throw new InvalidOperationException(message);
            results.Add("PASS " + message);
        }
        private static void Hold(params Key[] keys)
        {
            if (keyboard == null || !keyboard.added) return;
            keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            InputSystem.Update();
            if (Player != null) Player.SendMessage("Update");
        }
        private static void CheckProgress(string phase)
        {
            Check(PlayerPrefs.GetString(context.testKey) == context.fixture, phase + ": stored profile remains byte-identical");
            Check(GameProgress.HasEvent("rectoria.document.payment_receipt") && GameProgress.HasEvent("rectoria.archive.note_1")
                && GameProgress.HasEvent("idgs.cmd.files.completed") && GameProgress.HasEvent("story.test.keep_me")
                && GameProgress.PlayerName == "Prueba IDGS", phase + ": documents, clues, IDGS, narrative and name remain available");
        }
        private static List<Vector2Int> FindPath(Vector2Int goal)
        {
            var player = Player;
            var capsule = player.GetComponent<CapsuleCollider2D>();
            bool interior = SceneManager.GetActiveScene().name != "IDGS_Exterior";
            int width = interior ? 16 : 64, height = interior ? 12 : 52;
            var queue = new Queue<Vector2Int>();
            var previous = new Dictionary<Vector2Int, Vector2Int>();
            queue.Enqueue(player.Cell); previous[player.Cell] = player.Cell;
            while (queue.Count > 0 && !previous.ContainsKey(goal))
            {
                var from = queue.Dequeue();
                foreach (var direction in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                {
                    var next = from + direction;
                    if (next.x < 0 || next.y < 0 || next.x >= width || next.y >= height || previous.ContainsKey(next)) continue;
                    var hits = Physics2D.CapsuleCastAll(IDGSPlayerMotor.Center(from) + capsule.offset, capsule.size, capsule.direction, 0, direction, 1.01f);
                    if (hits.Any(h => h.collider != capsule && !h.collider.isTrigger)) continue;
                    previous[next] = from; queue.Enqueue(next);
                }
            }
            Check(previous.ContainsKey(goal), "Physical grid route exists to " + goal);
            var path = new List<Vector2Int>();
            for (var cell = goal; cell != player.Cell; cell = previous[cell]) path.Add(cell);
            path.Reverse();
            return path;
        }
        private static Key DirectionKey(Vector2Int direction)
        {
            if (direction.x > 0) return Key.D;
            if (direction.x < 0) return Key.A;
            return direction.y > 0 ? Key.W : Key.S;
        }
        private static IEnumerable<object> Walk(Vector2Int goal)
        {
            foreach (var cell in FindPath(goal))
            {
                var direction = cell - Player.Cell;
                float deadline = Time.realtimeSinceStartup + 6;
                while ((Player.Cell != cell || !Player.IsAtRest) && Time.realtimeSinceStartup < deadline)
                {
                    Hold(DirectionKey(direction));
                    yield return null;
                }
                Hold();
                Check(Player.Cell == cell && Player.IsAtRest, "Actual input walks one complete cell to " + cell);
                yield return null;
            }
        }
        private static IEnumerable<object> Travel(IDGSInteractionPoint point, Key confirm)
        {
            foreach (var step in Walk(point.approachCell)) yield return step;
            Hold(DirectionKey(point.cell - point.approachCell)); yield return null;
            Hold(); yield return null;
            Interactor.SendMessage("Update");
            Check(Interactor.AvailablePoint() == point && Interactor.prompt.text.Contains("E / Enter"), "Door prompt available: " + point.pointId);
            string destination = point.destinationScene;
            Vector2Int arrival = point.destinationCell, facing = point.destinationFacing;
            Hold(confirm); Interactor.SendMessage("Update");
            float deadline = Time.realtimeSinceStartup + 25;
            while ((SceneManager.GetActiveScene().name != destination || IDGSSceneTravel.IsChanging) && Time.realtimeSinceStartup < deadline) yield return null;
            Check(SceneManager.GetActiveScene().name == destination && !IDGSSceneTravel.IsChanging, "Confirm key loads " + destination);
            yield return null; yield return null;
            Check(Player.Cell == arrival && Player.IsAtRest && Player.Facing == facing, "Arrival cell and facing correct in " + destination);
            Check(SceneManager.GetActiveScene().name == destination, "Held confirm did not bounce to the previous scene");
            Hold(); yield return null;
            CheckProgress(destination);
        }
        private static IEnumerable<object> InteriorChecks(string name)
        {
            Check(Player.GetComponentInChildren<Animator>().GetInteger("Direction") == 3, name + ": Mateo initially faces north in the Animator");
            var stations = Interactor.points.Where(p => p.kind == IDGSPointKind.Station).OrderBy(p => p.cell.x).ToArray();
            Check(stations.Length == 3, name + ": three station anchors");
            foreach (var point in stations)
            {
                foreach (var step in Walk(point.approachCell)) yield return step;
                Hold(Key.S); Interactor.SendMessage("Update");
                Check(Interactor.AvailablePoint() == null, point.pointId + ": wrong facing does not activate the station");
                Hold(Key.W); yield return null; Hold(); yield return null;
                Interactor.SendMessage("Update");
                Check(Interactor.AvailablePoint() == point && Interactor.prompt.text.Contains(point.displayName), point.pointId + ": name and confirm prompt visible");
                var before = Player.Cell;
                for (int i = 0; i < 12; i++) { Hold(Key.W); yield return null; }
                Hold();
                Check(Player.Cell == before && Player.IsAtRest, point.pointId + ": workstation collision stops Mateo");
                Hold(Key.E); Interactor.SendMessage("Update");
                if (point.onInteracted.GetPersistentEventCount() > 0)
                {
                    var cables = UnityEngine.Object.FindFirstObjectByType<IDGSCablesChallenge>();
                    var circuit = UnityEngine.Object.FindFirstObjectByType<IDGSCircuitChallenge>();
                    var memory = UnityEngine.Object.FindFirstObjectByType<IDGSMemoryChallenge>();
                    var files = UnityEngine.Object.FindFirstObjectByType<IDGSFilesChallenge>();
                    var server = UnityEngine.Object.FindFirstObjectByType<IDGSServerChallenge>();
                    var virus = UnityEngine.Object.FindFirstObjectByType<IDGSVirusChallenge>();
                    Check((cables != null && cables.IsOpen) || (circuit != null && circuit.IsOpen) || (memory != null && memory.IsOpen) || (files != null && files.IsOpen) || (server != null && server.IsOpen) || (virus != null && virus.IsOpen), point.pointId + ": configured activity opens");
                    if (cables != null && cables.IsOpen) cables.Close();
                    if (circuit != null && circuit.IsOpen) circuit.Close();
                    if (memory != null && memory.IsOpen) memory.Close();
                    if (files != null && files.IsOpen) files.Close();
                    if (server != null && server.IsOpen) server.Close();
                    if (virus != null && virus.IsOpen) virus.Close();
                }
                else Check(Interactor.LastInteraction == point && Interactor.notice.text.Contains("Próximamente"), point.pointId + ": station activates its placeholder only");
                Hold(); yield return null;
            }
            CheckProgress(name + " stations");
            foreach (var step in Walk(new Vector2Int(8, 5))) yield return step;
            foreach (bool running in new[] { true, false })
            {
                var target = Player.Cell + (running ? Vector2Int.right : Vector2Int.left);
                float began = Time.fixedTime, deadline = Time.realtimeSinceStartup + 6;
                do
                {
                    Hold(running ? new[] { Key.LeftShift, Key.D } : new[] { Key.A });
                    Check(Mathf.Abs(Player.CurrentSpeed - (running ? 6 : 4)) < .001f, name + (running ? ": Shift selects run speed" : ": releasing Shift selects walk speed"));
                    yield return null;
                } while ((Player.Cell != target || !Player.IsAtRest) && Time.realtimeSinceStartup < deadline);
                Hold();
                float elapsed = Time.fixedTime - began;
                Check(Player.Cell == target && Player.IsAtRest && elapsed > .1f && elapsed < .55f,
                    name + (running ? ": complete running step" : ": complete walking step") + " in " + elapsed.ToString("F2") + "s");
                yield return null;
            }
            Hold(Key.RightShift); Check(Player.CurrentSpeed == 6, name + ": right Shift also runs"); Hold();
            foreach (var step in Walk(new Vector2Int(1, 6))) yield return step;
            for (int i = 0; i < 12; i++) { Hold(Key.A); yield return null; }
            Hold();
            Check(Player.Cell == new Vector2Int(1, 6), name + ": west wall blocks movement");
            var camera = Camera.main; float originalAspect = camera.aspect;
            foreach (float aspect in new[] { 16f / 9, 4f / 3, .8f })
            {
                camera.aspect = aspect; camera.SendMessage("LateUpdate");
                float h = camera.orthographicSize, w = h * aspect;
                Check(camera.transform.position.x - w >= -.001f && camera.transform.position.x + w <= 16.001f
                    && camera.transform.position.y - h >= -.001f && camera.transform.position.y + h <= 12.001f,
                    name + ": camera stays within room at aspect " + aspect.ToString("F2"));
            }
            camera.aspect = originalAspect; camera.SendMessage("LateUpdate");
            float size = camera.orthographicSize; Vector3 position = camera.transform.position;
            camera.transform.position = new Vector3(8, 6, -10); camera.orthographicSize = 6;
            IDGSVegetationValidation.Capture(camera, Folder + "/" + name + ".png", 1280, 960);
            camera.transform.position = position; camera.orthographicSize = size;
            Check(UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .All(go => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) == 0), name + ": no missing scripts");
        }
        private static IEnumerator Validate()
        {
            Check(SceneManager.GetActiveScene().name == "IDGS_Exterior", "Started in existing exterior");
            CheckProgress("Initial exterior");
            Hold(); yield return null;
            foreach (var step in Travel(Interactor.points.Single(p => p.pointId == "idgs.docencia"), Key.E)) yield return step;
            foreach (var step in InteriorChecks("Docencia")) yield return step;
            foreach (var step in Travel(Interactor.points.Single(p => p.kind == IDGSPointKind.Exit), Key.Enter)) yield return step;
            foreach (var step in Travel(Interactor.points.Single(p => p.pointId == "idgs.cmd"), Key.Enter)) yield return step;
            foreach (var step in InteriorChecks("CMD")) yield return step;
            foreach (var step in Travel(Interactor.points.Single(p => p.kind == IDGSPointKind.Exit), Key.E)) yield return step;
            Check(!EditorUtility.scriptCompilationFailed, "Project compiles without errors");
            VerifyProfile();
        }
        private static void VerifyProfile()
        {
            for (int i = 0; i < context.keys.Length; i++)
                Check(PlayerPrefs.HasKey(context.keys[i]) == context.existed[i] && PlayerPrefs.GetString(context.keys[i], "") == context.values[i], "Real progress key unchanged: " + context.keys[i]);
            for (int i = 0; i < context.optionKeys.Length; i++)
                Check(PlayerPrefs.HasKey(context.optionKeys[i]) == context.optionExisted[i] && OptionValue(context.optionKeys[i]) == context.optionValues[i], "Preference unchanged: " + context.optionKeys[i]);
        }
        public static string Cleanup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            context = JsonUtility.FromJson<Context>(File.ReadAllText(ContextPath));
            VerifyProfile();
            if (!context.testKey.StartsWith("CampusQuest.Test.IDGSInteriors.", StringComparison.Ordinal)) throw new InvalidOperationException("Invalid test key.");
            File.WriteAllText(Folder + "/final-test-save.json", PlayerPrefs.GetString(context.testKey, ""));
            foreach (string suffix in new[] { "", ".backup", ".legacy-backup", ".unreadable" }) PlayerPrefs.DeleteKey(context.testKey + suffix);
            PlayerPrefs.Save();
            if (string.IsNullOrEmpty(context.originalTestKey)) SessionState.EraseString(GameProgress.EditorTestStorageSetting);
            else SessionState.SetString(GameProgress.EditorTestStorageSetting, context.originalTestKey);
            return "Real progress and preferences verified; temporary slot archived and removed.";
        }
    }
}
#endif
