using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Populates the BreakRoomTrade UI with the current trade offer.
    /// Attach to TradePanel. Wire references in Inspector.
    /// </summary>
    public class BreakRoomTradeUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BreakRoomTrade trade;
        [SerializeField] private TMP_Text wantsItemName;
        [SerializeField] private TMP_Text offersItemName;
        [SerializeField] private Image wantsItemImage;
        [SerializeField] private Image offersItemImage;
        [SerializeField] private TMP_Text noTradeText;

        private void OnEnable() { Refresh(); }

        public void Refresh()
        {
            if (trade == null) return;

            var offer = trade.CurrentOffer;

            if (offer == null || offer.accepted || offer.declined)
            {
                ShowNoTrade();
                return;
            }

            if (noTradeText != null) noTradeText.gameObject.SetActive(false);

            // Populate wanted item
            PopulateItem(offer.requestedItemId, offer.tradeType, wantsItemName, wantsItemImage);

            // Populate offered item
            PopulateItem(offer.offeredItemId, offer.tradeType, offersItemName, offersItemImage);
        }

        private static void PopulateItem(string itemId, BreakRoomTrade.TradeType tradeType,
            TMP_Text nameText, Image itemImage)
        {
            if (tradeType == BreakRoomTrade.TradeType.CardForCard)
            {
                CardData card = Resources.Load<CardData>(itemId);
                if (nameText != null)
                    nameText.text = card != null ? card.cardName : itemId;
                if (itemImage != null && card != null && card.cardSprite != null)
                {
                    itemImage.sprite = card.cardSprite;
                    itemImage.color = Color.white;
                }
            }
            else
            {
                ToolData tool = Resources.Load<ToolData>(itemId);
                if (nameText != null)
                    nameText.text = tool != null ? tool.toolName : itemId;
                if (itemImage != null && tool != null && tool.toolSprite != null)
                {
                    itemImage.sprite = tool.toolSprite;
                    itemImage.color = Color.white;
                }
            }
        }

        private void ShowNoTrade()
        {
            if (wantsItemName != null) wantsItemName.text = "";
            if (offersItemName != null) offersItemName.text = "";
            if (wantsItemImage != null) wantsItemImage.color = Color.clear;
            if (offersItemImage != null) offersItemImage.color = Color.clear;
            if (noTradeText != null)
            {
                noTradeText.gameObject.SetActive(true);
                noTradeText.text = "No trade available";
            }
        }
    }
}
