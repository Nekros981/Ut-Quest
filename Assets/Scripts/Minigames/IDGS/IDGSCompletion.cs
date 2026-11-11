using System.Linq;
using CampusQuest.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static CampusQuest.IDGS.IDGSDesktopUI;

namespace CampusQuest.IDGS
{
    public sealed class IDGSCompletion : MonoBehaviour
    {
        public const string RequestEvent = "idgs.cmd.final_review_requested";
        public const string SealEvent = "idgs.seal.awarded";
        public const string LevelEvent = "idgs.level.completed";
        public static bool AllActivitiesCompleted => IDGSProgressHud.EventIds.All(GameProgress.HasEvent);
        public bool IsOpen => modal != null && modal.IsOpen;
        public bool AnimationFinished { get; private set; }
        public bool RewardGrantedThisRun { get; private set; }
        public int FrameIndex { get; private set; }
        public Button ContinueButton { get; private set; }
        public Canvas canvas { get; private set; }
        CampusQuestGameplayConfig config;
        IDGSActivityModal modal;
        Image document;
        TMP_Text status, buttonLabel;
        float frameElapsed;
        int openedFrame;
        public void Configure(CampusQuestGameplayConfig data)
        {
            config = data;
            canvas = new GameObject("Sello de IDGS", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.transform.SetParent(transform, false); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = Camera.main;
            canvas.planeDistance = 1; canvas.sortingOrder = 31000;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var shade = Box("Fondo", canvas.transform, Vector2.zero, Vector2.zero, new Color32(3, 10, 18, 250), true);
            shade.rectTransform.anchorMin = Vector2.zero; shade.rectTransform.anchorMax = Vector2.one; shade.rectTransform.sizeDelta = Vector2.zero;
            Text("Título", shade.transform, config.font, "SELLO DE IDGS", new Vector2(1100, 55), new Vector2(0, 305), 32, Gold);
            var paper = Box("Respaldo del documento", shade.transform, new Vector2(512, 512), new Vector2(0, 17), new Color32(174, 163, 145, 255));
            document = Box("Acta", paper.transform, new Vector2(512, 512), Vector2.zero, Color.white);
            document.preserveAspect = true;
            status = Text("Estado", shade.transform, config.font, "Revisión de los seis sistemas completada.", new Vector2(1100, 42), new Vector2(0, -271), 22, Cyan);
            ContinueButton = Button("Continuar", shade.transform, config.font, "CONTINUAR · E / ENTER", new Vector2(520, 45), new Vector2(0, -322), Continue);
            buttonLabel = ContinueButton.GetComponentInChildren<TMP_Text>();
            var i = FindFirstObjectByType<IDGSInteractionController>();
            modal = new IDGSActivityModal(i.player, i, i.prompt.canvas.gameObject, canvas.gameObject);
        }
        public bool TryShow()
        {
            if (gameObject.scene.name != "IDGS_Exterior" || config == null || config.sealFrames == null || config.sealFrames.Length == 0 || IsOpen || !AllActivitiesCompleted
                || !GameProgress.HasEvent(RequestEvent) || GameProgress.HasEvent(SealEvent) || !modal.TryOpenArrival()) return false;
            AnimationFinished = false; RewardGrantedThisRun = false; FrameIndex = 0; openedFrame = Time.frameCount;
            frameElapsed = 0; ContinueButton.gameObject.SetActive(false); document.sprite = config.sealFrames[0];
            return true;
        }
        void Update()
        {
            if (CampusQuestPauseMenu.BlocksInput) return;
            if (!IsOpen) { TryShow(); return; }
            if (!AnimationFinished)
            {
                // Display every source frame even when scene import/loading delays a render.
                frameElapsed += Mathf.Min(CampusQuestGameClock.Delta, .1f);
                float seconds = FrameIndex < config.sealSeconds.Length ? config.sealSeconds[FrameIndex] : .13f;
                if (frameElapsed < seconds) return;
                frameElapsed -= seconds;
                if (FrameIndex == config.sealFrames.Length - 1) { AnimationFinished = true; CompleteReview(); }
                else { FrameIndex++; document.sprite = config.sealFrames[FrameIndex]; }
                return;
            }
            if (Time.frameCount != openedFrame && DialogueSession.ConfirmPressedThisFrame()) Continue();
        }
        void CompleteReview()
        {
            bool prior = GameProgress.HasEvent(SealEvent);
            bool saved = GameProgress.RegisterEvents(SealEvent, LevelEvent);
            RewardGrantedThisRun = !prior && saved;
            bool complete = GameProgress.HasEvent(SealEvent) && GameProgress.HasEvent(LevelEvent);
            status.text = complete ? "¡Sello de IDGS conseguido!" : "No se pudo guardar. Intenta de nuevo.";
            buttonLabel.text = complete ? "CONTINUAR · E / ENTER" : "REINTENTAR GUARDADO";
            ContinueButton.gameObject.SetActive(true);
        }
        public void Continue()
        {
            if (!IsOpen || !AnimationFinished || CampusQuestPauseMenu.BlocksInput) return;
            if (!GameProgress.HasEvent(SealEvent) || !GameProgress.HasEvent(LevelEvent)) { CompleteReview(); return; }
            modal.Close();
        }
        void OnDisable() { modal?.Close(); }
    }
}
