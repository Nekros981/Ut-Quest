using UnityEngine;

namespace CampusQuest.Dialogue
{
    /// <summary>Bind the chosen game-profile name here when the new-game screen is implemented.</summary>
    public sealed class DialoguePlayerContext : MonoBehaviour
    {
        [SerializeField] private string playerName;
        public string PlayerName => string.IsNullOrWhiteSpace(playerName) ? "Estudiante" : playerName;

        public void SetPlayerName(string value)
        {
            playerName = value == null ? string.Empty : value.Trim();
        }

        public string Resolve(string text)
        {
            return (text ?? string.Empty).Replace("{playerName}", PlayerName);
        }
    }
}
