using System;
using System.Collections.Generic;
using CampusQuest.Dialogue;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace CampusQuest.Rectoria
{
    /// <summary>Opens only the existing gate tiles. The shared event registry owns persistence.</summary>
    [DefaultExecutionOrder(-140)]
    public sealed class RockyGateReview : MonoBehaviour
    {
        public const string ReviewId = "rectoria.rocky.review_dialogue";
        public const string ResumeId = "rectoria.rocky.resume_review";
        public const string UnlockId = "rectoria.blocks_gate.unlocked";
        public const string CompletionSignal = "rectoria.rocky.review_complete";
        public const string SceneCargaSp = "Carga_Sp";
        public DialogueSession session;
        public EnrollmentMission mission;
        public RectoriaPlayerTest player;
        public RectoriaGridMap map;
        public RectoriaMinimap minimap;
        public DialogueDefinition missingDialogue;
        public DialogueDefinition reviewDialogue;
        public DialogueDefinition resumeDialogue;
        public DialogueDefinition alreadyOpenDialogue;
        public Vector3Int[] gateCells;
        public GameObject lockRoot;
        public SpriteRenderer[] slots;
        public SpriteRenderer[] documents;
        public UnityEvent<string> onReviewCompleted = new UnityEvent<string>();
        public event Action ReviewCompleted;
        public bool IsOpen => session != null && session.HasCompleted(UnlockId);
        public bool IsOpening { get; private set; }
        public int LitSlots { get; private set; }
        public bool ConsumedInputThisFrame => lastInputFrame == Time.frameCount;
        public bool HasAllDocuments => mission != null
            && mission.HasDocument(EnrollmentDocument.BirthCertificate)
            && mission.HasDocument(EnrollmentDocument.StudentCard)
            && mission.HasDocument(EnrollmentDocument.PaymentReceipt);
        private TileBase[] closedTiles;
        private TileFlags[] closedFlags;
        private Color[] closedColors;
        private Matrix4x4[] closedMatrices;
        private Vector3[] documentTargets;
        private float elapsed;
        private int lastInputFrame = -1;
        private bool departing;
        private readonly Dictionary<Behaviour, bool> behaviours = new Dictionary<Behaviour, bool>();
        private readonly Dictionary<GameObject, bool> objects = new Dictionary<GameObject, bool>();
        private static readonly Color Gold = new Color32(233, 216, 166, 255);
        private static readonly Color Dark = new Color32(47, 51, 64, 255);

        private void Awake()
        {
            if (session == null || mission == null || map == null || map.doors == null || gateCells == null
                || gateCells.Length == 0 || slots == null || slots.Length != 3 || documents == null || documents.Length != 3)
            { Debug.LogError("RockyGateReview has incomplete gate references.", this); enabled = false; return; }
            if (player == null)
            {
                var interactor = FindAnyObjectByType<RectoriaDialogueInteractor>();
                if (interactor != null)
                    player = interactor.player;
            }
            closedTiles = new TileBase[gateCells.Length]; closedFlags = new TileFlags[gateCells.Length];
            closedColors = new Color[gateCells.Length]; closedMatrices = new Matrix4x4[gateCells.Length];
            for (int i = 0; i < gateCells.Length; i++)
            {
                closedTiles[i] = map.doors.GetTile(gateCells[i]); closedFlags[i] = map.doors.GetTileFlags(gateCells[i]);
                closedColors[i] = map.doors.GetColor(gateCells[i]); closedMatrices[i] = map.doors.GetTransformMatrix(gateCells[i]);
            }
            documentTargets = new Vector3[3];
            for (int i = 0; i < 3; i++) documentTargets[i] = documents[i].transform.localPosition;
            ApplyGate(IsOpen);
        }
        private void OnEnable()
        {
            if (session == null) return;
            session.Completed += OnDialogueCompleted; session.ProgressChanged += OnProgressChanged;
        }
        private void OnDisable()
        {
            if (session != null) { session.Completed -= OnDialogueCompleted; session.ProgressChanged -= OnProgressChanged; }
            CancelSequence();
        }

        public DialogueDefinition DialogueForRocky()
        {
            if (IsOpen) return alreadyOpenDialogue;
            if (!HasAllDocuments) return missingDialogue;
            return session.HasCompleted(ReviewId) ? resumeDialogue : reviewDialogue;
        }
        private void OnDialogueCompleted(string id, bool first)
        {
            if ((id == ReviewId || id == ResumeId) && HasAllDocuments && !IsOpen && !IsOpening) BeginSequence();
        }
        private void OnProgressChanged()
        {
            if (closedTiles == null) return;
            if (IsOpening && !HasAllDocuments) CancelSequence();
            if (!IsOpening) ApplyGate(IsOpen);
        }

        private void BeginSequence()
        {
            IsOpening = true; elapsed = 0; LitSlots = 0; lastInputFrame = Time.frameCount;
            behaviours.Clear(); objects.Clear();
            foreach (var behaviour in session.suspendWhileOpen)
            {
                if (behaviour == null || behaviour == this || behaviour == session || behaviours.ContainsKey(behaviour)) continue;
                behaviours.Add(behaviour, behaviour.enabled); behaviour.enabled = false;
            }
            foreach (var go in session.hideWhileOpen)
            {
                if (go == null || go == gameObject || objects.ContainsKey(go)) continue;
                objects.Add(go, go.activeSelf); go.SetActive(false);
            }
            session.view.SetPrompt(string.Empty);
            for (int i = 0; i < gateCells.Length; i++) map.doors.SetTileFlags(gateCells[i], TileFlags.None);
        }

        private void Update()
        {
            if (!IsOpening)
            {
                TryDepartForSp();
                return;
            }
            elapsed += Time.unscaledDeltaTime;
            for (int i = 0; i < 3; i++)
            {
                float t = Mathf.Clamp01((elapsed - i * .45f) / .4f);
                slots[i].color = Color.Lerp(Dark, Gold, t);
                documents[i].gameObject.SetActive(t > 0);
                documents[i].transform.localPosition = documentTargets[i] + new Vector3(0, -.65f * (1 - t), 0);
                documents[i].color = new Color(Gold.r, Gold.g, Gold.b, t);
            }
            LitSlots = Mathf.Clamp(Mathf.FloorToInt((elapsed - .4f) / .45f) + 1, 0, 3);
            float opening = Mathf.Clamp01((elapsed - 1.6f) / .5f);
            for (int i = 0; i < gateCells.Length; i++)
            {
                var color = closedColors[i]; color.a *= 1 - opening; map.doors.SetColor(gateCells[i], color);
                var offset = new Vector3((i < gateCells.Length / 2f ? -1 : 1) * opening * .7f, 0, 0);
                map.doors.SetTransformMatrix(gateCells[i], Matrix4x4.Translate(offset) * closedMatrices[i]);
            }
            if (elapsed < 2.1f) return;
            IsOpening = false; ApplyGate(true); ReleaseControls(); lastInputFrame = Time.frameCount;
            if (session.RegisterEvent(UnlockId))
            {
                // Fresh completion only; restoration and repeated conversations never emit it.
                onReviewCompleted.Invoke(CompletionSignal); ReviewCompleted?.Invoke();
            }
        }

        private void ApplyGate(bool open)
        {
            if (closedTiles == null) return;
            for (int i = 0; i < gateCells.Length; i++)
            {
                var cell = gateCells[i]; map.doors.SetTile(cell, open ? null : closedTiles[i]);
                if (!open)
                {
                    map.doors.SetTileFlags(cell, TileFlags.None); map.doors.SetColor(cell, closedColors[i]);
                    map.doors.SetTransformMatrix(cell, closedMatrices[i]); map.doors.SetTileFlags(cell, closedFlags[i]);
                }
            }
            var collider = map.doors.GetComponent<TilemapCollider2D>();
            if (collider != null && collider.hasTilemapChanges) collider.ProcessTilemapChanges();
            Physics2D.SyncTransforms();
            lockRoot.SetActive(!open); LitSlots = open ? 3 : 0;
            if (!open) for (int i = 0; i < 3; i++)
            { slots[i].color = Dark; documents[i].gameObject.SetActive(false); documents[i].transform.localPosition = documentTargets[i]; }
            if (minimap != null) minimap.RefreshMap();
        }
        private void ReleaseControls()
        {
            foreach (var state in behaviours) if (state.Key != null) state.Key.enabled = state.Value;
            foreach (var state in objects) if (state.Key != null) state.Key.SetActive(state.Value);
            behaviours.Clear(); objects.Clear();
        }
        private void CancelSequence()
        {
            if (!IsOpening) return;
            IsOpening = false; ReleaseControls(); ApplyGate(IsOpen); lastInputFrame = Time.frameCount;
        }

        private void TryDepartForSp()
        {
            if (departing || !IsOpen || player == null || !player.isActiveAndEnabled
                || map == null || map.grid == null || gateCells == null)
                return;

            var cell = map.grid.WorldToCell(player.transform.position);
            for (int i = 0; i < gateCells.Length; i++)
            {
                if (gateCells[i] != cell)
                    continue;
                departing = true;
                SceneManager.LoadScene(SceneCargaSp);
                return;
            }
        }
    }
}
