# Requirements Document

## Introduction

The Boss Encounter System governs how boss fights are scheduled across floors, how bosses behave during exploration, and how the transition from exploration into a boss battle is presented. This includes a revised boss floor schedule starting at floor 1, stationary boss behavior in the boss room, a pre-battle dialogue cutscene, and a cinematic boss intro screen that plays before the battle begins.

## Glossary

- **Level_Generator**: The procedural system (`LevelGenerator`) that selects floor prefabs and populates dynamic content (enemies, workboxes, elevators) for each floor.
- **Game_Config**: The `GameConfig` ScriptableObject that stores tunable game parameters including `bossFloorInterval`.
- **Boss_Floor**: A floor that contains a boss encounter. Determined by the boss floor schedule formula.
- **Boss_Room**: The specific room within a Boss_Floor where the boss enemy is placed and the encounter takes place.
- **Boss_Enemy**: An `EnemyCombatantData` asset with `isBoss = true` and `variant = Boss`.
- **Boss_Room_Tracker**: The `BossRoomTracker` component placed in the boss room that tracks boss defeat state and handles force-engage.
- **Scene_Loader**: The `SceneLoader` singleton that manages scene transitions, battle loading, and spawn positioning.
- **Loading_Screen**: The `LoadingScreen` singleton that displays fade-to-black transitions with loading text between scenes.
- **Battle_Manager**: The `BattleManager` singleton that initializes and runs the card battle.
- **Boss_Intro_Screen**: A UI-only cinematic sequence that plays after the loading screen fades out and before the battle begins, introducing the boss with animated labels.
- **Cutscene_System**: A dialogue display system that shows the boss's pre-fight dialogue when the player enters the boss room, before transitioning to battle.
- **Exploration_Enemy**: The in-world representation of an enemy during the exploration phase, controlled by `EnemyFollow`.
- **Boss_Pose**: An enum or field on `EnemyCombatantData` indicating whether the boss is displayed sitting in a chair or standing during the exploration phase.
- **Billboard_Rotation**: A rotation behavior where the boss's chair sprite always faces the player camera, rotating on the Y-axis only.
- **Boss_Animation_Controller**: The per-boss component or data that drives sprite-based frame animations for idle, damaged, attack, and death states during battle.
- **Sprite_Frame_Animation**: A 2D sprite animation system that cycles through sprite frames to animate boss characters rendered in 3D space.
- **Boss_Phase**: A numbered stage of a boss encounter (Phase 1 or Phase 2), each with its own idle animation, attack patterns, attack animations, and sprite set.
- **Phase_Transition**: The moment during a boss battle when the boss switches from Phase 1 to Phase 2 upon reaching an HP threshold, accompanied by a visual effect.

## Requirements

### Requirement 1: Boss Floor Schedule

**User Story:** As a player, I want to encounter a boss on floor 1 and then every 3 floors after that, so that boss fights are spaced evenly starting from the first floor.

#### Acceptance Criteria

1. WHEN floor number equals 1, THE Level_Generator SHALL identify that floor as a Boss_Floor.
2. WHEN floor number is greater than 1, THE Level_Generator SHALL identify a floor as a Boss_Floor if `(floor - 1) % bossFloorInterval == 0`, where `bossFloorInterval` is read from Game_Config.
3. THE Level_Generator SHALL produce the boss floor sequence 1, 4, 7, 10, 13 when `bossFloorInterval` is set to 3.
4. WHEN a floor is identified as a Boss_Floor, THE Level_Generator SHALL select a floor prefab from the `bossFloorPrefabs` pool.
5. FOR ALL positive floor numbers, THE Level_Generator SHALL return a deterministic boolean result from `IsBossFloor` for the same floor number and interval.

### Requirement 2: Boss Title Data Field

**User Story:** As a designer, I want each boss enemy to have a title field (e.g., "The Executive"), so that the boss intro screen can display the boss's name and title.

#### Acceptance Criteria

1. THE Boss_Enemy data asset SHALL include a `bossTitle` string field.
2. WHEN `bossTitle` is empty or null, THE Boss_Intro_Screen SHALL omit the title label from the intro sequence.
3. THE Boss_Enemy data asset SHALL retain all existing fields (`enemyName`, `maxHP`, `isBoss`, `preFightDialogue`, etc.) without modification.

