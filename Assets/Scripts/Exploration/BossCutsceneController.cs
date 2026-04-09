using System.Collections;
using UnityEngine;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Trigger-volume component in the boss room. When the player enters,
    /// displays <see cref="EnemyCombatantData.preFightDialogue"/>, pauses input,
    /// and on dismiss initiates the battle transition via <see cref="Battlescene_Trigger"/>.
    /// If preFightDialogue is null/empty/whitespace, skips dialogue and triggers battle immediately.
    ///
    /// Requirements: 3.3, 4.1, 4.2, 4.3, 4.4, 4.5
    /// </summary>
    public class BossCutsceneController : MonoBehaviour
    {
        /// <summary>
        /// Static flag checked by other interaction triggers to prevent
        /// overlapping interactions while boss dialogue is displayed (Req 4.5).
        /// </summary>
        public static bool IsInteracting { get; set; }

        [SerializeField] private EnemyCombatantData bossData;
        [SerializeField] private Battlescene_Trigger battleTrigger;

        [Header("Dialogue UI")]
        [Tooltip("TextMeshPro label for displaying boss dialogue. Auto-created if null.")]
        [SerializeField] private TextMeshProUGUI dialogueLabel;

        [Tooltip("Optional background panel behind the dialogue text.")]
        [SerializeField] private GameObject dialoguePanel;

        private bool _triggered;
        private bool _dialogueActive;
        private GameObject _playerRoot;
        private CursorLockMode _previousLockState;
        private bool _previousCursorVisible;

        /// <summary>
        /// Allows LevelGenerator to assign boss data at runtime.
        /// </summary>
        public void SetBossData(EnemyCombatantData data)
        {
            bossData = data;
        }

        /// <summary>
        /// Allows LevelGenerator to assign the battle trigger at runtime.
        /// </summary>
        public void SetBattleTrigger(Battlescene_Trigger trigger)
        {
            battleTrigger = trigger;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_triggered) return;
            if (!other.CompareTag("Player")) return;

            // Don't trigger if another interaction is active (Req 4.5)
            if (WorkBoxTrigger.IsInteracting) return;
            if (BathroomShopTrigger.IsInteracting) return;
            if (BreakRoomTradeTrigger.IsInteracting) return;

            _triggered = true;
            _playerRoot = other.transform.root.gameObject;

            // Req 4.4: skip dialogue if preFightDialogue is null/empty/whitespace
            if (ShouldSkipDialogue())
            {
                TriggerBattle();
                return;
            }

            StartCoroutine(DialogueSequence());
        }

        private void Update()
        {
            if (!_dialogueActive) return;

            // Req 4.3: dismiss on interact key press or mouse click
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return)
                || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            {
                DismissDialogue();
            }
        }

        /// <summary>
        /// Returns true when preFightDialogue is null, empty, or whitespace-only (Req 4.4).
        /// </summary>
        public static bool ShouldSkipDialogue(string preFightDialogue)
        {
            return string.IsNullOrWhiteSpace(preFightDialogue);
        }

        private bool ShouldSkipDialogue()
        {
            if (bossData == null) return true;
            return ShouldSkipDialogue(bossData.preFightDialogue);
        }

        private IEnumerator DialogueSequence()
        {
            // Req 4.2: pause player movement input
            IsInteracting = true;
            _dialogueActive = true;
            SetPlayerControllers(false);

            // Unlock cursor so the player can click to dismiss
            _previousLockState = Cursor.lockState;
            _previousCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Show dialogue text (Req 4.1)
            EnsureDialogueLabel();
            if (dialogueLabel != null)
            {
                dialogueLabel.text = bossData.preFightDialogue;
                dialogueLabel.gameObject.SetActive(true);
            }
            if (dialoguePanel != null)
                dialoguePanel.SetActive(true);

            // Wait for dismiss (handled in Update)
            yield break;
        }

        private void DismissDialogue()
        {
            if (!_dialogueActive) return;
            _dialogueActive = false;

            // Hide dialogue UI
            if (dialogueLabel != null)
                dialogueLabel.gameObject.SetActive(false);
            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);

            // Restore cursor state
            Cursor.lockState = _previousLockState;
            Cursor.visible = _previousCursorVisible;

            IsInteracting = false;
            SetPlayerControllers(true);

            // Req 4.3: initiate battle transition
            TriggerBattle();
        }

        private void TriggerBattle()
        {
            if (battleTrigger != null)
                battleTrigger.TriggerEncounter();
            else
                Debug.LogWarning("BossCutsceneController: No Battlescene_Trigger assigned.");
        }

        // ── Player input control ─────────────────────────────────────

        private void SetPlayerControllers(bool enabled)
        {
            if (_playerRoot == null) return;

            foreach (var mb in _playerRoot.GetComponentsInChildren<MonoBehaviour>())
            {
                string typeName = mb.GetType().Name;
                if (typeName == "FirstPersonController" || typeName == "StarterAssetsInputs")
                {
                    mb.enabled = enabled;
                    if (!enabled && typeName == "StarterAssetsInputs")
                    {
                        var lookField = mb.GetType().GetField("look");
                        lookField?.SetValue(mb, Vector2.zero);
                        var moveField = mb.GetType().GetField("move");
                        moveField?.SetValue(mb, Vector2.zero);
                    }
                }
            }
        }

        // ── Dialogue UI auto-creation ────────────────────────────────

        /// <summary>
        /// Auto-creates a screen-space overlay canvas with a TMP label if none
        /// is assigned, so the component works out of the box.
        /// </summary>
        private void EnsureDialogueLabel()
        {
            if (dialogueLabel != null) return;

            var canvasGO = new GameObject("BossDialogueCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Background panel
            var panelGO = new GameObject("DialoguePanel");
            panelGO.transform.SetParent(canvasGO.transform, false);
            var panelRt = panelGO.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.1f, 0.05f);
            panelRt.anchorMax = new Vector2(0.9f, 0.25f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            var panelImg = panelGO.AddComponent<UnityEngine.UI.Image>();
            panelImg.color = new Color(0f, 0f, 0f, 0.8f);
            dialoguePanel = panelGO;

            // Text label
            var labelGO = new GameObject("DialogueText");
            labelGO.transform.SetParent(panelGO.transform, false);
            var labelRt = labelGO.AddComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0.05f, 0.1f);
            labelRt.anchorMax = new Vector2(0.95f, 0.9f);
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            dialogueLabel = labelGO.AddComponent<TextMeshProUGUI>();
            dialogueLabel.alignment = TextAlignmentOptions.Center;
            dialogueLabel.fontSize = 24f;
            dialogueLabel.color = Color.white;
            dialogueLabel.enableWordWrapping = true;

            canvasGO.SetActive(true);
            labelGO.SetActive(false);
        }

        private void OnDestroy()
        {
            // Clean up static flag if destroyed while dialogue is active
            if (_dialogueActive)
            {
                _dialogueActive = false;
                IsInteracting = false;
            }
        }
    }
}
