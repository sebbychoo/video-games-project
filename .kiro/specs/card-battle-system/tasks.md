# Implementation Plan: Card Battle System

## Overview

This plan extends the existing `CardBattle` namespace in the Unity project to implement the full GDD-defined combat system. Tasks are ordered from foundational data models and core systems through dependent features, wiring, and polish. Existing files (BattleManager, CardData, DeckManager, HandManager, EnemyAction, TurnState, Health, BattleEventBus, etc.) are modified in-place rather than recreated. Property-based tests use FsCheck/NUnit in Unity Test Framework.

## Tasks

- [x] 1. Foundational data models and enums
  - [x] 1.1 Update CardData ScriptableObject and enums
    - Rename `energyCost` to `overtimeCost` in `Assets/Scripts/Battle/CardData.cs`
    - Replace `CardType` enum values (Attack/Skill/Power → Attack/Defense/Effect/Utility/Special)
    - Add `CardRarity` enum (Common, Rare, Legendary, Unknown)
    - Add `TargetMode` enum (SingleEnemy, AllEnemies, Self, NoTarget)
    - Add `UtilityEffectType` enum (None, Draw, Restore, Retrieve, Reorder, Heal)
    - Add new fields: `cardRarity`, `effectValue`, `parryMatchTags` (List<string>), `targetMode`, `cardSprite`, `statusEffectId`, `statusDuration`, `specialCardId`, `utilityEffectType`, `description`
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.6, 4.7, 4.8_

  - [x] 1.2 Update EnemyAction struct and add EnemyCombatantData ScriptableObject
    - Extend `EnemyActionType` enum in `Assets/Scripts/Battle/EnemyAction.cs` with Defend, Buff, Special
    - Add `EnemyBuffType` enum (None, DamageUp, DamageShield, Regen)
    - Add `EnemyActionCondition` enum (None, HPBelow50, HPBelow25, PlayerLowHP)
    - Add `buffType`, `buffDuration`, `condition` fields to `EnemyAction` struct
    - Add `IntentColor` enum (White, Yellow, Red, Unparryable) and `intentColor`, `parryMatchTags` fields to `EnemyAction` struct
    - Create `EnemyCombatantData` ScriptableObject with enemyName, maxHP, hoursReward, variant, sprite, attackPattern, isBoss, enemyParryChance, baseParryWindowDuration, dialogue fields
    - Add `EnemyVariant` enum (Coworker, Creature, Boss)
    - _Requirements: 26.1, 26.3, 26.4, 26.5, 25.7_

  - [x] 1.3 Create EncounterData, StatusEffectInstance, GameConfig, RunState, and MetaState data models
    - Create `EncounterData` ScriptableObject with enemies list (1–4), isBossEncounter, badReviewsReward
    - Create `StatusEffectInstance` struct with effectId, duration, value
    - Create `GameConfig` ScriptableObject with all tunable constants (baseHandSize, overtimeMaxCapacity, overtimeRegenPerTurn, finalFloor, bossFloorInterval, etc.)
    - Create `RunState` serializable class with floor, HP, deck, hours, tools, cutscene flags, etc.
    - Create `MetaState` serializable class with badReviews, hubUpgradeLevels, achievements, tutorialCompleted
    - _Requirements: 8.1, 10.1, 24.1, 24.2, 24.3, 27.5_

  - [x] 1.4 Create ToolData, HubUpgradeData, StartingDeckSet, and WorkBox data models
    - Create `ToolData` ScriptableObject with toolName, description, sprite, rarity, modifiers list
    - Create `ToolModifier` struct and `ToolModifierType` enum
    - Create `HubUpgradeData` ScriptableObject with upgradeId, displayName, maxLevel, costPerLevel, effectsPerLevel
    - Create `StartingDeckSet` ScriptableObject with setName, description, cards list (8)
    - Create `WorkBoxData` enums and structs (WorkBoxSize, WorkBoxSpawnRates)
    - _Requirements: 15.1, 28.3, 14.1, 21.2, 21.3_

  - [x] 1.5 Expand TurnState enum to TurnPhase
    - Replace `TurnState` enum in `Assets/Scripts/Battle/TurnState.cs` with `TurnPhase` (Draw, Play, Discard, Enemy)
    - Update all references from `TurnState` to `TurnPhase` across the codebase, including:
      - `BattleManager.cs`: `CurrentTurn` property and all `TurnState.PlayerTurn`/`EnemyTurn`/`BattleOver` checks
      - `CardInteractionHandler.cs`: `BattleManager.Instance.CurrentTurn != TurnState.PlayerTurn` guard checks
      - `CardTargetingManager.cs`: `BattleManager.Instance.CurrentTurn != TurnState.PlayerTurn` guard in `PlayOnTarget()`
    - _Requirements: 1.1, 1.2, 1.5, 1.6_

  - [x] 1.6 Extend BattleEventBus with new event types
    - Add `StatusEffectEvent` (applied/removed), `OverflowEvent`, `ParryEvent`, `TurnPhaseChangedEvent`, `RageBurstEvent` to `Assets/Scripts/Battle/BattleEventBus.cs`
    - Keep existing `CardPlayedEvent` and `DamageEvent`
    - _Requirements: 5.6, 9.4, 10.8, 10.9_

