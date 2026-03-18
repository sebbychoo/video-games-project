using UnityEngine;

namespace CardBattle
{
    public struct CardPlayedEvent
    {
        public CardData Card;
        public GameObject Source;
        public GameObject Target;
    }

    public struct DamageEvent
    {
        public GameObject Source;
        public GameObject Target;
        public int Amount;
    }

    public struct StatusEffectEvent
    {
        public GameObject Target;
        public string EffectName;
        public int Duration;
    }

    public struct EntityTransformedEvent
    {
        public GameObject Entity;
        public string NewFormId;
    }

    public class BattleEventBus : MonoBehaviour
    {
        public static BattleEventBus Instance { get; private set; }

        public event System.Action<CardPlayedEvent> OnCardPlayed;
        public event System.Action<DamageEvent> OnDamageDealt;
        public event System.Action<DamageEvent> OnDamageReceived;
        public event System.Action<StatusEffectEvent> OnStatusEffectApplied;
        public event System.Action<StatusEffectEvent> OnStatusEffectRemoved;
        public event System.Action<EntityTransformedEvent> OnEntityTransformed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Raise(CardPlayedEvent e) => OnCardPlayed?.Invoke(e);
        public void Raise(DamageEvent e)
        {
            OnDamageDealt?.Invoke(e);
            OnDamageReceived?.Invoke(e);
        }
        public void Raise(StatusEffectEvent e) => OnStatusEffectApplied?.Invoke(e);
        public void Raise(EntityTransformedEvent e) => OnEntityTransformed?.Invoke(e);
    }
}
