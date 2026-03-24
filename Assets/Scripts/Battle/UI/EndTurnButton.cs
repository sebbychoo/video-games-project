using UnityEngine;
using UnityEngine.UI;

namespace CardBattle
{
    /// <summary>
    /// End Turn button. Enabled only during Play_Phase, disabled during all other phases.
    /// Calls BattleManager.EndTurn() on click.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class EndTurnButton : MonoBehaviour
    {
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClick);
        }

        private void OnEnable()
        {
            if (BattleEventBus.Instance != null)
                BattleEventBus.Instance.OnTurnPhaseChanged += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            if (BattleEventBus.Instance != null)
                BattleEventBus.Instance.OnTurnPhaseChanged -= HandlePhaseChanged;
        }

        private void HandlePhaseChanged(TurnPhaseChangedEvent e)
        {
            _button.interactable = e.NewPhase == TurnPhase.Play;
        }

        private void OnClick()
        {
            if (BattleManager.Instance != null)
                BattleManager.Instance.EndTurn();
        }
    }
}