- [x] 2. Checkpoint — Ensure all data models compile and existing tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 3. Core battle subsystems — OvertimeMeter, OverflowBuffer, ParrySystem
  - [x] 3.1 Implement OvertimeMeter component
    - Create `Assets/Scripts/Battle/OvertimeMeter.cs` with `Current`, `Max`, `Spend(int)`, `Regenerate()`, `GainFromDamage(int hpLost, int maxHP)` methods
    - Initialize at full capacity (10) at encounter start
    - Regenerate 2/turn from turn 2 onward, capped at max
    - Route overflow to OverflowBuffer via event or direct reference
    - Support Tool modifier adjustments to regen value
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 15.2_

  - [x] 3.2 Write property tests for OvertimeMeter (Properties 5, 6, 7, 35)
    - Create `Assets/Tests/EditMode/Battle/OvertimeMeterPropertyTests.cs`
    - **Property 5: Overtime Spend Correctness** — For any meter value and card cost, spend succeeds iff cost <= current, and meter decreases by cost
    - **Validates: Requirements 2.3, 2.4**
    - **Property 6: Overtime Regeneration Capped** — Regen sets meter to min(v + 2, max)
    - **Validates: Requirements 2.2**
    - **Property 7: Damage-to-Overtime Gain with Overflow Routing** — Gain = floor(hpLost/maxHP * 10), capped at max, excess to overflow
    - **Validates: Requirements 2.5, 2.6**
    - **Property 35: Status Effect OT Gain Capped at 1 Per Tick** — Status effect damage grants at most 1 OT
    - **Validates: Requirements 2.6**

  - [x] 3.3 Implement OverflowBuffer component
    - Create `Assets/Scripts/Battle/OverflowBuffer.cs` with `Current`, `Add(int)`, `ConsumeAll() → int` methods
    - Initialize to 0 at encounter start
    - Clamp to reasonable max (999)
    - _Requirements: 3.1, 3.4_

  - [x] 3.4 Implement Rage Burst formula and consumption logic
    - Add `RageBurstCalculator` static class or method with piecewise linear interpolation between reference points (1→20%, 5→80%, 10→120%, 20→140%), clamped at 140%
    - Consume all overflow on Attack card play only; no consumption for other card types
    - _Requirements: 3.2, 3.3, 3.5, 3.6_

  - [x] 3.5 Write property tests for Rage Burst (Properties 8, 9)
    - Create `Assets/Tests/EditMode/Battle/RageBurstPropertyTests.cs`
    - **Property 8: Rage Burst Formula** — Piecewise linear interpolation produces exact values at reference points, clamped at 140%
    - **Validates: Requirements 3.3**
    - **Property 9: Rage Burst Consumption on Attack Only** — Overflow consumed only for Attack cards, unchanged for other types
    - **Validates: Requirements 3.2, 3.4, 3.5**

  - [x] 3.6 Implement ParrySystem component
    - Create `Assets/Scripts/Battle/ParrySystem.cs` with `StartParryWindow(EnemyAction, EnemyCombatant)`, `TryParry(CardInstance) → bool`, `IsParryWindowActive`, `GetMatchingCards(Hand) → List<CardInstance>` methods
    - During Enemy_Phase, when an enemy attacks: play attack animation, slow down near end, open Parry_Window
    - Highlight matching Defense cards in the player's Hand based on Parry_Match tags and Intent_Color
    - If player drags matching Defense card onto character during window: cancel damage, move card to Discard_Pile at no OT cost
    - If window expires without parry: deal full damage to player
    - Support configurable Parry_Window duration per EnemyCombatant, scaling with difficulty/floor
    - Support separate Parry_Window per individual enemy attack in multi-enemy encounters
    - Skip Parry_Window for attacks with IntentColor.Unparryable
    - Evaluate Enemy_Parry_Chance when player plays Attack cards (enemy parries cancel player attack damage)
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7, 6.8, 6.9, 6.10, 6.11, 6.12_

  - [x] 3.7 Write property tests for ParrySystem (Properties 10, 11, 12)
    - Create `Assets/Tests/EditMode/Battle/ParrySystemPropertyTests.cs`
    - **Property 10: Parry Cancels Damage During Parry Window** — Matching Defense card during Parry_Window cancels damage, card goes to discard at no OT cost
    - **Validates: Requirements 6.1, 6.2, 6.3, 9.2**
    - **Property 11: Missed Parry Deals Full Damage** — Expired Parry_Window without matching card deals full attack damage
    - **Validates: Requirements 6.4**
    - **Property 12: Proactive Defense Card Costs Overtime** — Defense card played during Play_Phase deducts OT cost, card goes to discard
    - **Validates: Requirements 5.3, 6.5**

- [x] 4. Status effect system
  - [x] 4.1 Implement StatusEffectSystem component
    - Create `Assets/Scripts/Battle/StatusEffectSystem.cs` with `Apply(target, StatusEffectInstance)`, `Tick(target)`, `GetEffects(target)`, `ClearAll(target)` methods
    - Maintain active effects list per target (player + each enemy)
    - Refresh duration on re-application (no stacking)
    - Decrement durations at turn end, remove at 0
    - Burn: deal damage at start of target's turn
    - Stun: skip enemy action; skip player Play_Phase
    - Bleed: add bonus damage on every damage instance (including Burn ticks)
    - Raise StatusEffectEvent on apply and remove via BattleEventBus
    - Clear all player effects on encounter end
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5, 10.6, 10.7, 10.8, 10.9, 10.11_

  - [x] 4.2 Write property tests for StatusEffectSystem (Properties 16, 17, 18, 19, 19a, 20, 36)
    - Create `Assets/Tests/EditMode/Battle/StatusEffectPropertyTests.cs`
    - **Property 16: Status Effect Duration Tick and Removal** — Each duration decremented by 1, removed at 0
    - **Validates: Requirements 10.3**
    - **Property 17: Status Effect Refresh (No Stacking)** — Re-applying same type refreshes duration, no duplicate
    - **Validates: Requirements 10.2**
    - **Property 18: Burn Deals Damage at Turn Start** — Burn value dealt at start of target's turn
    - **Validates: Requirements 10.4**
    - **Property 19: Stun Skips Enemy Action** — Stunned enemy action skipped in Enemy_Phase
    - **Validates: Requirements 10.5**
    - **Property 19a: Stun Skips Player Play Phase** — Stunned player skips Play_Phase (Draw → Discard → Enemy)
    - **Validates: Requirements 10.7**
    - **Property 20: Bleed Amplifies Incoming Damage** — Damage to target with Bleed = d + bleedValue
    - **Validates: Requirements 10.6**
    - **Property 36: Bleed Amplifies All Damage Sources Including Burn** — Bleed bonus applies to Burn ticks too
    - **Validates: Requirements 10.6**

- [x] 5. Turn phase controller and deck management
  - [x] 5.1 Implement TurnPhaseController component
    - Create `Assets/Scripts/Battle/TurnPhaseController.cs` with `CurrentPhase`, `AdvancePhase()`, `OnPhaseChanged` event
    - Enforce strict Draw → Play → Discard → Enemy cycle
    - Player always acts first on turn 1
    - Skip Play_Phase when player is stunned
    - _Requirements: 1.1, 1.2, 1.3, 1.5, 1.6, 1.7, 1.8, 10.7_

  - [x] 5.2 Write property test for TurnPhaseController (Property 4)
    - Create `Assets/Tests/EditMode/Battle/TurnPhasePropertyTests.cs`
    - **Property 4: Turn Phase Ordering** — Phase cycles Draw → Play → Discard → Enemy, first phase is Draw, Stun skips Play
    - **Validates: Requirements 1.2, 1.5, 1.6, 1.7, 10.7**

  - [x] 5.3 Update DeckManager for reshuffle and card conservation
    - Modify `Assets/Scripts/Battle/DeckManager.cs` to support draw-with-reshuffle (shuffle discard into draw when draw is empty)
    - Ensure Fisher-Yates shuffle is used
    - Add `AddCard(CardData)` method for mid-run deck additions
    - Ensure total card count across draw, hand, discard is preserved
    - Handle empty draw + empty discard gracefully (skip draw)
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 14.4_

  - [x] 5.4 Write property tests for DeckManager (Properties 1, 2, 3)
    - Create `Assets/Tests/EditMode/Battle/DeckManagerPropertyTests.cs`
    - **Property 1: Card Count Conservation** — Total cards across draw, hand, discard equals deck size at encounter start
    - **Validates: Requirements 7.4, 5.5, 19.7**
    - **Property 2: Deck Cycle Round Trip** — Shuffling discard into draw produces same card set, discard empty
    - **Validates: Requirements 7.1, 7.3**
    - **Property 3: Draw Phase Hand Size** — After draw, hand = min(baseHandSize, D + S)
    - **Validates: Requirements 1.1, 7.1, 7.2**

