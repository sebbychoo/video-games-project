using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// The Overtime resource meter. Starts at full capacity (default 10),
    /// regenerates per turn from turn 2 onward, and gains bonus points
    /// when the player takes damage. Excess points route to OverflowBuffer.
    /// </summary>
    public class OvertimeMeter : MonoBehaviour
    {
        [SerializeField] private OverflowBuffer overflowBuffer;

        public int Current { get; private set; }
        public int Max { get; private set; }

        /// <summary>Base regen per turn (before Tool modifiers).</summary>
        private int _baseRegen;

        /// <summary>Additive modifier from Tools applied at encounter start.</summary>
        private int _regenModifier;

        /// <summary>Effective regen = base + modifier.</summary>
        public int EffectiveRegen => _baseRegen + _regenModifier;

        /// <summary>Initialize the meter at full capacity for a new encounter.</summary>
        public void Initialize(int maxCapacity, int regenPerTurn, OverflowBuffer overflow)
        {
            Max = maxCapacity;
            Current = maxCapacity;
            _baseRegen = regenPerTurn;
            _regenModifier = 0;
            overflowBuffer = overflow;
        }

        /// <summary>Apply a Tool modifier to the regen value (additive).</summary>
        public void ApplyRegenModifier(int modifier)
        {
            _regenModifier += modifier;
        }

        /// <summary>
        /// Attempt to spend OT points. Returns true if successful.
        /// Rejects the spend if cost exceeds current value.
        /// </summary>
        public bool Spend(int cost)
        {
            if (cost > Current) return false;
            Current -= cost;
            return true;
        }

        /// <summary>
        /// Regenerate OT at the start of a player turn (turn 2 onward).
        /// Caps at max; excess routes to OverflowBuffer.
        /// </summary>
        public void Regenerate()
        {
            int regen = EffectiveRegen;
            if (regen <= 0) return;

            int newValue = Current + regen;
            if (newValue > Max)
            {
                int overflow = newValue - Max;
                Current = Max;
                if (overflowBuffer != null)
                    overflowBuffer.Add(overflow);
            }
            else
            {
                Current = newValue;
            }
        }

        /// <summary>
        /// Gain OT from taking damage. Gain = floor(hpLost / maxHP * 10).
        /// Status effect ticks cap at 1 OT per tick (caller passes isStatusTick=true).
        /// Excess beyond max routes to OverflowBuffer.
        /// </summary>
        public void GainFromDamage(int hpLost, int maxHP, bool isStatusTick = false)
        {
            if (hpLost <= 0 || maxHP <= 0) return;

            int gain = Mathf.FloorToInt((float)hpLost / maxHP * 10f);
            if (isStatusTick)
                gain = Mathf.Min(gain, 1);

            if (gain <= 0) return;

            int newValue = Current + gain;
            if (newValue > Max)
            {
                int overflow = newValue - Max;
                Current = Max;
                if (overflowBuffer != null)
                    overflowBuffer.Add(overflow);
            }
            else
            {
                Current = newValue;
            }
        }
    }
}
