using System.Collections;
using UnityEngine;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Spawns floating text numbers for damage, block, OT cost, status effects, and healing.
    /// Each text drifts in a direction and fades out. Uses object pooling via simple instantiation.
    /// Subscribe to BattleEventBus events for automatic display.
    /// Attach to a Canvas in the battle scene.
    /// </summary>
    public class FloatingCombatText : MonoBehaviour
    {
        public static FloatingCombatText Instance { get; private set; }

        [Header("Prefab")]
        [SerializeField] GameObject textPrefab; // Must have TextMeshProUGUI + CanvasGroup

        [Header("Damage Text")]
        [SerializeField] Color damageColor = Color.red;
        [SerializeField] float damageDriftUp = 80f;
        [SerializeField] float damageDuration = 0.8f;

        [Header("Block Text")]
        [SerializeField] Color blockColor = new Color(0.3f, 0.6f, 1f);
        [SerializeField] float blockDriftUp = 60f;
        [SerializeField] float blockDuration = 0.7f;

        [Header("OT Cost Text")]
        [SerializeField] Color otCostColor = new Color(1f, 0.8f, 0f);
        [SerializeField] float otDriftDown = -60f;
        [SerializeField] float otDuration = 0.7f;

        [Header("Status Effect Text")]
        [SerializeField] Color statusColor = new Color(0.8f, 0.2f, 1f);
        [SerializeField] float statusDriftUp = 70f;
        [SerializeField] float statusDuration = 1f;

        [Header("Heal Text")]
        [SerializeField] Color healColor = Color.green;
        [SerializeField] float healDriftUp = 80f;
        [SerializeField] float healDuration = 0.8f;

        [Header("General")]
        [SerializeField] float fontSize = 28f;
        [SerializeField] float startScale = 0.5f;
        [SerializeField] float peakScale = 1.2f;
        [SerializeField] Camera worldCamera;

        private Canvas _canvas;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            _canvas = GetComponent<Canvas>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            if (BattleEventBus.Instance != null)
            {
                BattleEventBus.Instance.OnDamageDealt += OnDamage;
                BattleEventBus.Instance.OnBlockChanged += OnBlock;
                BattleEventBus.Instance.OnStatusEffectApplied += OnStatusApplied;
            }
        }

        private void OnDisable()
        {
            if (BattleEventBus.Instance != null)
            {
                BattleEventBus.Instance.OnDamageDealt -= OnDamage;
                BattleEventBus.Instance.OnBlockChanged -= OnBlock;
                BattleEventBus.Instance.OnStatusEffectApplied -= OnStatusApplied;
            }
        }

        // ── Event handlers ──────────────────────────────────────────────────

        private void OnDamage(DamageEvent e)
        {
            if (e.Target != null && e.Amount > 0)
                SpawnDamageText(e.Amount, e.Target.transform.position);
        }

        private void OnBlock(BlockEvent e)
        {
            if (e.Target != null && e.Amount > 0)
                SpawnBlockText(e.Amount, e.Target.transform.position);
        }

        private void OnStatusApplied(StatusEffectEvent e)
        {
            if (e.Target != null && !e.IsRemoval)
                SpawnStatusText(e.EffectName, e.Target.transform.position);
        }

        // ── Public API ──────────────────────────────────────────────────────

        /// <summary>Show floating damage number at world position, drifting up.</summary>
        public void SpawnDamageText(int amount, Vector3 worldPos)
        {
            SpawnText($"-{amount}", damageColor, worldPos, Vector2.up * damageDriftUp, damageDuration);
        }

        /// <summary>Show floating block number at world position.</summary>
        public void SpawnBlockText(int amount, Vector3 worldPos)
        {
            SpawnText($"+{amount}", blockColor, worldPos, Vector2.up * blockDriftUp, blockDuration);
        }

        /// <summary>Show floating OT cost number, drifting down.</summary>
        public void SpawnOTCostText(int cost, Vector3 worldPos)
        {
            SpawnText($"-{cost}", otCostColor, worldPos, Vector2.up * otDriftDown, otDuration);
        }

        /// <summary>Show floating status effect name at world position.</summary>
        public void SpawnStatusText(string effectName, Vector3 worldPos)
        {
            SpawnText(effectName, statusColor, worldPos, Vector2.up * statusDriftUp, statusDuration);
        }

        /// <summary>Show floating heal number in green at world position.</summary>
        public void SpawnHealText(int amount, Vector3 worldPos)
        {
            SpawnText($"+{amount}", healColor, worldPos, Vector2.up * healDriftUp, healDuration);
        }

        // ── Core spawn ──────────────────────────────────────────────────────

        private void SpawnText(string text, Color color, Vector3 worldPos, Vector2 drift, float duration)
        {
            if (textPrefab == null) return;

            GameObject go = Instantiate(textPrefab, transform);
            RectTransform rt = go.GetComponent<RectTransform>();
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            CanvasGroup cg = go.GetComponent<CanvasGroup>();

            if (tmp == null)
            {
                Destroy(go);
                return;
            }

            if (cg == null)
                cg = go.AddComponent<CanvasGroup>();

            tmp.text = text;
            tmp.color = color;
            tmp.fontSize = fontSize;

            // Convert world position to canvas position
            Vector2 screenPos = WorldToScreenPos(worldPos);
            rt.position = screenPos;

            // Add slight random horizontal offset for variety
            float xJitter = Random.Range(-20f, 20f);
            drift += Vector2.right * xJitter;

            StartCoroutine(FloatRoutine(rt, cg, drift, duration));
        }

        private Vector2 WorldToScreenPos(Vector3 worldPos)
        {
            Camera cam = worldCamera != null ? worldCamera : Camera.main;
            if (cam == null) return Vector2.zero;
            return cam.WorldToScreenPoint(worldPos);
        }

        private IEnumerator FloatRoutine(RectTransform rt, CanvasGroup cg, Vector2 drift, float duration)
        {
            Vector2 startPos = rt.anchoredPosition;
            Vector2 endPos = startPos + drift;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (rt == null) yield break;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Position: ease out
                float posT = 1f - (1f - t) * (1f - t);
                rt.anchoredPosition = Vector2.Lerp(startPos, endPos, posT);

                // Scale: pop up then settle
                float scale;
                if (t < 0.2f)
                    scale = Mathf.Lerp(startScale, peakScale, t / 0.2f);
                else
                    scale = Mathf.Lerp(peakScale, 1f, (t - 0.2f) / 0.8f);
                rt.localScale = Vector3.one * scale;

                // Fade: hold for 60%, then fade out
                if (t > 0.6f)
                    cg.alpha = 1f - ((t - 0.6f) / 0.4f);
                else
                    cg.alpha = 1f;

                yield return null;
            }

            if (rt != null)
                Destroy(rt.gameObject);
        }
    }
}
