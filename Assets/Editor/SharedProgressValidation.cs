#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using CampusQuest.Dialogue;
using CampusQuest.Rectoria;
using UnityEditor;
using UnityEngine;

namespace CampusQuest.EditorTools
{
    /// <summary>Explicit MCP-driven integration checks. Requires an isolated slot prepared by the caller.</summary>
    public static class SharedProgressValidation
    {
        private const string Folder = "tmp/shared-progress-source";
        private static void Guard()
        {
            if (!Application.isPlaying || !GameProgress.StorageKey.StartsWith("CampusQuest.Test.SharedProgress.", StringComparison.Ordinal))
                throw new InvalidOperationException("Requires Play Mode with an isolated SharedProgress test slot.");
        }

        private static void Check(bool valid, string message)
        {
            if (!valid) throw new InvalidOperationException(message);
            File.AppendAllText(Folder + "/results.txt", "PASS " + message + "\n");
        }

        private static string Json => PlayerPrefs.GetString(GameProgress.StorageKey);
        private static readonly string[] Activities = {
            "idgs.docencia.cables.completed", "idgs.docencia.circuit.completed", "idgs.docencia.memory.completed",
            "idgs.cmd.files.completed", "idgs.cmd.server.completed", "idgs.cmd.virus.completed"
        };

        public static string LegacyAndRewards()
        {
            Guard();
            var mission = UnityEngine.Object.FindFirstObjectByType<EnrollmentMission>();
            Check(mission != null, "Existing Rectoría scene loads with its mission");
            var session = mission.session;
            string fixture = File.ReadAllText(Path.Combine(File.ReadAllText("tmp/shared-progress-backup-path.txt"), "legacy-test-fixture.json"));
            Check(Json == fixture, "Legacy JSON was loaded without a rewrite");
            Check(mission.IsActive && mission.DocumentCount == 1, "Legacy mission and birth certificate restored");
            Check(session.HasCompleted("rectoria.archive.note_1") && session.HasCompleted("story.legacy.unknown_event"), "Clues and arbitrary narrative IDs preserved");
            Check(!mission.TryAwardDocument(EnrollmentDocument.BirthCertificate), "Previously awarded birth certificate cannot be awarded again");
            int notifications = 0;
            Action listener = () => notifications++;
            GameProgress.Changed += listener;
            try { Check(!session.RegisterEvent(EnrollmentMission.BirthEvent) && notifications == 0, "Duplicate event emits no new progress notification"); }
            finally { GameProgress.Changed -= listener; }
            Check(mission.TryAwardDocument(EnrollmentDocument.StudentCard), "Credential reward uses the shared registry");
            Check(mission.TryAwardDocument(EnrollmentDocument.PaymentReceipt), "Payment reward uses the shared registry");
            Check(mission.DocumentCount == 3 && mission.counterLabel.text.Contains("3/3"), "Original document HUD updates");
            Check(PlayerPrefs.GetString(GameProgress.StorageKey + ".legacy-backup") == fixture, "First legacy snapshot retained byte for byte");

            var contextObject = new GameObject("Temporary profile name test");
            try
            {
                var context = contextObject.AddComponent<DialoguePlayerContext>();
                context.SetPlayerName("  Alex UT  ");
                Check(context.PlayerName == "Alex UT" && context.Resolve("Hola {playerName}") == "Hola Alex UT", "Name API persists the personalized name and resolves dialogue text");
            }
            finally { UnityEngine.Object.DestroyImmediate(contextObject); }

            foreach (string id in Activities)
                Check(session.RegisterEvent(id) && !session.RegisterEvent(id), "IDGS activity is idempotent: " + id);
            Check(GameProgress.RegisterEvent("idgs.seal.awarded") && !GameProgress.RegisterEvent("idgs.seal.awarded"), "IDGS seal can be recorded once");
            Check(GameProgress.RegisterEvent("idgs.level.completed"), "Level completion shares the same save");
            Check(mission.DocumentCount == 3, "IDGS events do not change the three tutorial documents");
            Check(GameProgress.CaptureCompletedIds().Distinct().Count() == GameProgress.CaptureCompletedIds().Length, "Registry contains no duplicate IDs");
            File.WriteAllText(Folder + "/before-scene-change.json", Json);
            return "Legacy, document, IDGS and name checks passed";
        }

        public static string AfterSceneChange()
        {
            Guard();
            Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path == "Assets/Scenes/IDGS_Exterior.unity", "Actually changed to IDGS exterior");
            Check(UnityEngine.Object.FindFirstObjectByType<EnrollmentMission>() == null, "Progress works without EnrollmentMission in the scene");
            Check(Json == File.ReadAllText(Folder + "/before-scene-change.json"), "Scene transition did not rewrite or truncate the save");
            Check(GameProgress.HasEvent(EnrollmentMission.BirthEvent) && GameProgress.HasEvent(EnrollmentMission.CardEvent) && GameProgress.HasEvent(EnrollmentMission.PaymentEvent), "All tutorial documents survived unloading Rectoría");
            Check(Activities.All(GameProgress.HasEvent) && GameProgress.HasEvent("idgs.seal.awarded"), "Six IDGS activities and seal survived the scene change");
            var temp = new GameObject("Temporary second-scene dialogue session");
            try
            {
                var session = temp.AddComponent<DialogueSession>();
                Check(session.HasCompleted("story.legacy.unknown_event"), "New scene session sees the complete narrative registry");
                Check(!session.RegisterEvent(EnrollmentMission.BirthEvent), "New scene session cannot duplicate an old reward");
                Check(session.RegisterEvent("idgs.story.scene_visit"), "New narrative event can be saved outside the tutorial");
                Check(temp.AddComponent<DialoguePlayerContext>().PlayerName == "Alex UT", "New scene context reads the shared name");
            }
            finally { UnityEngine.Object.DestroyImmediate(temp); }
            File.WriteAllText(Folder + "/before-play-restart.json", Json);
            return "Cross-scene checks passed";
        }