- [x] 6. Checkpoint — Ensure core subsystems compile and property tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 7. Card effect resolution and enemy combat
  - [x] 7.1 Implement CardEffectResolver and ISpecialCardEffect
    - Create `Assets/Scripts/Battle/CardEffectResolver.cs` that switches on CardType and dispatches to type-specific handlers
    - Attack: deal effectValue damage (single or all enemies), apply Rage Burst bonus if overflow > 0
    - Defense: move card to Discard_Pile, deduct OT cost (proactive parry during Play_Phase — card is prepared but may not match next enemy attack)
    - Effect: apply StatusEffect to target
    - Utility: dispatch by UtilityEffectType (Draw, Restore, Retrieve, Reorder, Heal)
    - Special: lookup in SpecialCardRegistry and execute
    - Move card from hand to discard after resolution
    - Raise CardPlayedEvent on BattleEventBus
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 4.5, 19.1, 19.2, 19.3, 19.4, 19.5, 19.6, 19.7, 19.8_

  - [x] 7.2 Write property tests for card resolution (Properties 13, 31, 32, 34)
    - Create `Assets/Tests/EditMode/Battle/CardResolutionPropertyTests.cs`
    - **Property 13: Attack Card Deals Correct Damage** — Single target takes effectValue; All_Enemies each take effectValue
    - **Validates: Requirements 5.1, 5.2, 8.4**
    - **Property 31: Utility Draw Card Effect** — Draw min(N, availableCards) additional cards
    - **Validates: Requirements 19.1**
    - **Property 32: Utility Restore Overtime Effect** — Meter set to min(v+N, m), excess to overflow
    - **Validates: Requirements 19.2, 2.6**
    - **Property 34: Heal Utility Capped at Max HP** — HP = min(hp + H, maxHP)
    - **Validates: Requirements 19.5**

  - [x] 7.3 Implement EnemyCombatant component
    - Create `Assets/Scripts/Battle/EnemyCombatant.cs` MonoBehaviour wrapping EnemyCombatantData
    - Track HP (reuse Health internally), attack pattern index, status effects, intent, enemyParryChance
    - `ExecuteAction()`: execute current pattern action, advance index (cycle with modulo)
    - `TakeDamage(int)`: evaluate Enemy_Parry_Chance, if parry fails apply HP reduction, apply Bleed bonus
    - `IsAlive` property, death handling
    - Support conditional action patterns (HP thresholds, player low HP)
    - _Requirements: 8.1, 8.6, 8.7, 8.8, 26.1, 26.2, 26.3, 26.4, 26.5_

  - [x] 7.4 Write property tests for enemy combat (Properties 14, 15, 21)
    - Create `Assets/Tests/EditMode/Battle/EnemyCombatPropertyTests.cs`
    - **Property 14: Dead Enemies Are Removed and Skipped** — Dead enemies excluded from Enemy_Phase, all dead = victory
    - **Validates: Requirements 8.5, 8.6, 8.7**
    - **Property 15: Player Defeat at Zero HP** — Player HP 0 → immediate defeat
    - **Validates: Requirements 9.5**
    - **Property 21: Enemy Attack Pattern Cycling** — Action at turn t = pattern[t % N]
    - **Validates: Requirements 26.2**

  - [x] 7.5 Implement SpecialCardRegistry
    - Create `Assets/Scripts/Battle/SpecialCardRegistry.cs` with `Register(string id, ISpecialCardEffect)`, `Execute(string id, CardEffectContext)`
    - Create `ISpecialCardEffect` interface and `CardEffectContext` struct
    - _Requirements: 19.6, 19.7_

- [x] 8. BattleManager orchestration and turn loop
  - [x] 8.1 Refactor BattleManager to orchestrate subsystems
    - Modify `Assets/Scripts/Battle/BattleManager.cs` to delegate to TurnPhaseController, OvertimeMeter, OverflowBuffer, ParrySystem, StatusEffectSystem, CardEffectResolver, DeckManager, HandManager
    - `StartEncounter(EncounterData)`: initialize all subsystems, spawn EnemyCombatants, set OT to full, overflow to 0, determine enemy intents
    - `TryPlayCard(CardInstance, GameObject)`: validate OT cost, resolve card effect, handle Rage Burst on Attack, move card to discard
    - `EndTurn()`: discard remaining hand, execute enemy phase sequentially with delays (each enemy attack triggers Parry_Window), tick status effects, advance turn, regen OT (turn 2+), draw new hand
    - Handle player stun (skip Play_Phase)
    - Handle enemy stun (skip action), enemy death (remove from encounter)
    - Victory condition: all enemies dead; Defeat condition: player HP 0
    - Query Tool inventory at encounter start for passive modifiers
    - _Requirements: 1.1–1.8, 2.1–2.4, 3.1–3.6, 5.1–5.6, 6.1–6.12, 8.1–8.8, 9.1–9.5, 10.1–10.11, 15.1–15.5_

  - [x] 8.2 Implement enemy turn execution with sequential actions and animations
    - Execute each living enemy's action in sequence with visible delay
    - Enemy attack: dash animation toward player, slow down near end for Parry_Window, if not parried: hit shake + reduce player HP, raise DamageEvent
    - Enemy defend: gain temporary damage reduction or defensive buff
    - Enemy buff: apply temporary modifier
    - Enemy status: apply StatusEffect to player
    - Check player defeat after each enemy action
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5_

  - [x] 8.3 Implement enemy intent display
    - Create `Assets/Scripts/Battle/EnemyIntentDisplay.cs` UI component above each enemy sprite
    - Show intent icon (attack/defend/buff/special) and damage number for attack intents
    - Update intent at encounter start, after each enemy phase, and in real-time during Play_Phase when conditions change
    - Hide intent on enemy death
    - _Requirements: 20.1, 20.2, 20.3, 20.4, 20.5_

