# Requirements Document

## Introduction

The Bloody Gloves Mana feature adds two separate visual systems to the player character's hands:

1. **Blood on Gloves** — a purely cosmetic fight-history layer. Blood accumulates on the gloves when the player punches (plays attack cards) during battle encounters. Early punches produce barely any blood, but as the battle goes on, each punch produces exponentially more blood — up to a maximum cap of 1.0 (fully red hands). Boss encounters have a higher blood multiplier, producing more blood per punch. Blood only goes up (ratchet mechanic) and can only be reduced by washing in a Bathroom (free, but each bathroom can only be used once for washing) or by being defeated (full reset). Blood has no gameplay effect.

2. **Mana Veins on Wrists** — a functional OT (Overtime/mana) indicator rendered as glowing veins on the wrists just below where the gloves end. Vein brightness maps to the player's current OT value. The player starts each run with 10 OT, so the veins have a faint glow from the beginning. More OT = brighter, more visible veins. OT is strictly a battle resource — veins in exploration are always static, showing the last battle's OT value. In battle, hovering over the wrist veins area shows an OT tooltip.

On defeat, everything resets — gloves go back to white (no blood), veins reset to the starting faint glow.

## Glossary

- **Blood_Level**: A float (0.0–1.0) representing accumulated blood on the gloves. Increases when the player plays attack cards during battle. Purely cosmetic. Persisted in RunState.
- **Blood_Tint**: The visual red tint applied to glove sprites based on Blood_Level. Higher Blood_Level = redder gloves.
- **Punch_Count**: The number of attack cards played so far in the current battle encounter. Used to compute the exponential blood curve.
- **Blood_Multiplier**: A per-encounter multiplier for blood accumulation. Boss encounters have a higher multiplier than regular encounters.
- **Mana_Veins**: Glowing vein visuals rendered on the wrists just below the gloves. Brightness driven by current OT value.
- **Vein_Glow_Intensity**: A normalized value (0.0–1.0+) mapping OT to vein brightness. Faint at low OT, intense at high/overflow OT.
- **Overtime_Meter**: The existing OvertimeMeter component that tracks the player's current and maximum Overtime (mana) resource during battle encounters.
- **Exploration_Gloves**: The glove visuals rendered on the first-person hands during exploration, tinted by Blood_Level.
- **Exploration_Veins**: The wrist vein visuals rendered during exploration, glowing based on the last known OT value. Always static during exploration (OT is battle-only).
- **Battle_Gloves**: The glove visuals rendered in the battle scene UI, tinted by Blood_Level.
- **Battle_Veins**: The wrist vein visuals rendered in the battle scene UI, glowing based on real-time OT value.
- **BathroomShop**: The existing bathroom shop system where the player can also wash blood off their gloves (free, once per bathroom instance).

## Requirements

### Requirement 1: Blood Accumulation on Gloves (Cosmetic Fight History)

**User Story:** As a player, I want blood to accumulate on my gloves when I punch during battle, with an exponential curve that makes later punches produce much more blood, so that my gloves tell a visual story of how intense my fights have been.

#### Acceptance Criteria

1. WHEN the player plays an attack card during a battle encounter, THE system SHALL increase the persistent Blood_Level based on an exponential curve tied to the current Punch_Count within that encounter, clamped to a maximum of 1.0 (fully red hands).
2. THE blood increment per punch SHALL follow an exponential curve: early punches produce barely any blood, while later punches in the same encounter produce significantly more blood per hit.
3. WHEN the encounter is a boss encounter, THE system SHALL apply a higher Blood_Multiplier than regular encounters, causing more blood to accumulate per punch.
4. THE Blood_Level SHALL only increase (ratchet mechanic) — no battle action SHALL reduce the Blood_Level.
5. WHEN a new run starts, THE system SHALL reset Blood_Level to 0.0 (clean white gloves).
6. WHEN the player is defeated, THE system SHALL reset Blood_Level to 0.0 (full reset).
7. THE Blood_Level SHALL be persisted in RunState and survive scene transitions within a run.
8. THE maximum Blood_Level of 1.0 SHALL represent fully red hands — the visual ceiling.

### Requirement 2: Bathroom Blood Washing

**User Story:** As a player, I want to be able to wash blood off my gloves in the Bathroom for free, so that I can choose to clean up my fight history if I want — but each bathroom can only be used for washing once.

#### Acceptance Criteria

