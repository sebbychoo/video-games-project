using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Stores excess Overtime points that exceed the meter's max capacity.
    /// Consumed on the next Attack card play to grant a Rage Burst damage bonus.
    /// </summary>
    public class OverflowBuffer : MonoBehaviour
    {
        private const int MaxOverflow = 999;

        public int Current { get; private set; }

        /// <summary>Initialize the buffer to 0 at encounter start.</summary>
        public void Initialize()
        {
            Current = 0;
        }

        /// <summary>Add overflow points, clamped to MaxOverflow.</summary>
        public void Add(int amount)
        {
            if (amount <= 0) return;
            Current = Mathf.Min(Current + amount, MaxOverflow);

            if (BattleEventBus.Instance != null)
            {
                BattleEventBus.Instance.Raise(new OverflowEvent
                {
                    Amount = amount,
                    NewTotal = Current
                });
            }
        }

        /// <summary>Consume all overflow points and return the amount consumed.</summary>
        public int ConsumeAll()
        {
            int consumed = Current;
            Current = 0;
            return consumed;
        }
    }
}
