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
    public static class GameplayFinishValidation
    {
        const string Folder = "tmp/gameplay-finish";
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
            keyboard = InputSystem.AddDevice<Keyboard>("FinishValidationKeyboard"); mouse = InputSystem.AddDevice<Mouse>("FinishValidationMouse");
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
            EditorApplication.update -= Tick; GameProgress.Changed -= Changed; if(CampusQuestPauseMenu.IsPaused)Pause?.Resume(); CampusQuestPauseMenu.QuitOverrideForValidation=null; if(gamepad!=null&&gamepad.added)InputSystem.RemoveDevice(gamepad); if(previousGamepad!=null&&previousGamepad.added)previousGamepad.MakeCurrent(); steps = null; Status = result;
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
            var path = Guide != null && Guide.TargetId == point.pointId ? Guide.Route.ToArray() : Array.Empty<Vector3Int>();
            if (path.Length > 1)
            {
                foreach (var waypoint in path.Skip(1)) foreach (var s in Walk(new Vector2Int(waypoint.x, waypoint.y))) yield return s;
            }
            else foreach (var s in Walk(point.approachCell)) yield return s;
            foreach (var s in Press(point.cell.y > point.approachCell.y ? Key.W : Key.S)) yield return s;
            string target = point.destinationScene;
            foreach (var s in Press(Key.Enter)) yield return s;
            float end = Time.realtimeSinceStartup + 30;
            while ((SceneManager.GetActiveScene().name != target || IDGSSceneTravel.IsChanging) && Time.realtimeSinceStartup < end) yield return null;
            foreach (var s in Frames(5)) yield return s;
            Check(SceneManager.GetActiveScene().name == target, "Door reaches " + target);
        }
        static CampusQuestPauseMenu Pause => UnityEngine.Object.FindFirstObjectByType<CampusQuestPauseMenu>();
        static CampusQuestGuidance Guide => UnityEngine.Object.FindFirstObjectByType<CampusQuestGuidance>();
        static Gamepad gamepad, previousGamepad;
        static IEnumerable<object> WaitSeconds(float seconds)
        {
            float end=Time.realtimeSinceStartup+seconds; while(Time.realtimeSinceStartup<end)yield return null;
        }
        static IEnumerable<object> Load(string scene)
        {
            var operation=SceneManager.LoadSceneAsync(scene);float end=Time.realtimeSinceStartup+40;
            while(!operation.isDone&&Time.realtimeSinceStartup<end)yield return null;
            foreach(var s in Frames(8))yield return s;
            Check(operation.isDone&&SceneManager.GetActiveScene().name==scene,"Scene loads: "+scene);
        }
        static void Capture(string file) { IDGSVegetationValidation.Capture(Camera.main,Folder+"/"+file,1280,720); }
        static IEnumerable<object> PauseServer()
        {
            foreach(var s in Approach(8,8))yield return s;
            foreach(var s in Press(Key.E))yield return s;
            var server=UnityEngine.Object.FindFirstObjectByType<IDGSServerChallenge>();
            float end=Time.realtimeSinceStartup+10;
            while(!server.CanCalibrate&&Time.realtimeSinceStartup<end)yield return null;
            Check(server.IsOpen&&server.CanCalibrate,"Server opens normally before pause");
            foreach(var s in Press(Key.P))yield return s;
            Check(CampusQuestPauseMenu.IsPaused&&Pause.Panel.activeInHierarchy&&Time.timeScale==0&&AudioListener.pause,"P opens visible pause and freezes gameplay and audio");
            Check(Pause.Buttons.Select(b=>b.GetComponentInChildren<TMPro.TMP_Text>().text).SequenceEqual(new[]{"Reanudar","Reiniciar","Salir","Menú principal"}),"Pause offers the four requested options");
            float position=server.IndicatorPosition,clock=CampusQuestGameClock.Now;int attempts=server.Attempts;Vector2Int cell=Player.Cell;
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            Keys(Key.Space,Key.D,Key.LeftShift);foreach(var s in WaitSeconds(.55f))yield return s;Keys();yield return null;
            Check(Mathf.Abs(server.IndicatorPosition-position)<.0001f&&Mathf.Abs(CampusQuestGameClock.Now-clock)<.0001f,"Server indicator and unscaled gameplay clock remain frozen");
            Check(Player.Cell==cell&&server.Attempts==attempts&&!server.CalibrateButton.IsInteractable(),"Paused input cannot walk, sprint or calibrate underneath the overlay");
            Capture("pausa.png");yield return null;Canvas.ForceUpdateCanvases();yield return null;
            foreach(var s in Click(Pause.Buttons[0].GetComponent<RectTransform>(),Pause.canvas))yield return s;
            Check(!CampusQuestPauseMenu.IsPaused&&Time.timeScale==1&&!AudioListener.pause&&server.IsOpen&&!Player.enabled,"Mouse Reanudar restores the active minigame without releasing Mateo");
            foreach(var s in WaitSeconds(.2f))yield return s;
            Check(Mathf.Abs(server.IndicatorPosition-position)>.01f,"Server movement resumes from paused timing");
            foreach(var s in Click(server.ExitButton.GetComponent<RectTransform>(),server.canvas))yield return s;
            foreach(var s in Press(Key.Escape))yield return s;
            Check(CampusQuestPauseMenu.IsPaused,"Escape pauses during exploration");
            foreach(var s in Press(Key.Escape))yield return s;
            Check(!CampusQuestPauseMenu.IsPaused&&Player.enabled,"Escape resumes exploration");
            previousGamepad=Gamepad.current;gamepad=InputSystem.AddDevice<Gamepad>("PauseValidationGamepad");gamepad.MakeCurrent();
            InputSystem.QueueStateEvent(gamepad,new GamepadState().WithButton(GamepadButton.Start));foreach(var s in Frames(3))yield return s;
            InputSystem.QueueStateEvent(gamepad,new GamepadState());foreach(var s in Frames(3))yield return s;
            Check(CampusQuestPauseMenu.IsPaused,"Gamepad Start opens pause");
            InputSystem.QueueStateEvent(gamepad,new GamepadState().WithButton(GamepadButton.Start));foreach(var s in Frames(3))yield return s;
            InputSystem.QueueStateEvent(gamepad,new GamepadState());foreach(var s in Frames(3))yield return s;
            Check(!CampusQuestPauseMenu.IsPaused,"Gamepad Start resumes");InputSystem.RemoveDevice(gamepad);gamepad=null;
            if(previousGamepad!=null&&previousGamepad.added)previousGamepad.MakeCurrent();
        }
        static IEnumerable<object> PauseMemory()
        {
            foreach(var s in Load("IDGS_Docencia_Interior"))yield return s;
            foreach(var s in Approach(12,7))yield return s;
            foreach(var s in Press(Key.E))yield return s;
            var memory=UnityEngine.Object.FindFirstObjectByType<IDGSMemoryChallenge>();
            float end=Time.realtimeSinceStartup+10;
            while(memory.Phase!=IDGSMemoryPhase.Showing&&Time.realtimeSinceStartup<end)yield return null;
            foreach(var s in Press(Key.P))yield return s;
            var phase=memory.Phase;int round=memory.Round,step=memory.PlaybackStep,lit=memory.LitIndex;
            foreach(var s in WaitSeconds(.75f))yield return s;
            Check(memory.Phase==phase&&memory.Round==round&&memory.PlaybackStep==step&&memory.LitIndex==lit,"Memory playback does not advance while paused");
            foreach(var s in Press(Key.P))yield return s;
            Check(memory.IsOpen&&!CampusQuestPauseMenu.IsPaused,"Memory remains open after resuming");
            memory.Close();foreach(var s in Frames(3))yield return s;
        }
        static IEnumerable<object> CompletionFlow()
        {
            foreach(var s in Load("IDGS_CMD_Interior"))yield return s;
            foreach(var id in IDGSProgressHud.EventIds.Where(id=>id!=IDGSMemoryChallenge.EventId))GameProgress.RegisterEvent(id);
            foreach(var s in Frames(3))yield return s;
            Check(!IDGSCompletion.AllActivitiesCompleted&&!UnityEngine.Object.FindFirstObjectByType<IDGSCompletion>().IsOpen,"Five activities cannot award or show the seal");
            foreach(var s in Travel(Interactor.points.Single(p=>p.kind==IDGSPointKind.Exit)))yield return s;
            foreach(var s in Travel(Interactor.points.Single(p=>p.pointId=="idgs.cmd")))yield return s;
            Check(!GameProgress.HasEvent(IDGSCompletion.RequestEvent)&&!UnityEngine.Object.FindFirstObjectByType<IDGSCompletion>().IsOpen,"Crossing CMD door with only five activities does not request the seal");
            foreach(var s in Load("IDGS_Docencia_Interior"))yield return s;
            GameProgress.RegisterEvent(IDGSMemoryChallenge.EventId);foreach(var s in Frames(3))yield return s;
            Check(Guide.CurrentHint.Contains("Dirígete al CMD"),"Sixth activity in Docencia directs player toward CMD");
            foreach(var s in Travel(Interactor.points.Single(p=>p.kind==IDGSPointKind.Exit)))yield return s;
            Check(Guide.TargetId=="idgs.cmd"&&Guide.Route.Count>0,"Exterior guidance leads to the existing CMD entrance");
            foreach(var s in Travel(Interactor.points.Single(p=>p.pointId=="idgs.cmd")))yield return s;
            Check(!GameProgress.HasEvent(IDGSCompletion.RequestEvent)&&!UnityEngine.Object.FindFirstObjectByType<IDGSCompletion>().IsOpen&&Guide.CurrentHint.Contains("Sal del CMD"),"Entering CMD with six directs player to exit without awarding inside");
            foreach(var s in Travel(Interactor.points.Single(p=>p.kind==IDGSPointKind.Exit)))yield return s;
            var completion=UnityEngine.Object.FindFirstObjectByType<IDGSCompletion>();
            float end=Time.realtimeSinceStartup+10;
            while(!completion.IsOpen&&Time.realtimeSinceStartup<end)yield return null;
            Check(SceneManager.GetActiveScene().name=="IDGS_Exterior"&&completion.IsOpen&&!Player.enabled&&!Interactor.enabled&&GameProgress.HasEvent(IDGSCompletion.RequestEvent),"Exiting CMD after six activities starts seal animation outside and blocks controls");
            foreach(var s in Press(Key.P))yield return s;
            int frame=completion.FrameIndex;foreach(var s in WaitSeconds(.5f))yield return s;
            Check(completion.FrameIndex==frame&&!completion.AnimationFinished,"Seal animation also pauses correctly");
            foreach(var s in Press(Key.P))yield return s;
            end=Time.realtimeSinceStartup+10;
            while(!completion.AnimationFinished&&Time.realtimeSinceStartup<end)yield return null;
            Check(completion.AnimationFinished&&completion.FrameIndex==8&&completion.RewardGrantedThisRun,"Nine original GIF frames finish and award the seal once");
            Check(GameProgress.HasEvent(IDGSCompletion.SealEvent)&&GameProgress.HasEvent(IDGSCompletion.LevelEvent),"Seal and level completion persist together through GameProgress");
            Capture("sello-idgs.png");yield return null;Canvas.ForceUpdateCanvases();yield return null;
            foreach(var s in Click(completion.ContinueButton.GetComponent<RectTransform>(),completion.canvas))yield return s;
            Check(!completion.IsOpen&&Player.enabled&&Guide.CurrentHint.Contains("Sello de IDGS conseguido"),"Continue restores controls and completed objective");
            string saved=PlayerPrefs.GetString(GameProgress.StorageKey);int count=changes;
            foreach(var s in Travel(Interactor.points.Single(p=>p.pointId=="idgs.cmd")))yield return s;
            foreach(var s in Travel(Interactor.points.Single(p=>p.kind==IDGSPointKind.Exit)))yield return s;
            foreach(var s in Travel(Interactor.points.Single(p=>p.pointId=="idgs.cmd")))yield return s;
            Check(!UnityEngine.Object.FindFirstObjectByType<IDGSCompletion>().IsOpen&&changes==count&&saved==PlayerPrefs.GetString(GameProgress.StorageKey),"Returning after award does not replay animation, duplicate events or rewrite save");
            File.WriteAllText(Folder+"/seal-completed.json",saved);
        }
        static IEnumerable<object> RectoryGuidance()
        {
            foreach(var s in Load("Rectoria_Tutorial_Arte_v01"))yield return s;
            GameProgress.ResetLevel("rectoria");foreach(var s in Frames(3))yield return s;
            var interactor=UnityEngine.Object.FindFirstObjectByType<CampusQuest.Rectoria.RectoriaDialogueInteractor>();var session=interactor.session;
            Check(Guide.TargetId=="rectora"&&Guide.Route.Count>0,"Tutorial starts with a reachable route to the Rectora");
            Check(Guide.Route.All(interactor.player.map.CanEnter),"Tutorial arrows follow logical walkable cells rather than walls");
            string[] names={"Rectora_Bienvenida","DonChuy_Introduccion","Mony_Fotografia","Beto_Recibos","Rocky_Faltan_Documentos"};
            foreach(string name in names)
            {
                var d=UnityEditor.AssetDatabase.LoadAssetAtPath<DialogueDefinition>("Assets/Data/Dialogs/Rectoria/"+name+".asset");
                Check(session.TryBegin(d),"Dialogue opens: "+name);
                Check(session.view.portraitImage.enabled&&session.view.portraitImage.sprite==d.defaultPortrait&&!session.view.portraitPlaceholder.gameObject.activeSelf,"Correct NPC sprite replaces provisional portrait: "+d.defaultSpeaker);
                if(name=="Rectora_Bienvenida")
                {
                    foreach(var s in Press(Key.P))yield return s;
                    int page=session.CurrentLineIndex;UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                    foreach(var s in Press(Key.Enter))yield return s;
                    Check(CampusQuestPauseMenu.IsPaused&&session.CurrentLineIndex==page,"Paused dialogue does not advance on Enter");
                    foreach(var s in Click(Pause.Buttons[0].GetComponent<RectTransform>(),Pause.canvas))yield return s;
                    Check(session.IsOpen&&!interactor.player.enabled,"Resuming preserves dialogue movement lock");
                    Capture("retrato-rectora.png");yield return null;
                }
                float end=Time.realtimeSinceStartup+10;
                while(session.IsOpen&&Time.realtimeSinceStartup<end)
                {
                    if(session.CurrentLineIndex==d.lines.Length-1)Check(session.view.bodyLabel.text.Contains("Siguiente paso:"),"Last dialogue page explains what to do next: "+d.defaultSpeaker);
                    foreach(var s in Press(Key.Enter))yield return s;
                }
                Check(!session.IsOpen,"Dialogue completes normally: "+name);foreach(var s in Frames(3))yield return s;
            }
            GameProgress.ResetLevel("rectoria");GameProgress.RegisterEvent(CampusQuest.Rectoria.RectoriaDialogueInteractor.WelcomeId);foreach(var s in Frames(3))yield return s;
            Check(Guide.TargetId=="don_chuy","After welcome guide points to Don Chuy");
            GameProgress.RegisterEvent(CampusQuest.Rectoria.ArchiveChallenge.IntroId);foreach(var s in Frames(3))yield return s;
            Check(Guide.TargetId.StartsWith("nota_"),"Don Chuy introduction guides toward an unread note");
            for(int n=1;n<=4;n++)GameProgress.RegisterEvent(CampusQuest.Rectoria.ArchiveChallenge.ClueId(n));foreach(var s in Frames(3))yield return s;
            Check(Guide.TargetId=="don_chuy","Four notes direct player back to Don Chuy");
            var archive=interactor.archive;interactor.player.transform.position=interactor.player.map.CellCenter(new Vector3Int(8,30,0));interactor.player.facing=Vector2Int.left;Physics2D.SyncTransforms();
            Check(archive.TryOpen(),"Archive still opens with four clues");
            for(int i=0;i<9;i++)if(archive.FolderAt(i).IsAnswer)
            {
                int guard=0;while(archive.SelectedIndex!=i&&guard++<10){int current=archive.SelectedIndex;archive.MoveSelection(current%3!=i%3?(current%3<i%3?Vector2Int.right:Vector2Int.left):(current/3<i/3?Vector2Int.down:Vector2Int.up));}
                break;
            }
            Check(archive.ConfirmSelection()&&archive.RewardGrantedThisRun,"Archive still awards birth certificate once");
            archive.Close();foreach(var s in Frames(3))yield return s;
            Check(session.IsOpen&&session.CurrentDialogueId=="rectoria.archive.acta_ready"&&session.view.bodyLabel.text.Contains("Siguiente paso:"),"Don Chuy now confirms the document and explains next destination");
            Capture("don-chuy-siguiente.png");yield return null;foreach(var s in Press(Key.Enter))yield return s;
            Check(Guide.TargetId=="lic_mony","After acta guidance moves to Credencialización");
            GameProgress.RegisterEvent("rectoria.credential.mony_intro");foreach(var s in Frames(3))yield return s;
            // The player may choose another order; preview the Mony step through its existing dialogue.
            var mony=AssetDatabase.LoadAssetAtPath<DialogueDefinition>("Assets/Data/Dialogs/Rectoria/Mony_Fotografia.asset");session.TryBegin(mony);session.Abort();
            var photo=interactor.points.Single(p=>p.kind==CampusQuest.Rectoria.RectoriaPointKind.PhotoMark);
            interactor.player.transform.position=interactor.player.map.CellCenter(photo.cell);Physics2D.SyncTransforms();
            foreach(var s in WaitSeconds(.25f))yield return s;
            foreach(var s in Press(Key.P))yield return s;
            float still=interactor.credential.StillSeconds;var stage=interactor.credential.Stage;
            foreach(var s in WaitSeconds(.6f))yield return s;
            Check(interactor.credential.StillSeconds==still&&interactor.credential.Stage==stage,"Photo stillness and countdown remain frozen in pause");
            foreach(var s in Press(Key.P))yield return s;interactor.credential.Close(false);
            interactor.player.transform.position=interactor.player.map.CellCenter(new Vector3Int(36,30,0));Physics2D.SyncTransforms();
            GameProgress.RegisterEvent(CampusQuest.Rectoria.EnrollmentMission.CardEvent);foreach(var s in Frames(3))yield return s;
            Check(Guide.TargetId=="beto","After credential guidance points to Caja");
            GameProgress.RegisterEvent("rectoria.cashier.beto_intro");foreach(var s in Frames(3))yield return s;
            Check(Guide.TargetId=="recibos","After Beto dialogue guidance points to receipt table");
            GameProgress.RegisterEvent(CampusQuest.Rectoria.EnrollmentMission.PaymentEvent);foreach(var s in Frames(3))yield return s;
            Check(Guide.TargetId=="rocky"&&Guide.CurrentHint.Contains("Puerta de Bloques"),"Three documents guide the player toward Rocky");
            Capture("flechas-rectoria.png");yield return null;
        }
        static IEnumerable<object> ScopedRestart(string scope,string destination)
        {
            string name=GameProgress.PlayerName;Pause.Open();foreach(var s in Frames(2))yield return s;
            foreach(var s in Click(Pause.Buttons[1].GetComponent<RectTransform>(),Pause.canvas))yield return s;
            float end=Time.realtimeSinceStartup+30;while(SceneManager.GetActiveScene().name!=destination&&Time.realtimeSinceStartup<end)yield return null;
            foreach(var s in Frames(5))yield return s;
            Check(SceneManager.GetActiveScene().name==destination&&!GameProgress.CaptureCompletedIds().Any(id=>id.StartsWith(scope+".",StringComparison.Ordinal)),"Reiniciar clears only "+scope+" events and returns to level entrance");
            Check(GameProgress.PlayerName==name&&Time.timeScale==1&&!CampusQuestPauseMenu.IsPaused,"Restart preserves name and releases pause state");
        }
        static IEnumerator Validate()
        {
            Check(Pause!=null&&Guide!=null,"Gameplay bootstrap installs pause and guidance in CMD");
            GameProgress.ResetLevel("idgs");GameProgress.RegisterEvent("sp.test.keep_me");
            foreach(var s in PauseServer())yield return s;
            foreach(var s in PauseMemory())yield return s;
            foreach(var s in CompletionFlow())yield return s;
            foreach(var s in RectoryGuidance())yield return s;
            foreach(var s in ScopedRestart("rectoria","Rectoria_Tutorial_Arte_v01"))yield return s;
            Check(IDGSCompletion.AllActivitiesCompleted&&GameProgress.HasEvent(IDGSCompletion.SealEvent)&&GameProgress.HasEvent("sp.test.keep_me"),"Rectory restart preserves IDGS seal and SP events");
            GameProgress.RegisterEvent("rectoria.rector_welcome");
            foreach(var s in Load("IDGS_CMD_Interior"))yield return s;
            foreach(var s in ScopedRestart("idgs","IDGS_Exterior"))yield return s;
            Check(GameProgress.HasEvent("rectoria.rector_welcome")&&GameProgress.HasEvent("sp.test.keep_me"),"IDGS restart preserves Rectoría and SP progress");
            GameProgress.RegisterEvent(IDGSCablesChallenge.EventId);
            foreach(var s in Load("Sp_Bunker"))yield return s;
            Check(Pause!=null&&Guide==null,"Pause is available in SP without adding tutorial guidance there");
            foreach(var s in ScopedRestart("sp","Sp_Bosque"))yield return s;
            Check(GameProgress.HasEvent("rectoria.rector_welcome")&&GameProgress.HasEvent(IDGSCablesChallenge.EventId),"SP restart preserves other levels");
            bool quitCalled=false;CampusQuestPauseMenu.QuitOverrideForValidation=()=>quitCalled=true;Pause.Open();foreach(var s in Frames(2))yield return s;
            foreach(var s in Click(Pause.Buttons[2].GetComponent<RectTransform>(),Pause.canvas))yield return s;
            Check(quitCalled&&!CampusQuestPauseMenu.IsPaused&&Time.timeScale==1,"Salir requests application exit and restores time in Editor-safe validation");CampusQuestPauseMenu.QuitOverrideForValidation=null;
            Pause.Open();foreach(var s in Frames(2))yield return s;
            foreach(var s in Click(Pause.Buttons[3].GetComponent<RectTransform>(),Pause.canvas))yield return s;
            foreach(var s in Frames(8))yield return s;
            Check(SceneManager.GetActiveScene().name=="SampleScene"&&UnityEngine.Object.FindFirstObjectByType<global::MainMenu>()!=null&&GameObject.Find("Btn_Iniciar")!=null,"Menú principal returns to original screen with Iniciar");
            string key=GameProgress.StorageKey;Check(key.StartsWith("CampusQuest.Test.IDGSInteriors.",StringComparison.Ordinal),"All destructive restart cases stayed inside temporary save slot");
            PlayerPrefs.SetString(key,File.ReadAllText(Folder+"/seal-completed.json"));PlayerPrefs.Save();GameProgress.Reload();
            foreach(var s in Load("IDGS_CMD_Interior"))yield return s;
            Check(!UnityEngine.Object.FindFirstObjectByType<IDGSCompletion>().IsOpen&&IDGSCompletion.AllActivitiesCompleted,"Saved completed review restores without replay");
            File.WriteAllText(Folder+"/completed-test-save.json",PlayerPrefs.GetString(key));
            Check(!EditorUtility.scriptCompilationFailed,"Project compiles without errors");
        }
        public static string VerifyRestart()
        {
            if(!Application.isPlaying||!GameProgress.StorageKey.StartsWith("CampusQuest.Test.IDGSInteriors.",StringComparison.Ordinal))throw new InvalidOperationException("Isolated Play Mode required");
            if(!IDGSCompletion.AllActivitiesCompleted||!GameProgress.HasEvent(IDGSCompletion.SealEvent)||UnityEngine.Object.FindFirstObjectByType<IDGSCompletion>().IsOpen||PlayerPrefs.GetString(GameProgress.StorageKey)!=File.ReadAllText(Folder+"/completed-test-save.json"))throw new InvalidOperationException("Completed seal failed restoration");
            File.AppendAllText(Folder+"/results.txt","PASS Real Play Mode restart preserves six activities and seal without replay\n");return "Seal and activities restored after Play Mode restart";
        }
    }
}
#endif
