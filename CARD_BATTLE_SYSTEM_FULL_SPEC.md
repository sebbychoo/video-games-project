# Overtime — Card Battle System: Complete Implementation Specification

## Table of Contents
1. [Project Context](#project-context)
2. [Technology Stack](#technology-stack)
3. [Architecture Overview](#architecture-overview)
4. [Data Models (C# Code)](#data-models)
5. [Component API Reference](#component-api-reference)
6. [Game Constants](#game-constants)
7. [Balance Tables](#balance-tables)
8. [Correctness Properties](#correctness-properties)
9. [Implementation Tasks](#implementation-tasks)
10. [Existing Codebase Notes](#existing-codebase-notes)
11. [Error Handling](#error-handling)
12. [Testing Strategy](#testing-strategy)

---

## 1. Project Context

**Overtime** is a first-person roguelike deck-builder built in Unity. The player (Jean-Guy) navigates procedurally generated office floors in 3D first-person, and transitions to a 2D turn-based card battle screen when encountering enemies. Cards are office-supply-themed and powered by an "Overtime" resource meter.

The existing codebase (namespace `CardBattle`) provides:
- A basic turn loop (`BattleManager`)
- Card rendering with arc layout (`CardLayoutController`, `CardAnimator`)
- Hover/select/targeting interactions (`CardInteractionHandler`, `CardTargetingManager`)
- A deck/discard cycle (`DeckManager`)
- Single-enemy combat with `Health` component
- Scene transitions (`SceneLoader`)
- A simple energy system (integer on BattleManager)

This spec **extends and replaces** that foundation with the full GDD-defined systems.

### What Already Exists (DO NOT recreate — modify in-place)
- `Assets/Scripts/Battle/BattleManager.cs` — has `CurrentTurn` property, `TurnState` enum references
- `Assets/Scripts/Battle/CardData.cs` — has `energyCost` field (rename to `overtimeCost`), old `CardType` enum (Attack/Skill/Power)
- `Assets/Scripts/Battle/DeckManager.cs` — basic draw/discard cycle
- `Assets/Scripts/Battle/HandManager.cs` — visual hand management
- `Assets/Scripts/Battle/CardAnimator.cs` — already has `PlayRejection()` (horizontal shake) and `PlayEntrance()` with staggered delay
- `Assets/Scripts/Battle/BattleAnimations.cs` — already has `PlayAttackDash()`, `PlayHitShake()`, `PlayDeath()` with coroutines
- `Assets/Scripts/Battle/BattleEventBus.cs` — already has `StatusEffectEvent`, `OnStatusEffectApplied/Removed`, `EntityTransformedEvent`, `CardPlayedEvent`, `DamageEvent`
- `Assets/Scripts/Battle/CardInteractionHandler.cs` — checks `TurnState.PlayerTurn`
- `Assets/Scripts/Battle/CardTargetingManager.cs` — checks `TurnState.PlayerTurn` in `PlayOnTarget()`
- `Assets/Scripts/Battle/TurnState.cs` — old enum (PlayerTurn/EnemyTurn/BattleOver)
- `Assets/Scripts/Battle/EnemyAction.cs` — has `EnemyActionType` enum (DealDamage/ApplyStatus)
- `Assets/Scripts/Enemy/Health.cs` — has `Die()` method that calls `SceneLoader.Instance.LoadExploration()` directly, has `suppressSceneLoad` flag
- `Assets/Scripts/Core/SceneLoader.cs` — uses simple fields (`enemyDefeated`, `useDefaultSpawn`, `playerPosition`), has `using static CardBattle.BattleManager`
- `Assets/Scripts/Player/SavePosition.cs` — reads `SceneLoader.Instance.playerPosition` on Start
- `Assets/Scripts/World/Battlescene_Trigger.cs` — global namespace, references `SceneLoader`
- `Assets/Scripts/Enemy/EnemyFollow.cs` — global namespace, patrol + chase AI
- `Assets/Scripts/UI/Menu.cs` — global namespace, basic pause menu
- `Assets/Scripts/Core/CameraManager.cs` — has `SetBattleMode(bool)` switching FOV 60↔45
- `Assets/Scripts/Procedural/RoomTemplate.cs` — has `roomName`, `roomPrefab`, `roomSize`, `spawnRules`, `doorPositions` but NO `roomType` field
- `Assets/Scripts/Player/Attack.cs` / `Assets/Scripts/Player/Projectile.cs` — real-time projectile combat (Space to shoot). These conflict with card battle and should be removed or disabled.

---

## 2. Technology Stack

- **Engine**: Unity 2022+ with URP (Universal Render Pipeline)
- **Language**: C#
- **UI Text**: TextMeshPro
- **Testing**: FsCheck + NUnit in Unity Test Framework (EditMode tests for property-based testing)
- **Serialization**: Unity JsonUtility or Newtonsoft JSON

---

## 3. Architecture Overview

Layered architecture within Unity's MonoBehaviour/ScriptableObject model:

```
Core Layer:        SaveManager, SceneLoader, RunState, CameraManager
Battle Layer:      BattleManager, OvertimeMeter, OverflowBuffer, ParrySystem,
                   StatusEffectSystem, DeckManager, HandManager, CardTargetingManager,
                   BattleEventBus, EnemyCombatant, EnemyIntentDisplay,
                   BattleAnimations, CardAnimator, TurnPhaseController
Cards Layer:       CardData (SO), CardInstance, CardEffectResolver, SpecialCardRegistry
Exploration Layer: FirstPersonHandsController, WorkBox, BathroomShop, BreakRoomTrade, EnemyFollow
Procedural Layer:  LevelGenerator, RoomGenerator, RoomTemplate
UI Layer:          MainMenu, PauseMenu, GameOverScreen, HubOffice
Narrative Layer:   OpeningDialogue, DeathScreen, WinCinematic, SuicidalWorkerEncounter
```

### Key Architectural Decisions

1. **TurnPhaseController as a state machine**: The old `TurnState` enum is replaced with `TurnPhase` (Draw, Play, Discard, Enemy) managed by a dedicated `TurnPhaseController` component. BattleManager orchestrates; phase transitions are explicit and testable.

2. **OvertimeMeter as a separate component**: Not a plain int on BattleManager. Its own MonoBehaviour with `Spend`, `Regenerate`, `GainFromDamage`, and overflow routing. Isolates resource math from turn logic.

3. **CardEffectResolver pattern**: Card play resolution delegated to a resolver that switches on `CardType`. Special cards use a `SpecialCardRegistry` dictionary mapping card IDs to `ISpecialCardEffect` implementations.

4. **EnemyCombatant as data+behaviour wrapper**: Each enemy is an `EnemyCombatant` MonoBehaviour holding HP, attack pattern index, status effects, and intent. Reuses `Health` internally.

5. **RunState as a serializable POCO**: All run-scoped data lives in `RunState` that `SaveManager` serializes to JSON. BattleManager reads/writes RunState.

6. **Event-driven communication**: `BattleEventBus` extended with new event types so UI subscribes without direct references to game logic.

---

## 4. Data Models

### CardData (ScriptableObject — extends existing)

```csharp
public enum CardType { Attack, Defense, Effect, Utility, Special }
public enum CardRarity { Common, Rare, Legendary, Unknown }
public enum TargetMode { SingleEnemy, AllEnemies, Self, NoTarget }
public enum UtilityEffectType { None, Draw, Restore, Retrieve, Reorder, Heal }

[CreateAssetMenu(menuName = "CardBattle/CardData")]
public class CardData : ScriptableObject
{
    public string cardName;
    public int overtimeCost;          // renamed from energyCost
    [TextArea] public string description;
    public CardType cardType;
    public CardRarity cardRarity;
    public int effectValue;           // damage, draw count, restore amount, heal amount, etc.
    public List<string> parryMatchTags; // Defense cards only — which enemy attack types this can parry
    public TargetMode targetMode;
    public Sprite cardSprite;

    // Effect card fields
    public string statusEffectId;     // "Burn", "Stun", "Bleed"
    public int statusDuration;

    // Special card fields
    public string specialCardId;      // lookup key in SpecialCardRegistry

    // Utility card fields
    public UtilityEffectType utilityEffectType;
}
```

### EnemyCombatantData (ScriptableObject)

```csharp
public enum EnemyVariant { Coworker, Creature, Boss }

[CreateAssetMenu(menuName = "CardBattle/EnemyCombatantData")]
public class EnemyCombatantData : ScriptableObject
{
    public string enemyName;
    public int maxHP;
    public int hoursReward;
    public EnemyVariant variant;
    public Sprite sprite;
    public List<EnemyAction> attackPattern;
    public bool isBoss;
    [Range(0f, 1f)]
    public float enemyParryChance;          // chance enemy parries player's Attack card
    public float baseParryWindowDuration;   // seconds, scales with difficulty/floor
    [TextArea] public string preFightDialogue;
    [TextArea] public string postFightDialogue;
}
```

### EnemyAction (extends existing struct)

```csharp
public enum EnemyActionType { DealDamage, ApplyStatus, Defend, Buff, Special }
public enum EnemyBuffType { None, DamageUp, DamageShield, Regen }
public enum IntentColor { White, Yellow, Red, Unparryable }
public enum EnemyActionCondition { None, HPBelow50, HPBelow25, PlayerLowHP }

[Serializable]
public struct EnemyAction
{
    public EnemyActionType actionType;
    public int value;
    public string statusEffectId;
    public int statusDuration;
    public EnemyBuffType buffType;
    public int buffDuration;
    public IntentColor intentColor;         // White/Yellow/Red/Unparryable
    public List<string> parryMatchTags;     // attack type tags for parry matching
    public EnemyActionCondition condition;
}
```

### StatusEffectInstance

```csharp
[Serializable]
public struct StatusEffectInstance
{
    public string effectId;     // "Burn", "Stun", "Bleed"
    public int duration;
    public int value;           // damage per tick for Burn, extra damage for Bleed
}
```

### EncounterData (ScriptableObject)

```csharp
[CreateAssetMenu(menuName = "CardBattle/EncounterData")]
public class EncounterData : ScriptableObject
{
    public List<EnemyCombatantData> enemies; // 1-4
    public bool isBossEncounter;
    public int badReviewsReward;            // > 0 only for bosses
}
```

### RunState (serializable POCO)

```csharp
[Serializable]
public class RunState
{
    public int currentFloor;
    public int playerHP;
    public int playerMaxHP;
    public int hours;
    public int hoursEarnedTotal;
    public int badReviewsEarnedTotal;
    public List<string> deckCardIds;        // CardData asset names
    public List<string> toolIds;            // Tool asset names
    public List<string> seenCutsceneIds;
    public string startingDeckSetId;
    public bool isActive;
    public int enemiesDefeated;
    public int cardRemovalsThisRun;         // tracks toilet removals for escalating cost
}
```

### MetaState (serializable POCO)

```csharp
[Serializable]
public class MetaState
{
    public int badReviews;
    public Dictionary<string, int> hubUpgradeLevels;
    public List<string> unlockedAchievements;
    public bool tutorialCompleted;
}
```


### ToolData (ScriptableObject)

```csharp
public enum ToolModifierType { OvertimeRegen, ParryWindowBonus, HandSize, DamageBonus, MaxHP, HealPerFloor, TechCardDamage, MaxDeckSize }

[CreateAssetMenu(menuName = "CardBattle/ToolData")]
public class ToolData : ScriptableObject
{
    public string toolName;
    [TextArea] public string description;
    public Sprite toolSprite;
    public CardRarity rarity;
    public List<ToolModifier> modifiers;
}

[Serializable]
public struct ToolModifier
{
    public ToolModifierType modifierType;
    public int value;
}
```

Tools are passive relics (like Slay the Spire relics). Persist for entire run, apply modifiers automatically, lost on death. Found from: Suicidal Worker Encounter rewards, Bathroom Shop purchases, Break Room trades.

### HubUpgradeData (ScriptableObject)

```csharp
[CreateAssetMenu(menuName = "CardBattle/HubUpgradeData")]
public class HubUpgradeData : ScriptableObject
{
    public string upgradeId;
    public string displayName;
    public int maxLevel;
    public List<int> costPerLevel;
    public List<HubUpgradeEffect> effectsPerLevel;
    [TextArea] public string description;
    public List<Sprite> furnitureSprites;
}

[Serializable]
public struct HubUpgradeEffect
{
    public ToolModifierType modifierType;
    public int value;
}
```

### StartingDeckSet (ScriptableObject)

```csharp
[CreateAssetMenu(menuName = "CardBattle/StartingDeckSet")]
public class StartingDeckSet : ScriptableObject
{
    public string setName;
    [TextArea] public string description;
    public List<CardData> cards; // exactly 8
}
```

### GameConfig (ScriptableObject)

```csharp
[CreateAssetMenu(menuName = "CardBattle/GameConfig")]
public class GameConfig : ScriptableObject
{
    public int finalFloor = 75;
    public int baseHandSize = 5;
    public int overtimeMaxCapacity = 10;
    public int overtimeRegenPerTurn = 2;
    public int workerEncounterFloor = 5;
    public int bossFloorInterval = 3;
    public int breakRoomFloorInterval = 2;
    public int workerHP = 12;
    public int workerSelfDamage = 4;
    public int playerBaseHP = 80;
    public int minimapBaseRevealLevel = 0;
    public int waterCoolerFloorInterval = 2;
    public float waterCoolerHealPercent = 0.35f;
    public int shopMinCards = 3;
    public int shopMaxCards = 5;
    public int shopMinTools = 0;
    public int shopMaxTools = 2;
    public int cardRemovalBaseCost = 25;
    public int cardRemovalCostIncrease = 10;
    public int minimumDeckSize = 1;
    public int maximumDeckSize = 25;
    public float safeRoomChaseTimeout = 5f;
    public float baseParryWindowDuration = 1.5f;
    public float parryWindowFloorScaling = 0.02f;
    public float parryWindowMinDuration = 0.3f;
}
```

### WorkBoxData

```csharp
public enum WorkBoxSize { Small, Big, Huge }

[Serializable]
public struct WorkBoxSpawnRates
{
    public float smallRate;
    public float bigRate;
    public float hugeRate;
}
```

### ISpecialCardEffect Interface

```csharp
public interface ISpecialCardEffect
{
    void Execute(CardEffectContext context);
}

public struct CardEffectContext
{
    public CardData Card;
    public GameObject Source;
    public List<EnemyCombatant> Targets;
    public BattleManager Battle;
}
```

---

## 5. Component API Reference

### Battle Components

| Component | File Path | Responsibility | Key API |
|---|---|---|---|
| `BattleManager` | `Assets/Scripts/Battle/BattleManager.cs` (modify) | Orchestrates encounter lifecycle | `StartEncounter(EncounterData)`, `TryPlayCard(CardInstance, GameObject)`, `EndTurn()` |
| `TurnPhaseController` | `Assets/Scripts/Battle/TurnPhaseController.cs` (create) | Draw→Play→Discard→Enemy state machine | `CurrentPhase`, `AdvancePhase()`, `OnPhaseChanged` event |
| `OvertimeMeter` | `Assets/Scripts/Battle/OvertimeMeter.cs` (create) | Tracks current/max OT, regen, overflow routing | `Spend(int)`, `Regenerate()`, `GainFromDamage(int hpLost, int maxHP)`, `Current`, `Max` |
| `OverflowBuffer` | `Assets/Scripts/Battle/OverflowBuffer.cs` (create) | Stores excess OT, consumed on next attack | `Add(int)`, `ConsumeAll() → int`, `Current` |
| `ParrySystem` | `Assets/Scripts/Battle/ParrySystem.cs` (create) | Parry windows, match validation, enemy parry chance | `StartParryWindow(EnemyAction, EnemyCombatant)`, `TryParry(CardInstance) → bool`, `IsParryWindowActive`, `GetMatchingCards(Hand)` |
| `StatusEffectSystem` | `Assets/Scripts/Battle/StatusEffectSystem.cs` (create) | Active effects on player and enemies | `Apply(target, StatusEffectInstance)`, `Tick(target)`, `GetEffects(target)`, `ClearAll(target)` |
| `CardEffectResolver` | `Assets/Scripts/Battle/CardEffectResolver.cs` (create) | Resolves card effects by type | `Resolve(CardData, source, target(s))` |
| `SpecialCardRegistry` | `Assets/Scripts/Battle/SpecialCardRegistry.cs` (create) | Maps special card IDs to effect implementations | `Register(string id, ISpecialCardEffect)`, `Execute(string id, context)` |
| `EnemyCombatant` | `Assets/Scripts/Battle/EnemyCombatant.cs` (create) | Per-enemy state: HP, pattern, status, intent | `ExecuteAction()`, `TakeDamage(int)`, `CurrentIntent`, `IsAlive` |
| `EnemyIntentDisplay` | `Assets/Scripts/Battle/EnemyIntentDisplay.cs` (create) | UI icon + damage number above enemy | `UpdateIntent(EnemyAction)`, `Hide()` |
| `DeckManager` | `Assets/Scripts/Battle/DeckManager.cs` (modify) | Draw pile, discard pile, shuffle | `Draw()`, `Discard(CardData)`, `ShuffleDiscardIntoDeck()`, `AddCard(CardData)` |
| `HandManager` | `Assets/Scripts/Battle/HandManager.cs` (modify) | Visual hand of CardInstances | `AddCards(List<CardData>)`, `RemoveCard(CardInstance)`, `DiscardAll()` |
| `CardTargetingManager` | `Assets/Scripts/Battle/CardTargetingManager.cs` (modify) | Card selection + target selection, supports targeting any entity | `SelectCard(CardInstance)`, `PlayOnTarget(GameObject)`, `CancelSelection()` |
| `BattleEventBus` | `Assets/Scripts/Battle/BattleEventBus.cs` (modify) | Pub/sub for battle events | `Raise<T>(T)`, events: CardPlayed, DamageDealt, StatusEffect, TurnPhaseChanged, Overflow, Parry, RageBurst |
| `RageBurstCalculator` | `Assets/Scripts/Battle/RageBurstCalculator.cs` (create) | Piecewise linear interpolation for bonus damage | `CalculateBonus(int overflowPoints) → float` |
| `DeckSizeLimiter` | `Assets/Scripts/Battle/DeckSizeLimiter.cs` (create) | Enforces max deck size | Reject additions exceeding limit |

### Exploration Components

| Component | File Path | Responsibility | Key API |
|---|---|---|---|
| `FirstPersonHandsController` | `Assets/Scripts/Exploration/FirstPersonHandsController.cs` (create) | 2D hand sprites on camera overlay | `SetState(HandState)`, `PlayInteraction()` |
| `WorkBox` | `Assets/Scripts/Exploration/WorkBox.cs` (create) | Chest under desks, rarity reveal | `Open()`, `RevealCard(int)`, `KeepCard(int)`, `LeaveCard(int)` |
| `BathroomShop` | `Assets/Scripts/Exploration/BathroomShop.cs` (create) | Buy cards/Tools, remove cards via toilet | `PurchaseCard(int)`, `PurchaseTool(int)`, `RemoveCard(CardData)` |
| `BreakRoomTrade` | `Assets/Scripts/Exploration/BreakRoomTrade.cs` (create) | NPC item-for-item trades | `AcceptTrade()`, `DeclineTrade()` |
| `WaterCooler` | `Assets/Scripts/Exploration/WaterCooler.cs` (create) | Rest stop, restores 35% max HP | `Interact()`, `IsUsed` |
| `FloorMinimap` | `Assets/Scripts/Exploration/FloorMinimap.cs` (create) | Corner minimap (Whiteboard upgrade) | `UpdateVisited(Room)`, `SetRevealLevel(int)` |
| `EnemyFollow` | `Assets/Scripts/Enemy/EnemyFollow.cs` (modify) | Patrol + chase AI | Extended with room-type avoidance, aggro flag, safe room chase timeout |

### Persistence Components

| Component | File Path | Key API |
|---|---|---|
| `SaveManager` | `Assets/Scripts/Core/SaveManager.cs` (create) | `SaveRun()`, `LoadRun()`, `SaveMeta()`, `LoadMeta()`, `WipeRun()`, `SnapshotPreEncounter()`, `RestorePreEncounter()` |

### UI Components

| Component | File Path |
|---|---|
| `OvertimeMeterUI` | `Assets/Scripts/Battle/UI/OvertimeMeterUI.cs` — displays OT dots + overflow, text format "total/max" |
| `ParryWindowUI` | `Assets/Scripts/Battle/UI/ParryWindowUI.cs` — parry window timer, highlights matching Defense cards |
| `DeckCounterUI` | `Assets/Scripts/Battle/UI/DeckCounterUI.cs` — draw/discard pile counts, clickable to inspect |
| `EndTurnButton` | `Assets/Scripts/Battle/UI/EndTurnButton.cs` — enabled only during Play_Phase |
| `TurnCounterUI` | `Assets/Scripts/Battle/UI/TurnCounterUI.cs` — turn number at top-center |
| `VictoryScreen` | `Assets/Scripts/Battle/UI/VictoryScreen.cs` — randomized victory verb + rewards |
| `StartingDeckCarousel` | `Assets/Scripts/Battle/UI/StartingDeckCarousel.cs` — deck set selection with arrows |
| `FloatingCombatText` | `Assets/Scripts/Battle/UI/FloatingCombatText.cs` — floating damage/parry/cost/status numbers |
| `StatusEffectIconStack` | `Assets/Scripts/Battle/UI/StatusEffectIconStack.cs` — vertical stack behind entities |
| `CardEffectPreview` | `Assets/Scripts/Battle/UI/CardEffectPreview.cs` — tooltip with calculated effective values |
| `MainMenu` | `Assets/Scripts/UI/MainMenu.cs` — New Game / Continue / Settings / Achievements / Quit |
| `PauseMenu` | `Assets/Scripts/UI/Menu.cs` (modify) — Resume / Hub / Settings / View Deck / View Tools / Quit |
| `GameOverScreen` | `Assets/Scripts/UI/GameOverScreen.cs` — run stats + New Run / Main Menu |
| `HubOffice` | `Assets/Scripts/UI/HubOffice.cs` — 2D diorama, cursor-only, Bad_Reviews upgrades |

### Narrative Components

| Component | File Path |
|---|---|
| `OpeningDialogue` | `Assets/Scripts/Narrative/OpeningDialogue.cs` — YES/NO choice, joke ending vs game start |
| `DeathScreen` | `Assets/Scripts/Narrative/DeathScreen.cs` — "Dragged back to desk" sequence |
| `WinCinematic` | `Assets/Scripts/Narrative/WinCinematic.cs` — quiet going-home cinematic |
| `SuicidalWorkerEncounter` | `Assets/Scripts/Narrative/SuicidalWorkerEncounter.cs` — floor 5 special encounter |
| `TutorialNPC` | `Assets/Scripts/Narrative/TutorialNPC.cs` — first-run coworker guide |

---

## 6. Game Constants

All centralized in `GameConfig` ScriptableObject:

| Constant | Value | Description |
|---|---|---|
| Final Floor | 75 | Must be multiple of 3 (boss floor) |
| Base Hand Size | 5 | Cards drawn per turn |
| Overtime Max Capacity | 10 | Starting and max OT points |
| Overtime Regen Per Turn | 2 | OT gained at start of turn 2+ |
| Boss Floor Interval | 3 | Boss every 3 floors (3, 6, 9...) |
| Break Room Interval | 2 | Break rooms every 2 floors |
| Worker Encounter Floor | 5 | Suicidal worker encounter |
| Worker HP | 12 | Worker's HP pool |
| Worker Self-Damage | 4 | Damage worker deals to self per turn |
| Player Base HP | 80 | Jean-Guy's starting max HP |
| Water Cooler Interval | 2 | Every 2 floors |
| Water Cooler Heal | 35% | Of max HP, rounded down |
| Shop Cards | 3–5 | Cards per bathroom shop |
| Shop Tools | 0–2 | Tools per bathroom shop |
| Card Removal Base Cost | 25 Hours | Toilet removal |
| Card Removal Cost Increase | +10 Hours | Per previous removal this run |
| Minimum Deck Size | 1 | Can't remove below this |
| Maximum Deck Size | 25 | Default cap, raised by Filing Cabinet |
| Safe Room Chase Timeout | 5 seconds | Enemy gives up at safe room door |
| Base Parry Window | 1.5 seconds | Default parry window length |
| Parry Window Floor Scaling | -0.02s per floor | Shorter on deeper floors |
| Parry Window Minimum | 0.3 seconds | Floor for parry window duration |

---

## 7. Balance Tables

### Rage Burst Diminishing Returns (Piecewise Linear Interpolation)

| Overflow Points | Bonus Damage % |
|---|---|
| 1 | +20% |
| 5 | +80% |
| 10 | +120% |
| 20 | +140% |

Between reference points: linearly interpolate. Example: 3 overflow → lerp(20%, 80%, (3-1)/(5-1)) = 50%. Above 20: clamped at +140%.

### Work Box Rarity Tables (by floor)

| Floor Range | Common | Rare | Legendary | Unknown |
|---|---|---|---|---|
| 1–3 | 72% | 25% | 3% | 0% |
| 3–6 | 52% | 38% | 10% | 0% |
| 7–10 | 33% | 45% | 21% | 1% |
| 11–15 | 18% | 45% | 32% | 5% |
| 16–20 | 8% | 30% | 47% | 15% |
| 21–24 | 0% | 12% | 58% | 30% |
| 25+ | 0% | 1% | 69% | 30% |

### Work Box Size Spawn Rates (by floor)

| Floor Range | Small | Big | Huge |
|---|---|---|---|
| 1–5 | 100% | 0% | 0% |
| 6–10 | 90% | 10% | 0% |
| 11+ | 70% | 25% | 5% |

### Work Box Card Counts (by size)

| Size | Min Cards | Max Cards |
|---|---|---|
| Small | 1 | 3 |
| Big | 3 | 5 |
| Huge | 5 | 7 |

### Bathroom Shop Pricing

| Card Rarity | Hours Cost |
|---|---|
| Common | 10 |
| Rare | 25 |
| Legendary | 100 |
| Unknown | 150 |

| Tool Rarity | Hours Cost |
|---|---|
| Common | 30 |
| Rare | 60 |
| Legendary | 200 |

Unknown rarity Tools are NOT available in shops. Boss floor shops guarantee at least 1 Tool.

### Overtime Gain from Damage

Formula: `floor(actualHPLost / maxHP * 10)` OT points gained. Status effect ticks (Burn) grant at most 1 OT per tick regardless of damage.

---

## 8. Correctness Properties (41 Total)

These are formal invariants that must hold across all valid executions. Each maps to exactly one property-based test using FsCheck/NUnit.

| # | Property Name | Rule | Validates |
|---|---|---|---|
| 1 | Card Count Conservation | Total cards across Draw_Pile + Hand + Discard_Pile = deck size at encounter start, for any sequence of operations | Req 7.4, 5.5, 19.7 |
| 2 | Deck Cycle Round Trip | Shuffling non-empty discard into empty draw produces same card set, discard empty afterward | Req 7.1, 7.3 |
| 3 | Draw Phase Hand Size | After Draw_Phase: hand = min(baseHandSize, drawPile + discardPile) | Req 1.1, 7.1, 7.2 |
| 4 | Turn Phase Ordering | Strict cycle: Draw→Play→Discard→Enemy. First phase = Draw. Stun skips Play. | Req 1.2, 1.5, 1.6, 1.7, 10.7 |
| 5 | Overtime Spend Correctness | cost <= current → meter = current - cost; cost > current → rejected, meter unchanged | Req 2.3, 2.4 |
| 6 | Overtime Regeneration Capped | Regen sets meter to min(v + 2, max) | Req 2.2 |
| 7 | Damage-to-OT Gain with Overflow | Gain = floor(hpLost/maxHP * 10), capped at max, excess to overflow | Req 2.5, 2.6 |
| 8 | Rage Burst Formula | Piecewise linear interpolation: exact at reference points, clamped at 140% | Req 3.3 |
| 9 | Rage Burst Consumption on Attack Only | Attack card: consume all overflow, apply bonus. Other types: overflow unchanged. | Req 3.2, 3.4, 3.5 |
| 10 | Parry Cancels Damage | Matching Defense card during Parry_Window → 0 damage, card to discard at no OT cost | Req 6.1, 6.2, 6.3, 9.2 |
| 11 | Missed Parry Deals Full Damage | Expired window without match → full damage | Req 6.4 |
| 12 | Proactive Defense Costs OT | Defense card during Play_Phase → deduct OT, card to discard | Req 5.3, 6.5 |
| 13 | Attack Card Deals Correct Damage | Single: target takes effectValue. All: every living enemy takes effectValue. | Req 5.1, 5.2, 8.4 |
| 14 | Dead Enemies Removed and Skipped | HP 0 → excluded from Enemy_Phase. All dead → victory. | Req 8.5, 8.6, 8.7 |
| 15 | Player Defeat at Zero HP | Player HP 0 → immediate defeat | Req 9.5 |
| 16 | Status Effect Duration Tick | Each duration -1 per turn end, removed at 0 | Req 10.3 |
| 17 | Status Effect Refresh (No Stacking) | Re-applying same type refreshes duration, no duplicate | Req 10.2 |
| 18 | Burn Deals Damage at Turn Start | Burn value dealt at start of target's turn | Req 10.4 |
| 19 | Stun Skips Enemy Action | Stunned enemy action skipped in Enemy_Phase | Req 10.5 |
| 19a | Stun Skips Player Play Phase | Stunned player: Draw→Discard→Enemy (skip Play) | Req 10.7 |
| 20 | Bleed Amplifies Incoming Damage | Damage to target with Bleed = d + bleedValue | Req 10.6 |
| 21 | Enemy Attack Pattern Cycling | Action at turn t = pattern[t % N] | Req 26.2 |
| 22 | Death Wipes Run, Preserves Meta | Run state wiped, Bad_Reviews + upgrades preserved | Req 24.1, 24.2, 24.3 |
| 23 | Run State Persistence Round Trip | Serialize → deserialize = identical RunState | Req 27.5, 27.6 |
| 24 | Cutscene Seen Flag Persistence | Seen stays seen on load; new run clears flags | Req 27.1–27.4 |
| 25 | Boss Floor Placement | Boss rooms at floors 3, 6, 9... only | Req 25.1, 33.6 |
| 26 | Work Box Card Count by Size | Small [1,3], Big [3,5], Huge [5,7] | Req 21.3 |
| 27 | Shop Purchase Deducts Currency | Hours = h - c, item added; rejected if c > h | Req 22.3, 22.4 |
| 28 | Card Removal via Toilet | Deck loses card, size -1, Hours -c | Req 22.5, 22.6 |
| 29 | Trade Conserves Inventory | Accept: have B not A. Decline: unchanged. | Req 23.5, 23.6 |
| 30 | Tool Modifier Application | Effective = baseValue + sum(tool modifiers of same type). Stack additively. | Req 15.2, 15.3, 15.4 |
| 31 | Utility Draw Card Effect | Draw min(N, availableCards) additional cards | Req 19.1 |
| 32 | Utility Restore OT Effect | Meter = min(v+N, m), excess to overflow | Req 19.2, 2.6 |
| 33 | Boss Floor Blocks Exit | Exit locked while boss alive, unlocked after defeat | Req 38.3, 38.4, 38.5 |
| 34 | Heal Utility Capped at Max HP | HP = min(hp + H, maxHP) | Req 19.5 |
| 35 | Status Effect OT Gain Capped at 1 | Status tick → at most 1 OT regardless of damage | Req 2.6 |
| 36 | Bleed Amplifies All Sources Including Burn | Bleed bonus applies to Burn ticks too | Req 10.6 |
| 37 | Minimum Deck Size Enforced | Removal rejected if deck <= 1 | Req 22.8 |
| 38 | One Toilet Flush Per Visit | Second removal rejected in same bathroom | Req 22.7 |
| 39 | Escalating Card Removal Cost | Cost = 25 + (r * 10) where r = previous removals | Req 43.5 |
| 40 | Water Cooler Heals 35% Once | HP = min(hp + floor(maxHP * 0.35), maxHP). Second use rejected. | Req 42.2, 42.3 |
| 41 | Mid-Combat Save Restores Pre-Encounter | Quit during encounter saves pre-combat state, resume in exploration | Req 27.7 |


---

## 9. Implementation Tasks

### Status Legend
- `[x]` = COMPLETED (already implemented in the codebase)
- `[ ]` = NOT STARTED (remaining work)
- `[x]*` or `[ ]*` = OPTIONAL (can be skipped for faster MVP)

### COMPLETED TASKS (1–22) — Already in the codebase

All tasks from 1 through 22 are complete. This includes:

**Task 1: Foundational data models and enums** — CardData updated (energyCost→overtimeCost, new CardType/CardRarity/TargetMode/UtilityEffectType enums, new fields), EnemyAction extended (Defend/Buff/Special, IntentColor, parryMatchTags, conditions), EnemyCombatantData SO created, EncounterData/StatusEffectInstance/GameConfig/RunState/MetaState created, ToolData/HubUpgradeData/StartingDeckSet/WorkBoxData created, TurnState→TurnPhase migration done across BattleManager/CardInteractionHandler/CardTargetingManager, BattleEventBus extended with OverflowEvent/ParryEvent/TurnPhaseChangedEvent/RageBurstEvent.

**Task 3: Core battle subsystems** — OvertimeMeter (capacity 10, regen 2/turn from turn 2, overflow routing, Tool modifier support), OverflowBuffer (Add/ConsumeAll, init 0, clamp 999), RageBurstCalculator (piecewise linear interpolation), ParrySystem (parry windows, match validation, enemy parry chance, configurable duration per enemy, floor scaling, Unparryable skip). Property tests for Properties 5-12, 35.

**Task 4: Status effect system** — StatusEffectSystem (apply/tick/getEffects/clearAll, refresh no-stack, Burn damage at turn start, Stun skip enemy/player, Bleed amplifies all damage including Burn). Property tests for Properties 16-20, 19a, 36.

**Task 5: Turn phase controller and deck management** — TurnPhaseController (Draw→Play→Discard→Enemy, stun skip), DeckManager updated (reshuffle, Fisher-Yates, AddCard, card conservation). Property tests for Properties 1-4.

**Task 7: Card effect resolution and enemy combat** — CardEffectResolver (Attack/Defense/Effect/Utility/Special dispatch, Rage Burst on Attack, proactive parry), EnemyCombatant (HP, pattern cycling, conditional patterns, enemy parry chance, Bleed bonus), SpecialCardRegistry. Property tests for Properties 13-15, 21, 31-32, 34.

**Task 8: BattleManager orchestration** — Refactored to delegate to all subsystems, StartEncounter/TryPlayCard/EndTurn, enemy turn execution with sequential actions and Parry_Windows, EnemyIntentDisplay with real-time updates and IntentColor.

**Task 10: Card targeting and animations** — CardTargetingManager updated for multi-enemy + free targeting (any entity including player/allies), CardEffectPreview tooltip, CardAnimator (rejection shake, staggered entrance, exit animation), BattleAnimations (attack dash, death, screen shake, Rage Burst pulse, overflow glow), FloatingCombatText, StatusEffectIconStack.

**Task 11: Battle UI** — OvertimeMeterUI (dots + text "total/max"), ParryWindowUI, DeckCounterUI (clickable pile inspection), EndTurnButton, TurnCounterUI, PlayerHPStack/EnemyHPBar updated.

**Task 13: Persistence** — SaveManager (SaveRun/LoadRun/SaveMeta/LoadMeta/WipeRun/SnapshotPreEncounter/RestorePreEncounter, JSON, corrupted file handling). Property tests for Properties 22-24, 41.

**Task 14: Battle transition** — SceneLoader updated (EncounterData passing, victory return, defeat reset, pre-encounter snapshot), Battlescene_Trigger updated, battle scene background (blurred 3D behind 2D), VictoryScreen, encounter victory rewards (Hours per enemy, Bad_Reviews for bosses, no card rewards).

**Task 15: Starting deck and Tools** — StartingDeckCarousel UI, Tool modifier system (OT regen, parry window, hand size, damage bonus, additive stacking), deck size limit enforcement (default 25, Filing Cabinet upgrade). Property test for Property 30.

**Task 17: Exploration systems** — WorkBox (size spawn rates, card counts, rarity tables, shake animation, rarity reveal sequence grey→yellow→red→black, Keep/Leave, revisit skip, walk-away persistence), BathroomShop (card/tool pricing, toilet removal with escalating cost, one removal per visit, min deck size 1, boss floor tool guarantee, fixed inventory per floor), BreakRoomTrade (card-for-card, equal/unfavorable trades, accept/decline). Property tests for Properties 26-29, 37-39.

**Task 18: Exploration behavior** — EnemyFollow updated (safe room avoidance, chase timeout, aggro variants, single enemy enters encounter), WaterCooler (35% heal, one-time use, Plant upgrade stacking), FirstPersonHandsController (idle bob, sprint pump, interaction reach, breathing, hide before battle), safe room NPC dialogue. Property test for Property 40.

**Task 20: Procedural generation** — LevelGenerator extended (Work_Boxes under desks, bathrooms every floor, boss rooms every 3 floors, break rooms every 2 floors, Suicidal_Worker on floor 5, enemy type weighting by depth, room types, constrained generation), boss floor gating (exit locked until boss defeated, intercept on leave attempt), FloorMinimap (levels 0-3 based on Whiteboard upgrade). Property tests for Properties 25, 33.

**Task 21: Hub Office** — HubOffice scene (2D diorama, cursor-only, hover→show upgrades, click→purchase, visual furniture updates, multiple levels with escalating costs), hub upgrade effects (Computer→tech card damage, Coffee Machine→OT regen, Desk Chair→parry window, Filing Cabinet→hand size + max deck, Plant→base HP + passive heal on floor exit, Whiteboard→minimap).

**Task 22: Narrative events** — OpeningDialogue (YES joke ending / NO proceed), DeathScreen ("dragged back to desk", wipe run, preserve meta), GameOverScreen (floor/enemies/Hours/Bad_Reviews stats, New Run / Main Menu), WinCinematic (door spawn after final boss, quiet going-home cinematic, return to Main Menu), SuicidalWorkerEncounter (floor 5, non-hostile NPC, worker HP 10-15, self-damage per turn, Shield resolution via Defense card parry, Empathy resolution via player self-damage, failure if worker dies, different Tool rewards per path).

---

### REMAINING TASKS (23–27) — Not yet implemented

#### Task 23: Menus and UI Screens

**23.1 Implement MainMenu**
- Create `Assets/Scripts/UI/MainMenu.cs`
- Options: New Game, Continue, Settings, Achievements, Quit
- New Game → opening dialogue (OpeningDialogue scene/overlay)
- Continue → load saved run via SaveManager.LoadRun() (greyed out if no save exists)
- Settings → audio volume, display resolution, controls panel
- Achievements → list with unlock status and descriptions
- Quit → close application
- Accessible after death screen and win cinematic
- First screen displayed on game launch

**23.2 Update PauseMenu**
- Modify `Assets/Scripts/UI/Menu.cs`
- Escape key pauses during exploration AND combat
- Options: Resume, Hub Office, Settings, View Deck, View Tools, Quit to Main Menu
- Resume → close overlay, unpause
- Hub Office → open HubOffice scene, preserve run state
- Settings → same panel as MainMenu settings
- View Deck → scrollable grid of all deck cards
- View Tools → all collected Tools with name, sprite, description
- Quit to Main Menu → save run state via SaveManager, return to MainMenu
- Freeze ALL gameplay while paused (enemy movement, turn timers, animations)

**23.3 Implement boss encounter dialogue**
- Display pre-fight dialogue before boss combat starts
- Display post-fight dialogue after boss victory
- Floors 1–9: corporate business language tone
- Floors 12+: unnatural, unsettling tone
- Dialogue text comes from EnemyCombatantData.preFightDialogue / postFightDialogue

#### Task 24: Tutorial System

**24.1 Implement TutorialNPC and first-run tutorial flow**
- Create `Assets/Scripts/Narrative/TutorialNPC.cs`
- Triggers ONLY on first run ever (no previous runs in MetaState, tutorialCompleted == false)
- Hub_Office walkthrough: coworker explains furniture as mundane office orientation ("This is your desk, you can upgrade your stuff here")
- NO combat explanation in hub — combat is a surprise
- First enemy encounter: NPC reacts with surprise ("What the — that guy looks pissed. Uh... I think you gotta fight him?")
- First combat: contextual UI prompts highlighting card hand, OT meter, enemy HP, intent icons, End Turn button. Confused/improvised language, NOT formal tutorial text.
- Work_Box discovery: NPC reacts as if finding it for first time ("Wait, there's stuff under the desks?")
- After first combat: NPC says farewell, disappears
- All prompts are non-blocking contextual tooltips (dismissible, not forced dialogue)
- Persist tutorialCompleted flag in MetaState so tutorial never replays

#### Task 25: Checkpoint
- Ensure all narrative, menus, and tutorial work
- All property tests pass

#### Task 26: Integration Wiring and Final Assembly

**26.1 Wire BattleManager to all subsystems and UI**
- Connect BattleManager → TurnPhaseController, OvertimeMeter, OverflowBuffer, ParrySystem, StatusEffectSystem, CardEffectResolver, DeckManager, HandManager, EnemyCombatant instances
- Connect ALL UI components to BattleEventBus subscriptions: OvertimeMeterUI, ParryWindowUI, DeckCounterUI, EndTurnButton, TurnCounterUI, FloatingCombatText, StatusEffectIconStack, CardEffectPreview, EnemyIntentDisplay, VictoryScreen
- Connect SaveManager to SceneLoader for battle transitions
- Connect RunState to BathroomShop, WorkBox, BreakRoomTrade for inventory/deck mutations
- Connect MetaState to HubOffice for upgrade purchases
- Connect Tool modifiers to BattleManager at encounter start

**26.2 Wire exploration scene systems**
- Connect EnemyFollow to SceneLoader for encounter triggering
- Connect FirstPersonHandsController to player movement and scene transitions
- Connect WorkBox, BathroomShop, BreakRoomTrade to RunState for deck/inventory mutations
- Connect WaterCooler to RunState for HP restoration
- Connect FloorMinimap to MetaState for upgrade level
- Connect LevelGenerator to floor progression and boss gating

**26.3 Wire menu and narrative flow**
- Full scene flow: MainMenu → OpeningDialogue → StartingDeckCarousel → HubOffice → Floor 1
- Death flow: Battlescene → DeathScreen → GameOverScreen → MainMenu or New Run
- Win flow: Battlescene → WinCinematic → MainMenu
- Pause flow: Exploration/Battle → PauseMenu → HubOffice (mid-run) or SaveManager (quit)
- Continue flow: MainMenu → SaveManager.LoadRun() → resume at saved state

**26.4 Write integration tests for full battle flow (OPTIONAL)**
- Create `Assets/Tests/PlayMode/BattleIntegrationTests.cs`
- Test complete encounter: start → draw → play cards → end turn → enemy phase → victory/defeat
- Test Rage Burst activation during multi-enemy encounter
- Test status effect interactions (Burn + Bleed combo)

**26.5 Write integration tests for scene transitions (OPTIONAL)**
- Create `Assets/Tests/PlayMode/SceneTransitionTests.cs`
- Test exploration → battle → exploration round trip
- Test save/load mid-run
- Test death → game over → new run flow

#### Task 27: Final Checkpoint
- Ensure ALL systems integrated
- All property tests pass
- All integration tests pass

---

### Scene Architecture (New Scenes Required)

| Scene Name | Purpose | Task |
|---|---|---|
| `MainMenuScene` | Main menu | 23.1 |
| `HubOfficeScene` | 2D diorama hub, cursor-only | 21.1 (done) |
| `OpeningDialogueScene` | Boss overtime question, YES/NO | 22.1 (done) |
| `DeathScreenScene` | "Dragged back to desk" | 22.2 (done) |
| `GameOverScene` | Run stats, New Run / Main Menu | 22.3 (done) |
| `WinCinematicScene` | Quiet going-home cinematic | 22.4 (done) |
| `DeckSelectionScene` | Starting deck carousel | 15.1 (done) |

Some of these can be full-screen UI overlays instead of separate scenes. SceneLoader must support the full navigation flow either way.

---

## 10. Existing Codebase Notes

### Critical Migration: TurnState → TurnPhase (DONE)
Files that were updated: BattleManager.cs, CardTargetingManager.cs, CardInteractionHandler.cs. Old enum values (PlayerTurn/EnemyTurn/BattleOver) replaced with TurnPhase (Draw/Play/Discard/Enemy).

### Health.cs Refactoring (DONE)
EnemyCombatant wraps Health internally. Health.Die() scene-loading behavior disabled via suppressSceneLoad flag set by EnemyCombatant.

### SceneLoader.cs Legacy Fields (DONE)
Old simple fields replaced with RunState/EncounterData-driven logic.

### Attack.cs / Projectile.cs — Legacy Scripts
Real-time projectile combat scripts. Conflict with card battle system. Should be removed or moved to Legacy folder.

### Namespace Notes
Several scripts are NOT in `CardBattle` namespace but interact with it: Battlescene_Trigger, EnemyFollow, Menu, SceneLoader, SavePosition, CameraManager. They need `using CardBattle;` imports as they integrate with new systems.

### RoomTemplate.cs
Originally had no `roomType` field. A `RoomType` enum was added as part of task 20.1.

### BattleAnimations.cs
Already had PlayAttackDash(), PlayHitShake(), PlayDeath(). Task 10.4 added screen shake, Rage Burst pulse, overflow edge glow on top of existing animations.

### CardAnimator.cs
Already had PlayRejection() and PlayEntrance(). Task 10.3 verified these match spec and added exit animation.

### BattleEventBus.cs
Already had StatusEffectEvent and EntityTransformedEvent. Task 1.6 added OverflowEvent, ParryEvent, TurnPhaseChangedEvent, RageBurstEvent.

---

## 11. Error Handling

| Scenario | Handling |
|---|---|
| Card play with insufficient OT | Reject play, shake animation, no state change |
| Draw from empty deck + empty discard | Skip draw, hand may be smaller than base size |
| Damage to dead enemy | No-op, skip targeting dead enemies |
| Shop purchase with insufficient Hours | Reject, display feedback |
| Hub upgrade with insufficient Bad_Reviews | Reject, display feedback |
| Save file corrupted or missing | Start fresh run, preserve meta state if possible |
| Overflow buffer at max | Clamp to 999 |
| Status effect on dead target | No-op |
| Card targeting cancelled mid-selection | Return card to hand, restore pre-selection state |
| Scene load failure | Log error, attempt reload, fallback to exploration |
| Empty encounter data (0 enemies) | Log error, skip encounter, return to exploration |
| Negative HP from multiple sources | Clamp to 0, trigger defeat once |
| Card removal at minimum deck size (1) | Disable toilet, display feedback |
| Stun on player during Play_Phase | Skip Play_Phase, auto-advance to Discard_Phase |
| Multiple enemies chasing, one catches | Only catching enemy enters encounter, others resume patrol |
| Quit during active encounter | Save pre-encounter snapshot, resume in exploration with enemy present |
| Heal exceeds max HP | Clamp to maxHP |
| Bleed + Burn tick | Bleed bonus applies to Burn damage (d + bleedValue) |
| Stunned enemy during Enemy_Phase | Action skipped, no parry window for that enemy |
| Enemy chasing into safe room | Stop at doorway, give up after ~5 seconds, resume patrol |
| Win final boss | Door spawns, win cinematic on interaction, return to Main Menu |
| Card addition at max deck size | Reject, display "deck full" feedback |

---

## 12. Testing Strategy

### Property-Based Tests (FsCheck/NUnit, EditMode)

Minimum 100 iterations per property test. Each test references its property number with tag comment format:
```
// Feature: card-battle-system, Property {number}: {property_text}
```

### Test File Organization

```
Assets/Tests/EditMode/
  Battle/
    OvertimeMeterPropertyTests.cs      — Properties 5, 6, 7, 35
    RageBurstPropertyTests.cs          — Properties 8, 9
    ParrySystemPropertyTests.cs        — Properties 10, 11, 12
    CardResolutionPropertyTests.cs     — Properties 13, 31, 32, 34
    DeckManagerPropertyTests.cs        — Properties 1, 2, 3
    StatusEffectPropertyTests.cs       — Properties 16, 17, 18, 19, 19a, 20, 36
    EnemyCombatPropertyTests.cs        — Properties 14, 15, 21
    TurnPhasePropertyTests.cs          — Property 4
  Persistence/
    RunStatePropertyTests.cs           — Properties 22, 23, 24, 41
  Economy/
    ShopPropertyTests.cs               — Properties 27, 28, 29, 37, 38, 39
    ToolModifierPropertyTests.cs       — Property 30
  Procedural/
    FloorGenerationPropertyTests.cs    — Properties 25, 26, 33
  Exploration/
    WaterCoolerPropertyTests.cs        — Property 40
Assets/Tests/PlayMode/
  BattleIntegrationTests.cs            — (optional)
  SceneTransitionTests.cs              — (optional)
```

### Custom FsCheck Generators Needed

- `CardData` generator: valid cards with random type (0–10 cost), effectValue (1–20), rarity, target mode, type-specific fields populated
- `DeckState` generator: random partition of N cards (8–30) across draw/hand/discard
- `OvertimeState` generator: random current/max OT (0–10 / 10) and overflow (0–50)
- `EnemyCombatant` generator: random HP (1–100), attack patterns (1–6 actions), optional status effects
- `StatusEffect` generator: Burn/Stun/Bleed with random duration (1–5) and value (1–10)
- `RunState` generator: valid run states with random floor (1–25), HP, deck, hours, tools
- `WaterCooler` generator: random player HP (1–80), maxHP (80), used/unused flag
- `ShopState` generator: random inventory, player hours (0–500), removal count (0–10)

---

## Appendix: Full Requirements Reference (43 Requirements)

1. **Turn Structure** — Draw→Play→Discard→Enemy cycle, End Turn button, player acts first
2. **Overtime Meter** — Capacity 10, regen 2/turn from turn 2, damage→OT gain (floor(hpLost/maxHP*10)), status tick caps at 1 OT, overflow routing
3. **Overflow and Rage Burst** — Overflow buffer stores excess, consumed on Attack only, diminishing returns formula, All_Enemies gets full boosted damage
4. **Card Data Model** — 5 types, 4 rarities, 4 target modes, type-specific fields (parryMatchTags, statusEffectId, utilityEffectType, specialCardId), free targeting any entity
5. **Card Play and Effect Resolution** — Attack deals effectValue, Defense is proactive parry (costs OT), Effect applies status, card→discard after resolve, CardPlayedEvent
6. **Parry System** — Parry_Window during enemy attack animation slowdown, matching Defense cards highlighted, drag to cancel damage (free), missed = full damage, proactive parry costs OT, separate window per enemy attack, IntentColor (White/Yellow/Red/Unparryable), configurable duration scaling, enemy parry chance on player attacks
7. **Deck Cycling** — Reshuffle discard→draw when draw empty, Fisher-Yates, card count conservation, empty+empty = skip draw
8. **Multi-Enemy Encounters** — 1–4 enemies, pre-defined groups or roaming 1v1, simultaneous display, Single_Enemy requires target selection, All_Enemies hits all, dead removed, all dead = victory
9. **Enemy Turn Execution** — Sequential with visible delay, attack dash + hit shake, DamageEvent, player HP 0 = immediate defeat
10. **Status Effects** — Burn (damage at turn start), Stun (skip enemy action / skip player Play_Phase), Bleed (bonus damage on ALL damage sources including Burn), refresh no-stack, duration tick, clear on encounter end, StatusEffectEvents, icon stack behind entities
11. **Battle Transition** — Enemy trigger → load battle scene with EncounterData, victory → return to exploration (enemy removed), defeat → run reset
12. **Card Targeting UI** — Hover preview with effective values (source-side only), Single_Enemy highlight + click, Self/No_Target immediate, All_Enemies highlight + confirm, right-click/Escape cancel, locked outside Play_Phase
13. **HP and Resource UI** — PlayerHPStack, OT meter (dots + "total/max" text), overflow when >0, EnemyHPBar per enemy, draw/discard pile counts (clickable to inspect), turn number
14. **Starting Deck** — 8-card pre-built sets, carousel UI, max deck size 25 (Filing Cabinet upgrade), reject additions over limit
15. **Tool Integration** — Query tools at encounter start, modify OT regen/parry window/hand size/damage, additive stacking, reflect in UI
16. **Encounter Victory Rewards** — Hours = sum of enemy hoursReward, Bad_Reviews for bosses, NO card rewards, victory splash with randomized verb
17. **Card Animation** — Staggered draw entrance, exit toward battlefield, rejection shake, attack dash + hit shake, death animation, screen shake >20% HP, Rage Burst pulse, overflow glow
17a. **Floating Combat Text** — Damage numbers, "PARRY" text, OT cost numbers, status effect labels, heal numbers (green), office-supply pixel art style
18. **First-Person Hands** — Two 2D pixel art hand sprites on camera, idle bob synced to movement, sprint pump, interaction reach, idle breathing, hide before battle
19. **Utility and Special Cards** — Draw (draw N cards), Restore (add N OT, excess to overflow), Retrieve (N random from discard to hand), Reorder (view/rearrange top N of draw), Heal (restore HP capped at max), Special (registry lookup), card→discard after resolve
20. **Enemy Intent Display** — Intent icon above each enemy, damage number for attacks ("EMAILS +N"), IntentColor for parry difficulty, real-time updates during Play_Phase, update after Enemy_Phase, hide on death
21. **Work Box System** — Under desks only, size by floor spawn rates, card count by size, rarity by floor tables, shake→open→grey tiles→rarity reveal sequence (grey→yellow→red→black with animations)→full card reveal→Keep/Leave, revisit skips reveal, walk-away persistence
22. **Bathroom Shop** — Cards (3–5) + Tools (0–2) with Hours prices, rarity-based pricing, toilet card removal (escalating cost 25+10r, one per visit, min deck 1), boss floor guarantees 1 Tool, fixed inventory per floor
23. **Break Room Trades** — Every 2 floors, card-for-card/item-for-item, no currency, equal or unfavorable to player, accept/decline
24. **Run State and Persistence** — Death wipes run (deck, Hours, Tools), preserves Bad_Reviews + upgrades, new run = 0 Hours, no Tools, choose new deck
25. **Boss Encounters** — Every 3 floors, pre/post-fight dialogue, corporate tone floors 1–9, unsettling tone floors 12+, Bad_Reviews reward
26. **Enemy Attack Patterns** — Ordered sequence cycling with modulo, Defend/Buff/Special actions, conditional patterns (HP thresholds), coworker early / creature deep, more complex patterns on deeper floors
27. **Cutscene Persistence** — Reset flags on new run, mark seen on play, skip on load, persist in save, mid-combat quit saves pre-encounter snapshot
28. **Hub Office Upgrades** — 2D diorama cursor-only, hover→show costs, click→purchase, Computer/Coffee Machine/Desk Chair/Filing Cabinet/Plant/Whiteboard upgrades, visual furniture updates, apply next run
29. **Opening Dialogue** — YES = joke ending (credits), NO = stand up → deck selection → Hub Office → run start, skip on saved load
30. **Death Screen** — "Dragged back to desk", office horror tone, wipe run, preserve meta, proceed to Game Over
31. **Win Cinematic** — Door spawns after final boss (floor 75), quiet cinematic (walk out, arrive home, see family), no dialogue, return to Main Menu
32. **Suicidal Worker Encounter** — Floor 5 only, non-hostile NPC, uses normal deck/hand/OT, worker HP 10–15, self-damage per turn, Shield resolution (Defense card parry), Empathy resolution (player self-damage), failure (worker dies), different Tool rewards per path
33. **Procedural Floor Generation** — Constrained procedural, Work_Boxes under desks, bathrooms every floor, break rooms every 2 floors, boss rooms every 3 floors, worker encounter floor 5, enemy type weighting by depth, room types (Office/Bathroom/Break/Boss)
34. **Battle Scene Background** — 3D exploration visible behind 2D battle UI, blurred/dimmed, from pre-battle camera position
35. **Main Menu** — New Game / Continue / Settings / Achievements / Quit, first screen on launch
36. **Game Over Screen** — Floor reached, enemies defeated, Hours earned, Bad_Reviews earned, New Run / Main Menu buttons
37. **Enemy Exploration Behavior** — Roam with patrol, no entering bathrooms/break rooms, chase on proximity (aggressive), stop at safe room door (5s timeout), only catching enemy enters encounter
38. **Floor Progression** — Exit on each floor, non-boss floors exit anytime, boss floors locked until boss defeated, boss intercepts on leave attempt, can explore after boss defeat
39. **Pause Menu** — Escape pauses, Resume/Hub Office/Settings/View Deck/View Tools/Quit to Main Menu, freeze all gameplay
40. **First-Run Tutorial** — First run only, coworker NPC, hub orientation as mundane walkthrough, no combat explanation in hub, surprise reaction at first enemy, confused UI prompts, Work_Box discovery reaction, farewell after first combat, non-blocking tooltips, persist tutorialCompleted
41. **Floor Minimap** — Whiteboard upgrade, Level 1 = visited rooms, Level 2 = room type icons, Level 3 = full layout, no minimap without upgrade
42. **Water Cooler** — Every 2 floors (elevator transition), 35% max HP heal (rounded down), one-time use, show heal amount before confirm, Plant upgrade heals first then water cooler (independent, stackable)
43. **Bathroom Shop Inventory** — 3–5 cards + 0–2 Tools, same rarity tables as Work Boxes, rarity-based pricing, escalating toilet cost, boss floor guarantees 1 Tool, inventory fixed per floor
