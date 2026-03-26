using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Reads CardData from the sibling CardInstance and displays the card's
    /// name, cost, art, type, and description on the card UI.
    /// Attach to the Card prefab root alongside CardInstance.
    /// </summary>
    public class CardVisual : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image cardArtImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI typeText;
        [SerializeField] private Image cardBackground;

        [Header("Type Colors")]
        [SerializeField] private Color attackColor = new Color(0.9f, 0.25f, 0.2f);
        [SerializeField] private Color defenseColor = new Color(0.2f, 0.6f, 0.9f);
        [SerializeField] private Color effectColor = new Color(0.6f, 0.2f, 0.8f);
        [SerializeField] private Color utilityColor = new Color(0.2f, 0.8f, 0.4f);
        [SerializeField] private Color specialColor = new Color(0.9f, 0.75f, 0.1f);

        [Header("Rarity Colors (for border/glow)")]
        [SerializeField] private Color commonColor = new Color(0.75f, 0.75f, 0.75f);
        [SerializeField] private Color rareColor = new Color(1f, 0.95f, 0.5f);
        [SerializeField] private Color legendaryColor = new Color(0.85f, 0.2f, 0.2f);
        [SerializeField] private Color unknownColor = new Color(0.1f, 0.1f, 0.1f);

        [Header("Rarity Border (optional)")]
        [SerializeField] private Image rarityBorder;

        [Header("Type Inner Background (optional)")]
        [SerializeField] private Image typeBackground;

        private CardInstance _cardInstance;

        private void Start()
        {
            _cardInstance = GetComponent<CardInstance>();
            Refresh();
        }

        /// <summary>Show the description as a floating tooltip at mouse position.</summary>
        public void ShowDescription()
        {
            if (_cardInstance == null || _cardInstance.Data == null) return;
            if (CardDescriptionTooltip.Instance != null)
                CardDescriptionTooltip.Instance.Show(_cardInstance.Data, _cardInstance.Data.description, _cardInstance.RectTransform);
        }

        /// <summary>Hide the floating tooltip.</summary>
        public void HideDescription()
        {
            if (CardDescriptionTooltip.Instance != null)
                CardDescriptionTooltip.Instance.Hide();
        }

        /// <summary>Call after CardInstance.Data is set to update visuals.</summary>
        public void Refresh()
        {
            if (_cardInstance == null)
                _cardInstance = GetComponent<CardInstance>();
            if (_cardInstance == null || _cardInstance.Data == null) return;

            CardData data = _cardInstance.Data;

            if (nameText != null)
                nameText.text = data.cardName;

            if (costText != null)
                costText.text = data.overtimeCost.ToString();

            if (descriptionText != null)
                descriptionText.text = data.description;

            if (typeText != null)
                typeText.text = data.cardType.ToString();

            if (cardArtImage != null && data.cardSprite != null)
                cardArtImage.sprite = data.cardSprite;

            if (cardBackground != null)
            {
                cardBackground.color = data.cardRarity switch
                {
                    CardRarity.Common => commonColor,
                    CardRarity.Rare => rareColor,
                    CardRarity.Legendary => legendaryColor,
                    CardRarity.Unknown => unknownColor,
                    _ => Color.white
                };
            }

            if (rarityBorder != null)
            {
                rarityBorder.color = data.cardRarity switch
                {
                    CardRarity.Common => commonColor,
                    CardRarity.Rare => rareColor,
                    CardRarity.Legendary => legendaryColor,
                    CardRarity.Unknown => unknownColor,
                    _ => Color.white
                };
            }

            if (typeBackground != null)
            {
                typeBackground.color = data.cardType switch
                {
                    CardType.Attack => attackColor,
                    CardType.Defense => defenseColor,
                    CardType.Effect => effectColor,
                    CardType.Utility => utilityColor,
                    CardType.Special => specialColor,
                    _ => Color.white
                };
            }
        }
    }
}
