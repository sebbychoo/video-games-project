using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        [SerializeField] private Transform deckCardContainer;
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
        /// Pre-initializes the work box with a known floor and size.
        /// Called by LevelGenerator when spawning boxes under desks.
        /// Overrides the floor/size that would otherwise be rolled on first interaction.
        /// </summary>
        public void InitializeForFloor(int floor, WorkBoxSize size)
        {
            if (_initialized) return;
            _initialized = true;
            _size = size;

            int cardCount = RollCardCount(_size);
            for (int i = 0; i < cardCount; i++)
            {
                CardRarity rolledRarity = RollRarity(floor);
                string cardId = PickCardIdForRarity(rolledRarity);

                CardRarity actualRarity = rolledRarity;
                CardData cardData = Resources.Load<CardData>(cardId);
                if (cardData != null)
                    actualRarity = cardData.cardRarity;

                _cards.Add(new WorkBoxCard
                {
                    cardId = cardId,
                    rarity = actualRarity,
                    revealState = RevealState.Hidden,
                    kept = false,
                    left = false
                });
            }
        }

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
                CardRarity rolledRarity = RollRarity(floor);
                string cardId = PickCardIdForRarity(rolledRarity);

                // Use the card's actual rarity, not the rolled one,
                // in case the fallback picked a card of a different rarity
                CardRarity actualRarity = rolledRarity;
                CardData cardData = Resources.Load<CardData>(cardId);
                if (cardData != null)
                    actualRarity = cardData.cardRarity;

                _cards.Add(new WorkBoxCard
                {
                    cardId = cardId,
                    rarity = actualRarity,
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
                StartCoroutine(PlayRevealAnimationThenRefresh(index, card.revealState));
            }
            else if (card.revealState == targetState && card.revealState != RevealState.FullReveal)
            {
                card.revealState = RevealState.FullReveal;
                _selectedCardIndex = index;
                ShowKeepLeaveButtons(true);
                SpawnCardTiles(); // Refresh to show glow
            }
            else if (card.revealState == RevealState.FullReveal)
            {
                // Already fully revealed — select it and refresh visuals
                _selectedCardIndex = index;
                ShowKeepLeaveButtons(true);
                SpawnCardTiles();
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

            // Refresh both panels so the kept card shows as empty and appears in deck
            SpawnCardTiles();
            SpawnDeckTiles();

            return true;
        }

        /// <summary>
        /// Leaves (discards) the card at the given index.
        /// </summary>
        public void LeaveCard(int index)
        {
            if (index < 0 || index >= _cards.Count) return;

            WorkBoxCard card = _cards[index];
            if (card.kept) return;

            // Don't permanently mark as left — just deselect so player can come back
            ShowKeepLeaveButtons(false);
            _selectedCardIndex = -1;

            // Refresh tiles to remove glow/dim
            SpawnCardTiles();
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

            yield return new WaitForSecondsRealtime(revealStepDuration);

            // Stop dust particles after animation
            if (state == RevealState.Red && dustParticles != null)
            {
                dustParticles.Stop();
            }

            _animating = false;
        }

        private IEnumerator PlayRevealAnimationThenRefresh(int index, RevealState state)
        {
            yield return StartCoroutine(PlayRevealAnimation(index, state));
            SpawnCardTiles(); // Refresh tiles to show updated colors
        }

        // ----------------------------------------------------------------
        // UI Helpers
        // ----------------------------------------------------------------

        /// <summary>Finds named UI children on a tile for reliable assignment.</summary>
        private static void SetupTile(GameObject tile, string name, Color bgColor, Sprite sprite, bool showSprite)
        {
            // Make the Button's own image transparent but keep it as raycast target
            Image buttonImg = tile.GetComponent<Image>();
            if (buttonImg != null)
            {
                buttonImg.color = Color.clear;
                buttonImg.raycastTarget = true;
            }

            // RarityBG — background color
            Transform rarityBG = tile.transform.Find("RarityBG");
            if (rarityBG != null)
            {
                Image bg = rarityBG.GetComponent<Image>();
                if (bg != null) bg.color = bgColor;
            }

            // CardSprite — the actual card art
            Transform cardSprite = tile.transform.Find("CardSprite");
            if (cardSprite != null)
            {
                Image img = cardSprite.GetComponent<Image>();
                if (img != null)
                {
                    if (showSprite && sprite != null)
                    {
                        img.sprite = sprite;
                        img.color = Color.white;
                    }
                    else
                    {
                        img.sprite = null;
                        img.color = Color.clear;
                    }
                }
            }

            // Pick contrasting text color based on background brightness
            Color textColor = GetContrastColor(bgColor);

            // CardName — try TMP first, then legacy Text
            Transform cardName = tile.transform.Find("CardName");
            if (cardName != null)
            {
                TMP_Text tmp = cardName.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.text = name;
                    tmp.color = textColor;
                }
                else
                {
                    Text txt = cardName.GetComponent<Text>();
                    if (txt != null)
                    {
                        txt.text = name;
                        txt.color = textColor;
                    }
                }
            }
        }

        /// <summary>Returns black or white depending on which contrasts better with the background.</summary>
        private static Color GetContrastColor(Color bg)
        {
            // Perceived luminance formula
            float luminance = 0.299f * bg.r + 0.587f * bg.g + 0.114f * bg.b;
            return luminance > 0.5f ? Color.black : Color.white;
        }

        /// <summary>
        /// Ensures a GridLayoutGroup exists on the container. Does not override Inspector settings.
        /// </summary>
        private static void EnsureGridLayout(Transform container)
        {
            if (container == null) return;
            // Don't add GridLayoutGroup if any layout group already exists
            if (container.GetComponent<LayoutGroup>() == null)
                container.gameObject.AddComponent<GridLayoutGroup>();
        }

        private void SpawnCardTiles()
        {
            if (cardTileContainer == null || cardTilePrefab == null) return;

            foreach (Transform child in cardTileContainer)
                Destroy(child.gameObject);

            EnsureGridLayout(cardTileContainer);

            for (int i = 0; i < _cards.Count; i++)
            {
                WorkBoxCard card = _cards[i];
                GameObject tile = Instantiate(cardTilePrefab, cardTileContainer);
                tile.SetActive(true);

                var button = tile.GetComponent<Button>();
                bool isSelected = (_selectedCardIndex == i);

                if (card.kept)
                {
                    // Show the card with its rarity color but dimmed + "(Kept)" label
                    CardData keptData = Resources.Load<CardData>(card.cardId);
                    Color keptColor = GetRarityColor(card.rarity);
                    // Darken the rarity color
                    keptColor = new Color(keptColor.r * 0.5f, keptColor.g * 0.5f, keptColor.b * 0.5f, 0.7f);
                    SetupTile(tile, "(Kept)", keptColor,
                        keptData != null ? keptData.cardSprite : null,
                        keptData != null && keptData.cardSprite != null);
                    AddDimOverlay(tile);
                    if (button != null) button.interactable = false;
                }
                else
                {
                    CardData cardData = Resources.Load<CardData>(card.cardId);

                    if (card.revealState == RevealState.FullReveal && cardData != null)
                    {
                        SetupTile(tile, cardData.cardName,
                            GetRarityColor(card.rarity), cardData.cardSprite, true);

                        if (isSelected)
                            AddGlowOutline(tile, card.rarity);
                        else
                            AddDimOverlay(tile);
                    }
                    else
                    {
                        SetupTile(tile, "???", GetRevealColor(card.revealState), null, false);

                        // Dim non-selected unrevealed cards when something is selected
                        if (_selectedCardIndex >= 0 && !isSelected)
                            AddDimOverlay(tile);
                    }

                    int index = i;
                    if (button != null)
                        button.onClick.AddListener(() => OnCardTileClicked(index));
                }
            }
        }

        /// <summary>Adds a semi-transparent dark overlay on top of a tile to dim it.</summary>
        private static void AddDimOverlay(GameObject tile)
        {
            GameObject dim = new GameObject("DimOverlay");
            dim.transform.SetParent(tile.transform, false);
            dim.transform.SetAsLastSibling(); // render on top

            RectTransform rt = dim.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = dim.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.5f);
            img.raycastTarget = false; // clicks pass through to the button
        }

        private static Color GetRevealColor(RevealState state)
        {
            switch (state)
            {
                case RevealState.Hidden: return Color.gray;
                case RevealState.Yellow: return new Color(1f, 0.95f, 0.7f);
                case RevealState.Red: return new Color(0.8f, 0.2f, 0.2f);
                case RevealState.Black: return new Color(0.1f, 0.1f, 0.1f);
                case RevealState.FullReveal: return Color.white;
                default: return Color.gray;
            }
        }

        private void SpawnDeckTiles()
        {
            if (deckCardContainer == null || cardTilePrefab == null) return;

            foreach (Transform child in deckCardContainer)
                Destroy(child.gameObject);

            RunState run = null;
            if (SaveManager.Instance != null)
                run = SaveManager.Instance.CurrentRun;

            if (run == null || run.deckCardIds == null) return;

            EnsureGridLayout(deckCardContainer);

            foreach (string cardId in run.deckCardIds)
            {
                GameObject tile = Instantiate(cardTilePrefab, deckCardContainer);
                tile.SetActive(true);

                CardData cardData = Resources.Load<CardData>(cardId);

                string name = cardData != null ? cardData.cardName : cardId;
                Color bgColor = cardData != null ? GetRarityColor(cardData.cardRarity) : Color.gray;
                Sprite sprite = cardData != null ? cardData.cardSprite : null;

                SetupTile(tile, name, bgColor, sprite, sprite != null);

                var button = tile.GetComponent<Button>();
                if (button != null)
                    button.interactable = false;
            }
        }

        private static Color GetRarityColor(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common: return new Color(0.6f, 0.6f, 0.6f);
                case CardRarity.Rare: return new Color(1f, 0.95f, 0.7f);
                case CardRarity.Legendary: return new Color(0.8f, 0.2f, 0.2f);
                case CardRarity.Unknown: return new Color(0.1f, 0.1f, 0.1f);
                default: return Color.gray;
            }
        }

        private static Color GetGlowColor(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common: return new Color(1f, 1f, 1f, 0.8f);
                case CardRarity.Rare: return new Color(1f, 0.85f, 0.2f, 0.9f);
                case CardRarity.Legendary: return new Color(1f, 0.15f, 0.15f, 0.9f);
                case CardRarity.Unknown: return new Color(0.6f, 0.2f, 1f, 0.9f);
                default: return Color.white;
            }
        }

        private static void AddGlowOutline(GameObject tile, CardRarity rarity)
        {
            // Create outline as first child so it renders behind everything
            GameObject glow = new GameObject("GlowOutline");
            glow.transform.SetParent(tile.transform, false);
            glow.transform.SetAsFirstSibling();

            RectTransform rt = glow.AddComponent<RectTransform>();
            // Stretch to fill tile but extend 6px on each side
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-6f, -6f);
            rt.offsetMax = new Vector2(6f, 6f);

            Image img = glow.AddComponent<Image>();
            Color glowColor = GetGlowColor(rarity);
            img.color = glowColor;

            // Add pulse — faster for higher rarities
            float speed = rarity == CardRarity.Legendary ? 3f :
                          rarity == CardRarity.Unknown ? 4f : 2f;
            RarityGlowPulse pulse = glow.AddComponent<RarityGlowPulse>();
            pulse.Initialize(glowColor, speed);
        }

        private void ShowInventory()
        {
            if (inventoryPanel != null)
                inventoryPanel.SetActive(true);

            SpawnCardTiles();
            SpawnDeckTiles();
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

            // Don't touch cursor here — let WorkBoxTrigger handle it
        }

        /// <summary>
        /// Called by the X button on the Canvas. Routes through WorkBoxTrigger
        /// so cursor/controller state is properly restored.
        /// </summary>
        public void OnCloseButtonClicked()
        {
            var trigger = GetComponent<WorkBoxTrigger>();
            if (trigger != null)
            {
                trigger.CloseBox();
            }
            else
            {
                // Fallback if no trigger (shouldn't happen)
                CloseInventory();
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
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
