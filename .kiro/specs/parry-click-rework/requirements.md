# Requirements Document

## Introduction

Replace the existing drag-to-parry mechanic with a click-to-parry ("Stamp of Denial") system during the Enemy_Phase parry window. When an enemy attacks and the parry window opens, matching Defense cards in the player's hand auto-highlight and slide forward. The player clicks a highlighted card (or presses a number key shortcut) to parry instead of dragging it. In multi-enemy encounters, an attack queue indicator shows how many attacks remain. A perfect parry timing bonus rewards precise clicks during the final portion of the window. Defense cards may carry optional on-parry effects that trigger when used to parry, adding strategic depth to card selection. All underlying parry logic (tag matching, intent colors, window duration scaling, tool modifiers, enemy parry chance) remains unchanged. This is a UI/interaction-only rework — no new art assets are required.

## Glossary

- **Parry_System**: The `ParrySystem` MonoBehaviour that manages parry window state, tag matching, window timing, and enemy parry evaluation
- **Parry_Window**: The timed interval during Enemy_Phase when the player can respond to an incoming attack with a Defense card
- **Parry_Window_UI**: The `ParryWindowUI` MonoBehaviour that displays the parry timer, intent color, attack queue indicator, and manages card highlight state
- **Card_Interaction_Handler**: The `CardInteractionHandler` MonoBehaviour attached to each card that handles pointer events (hover, click) and keyboard shortcut input
- **Card_Animator**: The `CardAnimator` MonoBehaviour that drives card movement, scaling, and shake animations
- **Card_Visual**: The `CardVisual` MonoBehaviour that renders card data (name, cost, art, type colors) onto the card UI
- **Hand_Manager**: The `HandManager` MonoBehaviour that manages the player's hand of cards, spawning, layout, and removal
- **Battle_Manager**: The `BattleManager` MonoBehaviour that orchestrates the battle flow including the Enemy_Phase coroutine and parry resolution
- **Matching_Card**: A Defense card whose `parryMatchTags` share at least one tag with the current enemy attack's `parryMatchTags`, as evaluated by `ParrySystem.IsParryMatch`
- **Non_Matching_Card**: Any card in the hand that is not a Matching_Card during an active Parry_Window (includes Attack, Effect, Utility, Special cards and Defense cards with no shared parry tags)
- **Stamp_Animation**: A brief scale-punch and positional snap animation played on a card when the player successfully clicks it to parry
- **Slide_Forward**: A subtle upward translation and slight scale increase applied to Matching_Cards when the Parry_Window opens, signaling they are clickable
- **Intent_Color**: The difficulty tier of an enemy attack — White (easy), Yellow (medium), Red (hard), or Unparryable (no window)
- **OT_Cost**: Overtime cost — the resource spent when playing a card during Play_Phase; parry-window clicks cost zero OT
- **Attack_Queue**: The ordered list of remaining enemy DealDamage actions in the current Enemy_Phase, tracked by the Battle_Manager as it iterates through living enemies
- **Attack_Queue_Indicator**: A UI element displayed during the Parry_Window showing the current attack's position within the Attack_Queue (e.g., "Attack 1 of 3")
- **Perfect_Parry**: A parry executed during the last 20% of the Parry_Window duration, granting a bonus reward of +1 OT restored
- **Perfect_Parry_Threshold**: The fraction of Parry_Window duration (0.20) that defines the Perfect_Parry timing zone, measured from the end of the window
- **On_Parry_Effect**: An optional effect defined on a Defense card's `CardData.onParryEffect` field that triggers when the card is used to successfully parry an attack
- **Parry_Effect_Type**: An enum defining the types of on-parry effects: None, CounterDamage, RestoreOT, DrawCard
- **CardData**: The `CardData` ScriptableObject that defines a card's properties including the new `onParryEffect` and `onParryEffectValue` fields

## Requirements

### Requirement 1: Matching Card Auto-Highlight on Parry Window Open

**User Story:** As a player, I want matching Defense cards to visually stand out when the parry window opens, so that I can instantly identify which cards I can use to parry.

#### Acceptance Criteria

1. WHEN the Parry_Window opens, THE Parry_Window_UI SHALL identify all Matching_Cards in the player's hand using `ParrySystem.GetMatchingCards`
2. WHEN the Parry_Window opens, THE Card_Animator SHALL apply a Slide_Forward animation to each Matching_Card, translating it upward and scaling it slightly larger than its resting arc position
3. WHILE the Parry_Window is active, THE Parry_Window_UI SHALL continuously re-evaluate Matching_Cards each frame to account for hand changes
4. WHEN the Parry_Window closes, THE Card_Animator SHALL return all previously highlighted Matching_Cards to their original arc positions and scale

### Requirement 2: Non-Matching Card Dimming During Parry Window

**User Story:** As a player, I want non-matching cards to appear dimmed and unresponsive during the parry window, so that I am not confused about which cards can parry.

#### Acceptance Criteria

