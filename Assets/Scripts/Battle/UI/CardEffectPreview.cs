using UnityEngine;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Tooltip that shows calculated effective values when hovering over a card.
    /// Reflects source-side modifiers only (Rage Burst bonus, Tool modifiers).
    /// Does not account for target-specific effects like Bleed since target is unknown.
    /// Attach to a UI panel with a TextMeshProUGUI child.
    /// </summary>
    public class CardEffectPreview : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI tooltipText;
        [SerializeField] RectTransform tooltipPanel;
        [SerializeField] Vector2 offset = new Vector2(0f, 120f);
        [SerializeField] Camera uiCamera;

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            Hide();
        }

        /// <summary>Show the tooltip for the given card near the card's position.</summary>
        public void Show(CardInstance card)
        {
            if (card == null || card.Data == null || tooltipText == null) return;

            string preview = BuildPreviewText(card.Data);
            if (string.IsNullOrEmpty(preview))
            {
                Hide();
                return;
            }

            tooltipText.text = preview;

            // Position near the card
            if (tooltipPanel != null && card.RectTransform != null)
            {
                tooltipPanel.position = card.RectTransform.position + (Vector3)offset;
            }

            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        /// <summary>Build the preview text showing effective values after source-side modifiers.</summary>
        private string BuildPreviewText(CardData data)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            switch (data.cardType)
            {
                case CardType.Attack:
                    int baseDmg = data.effectValue;
                    int rageBurstBonus = GetRageBurstPreview(baseDmg);
                    int toolDmgBonus = GetToolModifierValue(ToolModifierType.DamageBonus);
                    int totalDmg = baseDmg + rageBurstBonus + toolDmgBonus;

                    sb.Append($"Damage: {totalDmg}");
                    if (rageBurstBonus > 0)
                        sb.Append($" <color=#FF6600>(+{rageBurstBonus} Rage)</color>");
                    if (toolDmgBonus > 0)
                        sb.Append($" <color=#00CCFF>(+{toolDmgBonus} Tool)</color>");

                    if (data.targetMode == TargetMode.AllEnemies)
                        sb.Append("\n<i>Hits all enemies</i>");
                    break;

                case CardType.Defense:
                    int baseBlock = data.blockValue;
                    int toolBlockBonus = GetToolModifierValue(ToolModifierType.BlockBonus);
                    int totalBlock = baseBlock + toolBlockBonus;

                    sb.Append($"Block: {totalBlock}");
                    if (toolBlockBonus > 0)
                        sb.Append($" <color=#00CCFF>(+{toolBlockBonus} Tool)</color>");
                    break;

                case CardType.Effect:
                    sb.Append($"Apply {data.statusEffectId}");
                    if (data.statusDuration > 0)
                        sb.Append($" ({data.statusDuration} turns)");
                    if (data.effectValue > 0)
                        sb.Append($"\nValue: {data.effectValue}");
                    break;

                case CardType.Utility:
                    switch (data.utilityEffectType)
                    {
                        case UtilityEffectType.Draw:
                            sb.Append($"Draw {data.effectValue} card(s)");
                            break;
                        case UtilityEffectType.Restore:
                            sb.Append($"Restore {data.effectValue} OT");
                            break;
                        case UtilityEffectType.Retrieve:
                            sb.Append($"Retrieve {data.effectValue} card(s)");
                            break;
                        case UtilityEffectType.Reorder:
                            sb.Append($"Reorder top {data.effectValue} card(s)");
                            break;
                        case UtilityEffectType.Heal:
                            sb.Append($"Heal {data.effectValue} HP");
                            break;
                    }
                    break;

                case CardType.Special:
                    sb.Append("Special effect");
                    break;
            }

            sb.Append($"\nCost: {data.overtimeCost} OT");
            return sb.ToString();
        }

        /// <summary>Preview Rage Burst bonus without consuming overflow.</summary>
        private int GetRageBurstPreview(int baseDamage)
        {
            if (BattleManager.Instance == null) return 0;

            // Access overflow buffer through the scene
            OverflowBuffer overflow = FindObjectOfType<OverflowBuffer>();
            if (overflow == null || overflow.Current <= 0) return 0;

            return RageBurstCalculator.CalculateBonusDamage(baseDamage, overflow.Current);
        }

        /// <summary>Sum all active Tool modifiers of the given type.</summary>
        private int GetToolModifierValue(ToolModifierType modType)
        {
            // Query RunState for tools — returns 0 if no RunState/tools available
            // This will be fully wired when SaveManager is implemented
            return 0;
        }
    }
}
