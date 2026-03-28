using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Break room NPC trade encounter. Offers direct item-for-item swaps
    /// with no currency cost. Trades are equal or unfavorable to the player
    /// (coworkers looking out for themselves).
    /// </summary>
    public class BreakRoomTrade : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameConfig gameConfig;

        // --- Trade State ---
        private bool _initialized;
        private TradeOffer _currentOffer;

        /// <summary>Whether a trade has been generated.</summary>
        public bool IsInitialized => _initialized;

        /// <summary>The current trade offer, or null if none.</summary>
        public TradeOffer CurrentOffer => _currentOffer;

        // ----------------------------------------------------------------
        // Trade Offer Data
        // ----------------------------------------------------------------

        public enum TradeType { CardForCard, ToolForTool }

        [Serializable]
        public class TradeOffer
        {
            public TradeType tradeType;
            /// <summary>The item ID the NPC wants from the player.</summary>
            public string requestedItemId;
            /// <summary>The rarity of the requested item.</summary>
            public CardRarity requestedRarity;
            /// <summary>The item ID the NPC offers in return.</summary>
            public string offeredItemId;
            /// <summary>The rarity of the offered item.</summary>
            public CardRarity offeredRarity;
            /// <summary>Whether this trade has been accepted.</summary>
            public bool accepted;
            /// <summary>Whether this trade has been declined.</summary>
            public bool declined;
        }

        // ----------------------------------------------------------------
        // Initialization
        // ----------------------------------------------------------------

        /// <summary>
        /// Generates a trade offer based on the player's current inventory.
        /// Idempotent — does nothing if already initialized.
        /// </summary>
        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            _currentOffer = GenerateTradeOffer();
        }

        // ----------------------------------------------------------------
        // Public API
        // ----------------------------------------------------------------

        /// <summary>
        /// Accepts the current trade. Removes the requested item from the
        /// player's inventory and adds the offered item.
        /// Returns true if the trade was successfully completed.
        /// </summary>
        public bool AcceptTrade()
        {
            if (_currentOffer == null) return false;
            if (_currentOffer.accepted || _currentOffer.declined) return false;

            RunState run = GetRunState();
            if (run == null) return false;

            if (_currentOffer.tradeType == TradeType.CardForCard)
            {
                if (run.deckCardIds == null || !run.deckCardIds.Contains(_currentOffer.requestedItemId))
                    return false;

                // Check deck size limit before adding (remove first, then add)
                // Since we remove one and add one, net size stays the same — always valid
                run.deckCardIds.Remove(_currentOffer.requestedItemId);
                run.deckCardIds.Add(_currentOffer.offeredItemId);
            }
            else if (_currentOffer.tradeType == TradeType.ToolForTool)
            {
                if (run.toolIds == null || !run.toolIds.Contains(_currentOffer.requestedItemId))
                    return false;

                run.toolIds.Remove(_currentOffer.requestedItemId);
                run.toolIds.Add(_currentOffer.offeredItemId);
            }

            _currentOffer.accepted = true;
            return true;
        }

        /// <summary>
        /// Declines the current trade. No changes to inventory.
        /// </summary>
        public void DeclineTrade()
        {
            if (_currentOffer == null) return;
            if (_currentOffer.accepted || _currentOffer.declined) return;

            _currentOffer.declined = true;
        }

        /// <summary>
        /// Returns true if the player can fulfill the current trade offer.
        /// </summary>
        public bool CanFulfillTrade()
        {
            if (_currentOffer == null) return false;
            if (_currentOffer.accepted || _currentOffer.declined) return false;

            RunState run = GetRunState();
            if (run == null) return false;

            if (_currentOffer.tradeType == TradeType.CardForCard)
                return run.deckCardIds != null && run.deckCardIds.Contains(_currentOffer.requestedItemId);

            if (_currentOffer.tradeType == TradeType.ToolForTool)
                return run.toolIds != null && run.toolIds.Contains(_currentOffer.requestedItemId);

            return false;
        }

        // ----------------------------------------------------------------
        // Trade Generation
        // ----------------------------------------------------------------

        /// <summary>
        /// Generates a trade offer the player can fulfill.
        /// Returns null if no valid trade can be generated.
        /// </summary>
        internal TradeOffer GenerateTradeOffer()
        {
            RunState run = GetRunState();
            if (run == null) return null;

            // Decide trade type based on available inventory
            bool hasCards = run.deckCardIds != null && run.deckCardIds.Count > 0;
            bool hasTools = run.toolIds != null && run.toolIds.Count > 0;

            if (!hasCards && !hasTools) return null;

            TradeType type;
            if (hasCards && hasTools)
                type = UnityEngine.Random.value < 0.7f ? TradeType.CardForCard : TradeType.ToolForTool;
            else if (hasCards)
                type = TradeType.CardForCard;
            else
                type = TradeType.ToolForTool;

            if (type == TradeType.CardForCard)
                return GenerateCardTrade(run);
            else
                return GenerateToolTrade(run);
        }

        /// <summary>
        /// Generates a card-for-card trade. The NPC requests a card from the
        /// player's deck and offers a card of equal or lower rarity.
        /// </summary>
        internal TradeOffer GenerateCardTrade(RunState run)
        {
            if (run.deckCardIds == null || run.deckCardIds.Count == 0) return null;

            // Pick a random card from the player's deck
            int idx = UnityEngine.Random.Range(0, run.deckCardIds.Count);
            string requestedId = run.deckCardIds[idx];

            // Load the requested card to get its rarity
            CardData requestedCard = Resources.Load<CardData>(requestedId);
            CardRarity requestedRarity = requestedCard != null ? requestedCard.cardRarity : CardRarity.Common;

            // Pick an offered card of equal or lower rarity (unfavorable or equal trade)
            CardRarity offeredRarity = RollEqualOrLowerRarity(requestedRarity);
            string offeredId = PickDifferentCardIdForRarity(offeredRarity, requestedId);

            return new TradeOffer
            {
                tradeType = TradeType.CardForCard,
                requestedItemId = requestedId,
                requestedRarity = requestedRarity,
                offeredItemId = offeredId,
                offeredRarity = offeredRarity,
                accepted = false,
                declined = false
            };
        }

        /// <summary>
        /// Generates a tool-for-tool trade. The NPC requests a tool from the
        /// player's inventory and offers a tool of equal or lower rarity.
        /// </summary>
        internal TradeOffer GenerateToolTrade(RunState run)
        {
            if (run.toolIds == null || run.toolIds.Count == 0) return null;

            // Pick a random tool from the player's inventory
            int idx = UnityEngine.Random.Range(0, run.toolIds.Count);
            string requestedId = run.toolIds[idx];

            // Load the requested tool to get its rarity
            ToolData requestedTool = Resources.Load<ToolData>(requestedId);
            CardRarity requestedRarity = requestedTool != null ? requestedTool.rarity : CardRarity.Common;

            // Pick an offered tool of equal or lower rarity
            CardRarity offeredRarity = RollEqualOrLowerRarity(requestedRarity);
            string offeredId = PickDifferentToolIdForRarity(offeredRarity, requestedId);

            return new TradeOffer
            {
                tradeType = TradeType.ToolForTool,
                requestedItemId = requestedId,
                requestedRarity = requestedRarity,
                offeredItemId = offeredId,
                offeredRarity = offeredRarity,
                accepted = false,
                declined = false
            };
        }

        // ----------------------------------------------------------------
        // Trade Fairness (Req 23.4)
        // ----------------------------------------------------------------

        /// <summary>
        /// Returns a rarity equal to or lower than the given rarity.
        /// Equal trades are more common; unfavorable trades happen occasionally.
        /// Never returns a rarity higher than the input.
        /// </summary>
        public static CardRarity RollEqualOrLowerRarity(CardRarity rarity)
        {
            // 60% chance of equal rarity, 40% chance of lower
            float roll = UnityEngine.Random.value;

            if (roll < 0.6f || rarity == CardRarity.Common)
                return rarity;

            // Roll a lower rarity
            return GetLowerRarity(rarity);
        }

        /// <summary>
        /// Returns the next lower rarity tier. Common stays Common.
        /// Unknown → Legendary → Rare → Common
        /// </summary>
        public static CardRarity GetLowerRarity(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Unknown:   return CardRarity.Legendary;
                case CardRarity.Legendary: return CardRarity.Rare;
                case CardRarity.Rare:      return CardRarity.Common;
                default:                   return CardRarity.Common;
            }
        }

        /// <summary>
        /// Returns true if offeredRarity is less than or equal to requestedRarity.
        /// Rarity order: Common &lt; Rare &lt; Legendary &lt; Unknown.
        /// </summary>
        public static bool IsEqualOrLowerRarity(CardRarity offeredRarity, CardRarity requestedRarity)
        {
            return (int)offeredRarity <= (int)requestedRarity;
        }

        // ----------------------------------------------------------------
        // Item Picking Helpers
        // ----------------------------------------------------------------

        /// <summary>
        /// Picks a random card ID matching the given rarity, excluding the specified ID.
        /// Falls back to any card of that rarity, then any card at all.
        /// </summary>
        private static string PickDifferentCardIdForRarity(CardRarity rarity, string excludeId)
        {
            CardData[] allCards = Resources.LoadAll<CardData>("");
            List<CardData> matching = new List<CardData>();

            foreach (CardData card in allCards)
            {
                if (card.cardRarity == rarity && card.name != excludeId)
                    matching.Add(card);
            }

            if (matching.Count > 0)
            {
                int idx = UnityEngine.Random.Range(0, matching.Count);
                return matching[idx].name;
            }

            // Fallback: any card of that rarity (even same ID)
            foreach (CardData card in allCards)
            {
                if (card.cardRarity == rarity)
                    return card.name;
            }

            // Final fallback: any card
            if (allCards.Length > 0)
            {
                int idx = UnityEngine.Random.Range(0, allCards.Length);
                return allCards[idx].name;
            }

            return $"trade_card_{rarity}";
        }

        /// <summary>
        /// Picks a random tool ID matching the given rarity, excluding the specified ID.
        /// Falls back to any tool of that rarity, then any tool at all.
        /// </summary>
        private static string PickDifferentToolIdForRarity(CardRarity rarity, string excludeId)
        {
            ToolData[] allTools = Resources.LoadAll<ToolData>("");
            List<ToolData> matching = new List<ToolData>();

            foreach (ToolData tool in allTools)
            {
                if (tool.rarity == rarity && tool.name != excludeId)
                    matching.Add(tool);
            }

            if (matching.Count > 0)
            {
                int idx = UnityEngine.Random.Range(0, matching.Count);
                return matching[idx].name;
            }

            // Fallback: any tool of that rarity
            foreach (ToolData tool in allTools)
            {
                if (tool.rarity == rarity)
                    return tool.name;
            }

            // Final fallback: any tool
            if (allTools.Length > 0)
            {
                int idx = UnityEngine.Random.Range(0, allTools.Length);
                return allTools[idx].name;
            }

            return $"trade_tool_{rarity}";
        }

        // ----------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------

        private RunState GetRunState()
        {
            if (SaveManager.Instance != null)
                return SaveManager.Instance.CurrentRun;
            return null;
        }
    }
}
