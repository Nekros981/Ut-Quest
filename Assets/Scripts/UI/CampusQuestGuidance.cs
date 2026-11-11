using System;
using System.Collections.Generic;
using System.Linq;
using CampusQuest.Dialogue;
using CampusQuest.IDGS;
using CampusQuest.Rectoria;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using static CampusQuest.IDGS.IDGSDesktopUI;

[DefaultExecutionOrder(1000)]
public sealed class CampusQuestGuidance : MonoBehaviour
{
    public string CurrentHint { get; private set; }
    public string TargetId { get; private set; }
    public IReadOnlyList<Vector3Int> Route => route;
    public Vector3Int GoalCell { get; private set; }
    public int VisibleArrows => arrows.Count(a => a.enabled);
    CampusQuestGameplayConfig config;
    RectoriaDialogueInteractor rectoria;
    EnrollmentMission mission;
    RectoriaSceneGuide sceneGuide;
    IDGSInteractionController idgs;
    Tilemap floor;
    Grid grid;
    TMP_Text objective;
    readonly List<Vector3Int> route = new List<Vector3Int>();
    readonly List<SpriteRenderer> arrows = new List<SpriteRenderer>();
    readonly Collider2D[] hits = new Collider2D[16];
    Vector3Int lastCell = new Vector3Int(int.MinValue, 0, 0);
    int focus;
    bool dirty = true;

