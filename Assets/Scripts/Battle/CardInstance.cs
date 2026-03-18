using UnityEngine;

namespace CardBattle
{
    public class CardInstance : MonoBehaviour
    {
        public CardData       Data         { get; set; }
        public bool           IsHovered    { get; set; }
        public bool           IsSelected   { get; set; }
        public RectTransform  RectTransform { get; private set; }

        // The resting arc transform — set by HandManager after layout
        public CardTransformTarget ArcTarget { get; set; }

        private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
        }
    }
}