### Requirement 3: Stationary Boss Behavior

**User Story:** As a player, I want bosses to remain stationary in their room until I approach, so that boss encounters feel deliberate rather than random chases.

#### Acceptance Criteria

1. WHILE a Boss_Enemy is placed in the Boss_Room, THE Exploration_Enemy SHALL remain at a fixed position and not patrol or wander.
2. WHILE a Boss_Enemy is placed in the Boss_Room, THE Exploration_Enemy SHALL not chase the player regardless of proximity or line of sight.
3. WHEN the player enters the Boss_Room trigger volume, THE Cutscene_System SHALL initiate the boss dialogue sequence.
4. THE Exploration_Enemy for a Boss_Enemy SHALL face a fixed forward direction and not rotate to track the player.

### Requirement 4: Boss Room Dialogue Cutscene

**User Story:** As a player, I want the boss to speak to me when I enter the boss room, so that the encounter feels dramatic and story-driven.

#### Acceptance Criteria

1. WHEN the player enters the Boss_Room trigger volume, THE Cutscene_System SHALL display the boss's `preFightDialogue` text on screen.
2. WHILE the dialogue is displayed, THE Cutscene_System SHALL pause player movement input.
3. WHEN the player presses the interact key or the dialogue completes, THE Cutscene_System SHALL dismiss the dialogue and initiate the battle transition.
4. IF `preFightDialogue` is empty or null on the Boss_Enemy, THEN THE Cutscene_System SHALL skip the dialogue and immediately initiate the battle transition.
5. WHILE the dialogue is displayed, THE Cutscene_System SHALL prevent other interactions (workbox, shop, elevator) from activating.

### Requirement 5: Boss Intro Screen

**User Story:** As a player, I want a cinematic intro to play before a boss battle starts, so that the boss feels important and the moment has dramatic weight.

#### Acceptance Criteria

1. WHEN a boss battle is loaded and the Loading_Screen fade-in completes, THE Boss_Intro_Screen SHALL play before the Battle_Manager starts the encounter.
2. THE Boss_Intro_Screen SHALL display a fully black background covering the entire screen.
3. THE Boss_Intro_Screen SHALL animate a label with the text "Introducing..." sliding from the left edge to the center of the screen.
4. WHEN the "Introducing..." label reaches center, THE Boss_Intro_Screen SHALL animate a second label with the boss's `enemyName` sliding from the left edge to the center of the screen.
5. WHEN the Boss_Enemy has a non-empty `bossTitle`, THE Boss_Intro_Screen SHALL display the `bossTitle` text below the boss name label.
6. WHEN all labels have finished animating, THE Boss_Intro_Screen SHALL hold the final state for a configurable duration before dismissing.
7. WHEN the Boss_Intro_Screen sequence completes, THE Battle_Manager SHALL begin the encounter normally.
8. WHEN the encounter is not a boss encounter (`isBossEncounter` is false), THE Boss_Intro_Screen SHALL not appear and the Battle_Manager SHALL start immediately after the loading screen.

### Requirement 6: Battle Transition Integration

**User Story:** As a developer, I want the boss encounter flow to integrate cleanly with the existing scene loading and battle initialization pipeline, so that no existing non-boss encounters are affected.

#### Acceptance Criteria

1. WHEN a non-boss encounter is triggered, THE Scene_Loader SHALL load the battle scene using the existing flow without any boss-specific steps.
2. WHEN a boss encounter is triggered from the Boss_Room, THE Scene_Loader SHALL pass the encounter data (including `isBossEncounter = true`) to the Battle_Manager.
3. WHEN `isBossEncounter` is true, THE Battle_Manager SHALL defer its encounter start until the Boss_Intro_Screen sequence completes.
4. THE Scene_Loader SHALL continue to support the existing `LoadBattle` API for non-boss encounters without changes to the method signature.
5. IF the Boss_Intro_Screen fails to initialize, THEN THE Battle_Manager SHALL fall back to starting the encounter immediately.

### Requirement 7: Boss Positioning in Exploration

**User Story:** As a player, I want bosses to appear sitting in chairs that face me as I move around the room (except the first boss who stands), so that boss encounters feel visually distinct and imposing during exploration.

#### Acceptance Criteria