1. WHILE the Parry_Window is active, THE Parry_Window_UI SHALL apply a dimmed visual state (reduced alpha or grayscale tint) to all Non_Matching_Cards in the player's hand
2. WHILE the Parry_Window is active, THE Card_Interaction_Handler SHALL ignore pointer click events on Non_Matching_Cards
3. WHILE the Parry_Window is active, THE Card_Interaction_Handler SHALL ignore pointer hover events on Non_Matching_Cards
4. WHEN the Parry_Window closes, THE Parry_Window_UI SHALL restore all Non_Matching_Cards to their normal visual state

### Requirement 3: Click-to-Parry Interaction

**User Story:** As a player, I want to click a highlighted Defense card to parry an incoming attack, so that parrying feels responsive and satisfying instead of requiring a clunky drag.

#### Acceptance Criteria

1. WHILE the Parry_Window is active, WHEN the player left-clicks a Matching_Card, THE Card_Interaction_Handler SHALL call `BattleManager.TryParryWithCard` with the clicked card
2. WHILE the Parry_Window is active, THE Card_Interaction_Handler SHALL accept only left-click input for parry attempts (right-click and other buttons are ignored)
3. WHEN `BattleManager.TryParryWithCard` succeeds, THE Battle_Manager SHALL move the card to the discard pile at zero OT_Cost
4. WHEN `BattleManager.TryParryWithCard` succeeds, THE Parry_System SHALL close the Parry_Window and set `ParrySucceeded` to true
5. IF the player clicks a Matching_Card but `ParrySystem.TryParry` returns false, THEN THE Card_Interaction_Handler SHALL not remove the card from the hand

### Requirement 4: Stamp Animation on Successful Parry

**User Story:** As a player, I want a satisfying "stamp/slap" animation when I successfully parry, so that the action feels impactful and rewarding.

#### Acceptance Criteria

1. WHEN a parry succeeds via click, THE Card_Animator SHALL play a Stamp_Animation on the parried card consisting of a rapid scale-up followed by a scale-down to normal size
2. WHEN the Stamp_Animation completes, THE Hand_Manager SHALL remove the card from the hand and recalculate the hand layout for remaining cards
3. THE Stamp_Animation SHALL complete within 0.3 seconds to avoid blocking the battle flow

### Requirement 5: Parry Window Expiry Behavior

**User Story:** As a player, I want to understand the consequence of not clicking in time, so that I feel the tension of the parry window countdown.

#### Acceptance Criteria

1. WHILE the Parry_Window is active, WHEN the timer reaches zero without a successful parry click, THE Parry_System SHALL close the Parry_Window with `ParrySucceeded` set to false
2. WHEN the Parry_Window expires, THE Battle_Manager SHALL apply the full attack damage to the player (reduced by Block if applicable)
3. WHEN the Parry_Window expires, THE Parry_Window_UI SHALL clear all card highlights and dimming, restoring cards to their normal visual state

### Requirement 6: Preservation of Existing Parry Mechanics

**User Story:** As a developer, I want all existing parry subsystem logic to remain unchanged, so that the rework only affects the input method and visual feedback without introducing regressions.

#### Acceptance Criteria

1. THE Parry_System SHALL continue to use tag-based matching via `IsParryMatch` with no changes to the matching algorithm
2. THE Parry_System SHALL continue to support all four Intent_Color tiers: White, Yellow, Red, and Unparryable
3. WHEN an attack has Intent_Color Unparryable, THE Parry_System SHALL not open a Parry_Window
4. THE Parry_System SHALL continue to scale Parry_Window duration based on floor depth using `GameConfig.parryWindowFloorScaling`
5. THE Parry_System SHALL continue to apply tool modifiers to Parry_Window duration via `ApplyWindowDurationModifier`
6. THE Parry_System SHALL continue to evaluate enemy parry chance on player Attack cards via `EvaluateEnemyParry`
7. THE Battle_Manager SHALL continue to grant 1 Overtime on successful normal parry (non-Perfect_Parry)
8. THE Battle_Manager SHALL continue to use the two-phase dash animation (fast approach, slow finish over parry window) for parryable attacks

### Requirement 7: Drag-to-Parry Removal

**User Story:** As a developer, I want to remove the drag-based parry input path, so that there is a single, clean interaction model for parrying.

#### Acceptance Criteria

1. THE Card_Interaction_Handler SHALL not initiate any drag-based parry interaction during the Parry_Window
2. THE Card_Interaction_Handler SHALL handle parry exclusively through left-click or keyboard shortcut during the Parry_Window

### Requirement 8: Multi-Enemy Attack Queue Indicator

**User Story:** As a player, I want to see how many enemy attacks remain in the current Enemy_Phase when a parry window opens, so that I can decide whether to use a strong Defense card now or save it for a later attack.

#### Acceptance Criteria

