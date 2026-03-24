using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// 1 paper per HP. Stack fills the container height exactly.
    /// FINAL NOTICE floats above the stack when HP &lt;= 10%.
    /// 
    /// Setup:
    /// - PaperContainer: empty RectTransform, set desired height (e.g. 150)
    /// - FinalNotice: child of PlayerHPPanel, BELOW PaperContainer in hierarchy
    /// - Paper prefab: simple UI Image, any size (script overrides it)
    /// </summary>
    public class PlayerHPStack : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] RectTransform paperContainer;
        [SerializeField] TextMeshProUGUI hpText;
        [SerializeField] TextMeshProUGUI finalNoticeText;
        [SerializeField] GameObject paperPrefab;

        [Header("Paper Look")]
        [SerializeField] int maxPapers = 50;
        [SerializeField] float paperWidth = 70f;
        [SerializeField] float paperXJitter = 1.5f;
        [SerializeField] float paperTiltRange = 0.8f;

        [Header("Fly Off")]
        [SerializeField] float flyDuration = 0.4f;
        [SerializeField] float flyDistance = 200f;

        private List<RectTransform> _papers = new List<RectTransform>();
        private int _maxHP;
        private float _paperThickness; // calculated: container height / maxHP

        // ── Event-driven auto-refresh ─────────────────────────────────────────
        private Health _trackedHealth;

        /// <summary>
        /// Set the Health component to track. When damage events arrive for this
        /// target, the stack auto-refreshes without BattleManager calling UpdateHP.
        /// </summary>
        public void SetTrackedHealth(Health health)
        {
            _trackedHealth = health;
        }

        private void OnEnable()
        {
            if (BattleEventBus.Instance != null)
                BattleEventBus.Instance.OnDamageReceived += HandleDamageReceived;
        }

        private void OnDisable()
        {
            if (BattleEventBus.Instance != null)
                BattleEventBus.Instance.OnDamageReceived -= HandleDamageReceived;
        }

        private void HandleDamageReceived(DamageEvent e)
        {
            if (_trackedHealth == null) return;

            // Only refresh when the damage target is the player we're tracking
            if (e.Target == _trackedHealth.gameObject)
                UpdateHP(_trackedHealth.currentHealth, _trackedHealth.maxHealth);
        }

        public void Initialize(int currentHP, int maxHP)
        {
            _maxHP = maxHP;

            foreach (var p in _papers)
                if (p != null) Destroy(p.gameObject);
            _papers.Clear();

            if (paperPrefab == null)
            {
                Debug.LogError("PlayerHPStack: paperPrefab not assigned!", this);
                return;
            }

            // Each paper's thickness = container height / maxPapers
            _paperThickness = paperContainer.rect.height / maxPapers;

            int paperCount = Mathf.Clamp(
                Mathf.CeilToInt((float)currentHP / maxHP * maxPapers),
                0, maxPapers);

            for (int i = 0; i < paperCount; i++)
                SpawnPaper(i);

            UpdateText(currentHP);
            UpdateFinalNotice(currentHP);
        }

        public void UpdateHP(int currentHP, int maxHP)
        {
            _maxHP = maxHP;
            int targetCount = Mathf.Clamp(
                Mathf.CeilToInt((float)Mathf.Max(currentHP, 0) / maxHP * maxPapers), 
                0, maxPapers);

            // Remove papers from top
            while (_papers.Count > targetCount)
            {
                if (_papers.Count == 0) break;
                RectTransform top = _papers[_papers.Count - 1];
                _papers.RemoveAt(_papers.Count - 1);
                StartCoroutine(FlyOffRoutine(top));
            }

            // Add papers if healed
            while (_papers.Count < targetCount)
                SpawnPaper(_papers.Count);

            UpdateText(Mathf.Max(currentHP, 0));
            UpdateFinalNotice(Mathf.Max(currentHP, 0));
        }

        private void SpawnPaper(int index)
        {
            GameObject go = Instantiate(paperPrefab, paperContainer);
            RectTransform rt = go.GetComponent<RectTransform>();

            // Override size — width from config, height calculated to fill container
            rt.sizeDelta = new Vector2(paperWidth, _paperThickness);
            rt.localScale = Vector3.one;

            float x = Random.Range(-paperXJitter, paperXJitter);
            float y = index * _paperThickness;
            float rot = Random.Range(-paperTiltRange, paperTiltRange);

            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.localEulerAngles = Vector3.zero;

            _papers.Add(rt);
        }

        private void UpdateText(int current)
        {
            if (hpText != null)
                hpText.text = $"{current} / {_maxHP}";
        }

        private void UpdateFinalNotice(int currentHP)
        {
            if (finalNoticeText == null) return;

            bool show = currentHP > 0 && currentHP <= Mathf.CeilToInt(_maxHP * 0.1f);
            finalNoticeText.gameObject.SetActive(show);

            if (show && _papers.Count > 0)
            {
                RectTransform topPaper = _papers[_papers.Count - 1];

                // Convert top paper position to the same parent space as finalNoticeText
                Vector3 worldPos = topPaper.transform.TransformPoint(new Vector3(0f, _paperThickness + 5f, 0f));
                Vector3 localPos = finalNoticeText.transform.parent.InverseTransformPoint(worldPos);

                RectTransform noticeRT = finalNoticeText.rectTransform;
                Vector2 pos = noticeRT.anchoredPosition;
                pos.y = localPos.y;
                noticeRT.anchoredPosition = pos;
            }
        }

        private IEnumerator FlyOffRoutine(RectTransform paper)
        {
            Vector2 startPos = paper.anchoredPosition;
            Vector2 flyDir = new Vector2(
                Random.Range(0.3f, 1f),
                Random.Range(0.5f, 1f)).normalized;
            Vector2 endPos = startPos + flyDir * flyDistance;

            float startRot = paper.localEulerAngles.z;
            float endRot = startRot + Random.Range(-360f, 360f);

            CanvasGroup cg = paper.GetComponent<CanvasGroup>();
            if (cg == null) cg = paper.gameObject.AddComponent<CanvasGroup>();

            float elapsed = 0f;
            while (elapsed < flyDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / flyDuration);
                paper.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                paper.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(startRot, endRot, t));
                cg.alpha = 1f - t;
                yield return null;
            }

            Destroy(paper.gameObject);
        }
    }
}