        public static string AfterPlayRestart()
        {
            Guard();
            Check(GameProgress.EnsureLoaded(), "Shared profile loads after a real Play Mode restart");
            Check(Json == File.ReadAllText(Folder + "/before-play-restart.json"), "Restart preserves the exact stored snapshot");
            Check(Activities.All(GameProgress.HasEvent) && GameProgress.HasEvent("idgs.level.completed") && GameProgress.HasEvent("idgs.seal.awarded"), "Activities, level completion and seal restore after restart");
            Check(GameProgress.HasEvent("idgs.story.scene_visit") && GameProgress.HasEvent("story.legacy.unknown_event"), "Old and new narrative events restore after restart");
            Check(GameProgress.PlayerName == "Alex UT", "Personalized name restores after restart");
            var mission = UnityEngine.Object.FindFirstObjectByType<EnrollmentMission>();
            Check(mission != null && mission.DocumentCount == 3, "Returning Rectoría scene restores its complete HUD state");
            Check(!mission.TryAwardDocument(EnrollmentDocument.StudentCard), "Returning tutorial does not award the credential twice");

            string full = Json;
            Check(GameProgress.StartNewGame(), "Explicit new game succeeds");
            Check(GameProgress.CaptureCompletedIds().Length == 0 && GameProgress.PlayerName == "" && mission.DocumentCount == 0, "New game clears events, name and tutorial progress");
            Check(GameProgress.Reload() && GameProgress.CaptureCompletedIds().Length == 0, "New game remains empty after reload");
            Check(PlayerPrefs.GetString(GameProgress.StorageKey + ".backup") == full, "New game retains the prior profile as its backup");
            var legacyContext = new GameObject("Temporary legacy context after new game");
            legacyContext.SetActive(false);
            try
            {
                var context = legacyContext.AddComponent<DialoguePlayerContext>();
                var serialized = new SerializedObject(context);
                serialized.FindProperty("playerName").stringValue = "Old scene name";
                serialized.ApplyModifiedPropertiesWithoutUndo();
                legacyContext.SetActive(true);
                Check(context.PlayerName == "Estudiante", "Scene defaults cannot resurrect the old name after new game");
            }
            finally { UnityEngine.Object.DestroyImmediate(legacyContext); }
            Check(GameProgress.RegisterEvent("rectoria.rector_welcome") && mission.TryAwardDocument(EnrollmentDocument.BirthCertificate), "Fresh game can earn documents from zero");

            PlayerPrefs.SetString(GameProgress.StorageKey, full);
            PlayerPrefs.Save();
            Check(GameProgress.Reload() && mission.DocumentCount == 3, "Shared reload restores the existing mission presentation");
            Check(GameProgress.RegisterEvent("test.backup.checkpoint"), "A normal save creates a previous-snapshot backup");
            Check(PlayerPrefs.GetString(GameProgress.StorageKey + ".backup") == full, "Backup contains the exact previous valid snapshot");
            PlayerPrefs.SetString(GameProgress.StorageKey, "broken-json");
            PlayerPrefs.Save();
            Check(GameProgress.Reload() && GameProgress.RecoveredFromBackup && mission.DocumentCount == 3, "Unreadable primary can recover the valid backup");
            Check(Json == "broken-json", "Backup recovery does not automatically overwrite the unreadable primary");
            Check(GameProgress.RegisterEvent("test.recovery.saved"), "Recovered progress can be saved");
            Check(PlayerPrefs.GetString(GameProgress.StorageKey + ".unreadable") == "broken-json", "Original unreadable value is preserved before recovery writes");

            PlayerPrefs.SetString(GameProgress.StorageKey, "{\"version\":999,\"events\":[]}");
            PlayerPrefs.SetString(GameProgress.StorageKey + ".backup", "broken-backup");
            PlayerPrefs.Save();
            string unsupported = Json;
            Check(!GameProgress.Reload(), "Unsupported save with no valid backup reports failure");
            Check(!GameProgress.RegisterEvent("test.must.not.overwrite") && !GameProgress.Save() && Json == unsupported, "Failed load blocks automatic replacement of the existing save");
            PlayerPrefs.SetString(GameProgress.StorageKey, full);
            PlayerPrefs.Save();
            Check(GameProgress.Reload(), "Explicit valid snapshot restores the test profile");
            Check(!EditorUtility.scriptCompilationFailed, "Project compiles");
            return "Play restart, new game and backup checks passed";
        }
    }
}
#endif
