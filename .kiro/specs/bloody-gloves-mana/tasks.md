# Implementation Plan: Bloody Gloves Mana

## Overview

This plan implements two visual systems on the player's hands: blood tint on gloves (cosmetic fight history with exponential accumulation) and mana veins on wrists (functional OT indicator). Implementation starts with pure calculator functions, extends data models, builds battle and exploration UI components, integrates with BattleManager and BathroomShop, and wires everything together.

## Tasks

- [x] 1. Implement pure calculator functions
  - [x] 1.1 Create `BloodTintCalculator` static class
    - Create `Assets/Scripts/Battle/BloodTintCalculator.cs`
    - Implement `ComputeTint(float bloodLevel, Color baseColor, Color fullBloodColor)` returning `Color.Lerp(baseColor, fullBloodColor, Clamp01(bloodLevel))`
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 10.1_

  - [x] 1.2 Create `BloodAccumulationCalculator` static class
    - Create `Assets/Scripts/Battle/BloodAccumulationCalculator.cs`
    - Implement `ComputeIncrement(int punchCount, float bloodMultiplier, float baseIncrement, float growthRate)` using exponential formula `baseIncrement * Exp(growthRate * (punchCount - 1)) * bloodMultiplier`
    - Implement `ApplyIncrement(float currentBloodLevel, float increment)` enforcing ratchet and 1.0 cap
    - Return 0 for punchCount <= 0
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.8, 10.3_

  - [x] 1.3 Create `VeinGlowCalculator` static class
    - Create `Assets/Scripts/Battle/VeinGlowCalculator.cs`
    - Implement `ComputeGlow(int currentOT, int maxOT, int overflowOT, Color dimColor, Color brightColor)` with overflow intensification
    - Implement `ComputeGlowFromStored(int storedOT, int maxOT, Color dimColor, Color brightColor)` for exploration scene
    - Guard against maxOT <= 0 (return dimColor)
    - _Requirements: 4.2, 4.4, 10.2, 12.3_

  - [ ]* 1.4 Write property test: Blood accumulation ratchet and cap
    - **Property 1: Blood accumulation ratchet and cap**
    - Create `Assets/Tests/EditMode/Battle/BloodGlovesManaPropertyTests.cs`
    - Generate random (currentBloodLevel in [0,1], increment >= 0), verify `ApplyIncrement` output >= input and <= 1.0
    - Use 200 iterations with `System.Random` fixed seed pattern (matching `OvertimeMeterPropertyTests.cs` style)
    - **Validates: Requirements 1.1, 1.4, 1.8**

  - [ ]* 1.5 Write property test: Exponential curve monotonicity
    - **Property 3: Exponential curve monotonicity**
    - Generate pairs of punchCounts (a < b, both >= 1) with same positive params, verify `ComputeIncrement(b) >= ComputeIncrement(a)`
    - **Validates: Requirements 1.2**

  - [ ]* 1.6 Write property test: Boss multiplier produces more blood
    - **Property 4: Boss multiplier produces more blood**
    - Generate random punchCount >= 1 and positive curve params, verify higher multiplier yields >= increment
    - **Validates: Requirements 1.3**

  - [ ]* 1.7 Write property test: Blood tint interpolation correctness
    - **Property 6: Blood tint interpolation correctness**
    - Generate random (bloodLevel, baseColor, fullBloodColor), verify output matches `Color.Lerp(baseColor, fullBloodColor, Clamp01(bloodLevel))`
    - **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 10.1**

  - [ ]* 1.8 Write property test: Vein glow monotonicity
    - **Property 7: Vein glow monotonicity**
    - Generate pairs of OT inputs with ordered ratios, verify brightness (avg RGB) ordering
    - **Validates: Requirements 4.2, 4.4, 10.2**

  - [ ]* 1.9 Write property test: Pure function idempotence
    - **Property 9: Pure function idempotence**
    - Generate random inputs for all three calculator functions, call twice with identical args, assert equality
    - **Validates: Requirements 10.4**

- [x] 2. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

 - [x] 3. Extend data models (RunState and GameConfig)
  - [x] 3.1 Add blood and vein fields to `RunState`
    - Add `public float persistentBloodLevel;` to `Assets/Scripts/Core/RunState.cs`
    - Add `public int persistentOTLevel = 10;` to `RunState`
    - Add `public List<string> washedBathroomIds;` to `RunState`
    - All fields serialize via existing `SaveManager` JSON pipeline — no additional save/load code needed
    - _Requirements: 1.5, 1.6, 1.7, 2.4, 5.1, 6.1, 6.2_

  - [x] 3.2 Add blood curve config fields to `GameConfig`
    - Add `[Header("Blood Accumulation")]` section to `Assets/Scripts/Battle/GameConfig.cs`
    - Add `public float bloodBaseIncrement = 0.005f;`
    - Add `public float bloodGrowthRate = 0.15f;`
    - Add `public float regularBloodMultiplier = 1.0f;`
    - Add `public float bossBloodMultiplier = 2.0f;`
    - _Requirements: 1.2, 1.3_

  - [ ]* 3.3 Write property test: RunState persistence round-trip
    - **Property 2: RunState persistence round-trip**
    - Generate random (bloodLevel in [0,1], persistentOTLevel, washedBathroomIds list), serialize to JSON via `JsonUtility`, deserialize, verify equality within float precision
    - **Validates: Requirements 1.7, 2.4**