- [x] 9. Checkpoint — Ensure battle loop works end-to-end, all property tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 10. Card targeting, selection UI, and card animations
  - [x] 10.1 Update CardTargetingManager for multi-enemy targeting and free targeting
    - Modify `Assets/Scripts/Battle/CardTargetingManager.cs` to support:
    - Single_Enemy: highlight valid targets (enemies + player + allies), wait for click
    - All_Enemies: highlight all enemies, play on confirmation
    - Self/No_Target: play immediately without target selection
    - Right-click or Escape cancels selection
    - Prevent interactions when not in Play_Phase
    - Support targeting any entity including Jean-Guy and allied NPCs (Req 4.5)
    - _Requirements: 12.1, 12.3, 12.4, 12.5, 12.6, 12.7, 4.5_

  - [x] 10.2 Implement CardEffectPreview tooltip on hover
    - Create `Assets/Scripts/Battle/UI/CardEffectPreview.cs` tooltip component
    - Show calculated effective values on card hover (damage after Rage Burst + Tool modifiers, parry match tags for Defense cards, draw count, etc.)
    - Reflect source-side modifiers only (no target-specific effects like Bleed)
    - _Requirements: 12.2_

  - [x] 10.3 Update CardAnimator with rejection shake and new animations
    - Modify `Assets/Scripts/Battle/CardAnimator.cs` to add:
    - Rejection shake animation (horizontal shake when OT insufficient)
    - Staggered entrance animation during Draw_Phase
    - Exit animation toward battlefield on card play
    - _Requirements: 17.1, 17.2, 17.3_

  - [x] 10.4 Extend BattleAnimations with attack dash, death, screen shake, Rage Burst effects
    - Modify `Assets/Scripts/Battle/BattleAnimations.cs` to add:
    - Attack dash from attacker toward target + hit shake on target
    - Enemy death animation (backward tip + downward sink)
    - Screen shake proportional to damage when > 20% max HP
    - Screen pulse/flash on Rage Burst activation
    - Subtle screen edge glow while Overflow > 0
    - _Requirements: 17.4, 17.5, 17.6, 17.7, 17.8_

  - [x] 10.5 Implement FloatingCombatText system
    - Create `Assets/Scripts/Battle/UI/FloatingCombatText.cs`
    - Floating damage numbers (drift up, fade out) at target position
    - Floating parry text ("PARRY") near player character on successful parry
    - Floating OT cost numbers (drift down, fade out) near OT meter
    - Floating status effect name labels at target position
    - Floating heal numbers in green at target position
    - Office-supply pixel art aesthetic style
    - _Requirements: 17a.1, 17a.2, 17a.3, 17a.4, 17a.5, 17a.6_

  - [x] 10.6 Implement StatusEffectIconStack display
    - Create `Assets/Scripts/Battle/UI/StatusEffectIconStack.cs`
    - Vertical stack of status effect icons behind each affected entity's sprite
    - Each icon shows remaining duration number
    - Subscribe to StatusEffectEvents on BattleEventBus for updates
    - _Requirements: 10.10_

- [x] 11. Battle UI components
  - [x] 11.1 Implement OvertimeMeterUI display
    - Create `Assets/Scripts/Battle/UI/OvertimeMeterUI.cs`
    - Display current/max OT value
    - Display Overflow_Buffer value when > 0
    - Subscribe to OvertimeMeter and OverflowBuffer changes
    - _Requirements: 13.2, 13.3_

  - [x] 11.2 Implement ParryWindowUI display
    - Create `Assets/Scripts/Battle/UI/ParryWindowUI.cs`
    - Show active parry window timer during Enemy_Phase attacks
    - Highlight matching Defense cards in the player's hand during Parry_Window
    - Display intent color (White/Yellow/Red) for current attack's parry difficulty
    - Hide when no Parry_Window is active
    - _Requirements: 6.1, 6.2, 6.8_

  - [x] 11.3 Implement DeckCounterUI with pile inspection
    - Create `Assets/Scripts/Battle/UI/DeckCounterUI.cs`
    - Display draw pile count and discard pile count
    - Click draw pile counter → scrollable alphabetical list of draw pile cards (no order reveal)
    - Click discard pile counter → scrollable list of discard pile cards
    - _Requirements: 13.6, 13.8, 13.9_

  - [x] 11.4 Implement EndTurnButton
    - Create `Assets/Scripts/Battle/UI/EndTurnButton.cs`
    - Enabled only during Play_Phase, disabled during all other phases
    - Calls BattleManager.EndTurn() on click
    - _Requirements: 1.3, 1.4_

  - [x] 11.5 Implement TurnCounterUI
    - Create `Assets/Scripts/Battle/UI/TurnCounterUI.cs`
    - Display current turn number at top-center of battle screen
    - _Requirements: 13.7_

  - [x] 11.6 Update PlayerHPStack and EnemyHPBar
    - Modify existing `PlayerHPStack` to display current/max HP from BattleManager
    - Modify existing `EnemyHPBar` to work with EnemyCombatant component, support multiple enemies
    - _Requirements: 13.1, 13.5_

- [x] 12. Checkpoint — Ensure battle UI is functional and all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 13. Persistence — SaveManager, RunState, MetaState
  - [ ] 13.1 Implement SaveManager with JSON serialization
    - Create `Assets/Scripts/Core/SaveManager.cs` with `SaveRun()`, `LoadRun()`, `SaveMeta()`, `LoadMeta()`, `WipeRun()`, `SnapshotPreEncounter()`, `RestorePreEncounter()`
    - Serialize RunState and MetaState to JSON via Unity's JsonUtility or Newtonsoft
    - Handle corrupted/missing save files gracefully (start fresh, preserve meta if possible)
    - Persist cutscene-seen flags in RunState
    - Mid-combat quit saves pre-encounter snapshot
    - _Requirements: 27.1, 27.2, 27.3, 27.4, 27.5, 27.6, 27.7_

  - [ ]* 13.2 Write property tests for persistence (Properties 22, 23, 24, 41)
    - Create `Assets/Tests/EditMode/Persistence/RunStatePropertyTests.cs`
    - **Property 22: Death Wipes Run State, Preserves Meta State** — After death, run state wiped, Bad_Reviews and upgrades preserved
    - **Validates: Requirements 24.1, 24.2, 24.3**
    - **Property 23: Run State Persistence Round Trip** — Serialize then deserialize produces identical RunState
    - **Validates: Requirements 27.5, 27.6**
    - **Property 24: Cutscene Seen Flag Persistence** — Seen cutscene stays seen on load, new run clears flags
    - **Validates: Requirements 27.1, 27.2, 27.3, 27.4**
    - **Property 41: Mid-Combat Save Restores Pre-Encounter State** — Quit during encounter saves pre-combat state, resume in exploration
    - **Validates: Requirements 27.7**

- [ ] 14. Battle transition and encounter lifecycle
  - [ ] 14.1 Update SceneLoader for battle transitions with encounter data
    - Modify `Assets/Scripts/Core/SceneLoader.cs` to pass EncounterData when loading battle scene
    - On victory: return to exploration, remove defeated enemy, restore player position
    - On defeat: trigger run reset via SaveManager, proceed to death screen
    - Snapshot pre-encounter state before loading battle scene
    - _Requirements: 11.1, 11.2, 11.3, 11.4_

  - [ ] 14.2 Update Battlescene_Trigger for encounter data passing
    - Modify `Assets/Scripts/World/Battlescene_Trigger.cs` to determine EncounterData from the triggering enemy
    - Support single roaming enemies (1v1) and pre-defined multi-enemy groups
    - _Requirements: 8.2, 11.1_

  - [ ] 14.3 Implement battle scene background rendering
    - Keep 3D exploration environment visible as blurred/dimmed background behind 2D battle UI
    - Render background from pre-battle camera position
    - _Requirements: 34.1, 34.2, 34.3, 34.4_

  - [ ] 14.4 Implement VictoryScreen
    - Create `Assets/Scripts/Battle/UI/VictoryScreen.cs`
    - Display randomized victory verb + enemy name(s) + Hours earned (+ Bad_Reviews for bosses)
    - Dismiss on click or after short delay
    - _Requirements: 16.1, 16.2, 16.3, 16.4_

  - [ ] 14.5 Implement encounter victory rewards
    - Award Hours = sum of each defeated enemy's hoursReward
    - Award Bad_Reviews for boss encounters
    - No card rewards from encounters (cards from Work_Boxes, shops, trades only)
    - _Requirements: 16.1, 16.2, 16.3_

