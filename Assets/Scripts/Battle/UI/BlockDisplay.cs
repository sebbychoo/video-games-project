using UnityEngine;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Displays the player's current Block value.
    /// Visible when Block > 0, hidden when Block == 0.
    /// Subscribes to BattleEventBus.OnBlockChanged for reactive updates.
    /// </summary>
    public class BlockDisplay : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] TextMeshProUGUI blockText;

        [Header("Player Reference")]
        [SerializeField] GameObject playerTarget;

        /// <summary>
        /// Initialize the display with the player GameObject to track.
        /// Called by BattleManager after encounter setup.
        /// </summary>
        public void Initialize(GameObject player)
        {
            playerTarget = player;
            Refresh(0);
        }

        private void OnEnable()
        {
            if (BattleEventBus.Instance != null)
                BattleEventBus.Instance.OnBlockChanged += HandleBlockChanged;
        }

        private void OnDisable()
        {
            if (BattleEventBus.Instance != null)
                BattleEventBus.Instance.OnBlockChanged -= HandleBlockChanged;
        }

        private void HandleBlockChanged(BlockEvent e)
        {
            if (playerTarget == null || e.Target != playerTarget) return;
            Refresh(e.NewTotal);
        }

        private void Refresh(int blockValue)
        {
            if (blockValue > 0)
            {
                gameObject.SetActive(true);
                if (blockText != null)
                    blockText.text = blockValue.ToString();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