    public void Configure(CampusQuestGameplayConfig data)
    {
        config = data; rectoria = FindFirstObjectByType<RectoriaDialogueInteractor>();
        idgs = FindFirstObjectByType<IDGSInteractionController>();
        if (rectoria != null)
        {
            mission = FindFirstObjectByType<EnrollmentMission>(); sceneGuide = FindFirstObjectByType<RectoriaSceneGuide>();
            floor = rectoria.player.map.floor; grid = rectoria.player.map.grid;
            rectoria.session.Completed += DialogueCompleted;
            rectoria.session.view.nextStepHint = PreviewHint;
            if (rectoria.archive != null) rectoria.archive.successDialogue = config.archiveSuccess;
        }
        else if (idgs != null)
        {
            floor = FindObjectsByType<Tilemap>(FindObjectsSortMode.None).Where(t => t.GetComponent<Collider2D>() == null && t.cellBounds.size.x > 8 && t.cellBounds.size.y > 8).OrderByDescending(t => t.cellBounds.size.x * t.cellBounds.size.y).FirstOrDefault();
            grid = floor != null ? floor.layoutGrid as Grid : null;
            var hud = idgs.prompt.canvas.transform;
            var frame = Box("Siguiente objetivo", hud, new Vector2(450, 90), Vector2.zero, Gold);
            frame.rectTransform.anchorMin = frame.rectTransform.anchorMax = frame.rectTransform.pivot = new Vector2(0, 1);
            frame.rectTransform.anchoredPosition = new Vector2(20, -152);
            Box("Fondo", frame.transform, new Vector2(446, 86), Vector2.zero, Dark);
            objective = Text("Indicación", frame.transform, config.font, "", new Vector2(430, 80), Vector2.zero, 17, Gold);
            objective.textWrappingMode = TextWrappingModes.Normal;
        }
        for (int i = 0; i < 4; i++)
        {
            var arrow = new GameObject("Ruta · Flecha " + (i + 1)).AddComponent<SpriteRenderer>();
            arrow.transform.SetParent(transform, false); arrow.sprite = config.guidanceArrow; arrow.sortingOrder = 25000; arrow.enabled = false; arrows.Add(arrow);
        }
        GameProgress.Changed += Changed;
        Refresh();
    }
    void Changed()
    {
        if (rectoria != null && !GameProgress.HasEvent(RectoriaDialogueInteractor.WelcomeId)) focus = 0;
        dirty = true;
    }
    void DialogueCompleted(string id, bool first)
    {
        if (id.StartsWith("rectoria.archive.")) focus = 0;
        else if (id.StartsWith("rectoria.credential.")) focus = 1;
        else if (id.StartsWith("rectoria.cashier.")) focus = 2;
        dirty = true;
    }
    bool HasDoc(int doc) => GameProgress.HasEvent(doc == 0 ? EnrollmentMission.BirthEvent : doc == 1 ? EnrollmentMission.CardEvent : EnrollmentMission.PaymentEvent);
    int FirstMissing() { for (int i = 0; i < 3; i++) if (!HasDoc(i)) return i; return 3; }
    RectoriaScenePoint RectoriaTarget(string preview, out string hint)
    {
        var points = rectoria.points;
        Func<string, RectoriaScenePoint> npc = id => points.FirstOrDefault(p => p.pointId == id);
        if (!GameProgress.HasEvent(RectoriaDialogueInteractor.WelcomeId) && preview != RectoriaDialogueInteractor.WelcomeId)
        { hint = "Habla con la Rectora en su oficina central."; return npc("rectora"); }
        if (FirstMissing() == 3)
        {
            if (GameProgress.HasEvent(RockyGateReview.UnlockId))
            { hint = "Puerta abierta. Avanza al norte hacia Seguridad Pública."; return points.FirstOrDefault(p => p.kind == RectoriaPointKind.BlocksGate); }
            hint = preview == RockyGateReview.ReviewId || preview == RockyGateReview.ResumeId ? "Espera a que Rocky revise los documentos y abra la puerta." : "Dirígete a la Puerta de Bloques y habla con Guardia Rocky.";
            return npc("rocky");
        }
        int selected = focus;
        if (preview.StartsWith("rectoria.archive.")) selected = 0;
        else if (preview.StartsWith("rectoria.credential.")) selected = 1;
        else if (preview.StartsWith("rectoria.cashier.")) selected = 2;
        if (selected < 0 || selected > 2 || HasDoc(selected)) selected = FirstMissing();
        if (selected == 0)
        {
            if (!GameProgress.HasEvent(ArchiveChallenge.IntroId) && preview != ArchiveChallenge.IntroId)
            { hint = "Ve al Archivo (oeste) y habla con Don Chuy."; return npc("don_chuy"); }
            var note = points.Where(p => p.kind == RectoriaPointKind.Clue && p.dialogue != null && !GameProgress.HasEvent(p.dialogue.dialogueId) && p.dialogue.dialogueId != preview)
                .OrderBy(p => (rectoria.player.transform.position - World(p.approachCell)).sqrMagnitude).FirstOrDefault();
            if (note != null) { hint = "Lee " + note.displayName + " en Archivo: acércate, mírala y pulsa E o Enter."; return note; }
            hint = "Ya leíste las cuatro notas. Vuelve con Don Chuy para resolver las carpetas."; return npc("don_chuy");
        }
        if (selected == 1)
        {
            if (!GameProgress.HasEvent("rectoria.credential.mony_intro") && preview != "rectoria.credential.mony_intro")
            { hint = "Ve a Credencialización (este) y habla con Lic. Mony."; return npc("lic_mony"); }
            hint = "Párate sobre la marca de fotografía y permanece quieto para Mony.";
            return points.FirstOrDefault(p => p.kind == RectoriaPointKind.PhotoMark);
        }
        if (!GameProgress.HasEvent("rectoria.cashier.beto_intro") && preview != "rectoria.cashier.beto_intro")
        { hint = "Ve a Caja (noroeste) y habla con Cajero Beto."; return npc("beto"); }
        hint = "Acércate a la mesa de recibos en Caja y reúne exactamente $1,850.";
        return points.FirstOrDefault(p => p.kind == RectoriaPointKind.ReceiptsTable);
    }
    string PreviewHint()
    {
        if (rectoria == null) return null;
        string hint; RectoriaTarget(rectoria.session.CurrentDialogueId ?? "", out hint); return hint;
    }
    IDGSInteractionPoint IdgsTarget(out string hint)
    {
        var scene = gameObject.scene.name;
        if (IDGSCompletion.AllActivitiesCompleted)
        {
            if (GameProgress.HasEvent(IDGSCompletion.SealEvent)) { hint = "Sello de IDGS conseguido.\nLos seis sistemas están restaurados."; return null; }
            hint = scene == "IDGS_CMD_Interior" ? "Los seis sistemas están restaurados.\nSal del CMD para recibir el sello." : "Dirígete al CMD\nRecibirás el sello al salir de su edificio.";
            return idgs.points.FirstOrDefault(p => scene == "IDGS_Exterior" ? p.pointId == "idgs.cmd" : p.kind == IDGSPointKind.Exit);
        }
        string[] docIds = { IDGSCablesChallenge.EventId, IDGSCircuitChallenge.EventId, IDGSMemoryChallenge.EventId };
        string[] cmdIds = { IDGSFilesChallenge.EventId, IDGSServerChallenge.EventId, IDGSVirusChallenge.EventId };
        string[] stationIds = scene == "IDGS_Docencia_Interior" ? new[] { "idgs.docencia.cables", "idgs.docencia.circuit", "idgs.docencia.memory" } : new[] { "idgs.cmd.files", "idgs.cmd.server", "idgs.cmd.virus" };
        if (scene == "IDGS_Exterior")
        {
            bool doc = docIds.Any(id => !GameProgress.HasEvent(id));
            hint = doc ? "Ve a Docencia III (sur).\nSus estaciones restauran tres sistemas." : "Ve al CMD (noreste).\nCompleta sus estaciones restantes.";
            return idgs.points.FirstOrDefault(p => p.pointId == (doc ? "idgs.docencia" : "idgs.cmd"));
        }
        var events = scene == "IDGS_Docencia_Interior" ? docIds : cmdIds;
        for (int i = 0; i < 3; i++) if (!GameProgress.HasEvent(events[i]))
        {
            var p = idgs.points.FirstOrDefault(x => x.pointId == stationIds[i]);
            if (p != null) { hint = "Ve a " + p.displayName + "\nEstación " + new[] { "oeste", "central-norte", "este" }[i] + ". Puedes elegir otro orden."; return p; }
        }
        hint = scene == "IDGS_Docencia_Interior" ? "Docencia III restaurada.\nSal al campus y dirígete al CMD." : "CMD restaurado.\nSal al campus y dirígete a Docencia III.";
        return idgs.points.FirstOrDefault(p => p.kind == IDGSPointKind.Exit);
    }
    Vector3 World(Vector3Int cell) => grid != null ? grid.GetCellCenterWorld(cell) : (Vector3)cell + new Vector3(.5f, .5f, 0);
    Vector3Int PlayerCell => rectoria != null ? grid.WorldToCell(rectoria.player.transform.position) : new Vector3Int(idgs.player.Cell.x, idgs.player.Cell.y, 0);
    void Refresh()
    {
        if (rectoria == null && idgs == null) return;
        string hint;
        if (rectoria != null)
        {
            var p = RectoriaTarget("", out hint); TargetId = p != null ? p.pointId : "";
            GoalCell = p == null ? PlayerCell : p.kind == RectoriaPointKind.PhotoMark || (p.kind == RectoriaPointKind.BlocksGate && GameProgress.HasEvent(RockyGateReview.UnlockId)) ? p.cell : p.approachCell;
        }
        else
        {
            var p = IdgsTarget(out hint); TargetId = p != null ? p.pointId : "";
            GoalCell = p != null ? new Vector3Int(p.approachCell.x, p.approachCell.y, 0) : PlayerCell;
        }
        CurrentHint = hint; lastCell = PlayerCell; dirty = false; BuildRoute();
        if (objective != null) objective.text = CurrentHint;
    }
    bool Walkable(Vector3Int cell)
    {
        if (floor == null || !floor.HasTile(cell)) return false;
        if (rectoria != null) return rectoria.player.map.CanEnter(cell);
        var collider = idgs.player.GetComponent<Collider2D>();
        Vector3 offset = collider.bounds.center - idgs.player.transform.position;
        int count = Physics2D.OverlapBox(World(cell) + offset, collider.bounds.size * .92f, 0, new ContactFilter2D { useTriggers = false }, hits);
        for (int i = 0; i < count; i++) if (!hits[i].transform.IsChildOf(idgs.player.transform)) return false;
        return true;
    }
    void BuildRoute()
    {
        route.Clear();
        if (floor == null || GoalCell == PlayerCell || !Walkable(GoalCell)) return;
        var visited = new Dictionary<Vector3Int, Vector3Int>(); var queue = new Queue<Vector3Int>();
        Vector3Int start = PlayerCell; visited[start] = start; queue.Enqueue(start);
        var directions = new[] { Vector3Int.up, Vector3Int.right, Vector3Int.down, Vector3Int.left };
        while (queue.Count > 0 && visited.Count < 10000)
        {
            var cell = queue.Dequeue(); if (cell == GoalCell) break;
            foreach (var direction in directions)
            {
                var next = cell + direction;
                if (visited.ContainsKey(next) || !floor.cellBounds.Contains(next) || !Walkable(next)) continue;
                visited[next] = cell; queue.Enqueue(next);
            }
        }
        if (!visited.ContainsKey(GoalCell)) return;
        for (var cell = GoalCell; cell != start; cell = visited[cell]) route.Add(cell);
        route.Add(start); route.Reverse();
    }
    void LateUpdate()
    {
        if (config == null || (rectoria == null && idgs == null)) return;
        if (dirty || PlayerCell != lastCell) Refresh();
        if (rectoria != null)
        {
            if (sceneGuide != null && sceneGuide.contextLabel != null) { sceneGuide.contextLabel.font = config.font; sceneGuide.contextLabel.text = "SIGUIENTE: " + CurrentHint; }
            if (mission != null && mission.IsActive && mission.objectiveLabel != null)
            {
                string title = TargetId.StartsWith("nota_") ? "Archivo · Lee las cuatro notas" : TargetId == "don_chuy" ? "Archivo · Don Chuy" : TargetId == "lic_mony" ? "Credencialización · Lic. Mony"
                    : TargetId == "foto" ? "Ubícate sobre la marca de fotografía" : TargetId == "beto" ? "Caja · Cajero Beto" : TargetId == "recibos" ? "Caja · Reúne recibos por $1,850"
                    : TargetId == "rocky" ? "Dirígete a la Puerta de Bloques" : TargetId == "bloques" ? "Puerta abierta · Avanza al norte" : "Habla con la Rectora";
                mission.objectiveLabel.text = title;
            }
        }
        bool visible = !CampusQuestPauseMenu.BlocksInput && !DialogueSession.IsAnyOpen
            && (rectoria != null ? rectoria.player.isActiveAndEnabled && (rectoria.credential == null || !rectoria.credential.IsBusy) && (rectoria.rocky == null || !rectoria.rocky.IsOpening) : idgs.player.enabled);
        for (int i = 0; i < arrows.Count; i++)
        {
            int step = 1 + i * 3;
            arrows[i].enabled = visible && config.guidanceArrow != null && step < route.Count;
            if (!arrows[i].enabled) continue;
            int next = Mathf.Min(step + 1, route.Count - 1);
            Vector3Int direction = route[next] - route[step]; if (direction == Vector3Int.zero) direction = route[step] - route[step - 1];
            arrows[i].transform.position = World(route[step]) + new Vector3(0, .06f * Mathf.Sin(CampusQuestGameClock.Now * 3), 0);
            arrows[i].transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }
    }
    void OnDestroy()
    {
        GameProgress.Changed -= Changed;
        if (rectoria != null && rectoria.session != null)
        {
            rectoria.session.Completed -= DialogueCompleted;
            if (rectoria.session.view != null) rectoria.session.view.nextStepHint = null;
        }
    }
}
