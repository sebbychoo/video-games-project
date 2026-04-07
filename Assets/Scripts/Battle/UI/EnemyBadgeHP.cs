using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace CardBattle
{
    /// <summary>
    /// Enemy badge UI — same layout as player badge.
    /// Photo top-left, HP top-right, effects along bottom.
    /// Badge sprites come from EnemyCombatantData so each enemy type has unique art.
    /// </summary>
    public class EnemyBadgeHP : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] Image badgeImage;
        [SerializeField] TextMeshProUGUI hpText;
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] Transform effectsContainer;
        [SerializeField] GameObject effectIconPrefab;

        private Sprite _spriteHealthy;
        private Sprite _spriteConcerned;
        private Sprite _spriteStressed;
        private Sprite _spriteCritical;
        private Sprite _spriteDead;
        private Sprite _currentBadge;
        private readonly List<GameObject> _activeIcons = new List<GameObject>();

        private void Awake()
        {
            if (badgeImage == null)
                badgeImage = GetComponent<Image>();
        }

        /// <summary>
        /// Call once when the enemy spawns. Pass the 5 badge sprites from EnemyCombatantData.
        /// </summary>
        public void Initialize(string enemyName, int currentHP, int maxHP,
            Sprite healthy, Sprite concerned, Sprite stressed, Sprite critical, Sprite dead)
        {
            _spriteHealthy = healthy;
            _spriteConcerned = concerned;
            _spriteStressed = stressed;
            _spriteCritical = critical;
            _spriteDead = dead;

            if (nameText != null)
                nameText.text = enemyName;

            UpdateHP(currentHP, maxHP);
        }

        public void UpdateHP(int current, int max)
        {
            if (hpText != null)
                hpText.text = $"{current}/{max}";

            float ratio = max > 0 ? (float)current / max : 0f;
            Sprite target;

            if (current <= 0) target = _spriteDead;
            else if (ratio > 0.75f) target = _spriteHealthy;
            else if (ratio > 0.5f) target = _spriteConcerned;
            else if (ratio > 0.25f) target = _spriteStressed;
            else target = _spriteCritical;

            if (target != null && target != _currentBadge)
            {
                _currentBadge = target;
                if (badgeImage != null)
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
