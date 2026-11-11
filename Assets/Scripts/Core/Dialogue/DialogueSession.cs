using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace CampusQuest.Dialogue
{
    /// <summary>Shared conversation lifecycle. Knows nothing about NPCs, grid movement or missions.</summary>
    [DefaultExecutionOrder(-200)]
    public sealed class DialogueSession : MonoBehaviour
    {
        public DialogueView view;
        public DialoguePlayerContext playerContext;
        [Tooltip("Existing input behaviours temporarily suspended while a conversation is open.")]
        public Behaviour[] suspendWhileOpen = Array.Empty<Behaviour>();
        public GameObject[] hideWhileOpen = Array.Empty<GameObject>();
        public UnityEvent<string> onFirstCompletion = new UnityEvent<string>();
        public event Action<string, bool> Completed;
        public event Action ProgressChanged;

        private static DialogueSession active;
        private readonly Dictionary<Behaviour, bool> behaviourStates = new Dictionary<Behaviour, bool>();
        private readonly Dictionary<GameObject, bool> objectStates = new Dictionary<GameObject, bool>();
        private DialogueDefinition current;
        private int lineIndex = -1;
        private int lastInputFrame = -1;

        public static bool IsAnyOpen => active != null && active.IsOpen;
        public bool IsOpen => current != null;
        public int CurrentLineIndex => lineIndex;
        public int LastInputFrame => lastInputFrame;
        public string CurrentDialogueId => current != null ? current.dialogueId : string.Empty;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetActive() { active = null; }

        private void OnEnable()
        {
            GameProgress.Changed += OnProgressChanged;
            GameProgress.NewGameStarted += Abort;
            GameProgress.EnsureLoaded();
        }

        private void OnProgressChanged() { ProgressChanged?.Invoke(); }

        public static bool ConfirmPressedThisFrame()
        {
            var keyboard = Keyboard.current;
            return keyboard != null && (keyboard.eKey.wasPressedThisFrame
                || keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame);
        }

        private void Update()
        {
            if (IsOpen && ConfirmPressedThisFrame()) Advance();
        }

        public bool TryBegin(DialogueDefinition definition)
        {
            if (!isActiveAndEnabled || IsAnyOpen || lastInputFrame == Time.frameCount
                || definition == null || !definition.IsValid || view == null || !view.IsConfigured)
                return false;
            current = definition;
            lineIndex = 0;
            active = this;
            lastInputFrame = Time.frameCount;
            CaptureAndSuspend();
            view.SetPrompt(string.Empty);
            PresentLine();
            view.SetVisible(true);
            return true;
        }

        public bool Advance()
        {
            if (!IsOpen || lastInputFrame == Time.frameCount) return false;
            lastInputFrame = Time.frameCount;
            if (lineIndex + 1 < current.lines.Length)
            {
                lineIndex++;
                PresentLine();
            }
            else
            {
                string id = current.dialogueId;
                Close();
                bool first = RegisterEvent(id);
                if (first) onFirstCompletion.Invoke(id);
                Completed?.Invoke(id, first);
            }
            return true;
        }

        public bool HasCompleted(string dialogueId)
        {
            return GameProgress.HasEvent(dialogueId);
        }

        /// <summary>Shared idempotent story-event registry, also used by document rewards.</summary>
        public bool RegisterEvent(string id)
        {
            return GameProgress.RegisterEvent(id);
        }

        // Compatibility hooks operate on the complete shared profile, across all scenes.
        public string[] CaptureCompletedIds()
        {
            return GameProgress.CaptureCompletedIds();
        }

        public bool RestoreCompletedIds(IEnumerable<string> ids)
        {
            if (IsOpen) return false;
            return GameProgress.RestoreCompletedIds(ids);
        }

        public void Abort()
        {
            if (!IsOpen) return;
            lastInputFrame = Time.frameCount;
            Close();
        }

        private void PresentLine()
        {
            var line = current.lines[lineIndex];
            string speaker = string.IsNullOrWhiteSpace(line.speaker) ? current.defaultSpeaker : line.speaker;
            string text = line.text;
            if (playerContext != null) { speaker = playerContext.Resolve(speaker); text = playerContext.Resolve(text); }
            view.ShowLine(speaker, text, line.portrait != null ? line.portrait : current.defaultPortrait,
                lineIndex + 1, current.lines.Length);
        }

        private void CaptureAndSuspend()
        {
            behaviourStates.Clear(); objectStates.Clear();
            if (suspendWhileOpen != null)
                foreach (var behaviour in suspendWhileOpen)
                {
                    if (behaviour == null || behaviour == this || behaviour == view || behaviourStates.ContainsKey(behaviour)) continue;
                    behaviourStates.Add(behaviour, behaviour.enabled);
                    behaviour.enabled = false;
                }
            if (hideWhileOpen != null)
                foreach (var go in hideWhileOpen)
                {
                    if (go == null || go == gameObject || objectStates.ContainsKey(go)) continue;
                    objectStates.Add(go, go.activeSelf);
                    go.SetActive(false);
                }
        }

        private void Close()
        {
            current = null; lineIndex = -1;
            if (active == this) active = null;
            if (view != null) view.SetVisible(false);
            foreach (var state in behaviourStates) if (state.Key != null) state.Key.enabled = state.Value;
            foreach (var state in objectStates) if (state.Key != null) state.Key.SetActive(state.Value);
            behaviourStates.Clear(); objectStates.Clear();
        }

        private void OnDisable()
        {
            GameProgress.Changed -= OnProgressChanged;
            GameProgress.NewGameStarted -= Abort;
            Abort();
        }
        private void OnDestroy() { Abort(); }
    }
}
