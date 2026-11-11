using UnityEngine;

namespace CampusQuest.Dialogue
{
    /// <summary>Resolves the shared profile name; the serialized field imports older scene data.</summary>
    public sealed class DialoguePlayerContext : MonoBehaviour
    {
        [SerializeField] private string playerName;
        public string PlayerName => string.IsNullOrWhiteSpace(GameProgress.PlayerName) ? "Estudiante" : GameProgress.PlayerName;

        private void OnEnable() { GameProgress.ImportLegacyPlayerName(playerName); }

        public void SetPlayerName(string value)
        {
            GameProgress.SetPlayerName(value);
        }

        public string Resolve(string text)
        {
            return (text ?? string.Empty).Replace("{playerName}", PlayerName);
        }
    }
}
