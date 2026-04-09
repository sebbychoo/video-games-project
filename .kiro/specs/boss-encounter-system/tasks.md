# Implementation Plan: Boss Encounter System

## Overview

Implement the boss encounter system in incremental steps: data models first, then core logic (floor schedule, exploration behavior, cutscene, intro screen, animations, phase transitions), and finally integration with the existing BattleManager pipeline. Each step builds on the previous, with property-based tests validating correctness properties from the design document.

## Tasks

- [x] 1. Define data models and extend EnemyCombatantData
  - [x] 1.1 Create SpriteFrameAnimation, BossAnimationData, BossAttackAnimation, and BossPhase2Data serializable classes
    - Create `Assets/Scripts/Battle/Boss/BossDataModels.cs`
    - Define `BossPose` enum (Standing, Sitting)
    - Define `SpriteFrameAnimation` serializable class with `Sprite[] frames`, `float frameRate`, `bool loop`
    - Define `BossAttackAnimation` serializable class with `EnemyActionType actionType`, `SpriteFrameAnimation animation`
    - Define `BossAnimationData` serializable class with idle, damaged, death animations and `List<BossAttackAnimation> attackAnimations`
    - Define `BossPhase2Data` serializable class with `hpThresholdPercent`, `phase2AttackPattern`, `phase2SpriteSet`, `phase2Animations`
    - _Requirements: 2.1, 7.1, 8.1, 9.1, 9.2_

  - [x] 1.2 Add boss fields to EnemyCombatantData ScriptableObject
    - Add `bossTitle` string field (Req 2.1)
    - Add `bossPose` field of type `BossPose` (Req 7.1)
    - Add `bossAnimationData` field of type `BossAnimationData` (Req 8.1)
    - Add `phase2Data` field of type `BossPhase2Data` (Req 9.1, 9.2)
    - Ensure all existing fields are preserved unchanged (Req 2.3)
    - _Requirements: 2.1, 2.3, 7.1, 8.1, 9.1, 9.2_

  - [x] 1.3 Add boss intro timing fields to GameConfig
    - Add `bossIntroSlideDuration` (default 0.6f) and `bossIntroHoldDuration` (default 1.5f)
    - Add `phaseTransitionPauseDuration` (default 1.0f)
    - _Requirements: 5.6, 9.4_

- [x] 2. Update boss floor schedule formula in LevelGenerator
  - [x] 2.1 Update IsBossFloor to use new formula
    - Change `IsBossFloor(int floor)` from `floor % interval == 0` to `floor == 1 || (floor > 1 && (floor - 1) % bossFloorInterval == 0)`
    - Produces sequence 1, 4, 7, 10, 13 with interval=3
    - _Requirements: 1.1, 1.2, 1.3, 1.5_

  - [ ]* 2.2 Write property test for boss floor schedule formula
    - **Property 1: Boss Floor Schedule Formula**
    - Create `Assets/Tests/EditMode/Boss/BossFloorSchedulePropertyTests.cs`
    - For any positive floor and any positive bossFloorInterval, verify IsBossFloor returns true iff `floor == 1 || (floor > 1 && (floor - 1) % interval == 0)`
    - Verify determinism: same inputs always produce same result
    - Use 200 iterations with randomized floor numbers and intervals
    - **Validates: Requirements 1.2, 1.5**

- [x] 3. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Implement SpriteFrameAnimator component
  - [x] 4.1 Create SpriteFrameAnimator MonoBehaviour
    - Create `Assets/Scripts/Battle/Boss/SpriteFrameAnimator.cs`
    - Implement `Play(SpriteFrameAnimation anim, bool loop, Action onComplete)` — cycles through `frames` at `frameRate` on a `SpriteRenderer`
    - Implement `Stop()` and `IsPlaying` property
    - Handle edge case: animation with 0 frames logs warning and does nothing
    - _Requirements: 8.10_

  - [x] 4.2 Write property test for attack animation lookup
    - **Property 6: Attack Animation Lookup**
    - Create `Assets/Tests/EditMode/Boss/BossAnimationPropertyTests.cs`
    - For any BossAnimationData and any EnemyActionType with a mapped entry, verify lookup returns correct SpriteFrameAnimation
    - For any action type without a mapping, verify lookup returns null
    - Use 200 iterations with randomized action type sets
    - **Validates: Requirements 8.8**