- [x] 4. Implement battle scene UI components
  - [x] 4.1 Create `BattleGlovesUI` component
    - Create `Assets/Scripts/Battle/UI/BattleGlovesUI.cs`
    - MonoBehaviour with serialized `Image gloveImage`, `Color baseGloveColor`, `Color fullBloodColor`
    - Implement `Initialize(float bloodLevel)` and `Refresh(float bloodLevel)` using `BloodTintCalculator.ComputeTint`
    - Log warning if gloveImage is null, continue without rendering
    - _Requirements: 9.1, 9.3, 11.2, 12.1_

  - [x] 4.2 Create `BattleVeinsUI` component
    - Create `Assets/Scripts/Battle/UI/BattleVeinsUI.cs`
    - MonoBehaviour implementing `IPointerEnterHandler`, `IPointerExitHandler`
    - Serialized fields: `Image veinImage`, `Color dimGlowColor`, `Color brightGlowColor`, `OvertimeMeter`, `OverflowBuffer`, `TextMeshProUGUI tooltipText`
    - Implement `Initialize(OvertimeMeter meter, OverflowBuffer overflow)` and `Refresh()` using `VeinGlowCalculator.ComputeGlow`
    - Subscribe to `BattleEventBus` events (OnCardPlayed, OnTurnPhaseChanged, OnOverflow, OnDamageReceived) to call `Refresh()`
    - Implement tooltip: show on pointer enter with format `"Overtime: {current+overflow}/{max}"`, hide on pointer exit
    - Handle null OvertimeMeter: display dim glow, tooltip shows "Overtime: --/--", log warning
    - _Requirements: 4.2, 4.3, 4.4, 7.1, 7.2, 7.3, 7.4, 7.5, 9.2, 9.3, 11.4, 12.2, 13.1, 13.2_

  - [ ]* 4.3 Write property test: Tooltip format correctness
    - **Property 8: Tooltip format correctness**
    - Generate random (currentOT >= 0, maxOT > 0, overflowOT >= 0), verify tooltip string equals `"{currentOT + overflowOT}/{maxOT}"`
    - **Validates: Requirements 7.4**

  - [ ]* 4.4 Write unit tests for BattleGlovesUI and BattleVeinsUI
    - Create `Assets/Tests/EditMode/Battle/BattleGlovesUITests.cs`
    - Test null sprite logs warning (Req 11.5, 12.1)
    - Test null OvertimeMeter shows minimum glow with warning (Req 12.2)
    - Test tooltip shows on pointer enter, hides on pointer exit (Req 7.1, 7.2)
    - Test tooltip does NOT appear on glove hover (Req 7.5)
    - Test veins visible across all turn phases (Req 9.3)
    - _Requirements: 7.1, 7.2, 7.5, 9.3, 11.5, 12.1, 12.2_

- [x] 5. Implement exploration scene components
  - [x] 5.1 Create `ExplorationGlovesController` component
    - Create `Assets/Scripts/Exploration/ExplorationGlovesController.cs`
    - MonoBehaviour with serialized `SpriteRenderer leftGloveRenderer`, `SpriteRenderer rightGloveRenderer`, `Color baseGloveColor`, `Color fullBloodColor`
    - Implement `ApplyBloodTint(float bloodLevel)` using `BloodTintCalculator.ComputeTint`
    - In `Start()`, read `RunState.persistentBloodLevel` from `SaveManager.Instance.CurrentRun` and apply tint
    - Clamp NaN/negative bloodLevel to 0, clamp > 1.0 to 1.0, log warnings for invalid values
    - Log warning if sprite renderers are null
    - _Requirements: 8.1, 8.3, 8.4, 11.1, 12.1, 12.4_

  - [x] 5.2 Create `ExplorationVeinsController` component
    - Create `Assets/Scripts/Exploration/ExplorationVeinsController.cs`
    - MonoBehaviour with serialized `SpriteRenderer leftVeinRenderer`, `SpriteRenderer rightVeinRenderer`, `Color dimGlowColor`, `Color brightGlowColor`
    - Implement `ApplyVeinGlow(int storedOT, int maxOT)` using `VeinGlowCalculator.ComputeGlowFromStored`
    - In `Start()`, read `RunState.persistentOTLevel` and `GameConfig.overtimeMaxCapacity`, apply glow
    - Veins are always static during exploration — no updates after Start
    - Clamp negative OT to 0, log warning
    - _Requirements: 4.5, 5.2, 8.2, 8.3, 11.3, 12.1_

