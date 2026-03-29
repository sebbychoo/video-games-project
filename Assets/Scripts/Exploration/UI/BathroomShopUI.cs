using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Populates the BathroomShop UI panels with cards, tools, and deck for toilet removal.
    /// Attach to the ShopPanel GameObject. Wire references in Inspector.
    /// </summary>
    public class BathroomShopUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BathroomShop shop;
        [SerializeField] private TMP_Text hoursText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private Transform cardGrid;
        [SerializeField] private Transform toolGrid;
        [SerializeField] private Transform deckGrid;
        [SerializeField] private GameObject shopItemPrefab; // reuse CardTile prefab

        public void Refresh()
        {
            if (shop == null) return;
            UpdateHoursDisplay();
            UpdateCostDisplay();
            PopulateCards();
            PopulateTools();
            PopulateDeck();
        }

        private void OnEnable() { Refresh(); }

        private void UpdateHoursDisplay()
        {
            if (hoursText == null) return;
            int hours = 0;
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentRun != null)
                hours = SaveManager.Instance.CurrentRun.hours;
            hoursText.text = $"Hours: {hours}";
        }

        private void UpdateCostDisplay()
        {
            if (costText == null) return;
            int cost = shop.GetRemovalCost();
            bool canRemove = shop.CanRemoveCard();
            costText.text = canRemove ? $"Cost: {cost} Hours" : "No removals available";
        }

        private void PopulateCards()
        {
            if (cardGrid == null || shopItemPrefab == null) return;
            foreach (Transform child in cardGrid) Destroy(child.gameObject);

            for (int i = 0; i < shop.CardEntries.Count; i++)
            {
                var entry = shop.CardEntries[i];
                if (entry.sold) continue;

                GameObject tile = Instantiate(shopItemPrefab, cardGrid);
                tile.SetActive(true);

                CardData data = Resources.Load<CardData>(entry.cardId);
                string label = data != null ? $"{data.cardName}\n{entry.price}h" : $"{entry.cardId}\n{entry.price}h";
                Color bg = GetRarityColor(entry.rarity);
                Sprite sprite = data != null ? data.cardSprite : null;

                SetupTile(tile, label, bg, sprite);

                int index = i;
                var btn = tile.GetComponent<Button>();
                if (btn != null)
                    btn.onClick.AddListener(() => { shop.PurchaseCard(index); Refresh(); });
            }
        }

        private void PopulateTools()
        {
            if (toolGrid == null || shopItemPrefab == null) return;
            foreach (Transform child in toolGrid) Destroy(child.gameObject);

            for (int i = 0; i < shop.ToolEntries.Count; i++)
            {
                var entry = shop.ToolEntries[i];
                if (entry.sold) continue;

                GameObject tile = Instantiate(shopItemPrefab, toolGrid);
                tile.SetActive(true);

                ToolData data = Resources.Load<ToolData>(entry.toolId);
                string label = data != null ? $"{data.toolName}\n{entry.price}h" : $"{entry.toolId}\n{entry.price}h";
                Color bg = GetRarityColor(entry.rarity);
                Sprite sprite = data != null ? data.toolSprite : null;

                SetupTile(tile, label, bg, sprite);

                int index = i;
                var btn = tile.GetComponent<Button>();
                if (btn != null)
                    btn.onClick.AddListener(() => { shop.PurchaseTool(index); Refresh(); });
            }
        }

        private void PopulateDeck()
        {
            if (deckGrid == null || shopItemPrefab == null) return;
            foreach (Transform child in deckGrid) Destroy(child.gameObject);

            if (!shop.CanRemoveCard()) return;

            RunState run = SaveManager.Instance != null ? SaveManager.Instance.CurrentRun : null;
            if (run == null || run.deckCardIds == null) return;

            foreach (string cardId in run.deckCardIds)
            {
                GameObject tile = Instantiate(shopItemPrefab, deckGrid);
                tile.SetActive(true);

                CardData data = Resources.Load<CardData>(cardId);
                string label = data != null ? data.cardName : cardId;
                Color bg = data != null ? GetRarityColor(data.cardRarity) : Color.gray;
                Sprite sprite = data != null ? data.cardSprite : null;

                SetupTile(tile, label, bg, sprite);

                string id = cardId;
                var btn = tile.GetComponent<Button>();
                if (btn != null)
                    btn.onClick.AddListener(() => { shop.RemoveCard(id); Refresh(); });
            }
        }

        private static void SetupTile(GameObject tile, string name, Color bgColor, Sprite sprite)
        {
            Image buttonImg = tile.GetComponent<Image>();
            if (buttonImg != null) { buttonImg.color = Color.clear; buttonImg.raycastTarget = true; }

            Transform rarityBG = tile.transform.Find("RarityBG");
            if (rarityBG != null) { var bg = rarityBG.GetComponent<Image>(); if (bg != null) bg.color = bgColor; }

            Transform cardSprite = tile.transform.Find("CardSprite");
            if (cardSprite != null)
            {
                var img = cardSprite.GetComponent<Image>();
                if (img != null) { img.sprite = sprite; img.color = sprite != null ? Color.white : Color.clear; }
            }

            float lum = 0.299f * bgColor.r + 0.587f * bgColor.g + 0.114f * bgColor.b;
            Color textColor = lum > 0.5f ? Color.black : Color.white;

            // Split name and price if format is "Name\nPrice"
            string displayName = name;
            string displayPrice = "";
            int newline = name.IndexOf('\n');
            if (newline >= 0)
            {
                displayName = name.Substring(0, newline);
                displayPrice = name.Substring(newline + 1);
            }

            SetTMPOrText(tile.transform.Find("CardName"), displayName, textColor);
            SetTMPOrText(tile.transform.Find("PriceText"), displayPrice, textColor);
        }

        private static void SetTMPOrText(Transform t, string value, Color color)
        {
            if (t == null) return;
            var tmp = t.GetComponent<TMP_Text>();
            if (tmp != null) { tmp.text = value; tmp.color = color; return; }
            var txt = t.GetComponent<Text>();
            if (txt != null) { txt.text = value; txt.color = color; }
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
    }
}
