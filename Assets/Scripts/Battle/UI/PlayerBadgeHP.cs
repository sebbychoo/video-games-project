using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace CardBattle
{
    /// <summary>
    /// Employee badge UI — swaps the entire badge sprite at HP thresholds.
    /// HP number overlaid on the badge, status effects along the bottom.
    /// </summary>
    public class PlayerBadgeHP : MonoBehaviour
    {
        [Header("Full Badge Sprites (healthy → dead)")]
        [SerializeField] Sprite badgeHealthy;   // 75-100%
        [SerializeField] Sprite badgeConcerned;  // 50-74%
        [SerializeField] Sprite badgeStressed;   // 25-49%
        [SerializeField] Sprite badgeCritical;   // 1-24%
        [SerializeField] Sprite badgeDead;       // 0%

        [Header("UI References")]
        [SerializeField] Image badgeImage;
        [SerializeField] TextMeshProUGUI hpText;
        [SerializeField] Transform effectsContainer;
        [SerializeField] GameObject effectIconPrefab;

        private Sprite _currentBadge;
        private readonly List<GameObject> _activeIcons = new List<GameObject>();

        private void Awake()
        {
            if (badgeImage == null)
                badgeImage = GetComponent<Image>();
        }

        public void UpdateHP(int current, int max)
        {
            if (hpText != null)
                hpText.text = $"{current}/{max}";

            float ratio = max > 0 ? (float)current / max : 0f;
            Sprite target;

            if (ratio > 0.75f) target = badgeHealthy;
            else if (ratio > 0.5f) target = badgeConcerned;
            else if (ratio > 0.25f) target = badgeStressed;
            else if (current > 0) target = badgeCritical;
            else target = badgeDead;

            if (target != null && target != _currentBadge)
            {
                _currentBadge = target;
                badgeImage.sprite = target;
            }
        }

        public void UpdateEffects(List<Sprite> effectSprites)
        {
            ClearEffects();
            if (effectsContainer == null || effectIconPrefab == null) return;

            foreach (var sprite in effectSprites)
            {
                GameObject icon = Instantiate(effectIconPrefab, effectsContainer);
                Image img = icon.GetComponent<Image>();
                if (img != null) img.sprite = sprite;
                _activeIcons.Add(icon);
            }
        }

        public void ClearEffects()
        {
            foreach (var icon in _activeIcons)
                if (icon != null) Destroy(icon);
            _activeIcons.Clear();
        }
    }
}
