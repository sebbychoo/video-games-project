using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Manages card selection and targeting.
    /// Flow: hover card to preview → click card to select it → hover/click a target to play it.
    /// Attach to the BattleManager GameObject.
    /// </summary>
    public class CardTargetingManager : MonoBehaviour
    {
        public static CardTargetingManager Instance { get; private set; }

        public CardInstance SelectedCard { get; private set; }
        public CardInstance HoveredCard  { get; private set; }

        public bool HasSelectedCard => SelectedCard != null;
        public bool HasHoveredCard  => HoveredCard  != null;

        [SerializeField] Camera battleCamera;
        [SerializeField] float  tiltStrength = 30f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (battleCamera == null)
                battleCamera = Camera.main;
        }

        public void SetHoveredCard(CardInstance card)
        {
            HoveredCard = card;
        }

        public void ClearHoveredCard(CardInstance card)
        {
            if (HoveredCard == card)
                HoveredCard = null;
        }

        /// <summary>Called when player clicks a card — selects it for targeting.</summary>
        public void SelectCard(CardInstance card)
        {
            // If clicking the already-selected card, deselect it
            if (SelectedCard == card)
            {
                CancelSelection();
                return;
            }
            SelectedCard = card;
            card.IsSelected = true;

            HandManager hm = FindObjectOfType<HandManager>();
            hm?.OnCardSelected(card);
        }

        /// <summary>Called when a target is clicked while a card is selected.</summary>
        public void PlayOnTarget(GameObject target)
        {
            if (SelectedCard == null) return;
            if (BattleManager.Instance == null) return;
            if (BattleManager.Instance.CurrentTurn != TurnState.PlayerTurn) return;

            CardInstance card = SelectedCard;
            SelectedCard = null;
            card.IsSelected = false;
            BattleManager.Instance.TryPlayCard(card, target);
        }

        /// <summary>Cancel selection (right click or Escape).</summary>
        public void CancelSelection()
        {
            if (SelectedCard != null)
            {
                HandManager hm = FindObjectOfType<HandManager>();
                hm?.OnCardDeselected(SelectedCard);
                SelectedCard.IsSelected = false;
                SelectedCard = null;
            }
        }

        private void Update()
        {
            // Cancel on right click or Escape
            if (HasSelectedCard && (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)))
            {
                CancelSelection();
                return;
            }

            // Only tilt the selected card toward the mouse — don't touch hovered cards
            // (CardAnimator handles hover animation)
            if (SelectedCard == null) return;

            Camera cam = battleCamera != null ? battleCamera : Camera.main;
            if (cam == null) return;

            Vector2 cardScreenPos = RectTransformUtility.WorldToScreenPoint(
                cam, SelectedCard.RectTransform.position);

            Vector2 dir  = ((Vector2)Input.mousePosition - cardScreenPos).normalized;
            float   yTilt = dir.x * tiltStrength;
            float   xTilt = -dir.y * tiltStrength * 0.4f;

            SelectedCard.RectTransform.localEulerAngles = new Vector3(xTilt, yTilt, 0f);
        }
    }
}