- [ ] 15. Starting deck selection and Tool integration
  - [ ] 15.1 Implement StartingDeckCarousel UI
    - Create `Assets/Scripts/Battle/UI/StartingDeckCarousel.cs`
    - Carousel view: deck set name at top, 8 cards displayed with full details, left/right arrows to browse sets
    - Select button at bottom-center initializes Draw_Pile with chosen 8 cards
    - _Requirements: 14.1, 14.2, 14.3_

  - [ ] 15.2 Implement Tool modifier system in BattleManager
    - Query active Tool inventory at encounter start
    - Apply OT regen modifiers, Parry_Window duration modifiers, hand size modifiers, damage bonus modifiers
    - Multiple tools with same modifier type stack additively
    - Reflect modified values in UI
    - _Requirements: 15.1, 15.2, 15.3, 15.4, 15.5_

  - [ ]* 15.3 Write property test for Tool modifiers (Property 30)
    - Create `Assets/Tests/EditMode/Economy/ToolModifierPropertyTests.cs`
    - **Property 30: Tool Modifier Application** — Effective value = baseValue + sum of tool modifiers of same type
    - **Validates: Requirements 15.2, 15.3, 15.4**

  - [ ] 15.4 Implement deck size limit enforcement
    - Enforce maximum deck size (default 25, increased by Filing Cabinet upgrade)
    - Reject card additions that exceed limit with feedback message
    - _Requirements: 14.4, 14.5_

- [ ] 16. Checkpoint — Ensure deck selection, tools, and persistence work, all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 17. Exploration systems — Work Box, Bathroom Shop, Break Room Trade
  - [ ] 17.1 Implement WorkBox interaction and rarity reveal
    - Create `Assets/Scripts/Exploration/WorkBox.cs`
    - Spawn under work desks only, size determined by floor-based spawn rates
    - Card count by size: Small [1,3], Big [3,5], Huge [5,7]
    - Card rarity by floor-based probability tables
    - Shake animation before opening
    - Rarity reveal sequence: grey → whitish yellow → medium red (with dust particles) → black with glowing aura (click-to-advance, animation per step)
    - Keep/Leave buttons after full reveal
    - Revisit: skip shake, show true rarity colors immediately
    - Walk-away support: unrevealed cards persist
    - _Requirements: 21.1, 21.2, 21.3, 21.4, 21.5, 21.6, 21.7, 21.8, 21.9, 21.10, 21.11, 21.12_

  - [ ]* 17.2 Write property test for Work Box card count (Property 26)
    - Create `Assets/Tests/EditMode/Procedural/FloorGenerationPropertyTests.cs`
    - **Property 26: Work Box Card Count by Size** — Small → [1,3], Big → [3,5], Huge → [5,7]
    - **Validates: Requirements 21.3**

  - [ ] 17.3 Implement BathroomShop
    - Create `Assets/Scripts/Exploration/BathroomShop.cs`
    - Display cards (3–5) and Tools (0–2) with Hours prices
    - Card pricing by rarity: Common 10, Rare 25, Legendary 100, Unknown 150
    - Tool pricing by rarity: Common 30, Rare 60, Legendary 200 (Unknown tools not available in shops)
    - Purchase: deduct Hours, add card/tool to inventory
    - Reject purchase if insufficient Hours
    - Toilet card removal: display deck, select card, deduct escalating cost (25 + 10 per previous removal)
    - One removal per bathroom visit
    - No removal if deck size ≤ 1
    - Boss floor shops guarantee at least 1 Tool
    - Inventory generated once per floor, fixed for duration
    - _Requirements: 22.1, 22.2, 22.3, 22.4, 22.5, 22.6, 22.7, 22.8, 22.9, 43.1, 43.2, 43.3, 43.4, 43.5, 43.6, 43.7_

  - [ ]* 17.4 Write property tests for shop and card removal (Properties 27, 28, 37, 38, 39)
    - Create `Assets/Tests/EditMode/Economy/ShopPropertyTests.cs`
    - **Property 27: Shop Purchase Deducts Currency and Adds Item** — Hours = h - c, item in inventory; rejected if c > h
    - **Validates: Requirements 22.3, 22.4**
    - **Property 28: Card Removal via Toilet** — Deck no longer contains card, size -1, Hours -c
    - **Validates: Requirements 22.5, 22.6**
    - **Property 37: Minimum Deck Size Enforced** — Removal rejected if deck ≤ 1
    - **Validates: Requirements 22.8**
    - **Property 38: One Toilet Flush Per Bathroom Visit** — Second removal rejected in same visit
    - **Validates: Requirements 22.7**
    - **Property 39: Escalating Card Removal Cost** — Cost = 25 + (r * 10) where r = previous removals
    - **Validates: Requirements 43.5**

  - [ ] 17.5 Implement BreakRoomTrade
    - Create `Assets/Scripts/Exploration/BreakRoomTrade.cs`
    - Display offered trade (item wanted vs item offered)
    - Only offer trades player can fulfill
    - Card-for-card and item-for-item trades, no currency cost
    - Trades equal or unfavorable to player
    - Accept: remove requested item, add offered item
    - Decline: close interface, no penalty
    - _Requirements: 23.1, 23.2, 23.3, 23.4, 23.5, 23.6, 23.7_

  - [ ]* 17.6 Write property test for trades (Property 29)
    - Append to `Assets/Tests/EditMode/Economy/ShopPropertyTests.cs`
    - **Property 29: Trade Conserves Inventory** — Accept: have B not A; Decline: inventory unchanged
    - **Validates: Requirements 23.5, 23.6**