1. WHEN the player visits a BathroomShop that has not been used for washing, THE system SHALL offer an option to wash blood off the gloves.
2. WHEN the player chooses to wash blood, THE system SHALL reduce the Blood_Level fully to 0.0 at no currency cost.
3. WHEN the player washes blood at a bathroom, THE system SHALL mark that specific bathroom instance as "washed" so it cannot be used for washing again.
4. THE per-bathroom wash status SHALL be tracked in RunState (e.g., a list of washed bathroom instance IDs) and persist across scene transitions within a run.
5. WHEN the player visits a BathroomShop that has already been used for washing, THE system SHALL NOT offer the wash option (or show it as unavailable).
6. THE blood washing option SHALL be available alongside the existing card removal and shop functionality in the BathroomShop.
7. WHEN blood is washed, THE Exploration_Gloves SHALL immediately update to reflect the reduced Blood_Level.

### Requirement 3: Glove Blood Tint Rendering

**User Story:** As a player, I want to see the blood on my gloves as a red tint that gets more intense the more I have fought, so that I get clear visual feedback of my combat history.

#### Acceptance Criteria

1. THE Gloves_Renderer SHALL compute the Blood_Tint color by interpolating between a base glove color (white) and a full blood-red color based on Blood_Level.
2. WHEN Blood_Level equals 0.0, THE gloves SHALL display with no red tint (base white color).
3. WHEN Blood_Level equals 1.0, THE gloves SHALL display at full red blood intensity (fully red hands).
4. WHILE Blood_Level is between 0.0 and 1.0, THE Blood_Tint SHALL interpolate linearly between the base color and the full blood color.
5. THE Blood_Tint SHALL only be applied to the glove overlay sprites or glove UI Images; no other visual elements SHALL be affected.

### Requirement 4: Mana Veins on Wrists (Functional OT Indicator)

**User Story:** As a player, I want to see glowing veins on my wrists that show my current mana/OT level, so that I get intuitive visual feedback about my available resources.

#### Acceptance Criteria

1. THE Mana_Veins SHALL be rendered on the wrists just below where the gloves end, as a separate visual layer from the gloves.
2. THE Vein_Glow_Intensity SHALL be driven by the current OT value: faint glow at low OT (e.g., starting 10 OT), intense glow at max OT, and even more intense with overflow.
3. WHEN the Overtime_Meter value changes during battle, THE Battle_Veins SHALL update the Vein_Glow_Intensity within the same frame.
4. THE Mana_Veins SHALL include Overflow points from the OverflowBuffer when computing glow intensity, allowing the intensity to exceed the normal maximum for visual intensification.
5. WHILE the player is in the exploration scene, THE Exploration_Veins SHALL display the Vein_Glow_Intensity based on the last known OT value from the most recent battle. Veins are always static during exploration — OT is strictly a battle resource.
6. WHEN a new run starts, THE Mana_Veins SHALL display a faint glow corresponding to the starting OT of 10.

### Requirement 5: Starting OT of 10

**User Story:** As a player, I want to start each run with 10 OT so that my wrist veins have a faint natural glow from the beginning, reflecting my character's innate energy.

#### Acceptance Criteria

1. WHEN a new run starts, THE system SHALL initialize the persistent OT level to 10 (or the equivalent normalized ratio for 10/max).
2. THE Exploration_Veins SHALL display a faint glow at run start corresponding to 10 OT.
3. WHEN the first battle encounter begins, THE Battle_Veins SHALL initialize with the glow corresponding to the starting OT value.

### Requirement 6: Defeat Full Reset

**User Story:** As a player, I want everything to reset when I am defeated, so that a new run feels like a fresh start.

#### Acceptance Criteria

1. WHEN the player is defeated in battle, THE system SHALL reset Blood_Level to 0.0 (gloves return to white).
2. WHEN the player is defeated in battle, THE system SHALL reset the persistent OT level to the starting value (veins return to faint glow).
3. THE defeat reset SHALL occur as part of the existing run wipe flow (SaveManager.WipeRun).

### Requirement 7: Battle Veins Tooltip

**User Story:** As a player, I want to hover over the wrist veins area during battle to see a tooltip with my current OT points, so that I can get an exact numeric readout.

#### Acceptance Criteria

1. WHEN the player hovers the mouse pointer over the Battle_Veins area, THE tooltip SHALL appear displaying the text label "Overtime" and the current numeric OT value.
2. WHEN the player moves the mouse pointer away from the Battle_Veins area, THE tooltip SHALL hide within one frame.
3. WHILE the tooltip is visible and the Overtime_Meter value changes, THE tooltip SHALL update the displayed numeric value in real time.
4. THE tooltip SHALL display the total OT value including any Overflow points in the format "{current}/{max}" where current includes overflow.
5. THE tooltip SHALL NOT appear when hovering over the gloves/blood area — only the wrist veins area.

