using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Work Box chest found under work desks during exploration.
    /// Contains cards with rarity determined by floor-based probability tables.
    /// Supports rarity reveal sequence, keep/leave decisions, and revisit persistence.
    /// </summary>
    public class WorkBox : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private Transform cardTileContainer;
        [SerializeField] private GameObject cardTilePrefab;
        [SerializeField] private GameObject keepButton;
        [SerializeField] private GameObject leaveButton;
        [SerializeField] private ParticleSystem dustParticles;

        [Header("Animation Settings")]
        [SerializeField] private float shakeDuration = 0.5f;
        [SerializeField] private float shakeIntensity = 0.1f;
        [SerializeField] private float revealStepDuration = 0.4f;

        // --- State ---
        private bool _initialized;
        private bool _opened;
        private WorkBoxSize _size;
        private List<WorkBoxCard> _cards = new List<WorkBoxCard>();
        private int _selectedCardIndex = -1;
        private bool _animating;

        /// <summary>Whether this box has been opened at least once.</summary>
        public bool IsOpened => _opened;

        /// <summary>The determined size of this work box.</summary>
        public WorkBoxSize Size => _size;

        /// <summary>Read-only access to the cards in this box.</summary>
        public IReadOnlyList<WorkBoxCard> Cards => _cards;

        // ----------------------------------------------------------------
        // Work Box Card State
        // ----------------------------------------------------------------

        [Serializable]
        public class WorkBoxCard
        {
            public string cardId;
            public CardRarity rarity;
            public RevealState revealState;
            public bool kept;
            public bool left;
        }

        public enum RevealState
        {
            Hidden,       // grey tile
            Yellow,       // whitish yellow (Rare+ reveal step)
            Red,          // medium red with dust (Legendary+ reveal step)
            Black,        // black with glowing aura (Unknown reveal step)
            FullReveal    // card detail shown
        }

        // ----------------------------------------------------------------
        // Initialization
        // ----------------------------------------------------------------

        /// <summary>
        /// Generates box contents based on the current floor.
        /// Called on first interaction. Idempotent — does nothing if already initialized.
        /// </summary>
        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            int floor = GetCurrentFloor();
            _size = RollSize(floor);

            int cardCount = RollCardCount(_size);
            for (int i = 0; i < cardCount; i++)
            {
                CardRarity rarity = RollRarity(floor);
                string cardId = PickCardIdForRarity(rarity);

                _cards.Add(new WorkBoxCard
                {
                    cardId = cardId,
                    rarity = rarity,
                    revealState = RevealState.Hidden,
                    kept = false,
                    left = false
                });
            }
        }

        // ----------------------------------------------------------------
        // Public API
        // ----------------------------------------------------------------

        /// <summary>
        /// Opens the work box. Plays shake animation on first open,
        /// then shows the inventory panel.
        /// </summary>
        public void Open()
        {
            Initialize();

            if (!_opened)
            {
                _opened = true;
                StartCoroutine(OpenSequence(playShake: true));
            }
            else
            {
                // Revisit: skip shake, show true rarity colors immediately
                ApplyRevisitState();
                ShowInventory();
            }
        }

        /// <summary>
        /// Advances the rarity reveal for the card at the given index.
        /// Returns the new RevealState after advancing.
        /// </summary>
        public RevealState RevealCard(int index)
        {
            if (index < 0 || index >= _cards.Count) return RevealState.Hidden;
            if (_animating) return _cards[index].revealState;

            WorkBoxCard card = _cards[index];
            if (card.kept || card.left) return card.revealState;

            RevealState targetState = GetTargetRevealState(card.rarity);

            if (card.revealState < targetState)
            {
                card.revealState = GetNextRevealState(card.revealState, card.rarity);
                StartCoroutine(PlayRevealAnimation(index, card.revealState));
            }
            else if (card.revealState == targetState && card.revealState != RevealState.FullReveal)
            {
                card.revealState = RevealState.FullReveal;
                _selectedCardIndex = index;
                ShowKeepLeaveButtons(true);
            }

            return card.revealState;
        }

        /// <summary>
        /// Keeps the card at the given index, adding it to the player's deck.
        /// Returns true if the card was successfully kept.
        /// </summary>
        public bool KeepCard(int index)
        {
            if (index < 0 || index >= _cards.Count) return false;

            WorkBoxCard card = _cards[index];
            if (card.kept || card.left) return false;

            if (!DeckSizeLimiter.CanAddCard(gameConfig))
            {
                Debug.Log("WorkBox: Deck is full, cannot keep card.");
                return false;
            }

            card.kept = true;
            card.revealState = RevealState.FullReveal;

            // Add card to player's deck
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentRun != null)
            {
                if (SaveManager.Instance.CurrentRun.deckCardIds == null)
                    SaveManager.Instance.CurrentRun.deckCardIds = new List<string>();

                SaveManager.Instance.CurrentRun.deckCardIds.Add(card.cardId);
            }

            ShowKeepLeaveButtons(false);
            _selectedCardIndex = -1;
            return true;
        }

        /// <summary>
        /// Leaves (discards) the card at the given index.
        /// </summary>
        public void LeaveCard(int index)
        {
            if (index < 0 || index >= _cards.Count) return;

            WorkBoxCard card = _cards[index];
            if (card.kept || card.left) return;

            card.left = true;
            ShowKeepLeaveButtons(false);
            _selectedCardIndex = -1;
        }

        /// <summary>
        /// Returns true if all cards have been kept or left.
        /// </summary>
        public bool AllCardsDecided()
        {
            foreach (WorkBoxCard card in _cards)
            {
                if (!card.kept && !card.left) return false;
            }
            return true;
        }

        // ----------------------------------------------------------------
        // Size Spawn Rates (floor-based)
        // ----------------------------------------------------------------

        /// <summary>
        /// Determines the work box size based on floor-based spawn rates.
        /// </summary>
        public static WorkBoxSize RollSize(int floor)
        {
            WorkBoxSpawnRates rates = GetSpawnRates(floor);
            float roll = UnityEngine.Random.value;

            if (roll < rates.smallRate)
                return WorkBoxSize.Small;
            if (roll < rates.smallRate + rates.bigRate)
                return WorkBoxSize.Big;
            return WorkBoxSize.Huge;
        }

        /// <summary>
        /// Returns spawn rates for the given floor.
        /// Floors 1-5: 100% Small, 0% Big, 0% Huge
        /// Floors 6-10: 90% Small, 10% Big, 0% Huge
        /// Floors 11+: 70% Small, 25% Big, 5% Huge
        /// </summary>
        public static WorkBoxSpawnRates GetSpawnRates(int floor)
        {
            if (floor <= 5)
                return new WorkBoxSpawnRates { smallRate = 1.0f, bigRate = 0f, hugeRate = 0f };
            if (floor <= 10)
                return new WorkBoxSpawnRates { smallRate = 0.9f, bigRate = 0.1f, hugeRate = 0f };
            return new WorkBoxSpawnRates { smallRate = 0.7f, bigRate = 0.25f, hugeRate = 0.05f };
        }

        // ----------------------------------------------------------------
        // Card Count by Size
        // ----------------------------------------------------------------

        /// <summary>
        /// Rolls a random card count for the given box size.
        /// Small: [1,3], Big: [3,5], Huge: [5,7]
        /// </summary>
        public static int RollCardCount(WorkBoxSize size)
        {
            GetCardCountRange(size, out int min, out int max);
            return UnityEngine.Random.Range(min, max + 1);
        }

        /// <summary>
        /// Returns the min/max card count for a given box size.
        /// </summary>
        public static void GetCardCountRange(WorkBoxSize size, out int min, out int max)
        {
            switch (size)
            {
                case WorkBoxSize.Small:
                    min = 1; max = 3; break;
                case WorkBoxSize.Big:
                    min = 3; max = 5; break;
                case WorkBoxSize.Huge:
                    min = 5; max = 7; break;
                default:
                    min = 1; max = 3; break;
            }
        }

        // ----------------------------------------------------------------
        // Card Rarity Probability Tables (floor-based)
        // ----------------------------------------------------------------

        /// <summary>
        /// Rolls a card rarity based on floor-based probability tables.
        /// </summary>
        public static CardRarity RollRarity(int floor)
        {
            GetRarityWeights(floor, out float common, out float rare, out float legendary, out float unknown);
            float roll = UnityEngine.Random.value;

            if (roll < common)
                return CardRarity.Common;
            if (roll < common + rare)
                return CardRarity.Rare;
            if (roll < common + rare + legendary)
                return CardRarity.Legendary;
            return CardRarity.Unknown;
        }

        /// <summary>
        /// Returns rarity weights for the given floor.
        /// </summary>
        public static void GetRarityWeights(int floor, out float common, out float rare, out float legendary, out float unknown)
        {
            if (floor <= 3)
            {
                common = 0.72f; rare = 0.25f; legendary = 0.03f; unknown = 0f;
            }
            else if (floor <= 6)
            {
                common = 0.52f; rare = 0.38f; legendary = 0.10f; unknown = 0f;
            }
            else if (floor <= 10)
            {
                common = 0.33f; rare = 0.45f; legendary = 0.21f; unknown = 0.01f;
            }
            else if (floor <= 15)
            {
                common = 0.18f; rare = 0.45f; legendary = 0.32f; unknown = 0.05f;
            }
            else if (floor <= 20)
            {
                common = 0.08f; rare = 0.30f; legendary = 0.47f; unknown = 0.15f;
            }
            else if (floor <= 24)
            {
                common = 0f; rare = 0.12f; legendary = 0.58f; unknown = 0.30f;
            }
            else
            {
                common = 0f; rare = 0.01f; legendary = 0.69f; unknown = 0.30f;
            }
        }

        // ----------------------------------------------------------------
        // Rarity Reveal Sequence
        // ----------------------------------------------------------------

        /// <summary>
        /// Returns the target reveal state (the rarity color step) for a given card rarity.
        /// Common → Hidden (reveals immediately to FullReveal from grey)
        /// Rare → Yellow
        /// Legendary → Red
        /// Unknown → Black
        /// </summary>
        private static RevealState GetTargetRevealState(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common:    return RevealState.Hidden;
                case CardRarity.Rare:      return RevealState.Yellow;
                case CardRarity.Legendary: return RevealState.Red;
                case CardRarity.Unknown:   return RevealState.Black;
                default:                   return RevealState.Hidden;
            }
        }

        /// <summary>
        /// Returns the next reveal state in the sequence, capped at the card's true rarity.
        /// Grey → Yellow → Red → Black
        /// </summary>
        private static RevealState GetNextRevealState(RevealState current, CardRarity rarity)
        {
            RevealState target = GetTargetRevealState(rarity);

            switch (current)
            {
                case RevealState.Hidden:
                    // Common cards go straight to FullReveal
                    if (rarity == CardRarity.Common)
                        return RevealState.FullReveal;
                    return RevealState.Yellow;

                case RevealState.Yellow:
                    if (target == RevealState.Yellow)
                        return RevealState.Yellow; // at target, next click → FullReveal
                    return RevealState.Red;

                case RevealState.Red:
                    if (target == RevealState.Red)
                        return RevealState.Red;
                    return RevealState.Black;

                default:
                    return current;
            }
        }

        /// <summary>
        /// Returns the true rarity color RevealState for a card, used during revisits.
        /// </summary>
        public static RevealState GetTrueRarityRevealState(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common:    return RevealState.FullReveal;
                case CardRarity.Rare:      return RevealState.Yellow;
                case CardRarity.Legendary: return RevealState.Red;
                case CardRarity.Unknown:   return RevealState.Black;
                default:                   return RevealState.FullReveal;
            }
        }

        // ----------------------------------------------------------------
        // Coroutines and Animation
        // ----------------------------------------------------------------

        private IEnumerator OpenSequence(bool playShake)
        {
            if (playShake)
            {
                yield return StartCoroutine(PlayShakeAnimation());
            }

            ShowInventory();
        }

        private IEnumerator PlayShakeAnimation()
        {
            _animating = true;
            Vector3 originalPos = transform.localPosition;
            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                float x = UnityEngine.Random.Range(-shakeIntensity, shakeIntensity);
                float y = UnityEngine.Random.Range(-shakeIntensity, shakeIntensity);
                transform.localPosition = originalPos + new Vector3(x, y, 0f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localPosition = originalPos;
            _animating = false;
        }

        private IEnumerator PlayRevealAnimation(int index, RevealState state)
        {
            _animating = true;

            // Start dust particles for Legendary reveal
            if (state == RevealState.Red && dustParticles != null)
            {
                dustParticles.Play();
            }

            yield return new WaitForSeconds(revealStepDuration);

            // Stop dust particles after animation
            if (state == RevealState.Red && dustParticles != null)
            {
                dustParticles.Stop();
            }

            _animating = false;
        }

        // ----------------------------------------------------------------
        // UI Helpers
        // ----------------------------------------------------------------

        private void ShowInventory()
        {
            if (inventoryPanel != null)
                inventoryPanel.SetActive(true);
        }

        /// <summary>
        /// Hides the inventory panel (e.g., when the player walks away).
        /// </summary>
        public void CloseInventory()
        {
            if (inventoryPanel != null)
                inventoryPanel.SetActive(false);

            ShowKeepLeaveButtons(false);
            _selectedCardIndex = -1;
        }

        private void ShowKeepLeaveButtons(bool show)
        {
            if (keepButton != null) keepButton.SetActive(show);
            if (leaveButton != null) leaveButton.SetActive(show);
        }

        /// <summary>
        /// On revisit, set all unrevealed/undecided cards to their true rarity colors.
        /// </summary>
        private void ApplyRevisitState()
        {
            foreach (WorkBoxCard card in _cards)
            {
                if (card.kept || card.left) continue;

                // Show true rarity color immediately
                RevealState trueState = GetTrueRarityRevealState(card.rarity);
                if (card.revealState < trueState)
                    card.revealState = trueState;
            }
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

        /// <summary>
        /// Picks a random card ID matching the given rarity from the Resources folder.
        /// Falls back to returning a placeholder if no matching cards are found.
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

            // Fallback: pick any card if no matching rarity found
            if (allCards.Length > 0)
            {
                int idx = UnityEngine.Random.Range(0, allCards.Length);
                return allCards[idx].name;
            }

            return $"unknown_card_{rarity}";
        }

        // ----------------------------------------------------------------
        // Button Callbacks (wire in Inspector or via code)
        // ----------------------------------------------------------------

        /// <summary>Called by the Keep button.</summary>
        public void OnKeepClicked()
        {
            if (_selectedCardIndex >= 0)
                KeepCard(_selectedCardIndex);
        }

        /// <summary>Called by the Leave button.</summary>
        public void OnLeaveClicked()
        {
            if (_selectedCardIndex >= 0)
                LeaveCard(_selectedCardIndex);
        }

        /// <summary>Called when a card tile is clicked.</summary>
        public void OnCardTileClicked(int index)
        {
            RevealCard(index);
        }
    }
}
