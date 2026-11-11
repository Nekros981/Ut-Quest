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
    public static class IDGSMemoryValidation
    {
        private const string Folder = "tmp/idgs-memory";
        public static string Status { get; private set; } = "Not started";
        private static IEnumerator steps;
        private static Keyboard keyboard, previousKeyboard;
        private static Mouse mouse, previousMouse;
        private static bool previousBackground;
        private static int frame, changes;
        private static readonly List<string> results = new List<string>();
        private static int[] observed;
        private static IDGSMemoryChallenge Challenge => UnityEngine.Object.FindFirstObjectByType<IDGSMemoryChallenge>();
        private static IDGSPlayerMotor Player => UnityEngine.Object.FindFirstObjectByType<IDGSPlayerMotor>();
        private static IDGSInteractionController Interactor => UnityEngine.Object.FindFirstObjectByType<IDGSInteractionController>();
        private static Key NumberKey(int index) => new[] { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 }[index];

        public static string Prepare()
        {
            string result = IDGSInteriorValidation.Prepare();
            Directory.CreateDirectory(Folder);
            File.Copy("tmp/idgs-interiors/validation-context.json", Folder + "/profile-backup.json", true);
            string key = GameProgress.StorageKey;
            string fixture = PlayerPrefs.GetString(key).Replace("idgs.docencia.cables.completed", IDGSCablesChallenge.EventId);
            fixture = fixture.Replace("story.test.keep_me", IDGSCircuitChallenge.EventId);
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
            keyboard = InputSystem.AddDevice<Keyboard>("MemoryKeyboard");
            mouse = InputSystem.AddDevice<Mouse>("MemoryMouse");
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
        private static void Pointer(Vector2 point, bool pressed)
        {
            mouse.MakeCurrent();
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left, pressed));
        }
        private static IEnumerable<object> Press(Key key)
        {
            keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null; yield return null;
        }
        private static IEnumerable<object> Click(RectTransform rect)
        {
            Canvas.ForceUpdateCanvases();
            Vector2 p = RectTransformUtility.WorldToScreenPoint(Challenge.canvas.worldCamera, rect.position);
            Pointer(p, false); yield return null;
            Pointer(p, true); yield return null;
            Pointer(p, false); yield return null; yield return null;
        }
        private static IEnumerable<object> Walk(Vector2Int goal)
        {
            float end = Time.realtimeSinceStartup + 12;
            while ((Player.Cell != goal || !Player.IsAtRest) && Time.realtimeSinceStartup < end)
            {
                var d = goal - Player.Cell;
                Keys(d.x != 0 ? (d.x > 0 ? Key.D : Key.A) : (d.y > 0 ? Key.W : Key.S)); yield return null;
            }
            Keys(); yield return null;
            Check(Player.Cell == goal && Player.IsAtRest, "Walking reaches " + goal);
        }
        private static IEnumerable<object> Approach()
        {
            foreach (var step in Walk(new Vector2Int(8,6))) yield return step;
            foreach (var step in Walk(new Vector2Int(12,6))) yield return step;
            foreach (var step in Walk(new Vector2Int(12,7))) yield return step;
            Keys(Key.W); yield return null; Keys(); yield return null;
        }
        private static IEnumerable<object> Open(Key key)
        {
            Keys(); yield return null;
            Keys(key); Interactor.SendMessage("Update"); Keys(); yield return null; yield return null;
            Check(Challenge.IsOpen && Challenge.Round == 1, key + " opens the east station at round one");
        }
        private static IEnumerable<object> Observe(bool capture = false)
        {
            float end = Time.realtimeSinceStartup + 16;
            var lights = new List<int>();
            int priorStep = -1;
            bool captured = false;
            while (!Challenge.CanAcceptInput && Time.realtimeSinceStartup < end)
            {
                if (Challenge.Phase == IDGSMemoryPhase.Showing && Challenge.LitIndex >= 0 && priorStep != Challenge.PlaybackStep)
                {
                    priorStep = Challenge.PlaybackStep;
                    lights.Add(Challenge.LitIndex);
                    Check(!Challenge.pads.Any(b => b.interactable) && !Challenge.Choose((Challenge.LitIndex+1)%4), "Playback blocks all answer buttons");
                    if (capture && !captured)
                    {
                        IDGSVegetationValidation.Capture(Camera.main, Folder + "/secuencia.png",1280,720);
                        captured = true;
                    }
                }
                yield return null;
            }
            Check(Challenge.CanAcceptInput, "Playback hands control to the player");
            if (lights.Count > 0)
            {
                Check(lights.Count == Challenge.Round && lights.Select((color,index) => color == Challenge.SequenceStepForValidation(index)).All(v => v), "Observed light prefix matches round " + Challenge.Round);
                observed = lights.ToArray();
            }
        }
        private static IEnumerable<object> Answer(int color, bool useMouse)
        {
            if (useMouse)
                foreach (var step in Click(Challenge.pads[color].GetComponent<RectTransform>())) yield return step;
            else
                foreach (var step in Press(NumberKey(color))) yield return step;
        }
        private static IEnumerable<object> PlayFive(bool injectErrors)
        {
            int[] previous = Array.Empty<int>();
            for (int round = 1; round <= 5; round++)
            {
                foreach (var step in Observe(round == 3 && injectErrors)) yield return step;
                Check(Challenge.Round == round && observed.Length == round && previous.SequenceEqual(observed.Take(previous.Length)), "Sequence grows by exactly one light for round " + round);
                var expected = observed.ToArray();
                if (injectErrors && (round == 3 || round == 5))
                {
                    foreach (var step in Answer((expected[0]+1)%4,round==3)) yield return step;
                    Check(Challenge.Phase == IDGSMemoryPhase.Retry && Challenge.Round == round && Challenge.CompletedRounds == round-1, "Wrong answer preserves previous rounds at round " + round);
                    foreach (var step in Observe()) yield return step;
                    Check(expected.SequenceEqual(observed), "Retry replays the same pattern");
                }
                for (int i = 0; i < expected.Length; i++)
                    foreach (var step in Answer(expected[i], (round+i)%2==0)) yield return step;
                Check(round == 5 ? Challenge.IsSolved : Challenge.Phase == IDGSMemoryPhase.RoundPassed, "Round " + round + " accepts the full correct pattern");
                if (round < 5) Check(!GameProgress.HasEvent(IDGSMemoryChallenge.EventId) || !injectErrors, "No first-time event before round five");
                previous = expected;
            }
        }
        private static IEnumerable<object> Travel(IDGSInteractionPoint point)
        {
            foreach (var step in Walk(point.approachCell)) yield return step;
            Keys(point.cell.y > point.approachCell.y ? Key.W : Key.S); yield return null; Keys(); yield return null;
            string target = point.destinationScene; Keys(Key.Enter); Interactor.SendMessage("Update");
            float end = Time.realtimeSinceStartup+20;
            while ((SceneManager.GetActiveScene().name!=target || IDGSSceneTravel.IsChanging) && Time.realtimeSinceStartup<end) yield return null;
            Check(SceneManager.GetActiveScene().name==target,"Portal reaches "+target);
            Keys(); yield return null; yield return null;
        }
        private static IEnumerator Validate()
        {
            Check(!Challenge.TryOpen() && !Challenge.HasCompleted,"Arrival point cannot open memory and starts pending");
            foreach (var step in Approach()) yield return step;
            foreach (var step in Open(Key.E)) yield return step;
            var position = Player.transform.position;
            for (int i=0;i<12;i++){Keys(Key.W,Key.LeftShift);yield return null;}
            Keys();
            Check(!Player.enabled && !Interactor.enabled && Vector2.Distance(position,Player.transform.position)<.001f,"Player cannot walk, run or use another station while memory is open");
            Check(!UnityEngine.Object.FindFirstObjectByType<IDGSCircuitChallenge>().TryOpen(),"Other activities remain blocked");
            // Exercise every numeric mapping, regardless of the randomly generated pattern.
            for (int color=0;color<4;color++)
            {
                foreach (var step in Observe()) yield return step;
                keyboard.MakeCurrent();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(NumberKey(color)));
                yield return null;
                Check(Challenge.LitIndex==color,"Numeric key "+(color+1)+" lights the matching color");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                yield return null;yield return null;
            }
            foreach (var step in Press(Key.Escape)) yield return step;
            Check(!Challenge.IsOpen && Player.enabled && changes==0,"Cancel restores movement without awarding a partial sequence");
            foreach (var step in Open(Key.Enter)) yield return step;
            foreach (var step in PlayFive(true)) yield return step;
            Check(Challenge.status.text=="¡Secuencia restaurada!" && Challenge.RewardGrantedThisRun,"Five rounds show victory and first completion");
            Check(changes==1 && GameProgress.CaptureCompletedIds().Count(id=>id==IDGSMemoryChallenge.EventId)==1,"Memory event is registered exactly once");
            Check(GameProgress.HasEvent(IDGSCablesChallenge.EventId)&&GameProgress.HasEvent(IDGSCircuitChallenge.EventId)&&GameProgress.HasEvent("rectoria.document.payment_receipt")&&GameProgress.PlayerName=="Prueba IDGS","Other activities, documents and player name remain intact");
            IDGSVegetationValidation.Capture(Camera.main,Folder+"/restaurada.png",1280,720);
            yield return null;Canvas.ForceUpdateCanvases();yield return null;
            string json=PlayerPrefs.GetString(GameProgress.StorageKey);
            foreach (var step in Click(Challenge.finishButton.GetComponent<RectTransform>()))yield return step;
            Check(!Challenge.IsOpen && Challenge.stationState.text.Contains("COMPLETADO"),"Station marker updates and return button restores controls");
            foreach (var step in Walk(new Vector2Int(12,6)))yield return step;
            foreach (var step in Walk(new Vector2Int(8,6)))yield return step;
            foreach (var step in Travel(Interactor.points.Single(p=>p.kind==IDGSPointKind.Exit)))yield return step;
            foreach (var step in Travel(Interactor.points.Single(p=>p.pointId=="idgs.docencia")))yield return step;
            Check(Challenge.HasCompleted&&Challenge.stationState.text.Contains("COMPLETADO"),"Station completion restores after leaving and returning");
            foreach (var step in Approach())yield return step;
            foreach (var step in Open(Key.E))yield return step;
            foreach (var step in PlayFive(false))yield return step;
            Check(!Challenge.RewardGrantedThisRun&&changes==1&&PlayerPrefs.GetString(GameProgress.StorageKey)==json,"Replay succeeds without duplicate rewards or save changes");
            foreach (var step in Click(Challenge.finishButton.GetComponent<RectTransform>()))yield return step;
            Check(!EditorUtility.scriptCompilationFailed,"Project compiles");
            File.WriteAllText(Folder+"/completed-test-save.json",json);
        }
        public static string VerifyRestart()
        {
            if(!Application.isPlaying||!GameProgress.StorageKey.StartsWith("CampusQuest.Test.IDGSInteriors.",StringComparison.Ordinal))throw new InvalidOperationException("Isolated Play Mode required.");
            if(!Challenge.HasCompleted||!Challenge.stationState.text.Contains("COMPLETADO")||PlayerPrefs.GetString(GameProgress.StorageKey)!=File.ReadAllText(Folder+"/completed-test-save.json"))throw new InvalidOperationException("Memory did not restore.");
            File.AppendAllText(Folder+"/results.txt","PASS Real Play Mode restart restores memory completion\n");
            return "Memory completion restored after Play Mode restart";
        }
    }
}
#endif