- [x] 6. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 7. Integrate with BattleManager
  - [x] 7.1 Add blood tracking fields and glove/vein wiring to `BattleManager`
    - Add serialized fields: `BattleGlovesUI battleGlovesUI`, `BattleVeinsUI battleVeinsUI`
    - Add private fields: `int _punchCount`, `float _pendingBloodLevel`, `float _bloodMultiplier`
    - In `StartEncounter`: read `RunState.persistentBloodLevel` → set `_pendingBloodLevel`, reset `_punchCount = 0`, determine `_bloodMultiplier` from `EncounterData.isBossEncounter` (use `gameConfig.bossBloodMultiplier` for boss, `gameConfig.regularBloodMultiplier` for regular)
    - In `StartEncounter`: call `battleGlovesUI.Initialize(_pendingBloodLevel)` and `battleVeinsUI.Initialize(overtimeMeter, overflowBuffer)`
    - _Requirements: 1.1, 1.3, 9.1, 9.2_

  - [x] 7.2 Hook blood accumulation into attack card play
    - In `TryPlayCard` (after successful OT spend, when card is an attack card): increment `_punchCount`, compute increment via `BloodAccumulationCalculator.ComputeIncrement(_punchCount, _bloodMultiplier, gameConfig.bloodBaseIncrement, gameConfig.bloodGrowthRate)`, update `_pendingBloodLevel` via `BloodAccumulationCalculator.ApplyIncrement`, call `battleGlovesUI.Refresh(_pendingBloodLevel)`
    - _Requirements: 1.1, 1.2, 1.3, 1.4_

  - [x] 7.3 Persist blood and OT on victory and reset on defeat
    - In `OnVictory`: write `Mathf.Max(RunState.persistentBloodLevel, _pendingBloodLevel)` to `RunState.persistentBloodLevel` (ratchet), write `overtimeMeter.Current + overflowBuffer.Current` to `RunState.persistentOTLevel`, save via `SaveManager.Instance.SaveRun()`
    - On defeat (existing `SaveManager.WipeRun` flow): verify `persistentBloodLevel` resets to 0, `persistentOTLevel` resets to 10, `washedBathroomIds` clears (handled by RunState default values)
    - _Requirements: 1.4, 1.5, 1.6, 4.5, 6.1, 6.2, 6.3_

- [x] 8. Integrate BathroomShop blood washing
  - [x] 8.1 Add `WashBlood` and `CanWashBlood` methods to `BathroomShop`
    - Add `WashBlood(string bathroomId)` method: check `washedBathroomIds`, if not washed set `persistentBloodLevel = 0`, add bathroomId to list, save RunState, return true; return false if already washed or null/empty bathroomId
    - Add `CanWashBlood(string bathroomId)` method: return false if blood is 0, bathroom already washed, or RunState null
    - No currency cost — washing is free
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

  - [x] 8.2 Wire bathroom wash to exploration gloves refresh
    - After `WashBlood` succeeds, find `ExplorationGlovesController` in scene and call `ApplyBloodTint(0f)` for immediate visual update
    - _Requirements: 2.7_

  - [ ]* 8.3 Write property test: Bathroom wash resets blood and blocks re-wash
    - **Property 5: Bathroom wash resets blood and blocks re-wash**
    - Generate random Blood_Level > 0 and bathroomId string, verify wash sets persistentBloodLevel to 0 without deducting currency, and second wash with same ID returns false
    - **Validates: Requirements 2.2, 2.3, 2.5**

  - [ ]* 8.4 Write unit tests for BathroomShop blood washing
    - Add tests to `Assets/Tests/EditMode/Exploration/BathroomShopTests.cs`
    - Test WashBlood resets blood to 0 (Req 2.2)
    - Test WashBlood does not deduct currency (Req 2.2)
    - Test CanWashBlood returns false for already-washed bathroom (Req 2.5)
    - Test WashBlood with null/empty bathroomId returns false (Req 12 edge case)
    - Test Blood_Level resets to 0 on WipeRun (Req 1.5, 6.1)
    - Test persistentOTLevel resets to 10 on WipeRun (Req 5.1, 6.2)
    - Test washedBathroomIds cleared on WipeRun (Req 2.4)
    - _Requirements: 2.2, 2.3, 2.4, 2.5, 1.5, 5.1, 6.1, 6.2, 6.3_

- [ ] 9. Final checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document (9 properties total)
- Unit tests validate specific examples and edge cases
- The project uses C# with Unity and NUnit for testing, with a manual randomized iteration pattern (System.Random, fixed seed, 200 iterations)
- Both `OvertimeMeterUI` (existing) and `BattleVeinsUI` (new) coexist in the battle scene per Requirement 13.1