- [ ] 18. Exploration — Enemy behavior, Water Cooler, First-Person Hands
  - [ ] 18.1 Update EnemyFollow for safe room avoidance and aggro behavior
    - Modify `Assets/Scripts/Enemy/EnemyFollow.cs` to:
    - Prevent entering bathroom and break rooms during roaming
    - Stop at doorway when chasing player into safe room, give up after ~5 seconds
    - Support aggressive (chase on proximity) and passive (no chase) variants
    - Only the catching enemy enters encounter; others resume patrol
    - _Requirements: 37.1, 37.2, 37.3, 37.4, 37.6, 37.7_

  - [ ] 18.2 Implement WaterCooler rest stop
    - Create `Assets/Scripts/Exploration/WaterCooler.cs`
    - Restore 35% of max HP (rounded down), one-time use
    - Display heal amount before confirmation
    - Appears every 2 floors as elevator transition prompt
    - Plant upgrade healing triggers first, water cooler afterward (independent, stackable)
    - _Requirements: 42.1, 42.2, 42.3, 42.4, 42.5_

  - [ ]* 18.3 Write property test for Water Cooler (Property 40)
    - Create `Assets/Tests/EditMode/Exploration/WaterCoolerPropertyTests.cs`
    - **Property 40: Water Cooler Heals Exactly 35% Max HP Once** — HP = min(hp + floor(maxHP * 0.35), maxHP), second use rejected
    - **Validates: Requirements 42.2, 42.3**

  - [ ] 18.4 Implement FirstPersonHandsController
    - Create `Assets/Scripts/Exploration/FirstPersonHandsController.cs`
    - Render two 2D pixel art hand sprites overlaid on camera (bottom-left, bottom-right)
    - Idle bob synced to movement speed
    - Sprint pumping animation
    - Reach-forward interaction animation
    - Idle breathing animation when not moving
    - Hide hands before battle scene transition
    - _Requirements: 18.1, 18.2, 18.3, 18.4, 18.5, 18.6, 18.7, 18.8_

  - [ ] 18.5 Implement safe room NPC contextual dialogue
    - When player first interacts with NPC in bathroom/break room while enemies are on floor, NPC delivers safety line
    - _Requirements: 37.5_

- [ ] 19. Checkpoint — Ensure exploration systems work, all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 20. Procedural floor generation and boss floor gating
  - [ ] 20.1 Extend LevelGenerator for full floor generation rules
    - Modify `Assets/Scripts/Procedural/LevelGenerator.cs` to:
    - Spawn Work_Boxes only under work desks
    - Place bathrooms on every floor (subset with shops)
    - Place break rooms every 2 floors (subset with trade NPCs)
    - Place boss encounter room every 3 floors as fixed anchor
    - Place Suicidal_Worker_Encounter on floor 5 exactly once per run
    - Weight enemy types by floor depth (coworker early, creature deep)
    - Assign more complex attack patterns on deeper floors
    - Support room types: Office, Bathroom, Break room, Boss room
    - Place floor exit (stairs/elevator) on each floor
    - Constrained procedural generation (not purely random)
    - _Requirements: 33.1, 33.2, 33.3, 33.4, 33.5, 33.6, 33.7, 33.8, 33.9, 38.1_

  - [ ]* 20.2 Write property tests for floor generation (Properties 25, 33)
    - Append to `Assets/Tests/EditMode/Procedural/FloorGenerationPropertyTests.cs`
    - **Property 25: Boss Floor Placement** — Boss rooms at floors 3, 6, 9, ..., none on non-multiples of 3
    - **Validates: Requirements 25.1, 33.6**
    - **Property 33: Boss Floor Blocks Exit Until Defeated** — Exit locked while boss alive, unlocked after defeat
    - **Validates: Requirements 38.3, 38.4, 38.5**

  - [ ] 20.3 Implement boss floor gating
    - On boss floors: lock exit until boss defeated, boss intercepts player if they try to leave
    - On non-boss floors: exit available at any time
    - Player may explore boss floor freely before engaging boss
    - Player may continue exploring after boss defeat
    - _Requirements: 38.2, 38.3, 38.4, 38.5_

  - [ ] 20.4 Implement floor minimap (FloorMinimap)
    - Create `Assets/Scripts/Exploration/FloorMinimap.cs`
    - Level 0: no minimap (no Whiteboard upgrade)
    - Level 1: show visited rooms only
    - Level 2: show room type icons in visited rooms
    - Level 3: reveal full floor layout including unvisited rooms
    - _Requirements: 41.1, 41.2, 41.3, 41.4, 41.5_

- [ ] 21. Hub Office and meta-progression
  - [ ] 21.1 Implement HubOffice scene
    - Create `Assets/Scripts/UI/HubOffice.cs`
    - 2D diorama-style scene with 3D depth, cursor-based interaction only (no WASD)
    - Hover furniture → show upgrade options and Bad_Reviews costs
    - Click furniture → purchase upgrade, deduct Bad_Reviews, apply to MetaState
    - Reject purchase if insufficient Bad_Reviews with feedback
    - Accessible from main menu and during run (upgrades apply next run)
    - Visually update furniture appearance per upgrade level
    - Support multiple upgrade levels per item with escalating costs
    - _Requirements: 28.1, 28.2, 28.3, 28.4, 28.5, 28.11, 28.12_

  - [ ] 21.2 Implement hub upgrade effects
    - Computer: +1 damage to Technology-themed cards per level
    - Coffee Machine: +OT regen per turn
    - Desk Chair: +parry window duration per level
    - Filing Cabinet: +hand size (early levels), +max deck size (later levels)
    - Plant: +base HP (early levels), +passive heal per floor (later levels, triggers on floor exit)
    - Whiteboard: unlock/upgrade floor minimap
    - All effects apply from next run onward
    - _Requirements: 28.6, 28.7, 28.8, 28.9, 28.10, 28.13_

- [ ] 22. Narrative events and special encounters
  - [ ] 22.1 Implement OpeningDialogue
    - Create `Assets/Scripts/Narrative/OpeningDialogue.cs`
    - Boss leans over cubicle, asks about overtime
    - YES: fade to black, "YOU WORKED OVERTIME", roll credits, Back to Menu button
    - NO: Jean-Guy stands up, proceed to deck selection, then Hub_Office, then run start
    - Skip on saved mid-run load
    - _Requirements: 29.1, 29.2, 29.3, 29.4, 29.5_

  - [ ] 22.2 Implement DeathScreen
    - Create `Assets/Scripts/Narrative/DeathScreen.cs`
    - "Dragged back to desk" sequence with office horror tone
    - Wipe run state, preserve Bad_Reviews and upgrades
    - Proceed to GameOverScreen
    - _Requirements: 30.1, 30.2, 30.3, 30.4, 30.5_

  - [ ] 22.3 Implement GameOverScreen
    - Create `Assets/Scripts/UI/GameOverScreen.cs`
    - Display floor reached, enemies defeated, Hours earned, Bad_Reviews earned
    - New Run button → opening dialogue
    - Main Menu button → main menu
    - _Requirements: 36.1, 36.2, 36.3, 36.4, 36.5_

  - [ ] 22.4 Implement WinCinematic
    - Create `Assets/Scripts/Narrative/WinCinematic.cs`
    - Spawn door in boss room after final boss defeat (floor configurable, default 75, multiple of 3)
    - Interact with door → quiet cinematic (Jean-Guy walks out, arrives home, sees family)
    - No dialogue, no fanfare
    - Return to Main Menu after cinematic
    - _Requirements: 31.1, 31.2, 31.3, 31.4, 31.5_

  - [ ] 22.5 Implement SuicidalWorkerEncounter
    - Create `Assets/Scripts/Narrative/SuicidalWorkerEncounter.cs`
    - Floor 5 only, exactly once per run
    - Non-hostile NPC, special turn-based encounter using player's normal deck/hand/OT
    - Worker has small HP pool (10–15), deals fixed self-damage each turn after player acts
    - Shield resolution: Defense card targeting worker during worker's self-harm Parry_Window → parry cancels self-damage → success → specific Tool reward
    - Empathy resolution: player self-damages before worker acts → worker walks away → success → different Tool reward
    - Failure: worker HP reaches 0 → no reward, narrative consequence
    - Handle with highest care (emotional core)
    - _Requirements: 32.1, 32.2, 32.3, 32.4, 32.5, 32.6, 32.7, 32.8, 32.9, 32.10, 32.11_

