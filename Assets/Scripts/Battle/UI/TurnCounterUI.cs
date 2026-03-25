using UnityEngine;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Displays the current turn number at the top-center of the battle screen.
    /// Subscribes to BattleEventBus.OnTurnPhaseChanged to update reactively.
    /// </summary>
    public class TurnCounterUI : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI turnText;

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void Start()
        {
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            if (BattleEventBus.Instance == null) return;
            BattleEventBus.Instance.OnTurnPhaseChanged -= HandlePhaseChanged;
            BattleEventBus.Instance.OnTurnPhaseChanged += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            if (BattleEventBus.Instance != null)
                BattleEventBus.Instance.OnTurnPhaseChanged -= HandlePhaseChanged;
        }

        /// <summary>Initialize the display with turn 1.</summary>
        public void Initialize()
        {
            UpdateDisplay(1);
        }

        private void HandlePhaseChanged(TurnPhaseChangedEvent e)
        {
            UpdateDisplay(e.TurnNumber);
        }

        private void UpdateDisplay(int turnNumber)
        {
            if (turnText != null)
                turnText.text = $"Turn {turnNumber}";
        }
    }
}
