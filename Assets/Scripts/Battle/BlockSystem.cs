using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Tracks Block for the player and each enemy. Block absorbs damage before HP.
    /// Player Block resets at the start of each player turn.
    /// Enemy Block resets at the start of that enemy's turn in Enemy_Phase (even if stunned).
    /// </summary>
    public class BlockSystem : MonoBehaviour
    {
        private readonly Dictionary<GameObject, int> _blockValues = new Dictionary<GameObject, int>();

        /// <summary>Initialize the system, clearing all tracked Block.</summary>
        public void Initialize()
        {
            _blockValues.Clear();
        }

        /// <summary>Add Block to a target entity.</summary>
        public void AddBlock(int amount, GameObject target)
        {
            if (target == null || amount <= 0) return;

            if (!_blockValues.ContainsKey(target))
                _blockValues[target] = 0;

            _blockValues[target] += amount;

            if (BattleEventBus.Instance != null)
            {
                BattleEventBus.Instance.Raise(new BlockEvent
                {
                    Target = target,
                    Amount = amount,
                    NewTotal = _blockValues[target]
                });
            }
        }

        /// <summary>
        /// Apply damage to a target, absorbing with Block first.
        /// Returns the remaining damage that should be applied to HP.
        /// </summary>
        public int AbsorbDamage(int damage, GameObject target)
        {
            if (target == null || damage <= 0) return damage;

            int block = GetBlock(target);
            if (block <= 0) return damage;

            int absorbed = Mathf.Min(block, damage);
            int remaining = damage - absorbed;
            _blockValues[target] = block - absorbed;

            if (BattleEventBus.Instance != null)
            {
                BattleEventBus.Instance.Raise(new BlockEvent
                {
                    Target = target,
                    Amount = -absorbed,
                    NewTotal = _blockValues[target]
                });
            }

            return remaining;
        }

        /// <summary>Reset Block to 0 for a target.</summary>
        public void Reset(GameObject target)
        {
            if (target == null) return;

            if (_blockValues.ContainsKey(target) && _blockValues[target] > 0)
            {
                _blockValues[target] = 0;

                if (BattleEventBus.Instance != null)
                {
                    BattleEventBus.Instance.Raise(new BlockEvent
                    {
                        Target = target,
                        Amount = 0,
                        NewTotal = 0
                    });
                }
            }
        }

        /// <summary>Clear all tracked Block values (use between encounters).</summary>
        public void ClearAll()
        {
            _blockValues.Clear();
        }

        /// <summary>Get the current Block value for a target.</summary>
        public int GetBlock(GameObject target)
        {
            if (target == null) return 0;
            return _blockValues.TryGetValue(target, out int block) ? block : 0;
        }
    }
}