- [ ] 23. Menus and UI screens
  - [ ] 23.1 Implement MainMenu
    - Create `Assets/Scripts/UI/MainMenu.cs`
    - Options: New Game, Continue, Settings, Achievements, Quit
    - New Game → opening dialogue
    - Continue → load saved run (greyed out if no save)
    - Settings → audio, display, controls panel
    - Achievements → list with unlock status
    - Quit → close application
    - Accessible after death screen and win cinematic
    - _Requirements: 35.1, 35.2, 35.3, 35.4, 35.5, 35.6, 35.7, 35.8, 35.9_

  - [ ] 23.2 Update PauseMenu
    - Modify `Assets/Scripts/UI/Menu.cs` to support:
    - Escape key pauses during exploration and combat
    - Options: Resume, Hub Office, Settings, View Deck, View Tools, Quit to Main Menu
    - Freeze all gameplay while paused
    - View Deck: scrollable grid of all deck cards
    - View Tools: all collected Tools with name, sprite, description
    - Quit: save run state, return to main menu
    - _Requirements: 39.1, 39.2, 39.3, 39.4, 39.5, 39.6, 39.7, 39.8, 39.9_

  - [ ] 23.3 Implement boss encounter dialogue
    - Display pre-fight dialogue before boss combat starts
    - Display post-fight dialogue after boss victory
    - Floors 1–9: corporate business language
    - Floors 12+: unnatural, unsettling tone
    - _Requirements: 25.2, 25.3, 25.4, 25.5_

- [ ] 24. Tutorial system
  - [ ] 24.1 Implement TutorialNPC and first-run tutorial flow
    - Create `Assets/Scripts/Narrative/TutorialNPC.cs`
    - First run only (no previous runs in MetaState)
    - Hub_Office walkthrough: coworker explains furniture as mundane orientation
    - No combat explanation in hub (combat is a surprise)
    - First enemy encounter: NPC reacts with surprise, confused UI prompts
    - Work_Box discovery: NPC reacts as if finding it for first time
    - After first combat: NPC says farewell, disappears
    - Non-blocking contextual tooltips (dismissible, not forced dialogue)
    - Persist tutorial-completed flag in MetaState
    - _Requirements: 40.1, 40.2, 40.3, 40.4, 40.5, 40.6, 40.7, 40.8, 40.9, 40.10, 40.11_

- [ ] 25. Checkpoint — Ensure all narrative, menus, and tutorial work, all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 26. Integration wiring and final assembly
  - [ ] 26.1 Wire BattleManager to all subsystems and UI
    - Connect BattleManager → TurnPhaseController, OvertimeMeter, OverflowBuffer, ParrySystem, StatusEffectSystem, CardEffectResolver, DeckManager, HandManager, EnemyCombatant instances
    - Connect all UI components (OvertimeMeterUI, ParryWindowUI, DeckCounterUI, EndTurnButton, TurnCounterUI, FloatingCombatText, StatusEffectIconStack, CardEffectPreview, EnemyIntentDisplay, VictoryScreen) to BattleEventBus subscriptions
    - Connect SaveManager to SceneLoader for battle transitions
    - Connect RunState to BathroomShop, WorkBox, BreakRoomTrade for inventory/deck mutations
    - Connect MetaState to HubOffice for upgrade purchases
    - Connect Tool modifiers to BattleManager at encounter start
    - _Requirements: 1.1–1.8, 8.1–8.8, 11.1–11.4, 15.1–15.5_

  - [ ] 26.2 Wire exploration scene systems
    - Connect EnemyFollow to SceneLoader for encounter triggering
    - Connect FirstPersonHandsController to player movement and scene transitions
    - Connect WorkBox, BathroomShop, BreakRoomTrade to RunState for deck/inventory mutations
    - Connect WaterCooler to RunState for HP restoration
    - Connect FloorMinimap to MetaState for upgrade level
    - Connect LevelGenerator to floor progression and boss gating
    - _Requirements: 37.1–37.7, 38.1–38.5, 18.1–18.8_

  - [ ] 26.3 Wire menu and narrative flow
    - MainMenu → OpeningDialogue → StartingDeckCarousel → HubOffice → Floor 1
    - DeathScreen → GameOverScreen → MainMenu or New Run
    - WinCinematic → MainMenu
    - PauseMenu → HubOffice (mid-run), SaveManager (quit)
    - Continue → SaveManager.LoadRun() → resume at saved state
    - _Requirements: 29.1–29.5, 30.1–30.5, 31.1–31.5, 35.1–35.9, 36.1–36.5, 39.1–39.9_

  - [ ]* 26.4 Write integration tests for full battle flow
    - Create `Assets/Tests/PlayMode/BattleIntegrationTests.cs`
    - Test complete encounter: start → draw → play cards → end turn → enemy phase → victory/defeat
    - Test Rage Burst activation during multi-enemy encounter
    - Test status effect interactions (Burn + Bleed combo)
    - _Requirements: 1.1–1.8, 5.1–5.6, 8.1–8.8_

  - [ ]* 26.5 Write integration tests for scene transitions
    - Create `Assets/Tests/PlayMode/SceneTransitionTests.cs`
    - Test exploration → battle → exploration round trip
    - Test save/load mid-run
    - Test death → game over → new run flow
    - _Requirements: 11.1–11.4, 27.5–27.7_

- [ ] 27. Final checkpoint — Ensure all systems integrated, all property tests and integration tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation at natural break points
- Property tests validate universal correctness properties from the design document (41 properties total)
- Existing files (BattleManager, CardData, DeckManager, HandManager, EnemyAction, TurnState, Health, BattleEventBus, CardAnimator, BattleAnimations, EnemyFollow, SceneLoader, etc.) are modified in-place rather than recreated
- FsCheck/NUnit is used for property-based testing in Unity Test Framework EditMode tests

## Existing Codebase Considerations

The following existing files require specific attention during implementation. These notes supplement the task descriptions above.