1. WHEN the Enemy_Phase begins, THE Battle_Manager SHALL count the total number of parryable DealDamage actions across all living enemies and store this as the Attack_Queue total
2. WHEN the Parry_Window opens, THE Parry_Window_UI SHALL display the Attack_Queue_Indicator showing the current attack's ordinal position and the total count (e.g., "Attack 1 of 3")
3. WHEN each successive Parry_Window opens during the same Enemy_Phase, THE Parry_Window_UI SHALL increment the current attack position in the Attack_Queue_Indicator
4. WHILE the Parry_Window is active, THE Attack_Queue_Indicator SHALL remain visible alongside the parry timer and Intent_Color display
5. WHEN only one parryable attack exists in the Enemy_Phase, THE Parry_Window_UI SHALL display the Attack_Queue_Indicator as "Attack 1 of 1"
6. WHEN the Parry_Window closes, THE Parry_Window_UI SHALL hide the Attack_Queue_Indicator

### Requirement 9: Keyboard Shortcuts for Parry Card Selection

**User Story:** As a keyboard-focused player, I want to press number keys to select highlighted Defense cards during the parry window, so that I can parry faster without needing to move the mouse.

#### Acceptance Criteria

1. WHILE the Parry_Window is active, THE Card_Interaction_Handler SHALL listen for number key presses (Alpha1 through Alpha9) corresponding to the left-to-right positions of Matching_Cards in the hand
2. WHILE the Parry_Window is active, WHEN the player presses a number key that corresponds to a valid Matching_Card position, THE Card_Interaction_Handler SHALL call `BattleManager.TryParryWithCard` with the corresponding card
3. WHILE the Parry_Window is active, WHEN the player presses a number key that does not correspond to any Matching_Card position, THE Card_Interaction_Handler SHALL ignore the input
4. THE Card_Interaction_Handler SHALL assign shortcut numbers only to Matching_Cards, starting at 1 for the leftmost Matching_Card and incrementing left-to-right
5. WHILE the Parry_Window is active, THE Parry_Window_UI SHALL display the assigned number key label on each Matching_Card's Slide_Forward visual
6. WHEN the Parry_Window closes, THE Parry_Window_UI SHALL remove all number key labels from cards

### Requirement 10: Perfect Parry Timing Bonus

**User Story:** As a skilled player, I want to be rewarded for clicking at the last moment of the parry window, so that I have a reason to practice precise timing and feel a sense of mastery.

#### Acceptance Criteria

1. WHEN a parry succeeds and the Parry_Window time remaining is within the last 20% of the Parry_Window duration (time remaining divided by total duration is less than or equal to the Perfect_Parry_Threshold), THE Parry_System SHALL flag the parry as a Perfect_Parry
2. WHEN a Perfect_Parry occurs, THE Battle_Manager SHALL grant +2 Overtime total (the standard +1 OT plus an additional +1 OT bonus) instead of the normal +1 OT
3. WHEN a Perfect_Parry occurs, THE Parry_Window_UI SHALL display a distinct "PERFECT" visual flash feedback to the player
4. WHEN a parry succeeds outside the Perfect_Parry timing zone, THE Parry_System SHALL treat the parry as a normal successful parry with standard +1 OT reward
5. THE Parry_System SHALL expose a `WasPerfectParry` boolean property that the Battle_Manager reads after a successful parry to determine the OT reward amount
6. THE Perfect_Parry_Threshold SHALL be configurable via `GameConfig` with a default value of 0.20

### Requirement 11: On-Parry Bonus Effects for Defense Cards

**User Story:** As a player, I want some Defense cards to trigger bonus effects when used to parry, so that I have a meaningful choice between multiple matching Defense cards during the parry window.

#### Acceptance Criteria

1. THE CardData SHALL include an `onParryEffect` field of type Parry_Effect_Type (enum: None, CounterDamage, RestoreOT, DrawCard) with a default value of None
2. THE CardData SHALL include an `onParryEffectValue` integer field that parameterizes the on-parry effect (e.g., damage amount for CounterDamage, OT amount for RestoreOT, card count for DrawCard)
3. WHEN a parry succeeds with a card whose `onParryEffect` is CounterDamage, THE Battle_Manager SHALL deal `onParryEffectValue` damage to the attacking enemy
4. WHEN a parry succeeds with a card whose `onParryEffect` is RestoreOT, THE Battle_Manager SHALL restore `onParryEffectValue` additional Overtime to the player (in addition to the standard parry OT reward)
5. WHEN a parry succeeds with a card whose `onParryEffect` is DrawCard, THE Battle_Manager SHALL cause the player to draw `onParryEffectValue` cards from the deck
6. WHEN a parry succeeds with a card whose `onParryEffect` is None, THE Battle_Manager SHALL apply only the standard parry reward with no additional effect
7. WHILE the Parry_Window is active, THE Parry_Window_UI SHALL display the on-parry effect description on each Matching_Card that has a non-None `onParryEffect`, so the player can compare cards before choosing
8. WHEN a parry succeeds with a non-None On_Parry_Effect, THE Parry_Window_UI SHALL display a brief text indicator describing the triggered effect (e.g., "Counter: 3 DMG", "Restored 1 OT", "Drew 1 card")
