using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Attach to each enemy in Battlescene.
    /// Shows a red outline on hover when a card is selected. Click to play the card on this enemy.
    /// Requires an OutlineEffect component on the same GameObject.
    /// </summary>
    [RequireComponent(typeof(OutlineEffect))]
    public class EnemyTargetable : MonoBehaviour
    {
        [SerializeField] Color outlineColor = Color.red;

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
