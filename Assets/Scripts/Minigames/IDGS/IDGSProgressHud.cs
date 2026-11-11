using System.Collections.Generic;
using System.Linq;
using CampusQuest.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static CampusQuest.IDGS.IDGSDesktopUI;

namespace CampusQuest.IDGS
{
    public sealed class IDGSProgressHud : MonoBehaviour
    {
        // The last two retain the event IDs reserved in Propuesta_Interiores_v01.md.
        public static readonly IReadOnlyList<string> EventIds = System.Array.AsReadOnly(new[] {
            IDGSCablesChallenge.EventId, IDGSCircuitChallenge.EventId, IDGSMemoryChallenge.EventId,
            IDGSFilesChallenge.EventId, "idgs.cmd.server.completed", "idgs.cmd.virus.completed"
        });
        public TMP_FontAsset font;
        public TMP_Text Label { get; private set; }
        public int CompletedCount => EventIds.Count(GameProgress.HasEvent);
        readonly Image[] indicators = new Image[6];
        void Awake()
        {
            var border = Box("Progreso IDGS", transform, new Vector2(410, 68), Vector2.zero, Gold);
            border.rectTransform.anchorMin = border.rectTransform.anchorMax = new Vector2(0, 1);
            border.rectTransform.pivot = new Vector2(0, 1); border.rectTransform.anchoredPosition = new Vector2(20, -76);
            Box("Fondo", border.transform, new Vector2(406, 64), Vector2.zero, Dark);
            Label = Text("Sistemas restaurados", border.transform, font, "", new Vector2(400, 34), new Vector2(0, 9), 18, Gold);
            for (int i = 0; i < 6; i++) indicators[i] = Box("Sistema " + (i + 1), border.transform, new Vector2(52, 5), new Vector2((i - 2.5f) * 62, -20), Dark);
            Refresh();
        }
        void OnEnable() { GameProgress.Changed += Refresh; Refresh(); }
        void OnDisable() { GameProgress.Changed -= Refresh; }
        void Refresh()
        {
            if (Label == null) return;
            Label.text = "Sistemas restaurados: " + CompletedCount + "/6";
            for (int i = 0; i < 6; i++) indicators[i].color = GameProgress.HasEvent(EventIds[i]) ? Cyan : new Color32(63, 78, 89, 255);
        }
    }
}