### Requirement 8: Glove and Vein Layering in Exploration Scene

**User Story:** As a player, I want to see both the bloody gloves and the glowing wrist veins on my first-person hands during exploration, so that I can see my fight history and mana level at a glance.

#### Acceptance Criteria

1. THE Exploration_Gloves SHALL be rendered as a dedicated SpriteRenderer layer positioned in front of each hand sprite in the FirstPersonHandsController hierarchy.
2. THE Exploration_Veins SHALL be rendered as a dedicated SpriteRenderer layer positioned on the wrist area just below the glove sprites.
3. WHILE the player is in the exploration scene, BOTH the Exploration_Gloves and Exploration_Veins SHALL follow the same position, animation bob, and interaction offsets as the parent hand sprites.
4. THE Exploration_Gloves SHALL only tint the glove overlay sprites; the underlying hand sprites and vein sprites SHALL remain unaffected by blood tint.

### Requirement 9: Glove and Vein Layering in Battle Scene

**User Story:** As a player, I want to see both the bloody gloves and the glowing wrist veins in the battle scene, so that I can see my fight history and current mana at a glance during combat.

#### Acceptance Criteria

1. WHEN the battle scene loads, THE Battle_Gloves SHALL render a glove UI Image element tinted by Blood_Level.
2. WHEN the battle scene loads, THE Battle_Veins SHALL render a vein UI element with glow driven by current OT.
3. WHILE the battle encounter is active, BOTH the Battle_Gloves and Battle_Veins SHALL remain visible throughout all turn phases (Draw, Play, Discard, Enemy).

### Requirement 10: Tint and Glow Computation Pure Functions

**User Story:** As a developer, I want the blood tint, blood accumulation, and vein glow calculations to be pure functions, so that they can be tested independently of Unity rendering.

#### Acceptance Criteria

1. THE system SHALL compute the Blood_Tint color via a static pure function that accepts Blood_Level and base/full-blood colors as inputs and returns the tinted color.
2. THE system SHALL compute the Vein_Glow_Intensity via a static pure function that accepts current OT, max OT, overflow OT, and glow parameters as inputs and returns the glow intensity/color.
3. THE system SHALL compute the blood increment per punch via a static pure function that accepts Punch_Count, Blood_Multiplier, and curve parameters as inputs and returns the blood increment.
4. FOR ALL valid inputs, calling any of these pure functions multiple times with the same arguments SHALL produce identical output (idempotence).

### Requirement 11: Art Asset Configuration

**User Story:** As a developer, I want the glove and vein systems to use configurable sprite references, so that artists can provide custom art without modifying code.

#### Acceptance Criteria

1. THE Exploration_Gloves SHALL expose serialized Sprite fields for left and right glove overlay sprites in the Unity Inspector.
2. THE Battle_Gloves SHALL expose a serialized Sprite field for the battle glove UI Image in the Unity Inspector.
3. THE Exploration_Veins SHALL expose serialized Sprite fields for left and right wrist vein sprites in the Unity Inspector.
4. THE Battle_Veins SHALL expose a serialized Sprite field for the battle vein UI element in the Unity Inspector.
5. WHEN no custom sprites are assigned, THE system SHALL use a default white square sprite as a placeholder and log a warning.

### Requirement 12: Error Handling

**User Story:** As a developer, I want the glove and vein systems to handle missing references gracefully, so that the game does not crash if assets are not configured.

#### Acceptance Criteria

1. IF the glove or vein sprite references are null, THEN THE system SHALL log a warning and continue without rendering the affected element.
2. IF the Overtime_Meter reference is null during battle, THEN THE Battle_Veins SHALL display veins with minimum glow and log a warning.
3. IF the maximum Overtime capacity is zero, THEN THE vein glow function SHALL treat the intensity as 0.0 to avoid division by zero.
4. IF Blood_Level is NaN or negative in RunState, THEN THE system SHALL clamp to 0.0 before applying tint.

### Requirement 13: Future — Replace OvertimeMeterUI with Vein System

**User Story:** As a developer, I want to plan for eventually replacing the existing OvertimeMeterUI bar with the glove veins system, so that the UI is streamlined into a single visual metaphor.

#### Acceptance Criteria

1. FOR NOW, both the existing OvertimeMeterUI and the new BattleVeinsUI SHALL coexist in the battle scene.
2. THE BattleVeinsUI SHALL be designed as a drop-in replacement for OvertimeMeterUI's informational role (showing OT value via tooltip).
3. WHEN the vein system is mature and tested, THE OvertimeMeterUI SHALL be removable without affecting any other battle systems.
