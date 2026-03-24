using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Displays a vertical stack of status effect icons behind an entity's sprite.
    /// Each icon shows the effect name/icon and remaining duration number.
    /// Subscribes to StatusEffectEvents on BattleEventBus for real-time updates.
    /// Attach to a UI container positioned behind the entity.
    /// </summary>
    public class StatusEffectIconStack : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] GameObject iconPrefab; // Must have Image + TextMeshProUGUI children
        [SerializeField] GameObject trackedEntity; // The entity whose effects we display

        [Header("Layout")]
        [SerializeField] float iconSpacing = 30f;
        [SerializeField] float iconSize = 28f;

        [Header("Colors")]
        [SerializeField] Color burnColor = new Color(1f, 0.4f, 0f);
        [SerializeField] Color stunColor = new Color(1f, 1f, 0f);
        [SerializeField] Color bleedColor = new Color(0.8f, 0f, 0f);
        [SerializeField] Color defaultColor = Color.white;

        private readonly List<GameObject> _iconInstances = new List<GameObject>();

        /// <summary>Set the entity to track at runtime.</summary>
        public void Initialize(GameObject entity)
        {
            trackedEntity = entity;
            RefreshIcons();
        }

        private void OnEnable()
        {
            if (BattleEventBus.Instance != null)
            {
                BattleEventBus.Instance.OnStatusEffectApplied += OnStatusChanged;
                BattleEventBus.Instance.OnStatusEffectRemoved += OnStatusChanged;
            }
        }

        private void OnDisable()
        {
            if (BattleEventBus.Instance != null)
            {
                BattleEventBus.Instance.OnStatusEffectApplied -= OnStatusChanged;
                BattleEventBus.Instance.OnStatusEffectRemoved -= OnStatusChanged;
            }
        }

        private void OnStatusChanged(StatusEffectEvent e)
        {
            if (trackedEntity == null) return;
            if (e.Target != trackedEntity) return;
            RefreshIcons();
        }

        /// <summary>Rebuild the icon stack from the current status effects on the tracked entity.</summary>
        private void RefreshIcons()
        {
            // Clear existing icons
            foreach (GameObject icon in _iconInstances)
            {
                if (icon != null)
                    Destroy(icon);
            }
            _iconInstances.Clear();

            if (trackedEntity == null) return;

            // Get current effects from StatusEffectSystem
            StatusEffectSystem ses = FindObjectOfType<StatusEffectSystem>();
            if (ses == null) return;

            List<StatusEffectInstance> effects = ses.GetEffects(trackedEntity);
            if (effects == null || effects.Count == 0) return;

            for (int i = 0; i < effects.Count; i++)
            {
                StatusEffectInstance effect = effects[i];
                GameObject icon = CreateIcon(effect, i);
                if (icon != null)
                    _iconInstances.Add(icon);
            }
        }

        private GameObject CreateIcon(StatusEffectInstance effect, int index)
        {
            GameObject go;

            if (iconPrefab != null)
            {
                go = Instantiate(iconPrefab, transform);
            }
            else
            {
                // Fallback: create a simple icon with text
                go = new GameObject($"StatusIcon_{effect.effectId}");
                go.transform.SetParent(transform, false);

                RectTransform rt = go.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(iconSize, iconSize);

                Image bg = go.AddComponent<Image>();
                bg.color = GetEffectColor(effect.effectId);

                GameObject textGO = new GameObject("Duration");
                textGO.transform.SetParent(go.transform, false);
                RectTransform textRT = textGO.AddComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.sizeDelta = Vector2.zero;

                TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
                tmp.text = effect.duration.ToString();
                tmp.fontSize = iconSize * 0.6f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
            }

            // Position in vertical stack
            RectTransform iconRT = go.GetComponent<RectTransform>();
            if (iconRT != null)
            {
                iconRT.anchoredPosition = new Vector2(0f, index * iconSpacing);
                iconRT.sizeDelta = new Vector2(iconSize, iconSize);
            }

            // Update text if prefab has one
            TextMeshProUGUI label = go.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = effect.duration.ToString();

            // Update color if prefab has an Image
            Image img = go.GetComponent<Image>();
            if (img != null)
                img.color = GetEffectColor(effect.effectId);

            return go;
        }

        private Color GetEffectColor(string effectId)
        {
            if (effectId == StatusEffectSystem.Burn) return burnColor;
            if (effectId == StatusEffectSystem.Stun) return stunColor;
            if (effectId == StatusEffectSystem.Bleed) return bleedColor;
            return defaultColor;
        }

        /// <summary>Clear all icons (e.g., on entity death).</summary>
        public void ClearAll()
        {
            foreach (GameObject icon in _iconInstances)
            {
                if (icon != null)
                    Destroy(icon);
            }
            _iconInstances.Clear();
        }
    }
}
