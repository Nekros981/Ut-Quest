using System;
using System.Collections.Generic;
using UnityEngine;

namespace CampusQuest.Dialogue
{
    /// <summary>The shared story-event registry and its single persistence owner.</summary>
    public static class GameProgress
    {
        public const string SaveKey = "CampusQuest.Progress.v1";
#if UNITY_EDITOR
        public const string EditorTestStorageSetting = "CampusQuest.Progress.EditorTestKey";
#endif
        public static string StorageKey
        {
            get
            {
#if UNITY_EDITOR
                string testKey = UnityEditor.SessionState.GetString(EditorTestStorageSetting, "");
                if (testKey.StartsWith("CampusQuest.Test.", StringComparison.Ordinal)) return testKey;
#endif
                return SaveKey;
            }
        }

        [Serializable]
        private sealed class SaveData
        {
            public int version;
            public string[] events;
            public string playerName;
        }

        private static HashSet<string> completedIds = new HashSet<string>(StringComparer.Ordinal);
        private static string playerName = string.Empty;
        private static string loadedKey;
        private static bool writable;
        private static bool hasNameField;
        public static event Action Changed;
        public static event Action NewGameStarted;
        public static string LastLoadError { get; private set; }
        public static bool RecoveredFromBackup { get; private set; }
        public static string PlayerName { get { EnsureLoaded(); return playerName; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntime()
        {
            completedIds = new HashSet<string>(StringComparer.Ordinal);
            playerName = string.Empty;
            loadedKey = null;
            writable = false;
            hasNameField = false;
            LastLoadError = null;
            RecoveredFromBackup = false;
            Changed = null;
            NewGameStarted = null;
        }

        public static bool EnsureLoaded()
        {
            return loadedKey == StorageKey ? writable : Reload();
        }

        public static bool HasEvent(string id)
        {
            EnsureLoaded();
            return id != null && completedIds.Contains(id);
        }

        /// <summary>Returns true only when a new event has been persisted successfully.</summary>
        public static bool RegisterEvent(string id)
        {
            if (!EnsureLoaded() || string.IsNullOrWhiteSpace(id) || completedIds.Contains(id)) return false;
            var next = new HashSet<string>(completedIds, StringComparer.Ordinal) { id };
            return Commit(next, playerName);
        }

        /// <summary>Commits related completion events together using the same save and backup.</summary>
        public static bool RegisterEvents(params string[] ids)
        {
            if (ids == null || !EnsureLoaded()) return false;
            var next = new HashSet<string>(completedIds, StringComparer.Ordinal);
            foreach (string id in ids) if (!string.IsNullOrWhiteSpace(id)) next.Add(id);
            return !next.SetEquals(completedIds) && Commit(next, playerName);
        }

        public static bool ResetLevel(string level)
        {
            if ((level != "rectoria" && level != "idgs" && level != "sp") || !EnsureLoaded()) return false;
            var next = new HashSet<string>(completedIds, StringComparer.Ordinal);
            next.RemoveWhere(id => id.StartsWith(level + ".", StringComparison.Ordinal));
            return Commit(next, playerName);
        }

        public static string[] CaptureCompletedIds()
        {
            EnsureLoaded();
            return Sorted(completedIds);
        }

        /// <summary>Explicitly replaces the complete profile event snapshot, preserving its name.</summary>
        public static bool RestoreCompletedIds(IEnumerable<string> ids)
        {
            if (!EnsureLoaded()) return false;
            return Commit(Normalize(ids), playerName);
        }

        public static bool SetPlayerName(string value)
        {
            if (!EnsureLoaded()) return false;
            string next = (value ?? string.Empty).Trim();
            if (next == playerName && hasNameField) return true;
            return Commit(new HashSet<string>(completedIds, StringComparer.Ordinal), next);
        }

        internal static void ImportLegacyPlayerName(string value)
        {
            if (EnsureLoaded() && !hasNameField && !string.IsNullOrWhiteSpace(value)) SetPlayerName(value);
        }

        public static bool Save()
        {
            return EnsureLoaded() && Write(completedIds, playerName);
        }

        /// <summary>Reads without rewriting the save. Failed reads keep the current profile in memory.</summary>
        public static bool Reload()
        {
            string key = StorageKey;
            SaveData data;
            string error = null;
            bool recovered = false;
            if (!PlayerPrefs.HasKey(key)) data = new SaveData { version = 1, events = Array.Empty<string>() };
            else if (!TryRead(PlayerPrefs.GetString(key), out data, out error))
            {
                string backupError;
                if (!PlayerPrefs.HasKey(key + ".backup") || !TryRead(PlayerPrefs.GetString(key + ".backup"), out data, out backupError))
                {
                    loadedKey = key;
                    writable = false;
                    LastLoadError = error;
                    RecoveredFromBackup = false;
                    Debug.LogWarning("No se pudo cargar el progreso. El guardado se conserva: " + error);
                    return false;
                }
                recovered = true;
                Debug.LogWarning("Se cargó el respaldo de progreso. El archivo principal se conserva hasta el siguiente guardado.");
            }

            completedIds = Normalize(data.events);
            playerName = (data.playerName ?? string.Empty).Trim();
            hasNameField = data.playerName != null;
            loadedKey = key;
            writable = true;
            LastLoadError = recovered ? error : null;
            RecoveredFromBackup = recovered;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Explicit new-game action. Only progress keys are written.</summary>
        public static bool StartNewGame()
        {
            EnsureLoaded();
            var empty = new HashSet<string>(StringComparer.Ordinal);
            if (!Write(empty, string.Empty)) return false;
            completedIds = empty;
            playerName = string.Empty;
            hasNameField = true;
            writable = true;
            loadedKey = StorageKey;
            LastLoadError = null;
            RecoveredFromBackup = false;
            NewGameStarted?.Invoke();
            Changed?.Invoke();
            return true;
        }

        private static bool Commit(HashSet<string> ids, string name)
        {
            if (!Write(ids, name)) return false;
            completedIds = ids;
            playerName = name;
            hasNameField = true;
            Changed?.Invoke();
            return true;
        }

        private static bool Write(HashSet<string> ids, string name)
        {
            string key = StorageKey;
            string json = JsonUtility.ToJson(new SaveData { version = 1, events = Sorted(ids), playerName = name });
            bool existed = PlayerPrefs.HasKey(key);
            string old = PlayerPrefs.GetString(key, "");
            if (existed && old == json) return true;
            try
            {
                SaveData previous;
                string error;
                if (existed && TryRead(old, out previous, out error))
                {
                    PlayerPrefs.SetString(key + ".backup", old);
                    if (previous.playerName == null && !PlayerPrefs.HasKey(key + ".legacy-backup"))
                        PlayerPrefs.SetString(key + ".legacy-backup", old);
                }
                else if (existed && !PlayerPrefs.HasKey(key + ".unreadable"))
                    PlayerPrefs.SetString(key + ".unreadable", old);

                // Persist the backup before replacing the primary value.
                PlayerPrefs.Save();
                PlayerPrefs.SetString(key, json);
                PlayerPrefs.Save();
                return true;
            }
            catch (Exception error)
            {
                if (existed) PlayerPrefs.SetString(key, old);
                else PlayerPrefs.DeleteKey(key);
                Debug.LogError("No se pudo guardar el progreso: " + error.Message);
                return false;
            }
        }

        private static bool TryRead(string json, out SaveData data, out string error)
        {
            data = null;
            try
            {
                data = JsonUtility.FromJson<SaveData>(json);
                if (data == null || data.version != 1 || data.events == null)
                    throw new FormatException("Versión o lista de eventos inválida.");
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private static HashSet<string> Normalize(IEnumerable<string> ids)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            if (ids != null)
                foreach (string id in ids)
                    if (!string.IsNullOrWhiteSpace(id)) result.Add(id);
            return result;
        }

        private static string[] Sorted(HashSet<string> ids)
        {
            var result = new string[ids.Count];
            ids.CopyTo(result);
            Array.Sort(result, StringComparer.Ordinal);
            return result;
        }
    }
}