- [x] 5. Implement BossAnimationController component
  - [x] 5.1 Create BossAnimationController MonoBehaviour
    - Create `Assets/Scripts/Battle/Boss/BossAnimationController.cs`
    - Hold `Phase1Animations` and `Phase2Animations` of type `BossAnimationData`
    - Implement `PlayIdle()`, `PlayDamaged(Action onComplete)`, `PlayAttack(EnemyActionType, Action onComplete)`, `PlayDeath(Action onComplete)`
    - Implement `SwitchToPhase2()` — swaps active animation set to phase 2
    - Implement attack animation lookup: find matching `BossAttackAnimation` by action type, return null if not found
    - _Requirements: 8.2, 8.3, 8.4, 8.5, 8.6, 8.7, 8.8, 8.9, 9.7_

- [x] 6. Implement BossExplorationEntity for stationary boss behavior
  - [x] 6.1 Create BossExplorationEntity MonoBehaviour
    - Create `Assets/Scripts/Exploration/BossExplorationEntity.cs`
    - Accept `EnemyCombatantData` reference for boss data
    - Render sitting (chair + boss sprite) or standing (boss sprite only) based on `bossPose`
    - Implement Y-axis billboard rotation to face the player camera in `Update()`
    - Ensure no patrol, no wander, no chase — boss stays at fixed position (Req 3.1, 3.2, 3.4)
    - _Requirements: 3.1, 3.2, 3.4, 7.2, 7.3, 7.4_

  - [x] 6.2 Write property test for boss pose assignment by floor
    - **Property 5: Boss Pose Assignment by Floor**
    - Create `Assets/Tests/EditMode/Boss/BossPosePropertyTests.cs`
    - For floor 1, verify assigned boss has `bossPose == Standing`
    - For any boss floor > 1, verify assigned boss has `bossPose == Sitting`
    - Use 200 iterations with randomized floor numbers
    - **Validates: Requirements 7.5, 7.6**

- [x] 7. Implement BossCutsceneController for pre-battle dialogue
  - [x] 7.1 Create BossCutsceneController MonoBehaviour
    - Create `Assets/Scripts/Exploration/BossCutsceneController.cs`
    - Trigger on player entering boss room trigger volume (Req 4.1)
    - Display `preFightDialogue` text, pause player movement input (Req 4.2)
    - On dismiss (interact key or dialogue complete), initiate battle transition via `Battlescene_Trigger` (Req 4.3)
    - If `preFightDialogue` is null/empty/whitespace, skip dialogue and trigger battle immediately (Req 4.4)
    - Prevent other interactions while dialogue is displayed (Req 4.5)
    - _Requirements: 3.3, 4.1, 4.2, 4.3, 4.4, 4.5_

  - [x] 7.2 Write property test for dialogue skip on empty pre-fight dialogue
    - **Property 3: Dialogue Skip on Empty Pre-Fight Dialogue**
    - Create `Assets/Tests/EditMode/Boss/BossIntroLogicPropertyTests.cs`
    - For any EnemyCombatantData where preFightDialogue is null, empty, or whitespace-only, verify cutscene controller skips dialogue
    - For any non-empty preFightDialogue, verify dialogue is displayed
    - Use 200 iterations with randomized string inputs
    - **Validates: Requirements 4.4**

- [x] 8. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 9. Implement BossIntroScreen UI component
  - [x] 9.1 Create BossIntroScreen MonoBehaviour
    - Create `Assets/Scripts/Battle/Boss/BossIntroScreen.cs`
    - Full-screen black background Canvas panel (Req 5.2)
    - Implement `Play(string bossName, string bossTitle, Action onComplete)`
    - Animate "Introducing..." label sliding left-to-center (Req 5.3)
    - Animate boss name label sliding left-to-center (Req 5.4)
    - If bossTitle is non-empty, display title below name (Req 5.5); omit if null/empty (Req 2.2)
    - Hold final state for configurable duration from GameConfig (Req 5.6)
    - Call `onComplete` when sequence finishes (Req 5.7)
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7_

  - [ ]* 9.2 Write property tests for boss title visibility and intro screen gating
    - **Property 2: Boss Title Visibility Decision**
    - For any bossTitle string, verify intro screen displays title iff non-null and non-empty
    - **Property 4: Boss Intro Screen Gating**
    - For any EncounterData, verify boss intro plays iff `isBossEncounter` is true
    - Add both tests to `Assets/Tests/EditMode/Boss/BossIntroLogicPropertyTests.cs`
    - Use 200 iterations each
    - **Validates: Requirements 2.2, 5.5, 5.8**

