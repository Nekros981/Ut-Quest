using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CampusQuest.Dialogue
{
    public sealed class DialogueView : MonoBehaviour
    {
        public CanvasGroup panel;
        public TMP_Text speakerLabel;
        public TMP_Text bodyLabel;
        public TMP_Text pageLabel;
        public TMP_Text portraitPlaceholder;
        public TMP_Text interactionHint;
        public TMP_Text continueHint;
        public GameObject promptRoot;
        public Image portraitImage;

        bool dialogueVisible;

        public bool IsConfigured => panel != null && speakerLabel != null && bodyLabel != null;

        void Awake()
        {
            if (continueHint == null)
                continueHint = FindContinueHint();
            SetVisible(false);
            SetPrompt(string.Empty);
        }

        void OnEnable() => CampusQuestActiveControls.Changed += OnControlsChanged;
        void OnDisable() => CampusQuestActiveControls.Changed -= OnControlsChanged;

        TMP_Text FindContinueHint()
        {
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (text != null && text.name == "Avanzar")
                    return text;
            }

            return null;
        }

        void OnControlsChanged()
        {
            if (dialogueVisible)
                RefreshContinueHint();
        }

        public void SetVisible(bool visible)
        {
            if (panel == null) return;
            dialogueVisible = visible;
            panel.alpha = visible ? 1 : 0;
            panel.interactable = visible;
            panel.blocksRaycasts = visible;
            if (visible)
                RefreshContinueHint();
        }

        public void ShowLine(string speaker, string text, Sprite portrait, int page, int total)
        {
            speakerLabel.richText = false; speakerLabel.text = speaker;
            bodyLabel.richText = false; bodyLabel.text = text;
            if (pageLabel != null) pageLabel.text = page + " / " + total;
            if (portraitImage != null) { portraitImage.sprite = portrait; portraitImage.enabled = portrait != null; }
            if (portraitPlaceholder != null)
            {
                portraitPlaceholder.text = "RETRATO\nPROVISIONAL";
                portraitPlaceholder.gameObject.SetActive(portrait == null);
            }

            RefreshContinueHint();
        }

        void RefreshContinueHint()
        {
            if (continueHint == null)
                continueHint = FindContinueHint();
            if (continueHint == null)
                return;

            string label = CampusQuestActiveControls.BindingLabel("UI", "Submit");
            continueHint.text = label + " · Continuar";
        }

        public void SetPrompt(string value)
        {
            if (interactionHint == null) return;
            interactionHint.text = value;
            if (promptRoot != null) promptRoot.SetActive(!string.IsNullOrEmpty(value));
            else interactionHint.gameObject.SetActive(!string.IsNullOrEmpty(value));
        }
    }
}
