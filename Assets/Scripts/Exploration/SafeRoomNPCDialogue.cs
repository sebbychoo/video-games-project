using System.Collections;
using UnityEngine;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Attach to a bathroom or break room NPC. When the player first interacts
    /// with this NPC while at least one aggressive enemy is present on the floor,
    /// the NPC delivers a one-time contextual safety line.
    ///
    /// Requirements: 37.5
    /// </summary>
    public class SafeRoomNPCDialogue : MonoBehaviour
    {
        [Header("Dialogue Lines")]
        [Tooltip("Lines shown when enemies are on the floor. One is chosen at random.")]
        [SerializeField] private string[] safetyLines = new string[]
        {
            "Relax, they can't reach you in here.",
            "Don't worry — those things never come in here.",
            "You're safe. They stop at the door.",
            "Take a breath. This room's off-limits to them.",
        };

        [Header("UI")]
        [Tooltip("TextMeshPro label used to display the dialogue bubble. " +
                 "If null a world-space canvas is created automatically.")]
        [SerializeField] private TextMeshProUGUI dialogueLabel;

        [Tooltip("How long the line stays visible before fading out.")]
        [SerializeField] private float displayDuration = 3.5f;

        // Tracks whether the line has already been shown this session.
        private bool _hasSpoken;
        private Coroutine _hideCoroutine;

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Call this when the player opens the shop / trade UI.
        /// Shows the safety line once if enemies are present on the floor.
        /// </summary>
        public void OnPlayerInteract()
        {
            if (_hasSpoken) return;
            if (!EnemiesOnFloor()) return;

            _hasSpoken = true;
            string line = safetyLines[Random.Range(0, safetyLines.Length)];
            ShowLine(line);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static bool EnemiesOnFloor()
        {
            // Any EnemyFollow that is enabled (not frozen by a shop interaction)
            // and is aggressive counts as "present on the floor".
            foreach (var enemy in Object.FindObjectsByType<EnemyFollow>(FindObjectsSortMode.None))
            {
                if (enemy.enabled && enemy.isAggressive)
                    return true;
            }
            return false;
        }

        private void ShowLine(string line)
        {
            EnsureLabel();
            if (dialogueLabel == null) return;

            dialogueLabel.text = line;
            dialogueLabel.gameObject.SetActive(true);

            if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
            _hideCoroutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(displayDuration);
            if (dialogueLabel != null)
                dialogueLabel.gameObject.SetActive(false);
        }

        /// <summary>
        /// Auto-creates a minimal world-space canvas + TMP label if none is
        /// assigned in the Inspector, so the component works out of the box.
        /// </summary>
        private void EnsureLabel()
        {
            if (dialogueLabel != null) return;

            var canvasGO = new GameObject("NPCDialogueCanvas");
            canvasGO.transform.SetParent(transform, false);
            canvasGO.transform.localPosition = new Vector3(0f, 2.2f, 0f);

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;

            var rt = canvasGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(3f, 1f);

            var labelGO = new GameObject("DialogueText");
            labelGO.transform.SetParent(canvasGO.transform, false);

            var labelRt = labelGO.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            dialogueLabel = labelGO.AddComponent<TextMeshProUGUI>();
            dialogueLabel.alignment = TextAlignmentOptions.Center;
            dialogueLabel.fontSize = 0.18f;
            dialogueLabel.color = Color.white;
            dialogueLabel.enableWordWrapping = true;

            canvasGO.SetActive(false);
        }
    }
}
