using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Attach to allied NPCs in Battlescene (e.g., the worker in Suicidal_Worker_Encounter).
    /// Shows a green outline on hover when a card is selected. Click to play the card on this ally.
    /// Requires an OutlineEffect component on the same GameObject.
    /// </summary>
    [RequireComponent(typeof(OutlineEffect))]
    public class AllyTargetable : MonoBehaviour
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
            if (_outline != null)
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