### Scene Architecture

The project currently has 3 scenes: `Explorationscene`, `Battlescene`, `SampleScene`. The following new Unity scenes must be created and added to Build Settings:

| Scene Name | Purpose | Task |
|---|---|---|
| `MainMenuScene` | Main menu (New Game, Continue, Settings, Achievements, Quit) | 23.1 |
| `HubOfficeScene` | 2D diorama hub with cursor-only interaction, no WASD | 21.1 |
| `OpeningDialogueScene` | Boss overtime question, YES/NO choice, joke ending | 22.1 |
| `DeathScreenScene` | "Dragged back to desk" narrative sequence | 22.2 |
| `GameOverScene` | Run stats display, New Run / Main Menu buttons | 22.3 |
| `WinCinematicScene` | Quiet going-home cinematic after final boss | 22.4 |
| `DeckSelectionScene` | Starting deck carousel UI before first floor | 15.1 |

SceneLoader (task 14.1) must be extended to handle transitions between all of these scenes, not just Explorationscene ↔ Battlescene. The full scene flow is:
- `MainMenuScene` → `OpeningDialogueScene` → `DeckSelectionScene` → `HubOfficeScene` → `Explorationscene` ↔ `Battlescene`
- Death: `Battlescene` → `DeathScreenScene` → `GameOverScene` → `MainMenuScene` or `OpeningDialogueScene`
- Win: `Battlescene` → `WinCinematicScene` → `MainMenuScene`
- Pause → Hub: `Explorationscene`/`Battlescene` → `HubOfficeScene` → back

Alternatively, some of these (OpeningDialogue, DeathScreen, GameOver, DeckSelection) could be implemented as full-screen UI overlays within existing scenes rather than separate scenes, at the implementer's discretion. The key requirement is that SceneLoader supports the full navigation flow.

### Files Requiring TurnState → TurnPhase Migration (Task 1.5)

When replacing `TurnState` with `TurnPhase`, the following files reference `TurnState` and must be updated:
- `Assets/Scripts/Battle/BattleManager.cs` — `CurrentTurn` property uses `TurnState`
- `Assets/Scripts/Battle/CardTargetingManager.cs` — checks `BattleManager.Instance.CurrentTurn != TurnState.PlayerTurn`
- `Assets/Scripts/Battle/CardInteractionHandler.cs` — checks `BattleManager.Instance.CurrentTurn != TurnState.PlayerTurn`

### Health.cs Refactoring (Task 7.3)

`Assets/Scripts/Enemy/Health.cs` currently calls `SceneLoader.Instance.LoadExploration()` directly in its `Die()` method and has a `suppressSceneLoad` flag. When EnemyCombatant wraps Health internally (task 7.3), the `Die()` method's scene-loading behavior must be disabled or refactored — EnemyCombatant should handle death logic (remove from encounter, play death animation) without triggering scene loads. The `suppressSceneLoad` flag exists for this purpose but should be set automatically by EnemyCombatant.

### SceneLoader.cs Legacy Fields (Task 14.1)

`Assets/Scripts/Core/SceneLoader.cs` currently uses simple fields (`enemyDefeated` bool, `useDefaultSpawn` bool, `playerPosition` Vector3) for state tracking. Task 14.1 must replace these with proper RunState/EncounterData-driven logic. The `using static CardBattle.BattleManager;` import at the top will also need updating as BattleManager's API changes.

### SavePosition.cs (Task 14.1)

`Assets/Scripts/Player/SavePosition.cs` reads `SceneLoader.Instance.playerPosition` on Start. If SceneLoader's position tracking changes in task 14.1, SavePosition must be updated to match.

### Attack.cs / Projectile.cs — Exploration Combat Scripts

`Assets/Scripts/Player/Attack.cs` and `Assets/Scripts/Player/Projectile.cs` implement real-time projectile combat (Space to shoot). The GDD uses card-based combat only — there is no real-time exploration combat. These scripts reference `Health.TakeDamage()` directly. They should be removed or disabled during implementation, as they conflict with the card battle system. If the user wants to keep them for a future feature, they should be moved to a `Legacy` folder and excluded from active use.

### Namespace Inconsistencies

Several existing scripts are NOT in the `CardBattle` namespace but interact with CardBattle code:
- `Battlescene_Trigger.cs` — global namespace, references `SceneLoader`
- `EnemyFollow.cs` — global namespace, references nothing from CardBattle currently but will need to
- `Menu.cs` — global namespace, basic pause menu
- `SceneLoader.cs` — global namespace, has `using static CardBattle.BattleManager`
- `SavePosition.cs` — global namespace
- `Attack.cs` / `Projectile.cs` — global namespace, reference `CardBattle.Health`
- `CameraManager.cs` — global namespace

These can remain in the global namespace but will need `using CardBattle;` imports added as they integrate with new CardBattle systems. Alternatively, they can be migrated into the `CardBattle` namespace during their respective tasks.

### CameraManager.cs (Task 14.3)

`Assets/Scripts/Core/CameraManager.cs` has a `SetBattleMode(bool)` method that switches FOV between exploration (60) and battle (45). Battle transition tasks (14.1, 14.3) should call this during scene transitions. The battle scene background rendering (task 14.3) should coordinate with CameraManager for proper FOV and rendering setup.

### RoomTemplate.cs — Missing RoomType (Task 20.1)

`Assets/Scripts/Procedural/RoomTemplate.cs` currently has no `roomType` field — it only has `roomName`, `roomPrefab`, `roomSize`, `spawnRules`, and `doorPositions`. Task 20.1 requires room type constraints (Office, Bathroom, Break room, Boss room). A `RoomType` enum and corresponding field must be added to RoomTemplate as part of task 20.1.

### BattleAnimations.cs — Already Has Attack Dash and Death (Task 10.4)

`Assets/Scripts/Battle/BattleAnimations.cs` already implements `PlayAttackDash()`, `PlayHitShake()`, and `PlayDeath()` with full coroutine-based animations. Task 10.4 should extend this file with the NEW animations (screen shake, Rage Burst pulse, overflow edge glow) rather than reimplementing the existing ones.

### CardAnimator.cs — Already Has Rejection Shake (Task 10.3)

`Assets/Scripts/Battle/CardAnimator.cs` already has `PlayRejection()` (horizontal shake) and `PlayEntrance()` with staggered delay. Task 10.3 should verify these match the spec requirements and only add/modify what's missing (e.g., exit animation toward battlefield if not already matching spec).

### BattleEventBus.cs — Already Has StatusEffectEvent (Task 1.6)

`Assets/Scripts/Battle/BattleEventBus.cs` already defines `StatusEffectEvent` struct and `OnStatusEffectApplied`/`OnStatusEffectRemoved` events. It also has `EntityTransformedEvent`. Task 1.6 should add the missing event types (OverflowEvent, ParryEvent, TurnPhaseChangedEvent, RageBurstEvent) without duplicating existing ones.