- [x] 10. Integrate boss intro and animations into BattleManager
  - [x] 10.1 Update BattleManager to support boss intro screen
    - Add `[SerializeField] BossIntroScreen bossIntroScreen` field
    - In `StartEncounter`, check `encounter.isBossEncounter`
    - If true and `bossIntroScreen` is not null, call `bossIntroScreen.Play()` and defer encounter start until `onComplete` (Req 5.1, 6.3)
    - If `bossIntroScreen` is null, fall back to starting encounter immediately (Req 6.5)
    - If `isBossEncounter` is false, start encounter immediately without intro (Req 5.8, 6.1)
    - _Requirements: 5.1, 5.7, 5.8, 6.1, 6.2, 6.3, 6.4, 6.5_

  - [x] 10.2 Integrate BossAnimationController into BattleManager enemy phase
    - When spawning a boss enemy, attach and initialize `BossAnimationController` from `bossAnimationData`
    - During idle, play boss idle animation (Req 8.6)
    - On boss taking damage, play damaged animation then return to idle (Req 8.7)
    - On boss attack, play attack animation mapped to action type (Req 8.8)
    - On boss death, play death animation before victory sequence (Req 8.9)
    - _Requirements: 8.6, 8.7, 8.8, 8.9_

- [x] 11. Implement boss phase 2 transition system
  - [x] 11.1 Add phase transition logic to BattleManager
    - After each damage application to a boss, check if `currentHP <= floor(maxHP * hpThresholdPercent)` and `currentHP > 0`
    - If phase2Data is not null and transition hasn't occurred yet, trigger phase transition (Req 9.3, 9.9)
    - Pause gameplay briefly, play visual effect (flash/screen shake) (Req 9.4)
    - Swap boss sprite set to phase2SpriteSet (Req 9.5)
    - Swap boss attack pattern to phase2AttackPattern (Req 9.6)
    - Call `BossAnimationController.SwitchToPhase2()` to swap animations (Req 9.7)
    - Track transition with a boolean flag — at most once per encounter (Req 9.9)
    - If boss killed through threshold in one hit (HP <= 0), skip transition and go to death (Req 9.10)
    - _Requirements: 9.3, 9.4, 9.5, 9.6, 9.7, 9.8, 9.9, 9.10_

  - [ ]* 11.2 Write property tests for phase 2 transition
    - **Property 7: Phase 2 Trigger Condition**
    - Create `Assets/Tests/EditMode/Boss/BossPhaseTransitionPropertyTests.cs`
    - For any maxHP > 0 and hpThresholdPercent in (0,1], verify transition triggers when `currentHP <= floor(maxHP * hpThresholdPercent)` and `currentHP > 0`
    - **Property 8: Phase 2 Attack Pattern Swap**
    - After phase transition, verify active attack pattern is phase2AttackPattern
    - **Property 9: Phase Transition Occurs At Most Once**
    - Verify transition flag prevents multiple transitions regardless of HP fluctuations
    - Use 200 iterations each with randomized HP values and thresholds
    - **Validates: Requirements 9.3, 9.6, 9.8, 9.9**

- [x] 12. Update LevelGenerator to spawn boss entities on boss floors
  - [x] 12.1 Wire boss spawning into LevelGenerator.PopulateFloor
    - On boss floors, spawn `BossExplorationEntity` + `BossCutsceneController` in the boss room instead of roaming `EnemyFollow` enemies
    - Set `bossPose` to Standing for floor 1, Sitting for all other boss floors (Req 7.5, 7.6)
    - Select boss from `bossEnemies` pool and wire data into the boss entity
    - Use `bossFloorPrefabs` pool for boss floor prefab selection (Req 1.4)
    - _Requirements: 1.4, 3.1, 3.2, 7.5, 7.6_

- [x] 13. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Property tests follow the existing NUnit iteration-loop pattern (200 iterations) used in `FloorGenerationPropertyTests.cs` and `EnemyCombatPropertyTests.cs`
- The design uses C# throughout — all implementations target Unity 2022+ with the existing CardBattle namespace
- Checkpoints ensure incremental validation at key integration points