1. THE Boss_Enemy data asset SHALL include a `bossPose` field of type Boss_Pose indicating whether the boss is displayed sitting or standing during exploration.
2. WHEN a Boss_Enemy has `bossPose` set to Sitting, THE Exploration_Enemy SHALL render the boss seated in a chair sprite.
3. WHEN a Boss_Enemy has `bossPose` set to Standing, THE Exploration_Enemy SHALL render the boss in a standing pose without a chair.
4. WHILE the player is in the Boss_Room, THE Exploration_Enemy for a sitting boss SHALL apply Billboard_Rotation so the chair-and-boss sprite always faces the player camera on the Y-axis.
5. WHEN the Boss_Floor is floor 1, THE Boss_Enemy assigned to that floor SHALL have `bossPose` set to Standing.
6. WHEN the Boss_Floor is any floor other than floor 1, THE Boss_Enemy assigned to that floor SHALL have `bossPose` set to Sitting.

### Requirement 8: Boss Animation System

**User Story:** As a player, I want each boss to have unique sprite-based animations for idling, taking damage, attacking, and dying, so that boss battles feel visually dynamic and each boss has a distinct personality.

#### Acceptance Criteria

1. THE Boss_Enemy data asset SHALL include a reference to a Boss_Animation_Controller containing idle, damaged, attack, and death Sprite_Frame_Animation data.
2. THE Boss_Animation_Controller SHALL define an idle animation as a looping Sprite_Frame_Animation that plays while the boss is waiting or in battle.
3. THE Boss_Animation_Controller SHALL define a damaged animation as a non-looping Sprite_Frame_Animation that plays when the boss receives damage.
4. THE Boss_Animation_Controller SHALL define one or more attack animations, each mapped to a specific attack action type in the boss's attack pattern.
5. THE Boss_Animation_Controller SHALL define a death animation as a non-looping Sprite_Frame_Animation that plays when the boss's HP reaches zero.
6. WHEN the boss is idle during battle, THE Battle_Manager SHALL play the idle Sprite_Frame_Animation in a loop.
7. WHEN the boss takes damage, THE Battle_Manager SHALL interrupt the current animation and play the damaged Sprite_Frame_Animation, then return to idle.
8. WHEN the boss executes an attack action, THE Battle_Manager SHALL play the attack Sprite_Frame_Animation mapped to that action type.
9. WHEN the boss's HP reaches zero, THE Battle_Manager SHALL play the death Sprite_Frame_Animation before proceeding to the victory sequence.
10. THE Sprite_Frame_Animation system SHALL render 2D sprite frames on a quad or sprite renderer positioned in 3D space.

### Requirement 9: Boss Second Phase

**User Story:** As a player, I want each boss to transform into a more dangerous second phase at low HP, so that boss fights have a dramatic escalation and require me to adapt my strategy.

#### Acceptance Criteria

1. THE Boss_Enemy data asset SHALL include phase 2 data containing an HP threshold percentage, a phase 2 attack pattern, and a phase 2 sprite set.
2. THE Boss_Enemy data asset SHALL include phase 2 animation data containing a phase 2 idle animation, phase 2 attack animations, and a phase 2 damaged animation.
3. WHEN the boss's current HP falls to or below the phase 2 HP threshold percentage of max HP, THE Battle_Manager SHALL trigger a Phase_Transition from Phase 1 to Phase 2.
4. WHEN a Phase_Transition is triggered, THE Battle_Manager SHALL pause gameplay briefly and play a visual transition effect (flash, screen shake, or dedicated transition animation).
5. WHEN the Phase_Transition completes, THE Battle_Manager SHALL replace the boss's active sprite set with the phase 2 sprite set.
6. WHEN the Phase_Transition completes, THE Battle_Manager SHALL replace the boss's active attack pattern with the phase 2 attack pattern.
7. WHEN the Phase_Transition completes, THE Boss_Animation_Controller SHALL switch to the phase 2 idle animation, phase 2 attack animations, and phase 2 damaged animation.
8. WHILE the boss is in Phase 2, THE Battle_Manager SHALL use the phase 2 attack pattern for all subsequent enemy turns.
9. THE Phase_Transition SHALL occur at most once per boss encounter.
10. IF the boss's HP drops below the phase 2 threshold due to a single hit that also defeats the boss, THEN THE Battle_Manager SHALL skip the Phase_Transition and proceed directly to the death animation.
