using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Attach to the Player in Battlescene.
    /// Shows a green outline on hover when a card is selected. Click to play the card on the player.
    /// Requires an OutlineEffect component on the same GameObject.
    /// </summary>
    [RequireComponent(typeof(OutlineEffect))]
    public class PlayerTargetable : MonoBehaviour
    {
        [SerializeField] Color outlineColor = Color.green;

        private OutlineEffect _outline;

        private void Awake()
        {
            _outline = GetComponent<OutlineEffect>();
        }

        private void OnMouseEnter()
        {
            if (CardTargetingManager.Instance != null && CardTargetingManager.Instance.HasSelectedCard)
                _outline.ShowOutline(outlineColor);
        }

        private void OnMouseExit()
        {
            _outline.HideOutline();
        }

        private void OnMouseDown()
        {
            if (CardTargetingManager.Instance == null) return;
            if (!CardTargetingManager.Instance.HasSelectedCard) return;

            _outline.HideOutline();
            CardTargetingManager.Instance.PlayOnTarget(gameObject);
        }
    }
}
