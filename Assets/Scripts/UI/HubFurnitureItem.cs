using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CardBattle
{
    /// <summary>
    /// Attach to each furniture Image/Button in the Hub Office scene.
    /// Handles hover and click via Unity's EventSystem (cursor-only, no WASD).
    /// References a HubUpgradeData ScriptableObject for upgrade info.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class HubFurnitureItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private HubUpgradeData upgradeData;
        [SerializeField] private HubOffice hubOffice;

        private Image _image;

        /// <summary>The upgrade data asset this furniture represents.</summary>
        public HubUpgradeData UpgradeData => upgradeData;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        /// <summary>
        /// Updates the furniture sprite to match the given upgrade level.
        /// Uses the furnitureSprites list from HubUpgradeData.
        /// Level 0 uses index 0 (base appearance), level 1 uses index 1, etc.
        /// </summary>
        public void UpdateVisual(int level)
        {
            if (_image == null) _image = GetComponent<Image>();
            if (upgradeData == null || upgradeData.furnitureSprites == null) return;
            if (upgradeData.furnitureSprites.Count == 0) return;

            int spriteIndex = Mathf.Clamp(level, 0, upgradeData.furnitureSprites.Count - 1);
            Sprite sprite = upgradeData.furnitureSprites[spriteIndex];
            if (sprite != null)
                _image.sprite = sprite;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (hubOffice != null)
                hubOffice.OnFurnitureHover(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (hubOffice != null)
                hubOffice.OnFurnitureExit(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (hubOffice != null)
                hubOffice.OnFurnitureClick(this);
        }
    }
}
