using TMPro;
using UnityEngine;

namespace CampusQuest.Rectoria
{
    /// <summary>Orientation for the prepared scene; dialogues, puzzles and saving are a later stage.</summary>
    public sealed class RectoriaSceneGuide : MonoBehaviour
    {
        public RectoriaPlayerTest player;
        public RectoriaScenePoint[] points;
        public TMP_Text areaLabel;
        public TMP_Text contextLabel;
        private string lastArea;
        private string lastContext;

        public static string AreaAt(Vector3Int cell)
        {
            if (cell.y <= 8) return "RECIBIDOR";
            if (cell.y >= 11 && cell.y <= 20 && cell.x >= 15 && cell.x <= 28) return "OFICINA DE LA RECTORA";
            if (cell.y >= 21 && cell.y <= 32 && cell.x <= 15) return "ARCHIVO · DON CHUY";
            if (cell.y >= 21 && cell.y <= 32 && cell.x >= 28) return "CREDENCIALIZACIÓN · LIC. MONY";
            if (cell.y >= 33 && cell.x <= 14) return "CAJA · CAJERO BETO";
            if (cell.y >= 37 && cell.x >= 15 && cell.x <= 18) return "OFICINA DE TRÁMITES · ACCESO RESTRINGIDO";
            if (cell.y >= 37) return "PUERTA DE BLOQUES · GUARDIA ROCKY";
            return "PASILLO DE RECTORÍA";
        }

        private void LateUpdate()
        {
            if (player == null || player.map == null || player.map.grid == null) return;
            var cell = player.map.grid.WorldToCell(player.transform.position);
            string area = AreaAt(cell);
            string context = "WASD / Flechas: caminar     Z: correr";
            if (points != null)
                foreach (var point in points)
                {
                    if (point == null || !point.isActiveAndEnabled) continue;
                    if (point.kind == RectoriaPointKind.PhotoMark && point.cell == cell)
                    {
                        context = "Marca de fotografía · Credencialización";
                        break;
                    }
                    if (!point.IsFacing(player)) continue;
                    context = point.kind == RectoriaPointKind.LockedOffice
                        ? "Solo personal autorizado · oficina cerrada"
                        : "Frente a: " + point.displayName;
                    break;
                }
            if (areaLabel != null && area != lastArea) areaLabel.text = "RECTORÍA  /  " + area;
            if (contextLabel != null && context != lastContext) contextLabel.text = context;
            lastArea = area;
            lastContext = context;
        }
    }
}
