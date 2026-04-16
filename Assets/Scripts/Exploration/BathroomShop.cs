using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Bathroom shop where the player can buy cards and Tools with Hours,
    /// or remove a card from their deck via the toilet.
    /// Inventory is generated once per floor and remains fixed.
    /// </summary>
    public class BathroomShop : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameConfig gameConfig;

        // --- Shop Inventory ---
        private List<ShopCardEntry> _cardEntries = new List<ShopCardEntry>();
        private List<ShopToolEntry> _toolEntries = new List<ShopToolEntry>();
        private bool _initialized;
        private bool _removedCardThisVisit;

        /// <summary>Read-only access to the card inventory.</summary>
        public IReadOnlyList<ShopCardEntry> CardEntries => _cardEntries;

        /// <summary>Read-only access to the tool inventory.</summary>
        public IReadOnlyList<ShopToolEntry> ToolEntries => _toolEntries;

        /// <summary>Whether a card has been removed via toilet this visit.</summary>
        public bool RemovedCardThisVisit => _removedCardThisVisit;

        /// <summary>Whether the shop inventory has been generated.</summary>
        public bool IsInitialized => _initialized;

        // ----------------------------------------------------------------
        // Shop Entry Types
        // ----------------------------------------------------------------

        [Serializable]
        public class ShopCardEntry
        {
            public string cardId;
            public CardRarity rarity;
            public int price;
            public bool sold;
        }

        [Serializable]
        public class ShopToolEntry
        {
            public string toolId;
            public CardRarity rarity;
            public int price;
            public bool sold;
        }

        // ----------------------------------------------------------------
        // Pricing Tables
        // ----------------------------------------------------------------

        /// <summary>Returns the Hours price for a card of the given rarity.</summary>
        public static int GetCardPrice(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common:    return 10;
                case CardRarity.Rare:      return 25;
                case CardRarity.Legendary: return 100;
                case CardRarity.Unknown:   return 150;
                default:                   return 10;
            }
        }

        /// <summary>Returns the Hours price for a tool of the given rarity.</summary>
        public static int GetToolPrice(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common:    return 30;
                case CardRarity.Rare:      return 60;
                case CardRarity.Legendary: return 200;
                default:                   return 30;
            }
        }

        /// <summary>
        /// Returns the current toilet card removal cost based on previous removals this run.
        /// Cost = baseCost + (cardRemovalsThisRun * costIncrease)
        /// </summary>
        public int GetRemovalCost()
        {
            int baseCost = gameConfig != null ? gameConfig.cardRemovalBaseCost : 25;
            int increase = gameConfig != null ? gameConfig.cardRemovalCostIncrease : 10;
            int removals = 0;

            if (SaveManager.Instance != null && SaveManager.Instance.CurrentRun != null)
                removals = SaveManager.Instance.CurrentRun.cardRemovalsThisRun;

            return baseCost + (removals * increase);
        }

        // ----------------------------------------------------------------
        // Initialization
        // ----------------------------------------------------------------

        /// <summary>
        /// Generates shop inventory based on the current floor.
        /// Idempotent — does nothing if already initialized.
        /// </summary>
        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            int floor = GetCurrentFloor();
            bool isBossFloor = IsBossFloor(floor);

            // Roll card count (3–5)
            int minCards = gameConfig != null ? gameConfig.shopMinCards : 3;
            int maxCards = gameConfig != null ? gameConfig.shopMaxCards : 5;
            int cardCount = UnityEngine.Random.Range(minCards, maxCards + 1);

            // Roll tool count (0–2)
            int minTools = gameConfig != null ? gameConfig.shopMinTools : 0;
            int maxTools = gameConfig != null ? gameConfig.shopMaxTools : 2;
            int toolCount = UnityEngine.Random.Range(minTools, maxTools + 1);

            // Boss floors guarantee at least 1 tool
            if (isBossFloor && toolCount < 1)
                toolCount = 1;

            // Generate card entries
            for (int i = 0; i < cardCount; i++)
            {
                CardRarity rarity = WorkBox.RollRarity(floor);
                string cardId = PickCardIdForRarity(rarity);
                int price = GetCardPrice(rarity);

                _cardEntries.Add(new ShopCardEntry
                {
                    cardId = cardId,
                    rarity = rarity,
                    price = price,
                    sold = false
                });
            }

            // Generate tool entries (Unknown tools not available in shops)
            for (int i = 0; i < toolCount; i++)
            {
                CardRarity rarity = RollToolRarity(floor);
                string toolId = PickToolIdForRarity(rarity);
                int price = GetToolPrice(rarity);

                _toolEntries.Add(new ShopToolEntry
                {
                    toolId = toolId,
                    rarity = rarity,
                    price = price,
                    sold = false
                });
            }
        }

        // ----------------------------------------------------------------
        // Purchase API
        // ----------------------------------------------------------------

        /// <summary>
        /// Attempts to purchase the card at the given index.
        /// Returns true if the purchase succeeded.
        /// </summary>
        public bool PurchaseCard(int index)
        {
            if (index < 0 || index >= _cardEntries.Count) return false;

            ShopCardEntry entry = _cardEntries[index];
            if (entry.sold) return false;

            RunState run = GetRunState();
            if (run == null) return false;

            // Check sufficient Hours
            if (run.hours < entry.price)
            {
                Debug.Log("BathroomShop: Insufficient Hours for card purchase.");
                return false;
            }

            // Check deck size limit
            if (!DeckSizeLimiter.CanAddCard(gameConfig))
            {
                Debug.Log("BathroomShop: Deck is full, cannot add card.");
                return false;
            }

            // Deduct Hours and add card
            run.hours -= entry.price;

            if (run.deckCardIds == null)
                run.deckCardIds = new List<string>();

            run.deckCardIds.Add(entry.cardId);
            entry.sold = true;

            return true;
        }

        /// <summary>
        /// Attempts to purchase the tool at the given index.
        /// Returns true if the purchase succeeded.
        /// </summary>
        public bool PurchaseTool(int index)
        {
            if (index < 0 || index >= _toolEntries.Count) return false;

            ShopToolEntry entry = _toolEntries[index];
            if (entry.sold) return false;

            RunState run = GetRunState();
            if (run == null) return false;

            // Check sufficient Hours
            if (run.hours < entry.price)
            {
                Debug.Log("BathroomShop: Insufficient Hours for tool purchase.");
                return false;
            }

            // Deduct Hours and add tool
            run.hours -= entry.price;

            if (run.toolIds == null)
                run.toolIds = new List<string>();

            run.toolIds.Add(entry.toolId);
            entry.sold = true;

            return true;
        }

        // ----------------------------------------------------------------
        // Toilet Card Removal
        // ----------------------------------------------------------------

        /// <summary>
        /// Attempts to remove the specified card from the player's deck via the toilet.
        /// Returns true if the removal succeeded.
        /// </summary>
        public bool RemoveCard(string cardId)
        {
            RunState run = GetRunState();
            if (run == null) return false;

            // One removal per visit
            if (_removedCardThisVisit)
            {
                Debug.Log("BathroomShop: Already removed a card this visit.");
                return false;
            }

            // Check minimum deck size
            int minDeckSize = gameConfig != null ? gameConfig.minimumDeckSize : 1;
            if (run.deckCardIds == null || run.deckCardIds.Count <= minDeckSize)
            {
                Debug.Log("BathroomShop: Cannot remove card — deck at minimum size.");
                return false;
            }

            // Check card exists in deck
            if (!run.deckCardIds.Contains(cardId))
            {
                Debug.Log("BathroomShop: Card not found in deck.");
                return false;
            }

            // Check sufficient Hours
            int cost = GetRemovalCost();
            if (run.hours < cost)
            {
                Debug.Log("BathroomShop: Insufficient Hours for card removal.");
                return false;
            }

            // Deduct Hours, remove card, increment removal counter
            run.hours -= cost;
            run.deckCardIds.Remove(cardId);
            run.cardRemovalsThisRun++;
            _removedCardThisVisit = true;

            return true;
        }

        /// <summary>
        /// Returns true if the toilet card removal is currently available.
        /// </summary>
        public bool CanRemoveCard()
        {
            if (_removedCardThisVisit) return false;

            RunState run = GetRunState();
            if (run == null) return false;

            int minDeckSize = gameConfig != null ? gameConfig.minimumDeckSize : 1;
            if (run.deckCardIds == null || run.deckCardIds.Count <= minDeckSize)
                return false;

            int cost = GetRemovalCost();
            return run.hours >= cost;
        }

        /// <summary>
        /// Resets the per-visit removal flag. Call when the player leaves and re-enters.
        /// </summary>
        public void ResetVisit()
        {
            _removedCardThisVisit = false;
        }

        // ----------------------------------------------------------------
        // Blood Washing
        // ----------------------------------------------------------------

        /// <summary>
        /// Washes blood off the player's gloves, resetting Blood_Level to 0.
        /// Free (no currency cost), but each bathroom can only be used once for washing.
        /// </summary>
        /// <param name="bathroomId">Unique ID for this bathroom instance</param>
        /// <returns>True if blood was washed, false if already washed or invalid</returns>
        public bool WashBlood(string bathroomId)
        {
            if (string.IsNullOrEmpty(bathroomId))
            {
                Debug.LogWarning("BathroomShop: WashBlood called with null or empty bathroomId.");
                return false;
            }

            RunState run = GetRunState();
            if (run == null) return false;

            if (run.washedBathroomIds != null && run.washedBathroomIds.Contains(bathroomId))
            {
                Debug.Log("BathroomShop: This bathroom has already been used for washing.");
                return false;
            }

            // Reset blood level (free — no currency cost)
            run.persistentBloodLevel = 0f;

            // Mark this bathroom as washed
            if (run.washedBathroomIds == null)
                run.washedBathroomIds = new List<string>();
            run.washedBathroomIds.Add(bathroomId);

            if (SaveManager.Instance != null)
                SaveManager.Instance.SaveRun();

            // Immediately update exploration gloves visual
            ExplorationGlovesController glovesController = FindObjectOfType<ExplorationGlovesController>();
            if (glovesController != null)
                glovesController.ApplyBloodTint(0f);

            return true;
        }

        /// <summary>
        /// Returns true if this bathroom can still be used for blood washing.
        /// </summary>
        public bool CanWashBlood(string bathroomId)
        {
            RunState run = GetRunState();
            if (run == null) return false;
            if (run.persistentBloodLevel <= 0f) return false;
            if (string.IsNullOrEmpty(bathroomId)) return false;
            if (run.washedBathroomIds != null && run.washedBathroomIds.Contains(bathroomId))
                return false;
            return true;
        }

        // ----------------------------------------------------------------
        // Tool Rarity Rolling (excludes Unknown)
        // ----------------------------------------------------------------

        /// <summary>
        /// Rolls a tool rarity using the same floor-based tables as WorkBox,
        /// but re-rolls Unknown results since Unknown tools are not available in shops.
        /// </summary>
        public static CardRarity RollToolRarity(int floor)
        {
            // Try up to 20 times to avoid Unknown
            for (int i = 0; i < 20; i++)
            {
                CardRarity rarity = WorkBox.RollRarity(floor);
                if (rarity != CardRarity.Unknown)
                    return rarity;
            }

            // Fallback to Legendary if we keep rolling Unknown
            return CardRarity.Legendary;
        }

        // ----------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------

        private int GetCurrentFloor()
        {
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentRun != null)
                return SaveManager.Instance.CurrentRun.currentFloor;
            return 1;
        }

        private bool IsBossFloor(int floor)
        {
            int interval = gameConfig != null ? gameConfig.bossFloorInterval : 3;
            return floor > 0 && floor % interval == 0;
        }

        private RunState GetRunState()
        {
            if (SaveManager.Instance != null)
                return SaveManager.Instance.CurrentRun;
            return null;
        }

        /// <summary>
        /// Picks a random card ID matching the given rarity from the Resources folder.
        /// </summary>
        private static string PickCardIdForRarity(CardRarity rarity)
        {
            CardData[] allCards = Resources.LoadAll<CardData>("");
            List<CardData> matching = new List<CardData>();

            foreach (CardData card in allCards)
            {
                if (card.cardRarity == rarity)
                    matching.Add(card);
            }

            if (matching.Count > 0)
            {
                int idx = UnityEngine.Random.Range(0, matching.Count);
                return matching[idx].name;
            }

            // Fallback: pick any card
            if (allCards.Length > 0)
            {
                int idx = UnityEngine.Random.Range(0, allCards.Length);
                return allCards[idx].name;
            }

            return $"unknown_card_{rarity}";
        }

        /// <summary>
        /// Picks a random tool ID matching the given rarity from the Resources folder.
        /// </summary>
        private static string PickToolIdForRarity(CardRarity rarity)
        {
            ToolData[] allTools = Resources.LoadAll<ToolData>("");
            List<ToolData> matching = new List<ToolData>();

            foreach (ToolData tool in allTools)
            {
                if (tool.rarity == rarity)
                    matching.Add(tool);
            }

            if (matching.Count > 0)
            {
                int idx = UnityEngine.Random.Range(0, matching.Count);
                return matching[idx].name;
            }

            // Fallback: pick any tool
            if (allTools.Length > 0)
            {
                int idx = UnityEngine.Random.Range(0, allTools.Length);
                return allTools[idx].name;
            }

            return $"unknown_tool_{rarity}";
        }
    }
}
