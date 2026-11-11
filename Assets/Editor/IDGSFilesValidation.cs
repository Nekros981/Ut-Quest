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
    public static class IDGSFilesValidation
    {
        const string Folder = "tmp/idgs-files";
        public static string Status { get; private set; } = "Not started";
        static IEnumerator steps;
        static readonly List<string> results = new List<string>();
        static Keyboard keyboard, priorKeyboard;
        static Mouse mouse, priorMouse;
        static bool priorBackground;
        static int frame, changes;
        static IDGSFilesChallenge Challenge => UnityEngine.Object.FindFirstObjectByType<IDGSFilesChallenge>();
        static IDGSPlayerMotor Player => UnityEngine.Object.FindFirstObjectByType<IDGSPlayerMotor>();
        static IDGSInteractionController Interactor => UnityEngine.Object.FindFirstObjectByType<IDGSInteractionController>();
        static IDGSProgressHud Hud => UnityEngine.Object.FindFirstObjectByType<IDGSProgressHud>();
        public static string Prepare()
        {
            if (!string.IsNullOrEmpty(SessionState.GetString(GameProgress.EditorTestStorageSetting, ""))) throw new InvalidOperationException("An isolated profile is already active.");
            string result = IDGSInteriorValidation.Prepare();
            Directory.CreateDirectory(Folder);
            File.Copy("tmp/idgs-interiors/validation-context.json", Folder + "/profile-backup.json", true);
            return result;
        }
        static void Check(bool valid, string message)
        {
            if (!valid) throw new InvalidOperationException(message);
            results.Add("PASS " + message);
        }
        public static void Begin()
        {
            if (!Application.isPlaying || steps != null || !GameProgress.StorageKey.StartsWith("CampusQuest.Test.IDGSInteriors.", StringComparison.Ordinal)) throw new InvalidOperationException("Isolated Play Mode required.");
            priorKeyboard = Keyboard.current; priorMouse = Mouse.current;
            keyboard = InputSystem.AddDevice<Keyboard>("FilesValidationKeyboard"); mouse = InputSystem.AddDevice<Mouse>("FilesValidationMouse");
            priorBackground = Application.runInBackground; Application.runInBackground = true;
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Show();
            results.Clear(); frame = -1; changes = 0; Status = "Running";
            steps = Validate(); GameProgress.Changed += Changed; EditorApplication.update += Tick;
        }
        static void Changed() { changes++; }
        static void Tick()
        {
            try
            {
                if (!Application.isPlaying) throw new InvalidOperationException("Play Mode ended early.");
                EditorApplication.QueuePlayerLoopUpdate();
                if (frame == Time.frameCount) return; frame = Time.frameCount;
                if (!steps.MoveNext()) Finish("PASS");
            }
            catch (Exception error) { Finish("FAILED " + error); }
        }
        static void Finish(string result)
        {
            EditorApplication.update -= Tick; GameProgress.Changed -= Changed; steps = null; Status = result;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            if (priorKeyboard != null && priorKeyboard.added) priorKeyboard.MakeCurrent();
            if (priorMouse != null && priorMouse.added) priorMouse.MakeCurrent();
            Application.runInBackground = priorBackground; File.WriteAllLines(Folder + "/results.txt", results.Append(result));
        }
        static IEnumerable<object> WaitFrames(int count) { for (int i = 0; i < count; i++) yield return null; }
        static void KeyState(params Key[] keys) { keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); }
        static IEnumerable<object> Press(Key key)
        {
            KeyState(key); yield return null; yield return null; KeyState(); yield return null; yield return null;
        }
        static void Pointer(Vector2 p, bool down)
        {
            mouse.MakeCurrent(); InputSystem.QueueStateEvent(mouse, new MouseState { position = p }.WithButton(MouseButton.Left, down));
        }
        static IEnumerable<object> Drag(int file, Vector2 target)
        {
            Canvas.ForceUpdateCanvases();
            Vector2 start = RectTransformUtility.WorldToScreenPoint(Challenge.canvas.worldCamera, Challenge.Files[file].position);
            Pointer(start, false); yield return null; yield return null;
            Pointer(start, true); yield return null; yield return null;
            Pointer(start + new Vector2(16, 0), true); yield return null; yield return null;
            Check(Challenge.DraggingIndex == file, IDGSFilesChallenge.FileNames[file] + " mouse starts real EventSystem drag");
            Pointer(target, true); yield return null; yield return null;
            Pointer(target, false); yield return null; yield return null;
            Check(Challenge.DraggingIndex == -1, "Mouse release finishes drag");
        }
        static Vector2 FolderCenter(int folder) => RectTransformUtility.WorldToScreenPoint(Challenge.canvas.worldCamera, Challenge.Folders[folder].position);
        static IEnumerable<object> Click(RectTransform rect)
        {
            Vector2 p=RectTransformUtility.WorldToScreenPoint(Challenge.canvas.worldCamera,rect.position);
            Pointer(p,false);yield return null;Pointer(p,true);yield return null;Pointer(p,false);yield return null;yield return null;
        }
        static IEnumerable<object> Walk(Vector2Int goal)
        {
            float end = Time.realtimeSinceStartup + 15;
            while ((Player.Cell != goal || !Player.IsAtRest) && Time.realtimeSinceStartup < end)
            {
                Vector2Int d=goal-Player.Cell;
                if(d==Vector2Int.zero)KeyState(); else KeyState(d.x!=0?(d.x>0?Key.D:Key.A):(d.y>0?Key.W:Key.S));
                yield return null;
            }
            KeyState();yield return null;yield return null;
            Check(Player.Cell==goal&&Player.IsAtRest,"Normal movement reaches " + goal);
        }
        static IEnumerable<object> Approach()
        {
            foreach(var s in Walk(new Vector2Int(8,6)))yield return s;
            foreach(var s in Walk(new Vector2Int(3,6)))yield return s;
            foreach(var s in Walk(new Vector2Int(3,7)))yield return s;
            foreach(var s in Press(Key.W))yield return s;
        }
        static IEnumerable<object> Load(string name)
        {
            var load=SceneManager.LoadSceneAsync(name); float end=Time.realtimeSinceStartup+30;
            while(!load.isDone&&Time.realtimeSinceStartup<end)yield return null;
            foreach(var s in WaitFrames(6))yield return s;
            Check(load.isDone&&SceneManager.GetActiveScene().name==name,"Loads " + name);
        }
        static IEnumerable<object> Travel(IDGSInteractionPoint point)
        {
            foreach(var s in Walk(point.approachCell))yield return s;
            foreach(var s in Press(point.cell.y>point.approachCell.y?Key.W:Key.S))yield return s;
            var destination=point.destinationScene;
            foreach(var s in Press(Key.E))yield return s;
            float end=Time.realtimeSinceStartup+30;
            while((SceneManager.GetActiveScene().name!=destination||IDGSSceneTravel.IsChanging)&&Time.realtimeSinceStartup<end)yield return null;
            foreach(var s in WaitFrames(5))yield return s;
            Check(SceneManager.GetActiveScene().name==destination,"Door transition reaches " + destination);
        }
        static IEnumerator Validate()
        {
            var baseline=GameProgress.CaptureCompletedIds().Where(id=>!IDGSProgressHud.EventIds.Contains(id)).ToArray();
            GameProgress.RestoreCompletedIds(baseline);
            for(int n=0;n<=6;n++)
            {
                if(n>0)GameProgress.RegisterEvent(IDGSProgressHud.EventIds[n-1]);
                Check(Hud.CompletedCount==n&&Hud.Label.text=="Sistemas restaurados: "+n+"/6","HUD derives " +n+"/6 from unique events");
                Check(!Hud.GetComponentsInChildren<UnityEngine.UI.Graphic>().Any(g=>g.raycastTarget),"HUD never captures pointer input");
            }
            GameProgress.RestoreCompletedIds(baseline);
            foreach(string scene in new[]{"IDGS_Exterior","IDGS_Docencia_Interior","IDGS_CMD_Interior","Rectoria_Tutorial_Arte_v01","Sp_Bosque","Sp_Bunker","IDGS_CMD_Interior"})
            {
                foreach(var s in Load(scene))yield return s;
                Check((Hud!=null)==scene.StartsWith("IDGS_",StringComparison.Ordinal),"HUD scope is correct in " + scene);
            }
            Check(!Challenge.TryOpen(),"Spawn cannot activate west workstation remotely");
            foreach(var s in Approach())yield return s;
            foreach(var s in Press(Key.S))yield return s;
            Check(!Challenge.TryOpen(),"Wrong facing cannot activate files");
            foreach(var s in Walk(new Vector2Int(3,7)))yield return s;
            foreach(var s in Press(Key.W))yield return s;
            changes=0;
            foreach(var s in Press(Key.E))yield return s;
            Check(Challenge.IsOpen&&Challenge.SortedCount==0,"E opens west CMD station with six unsorted files");
            Check(!Player.enabled&&!Interactor.enabled&&!Challenge.gameplayHud.activeSelf,"Shared modal blocks movement, other stations and HUD");
            Vector2Int before=Player.Cell;
            KeyState(Key.D,Key.LeftShift);foreach(var s in WaitFrames(15))yield return s;KeyState();yield return null;
            Check(Player.Cell==before,"Walking and sprinting cannot move Mateo while desktop is open");
            Vector2 home=Challenge.Files[0].anchoredPosition;
            foreach(var s in Drag(0,FolderCenter(1)))yield return s;
            Check(Challenge.SortedCount==0&&Challenge.Files[0].anchoredPosition==home&&Challenge.Status.text.Contains("no corresponde"),"Wrong folder rejects file, returns it home and shows feedback");
            foreach(var s in Drag(0,new Vector2(5,5)))yield return s;
            Check(Challenge.SortedCount==0&&Challenge.Files[0].anchoredPosition==home,"Drop outside folders returns file home");
            IDGSVegetationValidation.Capture(Camera.main,Folder+"/organiza-archivos.png",1280,720);yield return null;Canvas.ForceUpdateCanvases();yield return null;
            for(int f=0;f<6;f++)
            {
                foreach(var s in Drag(f,FolderCenter(f/2)))yield return s;
                Check(Challenge.SortedCount==f+1&&!Challenge.Files[f].gameObject.activeSelf,"Correct classification accepts " +IDGSFilesChallenge.FileNames[f]);
                if(f<5)Check(!Challenge.IsSolved&&!GameProgress.HasEvent(IDGSFilesChallenge.EventId),"All six files are required before completing");
            }
            Check(Challenge.IsSolved&&Challenge.Status.text=="¡Archivos organizados!"&&Challenge.RewardGrantedThisRun,"Six files trigger success and first completion");
            Check(changes==1&&GameProgress.CaptureCompletedIds().Count(id=>id==IDGSFilesChallenge.EventId)==1,"Completion saves exactly one archivos event");
            Check(GameProgress.PlayerName=="Prueba IDGS"&&baseline.All(GameProgress.HasEvent),"Existing documents, narrative and player name remain intact");
            string saved=PlayerPrefs.GetString(GameProgress.StorageKey);
            foreach(var s in Click(Challenge.FinishButton.GetComponent<RectTransform>()))yield return s;
            Check(!Challenge.IsOpen&&Player.enabled&&Interactor.enabled&&Hud.Label.text=="Sistemas restaurados: 1/6","Close restores controls and HUD immediately shows 1/6");
            Check(Challenge.stationState.text.Contains("COMPLETADO"),"West station visibly marks completed");
            IDGSVegetationValidation.Capture(Camera.main,Folder+"/hud-cmd.png",1280,720);yield return null;Canvas.ForceUpdateCanvases();yield return null;
            foreach(var s in Press(Key.Enter))yield return s;
            Check(Challenge.IsOpen,"Enter reopens the west CMD station");
            for(int f=0;f<6;f++)foreach(var s in Drag(f,FolderCenter(f/2)))yield return s;
            Check(Challenge.IsSolved&&!Challenge.RewardGrantedThisRun&&changes==1&&PlayerPrefs.GetString(GameProgress.StorageKey)==saved,"Replay wins without another event, reward or save rewrite");
            foreach(var s in Press(Key.Escape))yield return s;
            foreach(var s in Walk(new Vector2Int(3,6)))yield return s;
            foreach(var s in Walk(new Vector2Int(8,6)))yield return s;
            foreach(var s in Travel(Interactor.points.Single(p=>p.kind==IDGSPointKind.Exit)))yield return s;
            Check(Hud.CompletedCount==1,"Exterior restores the same progress after exiting CMD");
            foreach(var s in Travel(Interactor.points.Single(p=>p.pointId=="idgs.cmd")))yield return s;
            Check(Challenge.HasCompleted&&Challenge.stationState.text.Contains("COMPLETADO")&&Hud.CompletedCount==1,"Reentering CMD restores station and HUD");
            foreach(var id in IDGSProgressHud.EventIds)GameProgress.RegisterEvent(id);
            Check(Hud.CompletedCount==6,"HUD reaches 6/6 with simulated remaining activities");
            IDGSVegetationValidation.Capture(Camera.main,Folder+"/hud-seis.png",1280,720);yield return null;
            GameProgress.RestoreCompletedIds(baseline.Concat(new[]{IDGSFilesChallenge.EventId}));
            File.WriteAllText(Folder+"/completed-test-save.json",PlayerPrefs.GetString(GameProgress.StorageKey));
            Check(!EditorUtility.scriptCompilationFailed,"Project compiles without errors");
        }
        public static string VerifyRestart()
        {
            if(!Application.isPlaying||!GameProgress.StorageKey.StartsWith("CampusQuest.Test.IDGSInteriors.",StringComparison.Ordinal))throw new InvalidOperationException("Isolated Play Mode required.");
            if(!Challenge.HasCompleted||Hud.CompletedCount!=1||!Challenge.stationState.text.Contains("COMPLETADO")||PlayerPrefs.GetString(GameProgress.StorageKey)!=File.ReadAllText(Folder+"/completed-test-save.json"))throw new InvalidOperationException("Progress did not restore.");
            File.AppendAllText(Folder+"/results.txt","PASS Real Play Mode restart restores files and HUD\n");
            return "Files and HUD restored after restarting Play Mode";
        }
    }
}
#endif
