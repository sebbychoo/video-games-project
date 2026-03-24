# Requirements Document

## Introduction

This document specifies the requirements for the Card Battle System in Overtime, a first-person roguelike deck-builder. The battle system is turn-based, inspired by Slay the Spire, where the player uses office-supply-themed cards powered by an Overtime resource meter. Combat transitions from the 3D exploration view to a 2D battle screen. The player draws cards, spends Overtime points to play them, and defeats enemies to progress through procedurally generated office floors.

The existing codebase (namespace `CardBattle`) already provides a basic turn loop, card rendering with arc layout, hover/select/targeting interactions, a deck/discard cycle, single-enemy combat, and a simple energy system. This spec extends and replaces that foundation with the full GDD-defined Overtime meter, multi-enemy encounters, expanded card types, status effects, and the overflow/rage burst mechanic.

## Glossary

- **Battle_Manager**: The central controller that orchestrates combat flow, turn sequencing, and win/loss conditions.
- **Overtime_Meter**: The resource system replacing generic energy. Has a base capacity of 10 points, regenerates 2 points per turn, and gains bonus points from damage taken.
- **Overflow_Buffer**: Storage for Overtime points that exceed the Overtime_Meter capacity. Consumed on the next attack to grant a diminishing-returns damage bonus.
- **Hand**: The set of cards currently available for the player to play during a turn, drawn from the Draw_Pile up to a base hand size of 5.
- **Draw_Pile**: The shuffled pile of cards from which the player draws each turn.
- **Discard_Pile**: The pile where played and unplayed cards go at end of turn. Reshuffled into the Draw_Pile when the Draw_Pile is empty.
- **Card_Instance**: A runtime representation of a card in the player's hand, referencing a Card_Data asset.
- **Card_Data**: A ScriptableObject asset defining a card's name, Overtime cost, type, rarity, effect value, target mode, and description.
- **Card_Type**: Classification of a card: Attack, Defense, Effect, Utility, or Special.
- **Card_Rarity**: The rarity tier of a card: Common (grey), Rare (blue), Epic (orange), Legendary (red), or Unknown (black). Unknown is the highest power tier, containing the most powerful cards in the game.
- **Target_Mode**: Defines how a card selects its target: Single_Enemy, All_Enemies, Self, or No_Target.
- **Status_Effect**: A temporary modifier applied to the player or an enemy (e.g., Burn, Stun, Bleed) with a defined duration in turns.
- **Enemy_Combatant**: An enemy entity participating in a battle encounter, with its own HP, attack patterns, and status effect tracking.
- **Encounter**: A combat instance containing one or more Enemy_Combatants that the player must defeat.
- **Turn_Phase**: A discrete step within a turn: Draw_Phase, Play_Phase, Discard_Phase, or Enemy_Phase.
- **Rage_Burst**: The mechanic where accumulated Overflow_Buffer points are consumed on the next attack card to add bonus damage.
- **Tool**: A passive relic item that persists for the duration of a run and modifies battle mechanics.
- **Block**: A defensive value that absorbs incoming damage before HP is reduced. Block resets to zero at the start of the player's turn.
- **First_Person_Hands_Controller**: The component responsible for rendering and animating Jean-Guy's two pixel art hand sprites overlaid on the camera view during exploration, handling idle, walking, sprinting, and interaction animations.
- **Work_Box**: A chest found under work desks containing cards. Comes in Small, Big, and Huge sizes.
- **Bathroom_Shop**: A shop found in bathroom rooms where the player can buy cards and Tools with Hours, or remove cards via the toilet.
- **Break_Room_Trade**: An NPC trade encounter in break rooms offering direct item swaps with no currency cost.
- **Hours**: The in-run currency earned from defeating enemies, used for shop purchases and card removal. Lost on death.
- **Bad_Reviews**: The persistent meta-progression currency earned from boss defeats, used for hub office upgrades. Never lost.
- **Enemy_Intent**: A visual indicator displayed above an Enemy_Combatant showing their planned action for the next turn.
- **Hub_Office**: Jean-Guy's personal office, a 2D diorama-style scene with cursor-based interaction for purchasing upgrades using Bad_Reviews.
- **Save_Manager**: The component responsible for persisting meta-progression data and mid-run state across game sessions.
- **Suicidal_Worker_Encounter**: A unique non-combat encounter on floor 5 where the player must intervene to save a coworker.

## Requirements

### Requirement 1: Turn Structure

**User Story:** As a player, I want a clearly defined turn structure, so that I understand when I can act and when enemies act.

#### Acceptance Criteria

1. WHEN a player turn begins, THE Battle_Manager SHALL set the Turn_Phase to Draw_Phase and draw cards from the Draw_Pile until the Hand contains cards equal to the base hand size of 5 or the Draw_Pile and Discard_Pile are both empty.
2. WHEN the Draw_Phase completes, THE Battle_Manager SHALL set the Turn_Phase to Play_Phase and allow the player to play cards.
3. WHILE the Turn_Phase is Play_Phase, THE Battle_Manager SHALL allow the player to play cards from the Hand by spending Overtime points or to end the turn voluntarily by clicking the End Turn button.
4. THE Battle_Manager SHALL display an End Turn button in the battle UI that is enabled only during the Play_Phase and disabled during all other phases.
5. WHEN the player ends the turn, THE Battle_Manager SHALL set the Turn_Phase to Discard_Phase and move all remaining cards in the Hand to the Discard_Pile.
6. WHEN the Discard_Phase completes, THE Battle_Manager SHALL set the Turn_Phase to Enemy_Phase and execute each Enemy_Combatant action in sequence.
7. WHEN all Enemy_Combatant actions in the Enemy_Phase complete, THE Battle_Manager SHALL begin a new player turn.
8. THE Battle_Manager SHALL ensure the player always acts before enemies on the first turn of an Encounter.

### Requirement 2: Overtime Meter Resource System

**User Story:** As a player, I want an Overtime meter that regenerates each turn and rewards me for taking damage, so that combat has interesting resource management decisions.

#### Acceptance Criteria

1. THE Battle_Manager SHALL initialize the Overtime_Meter at full capacity (10 points) at the start of each Encounter.
2. WHEN a player turn begins (starting from turn 2 onward), THE Overtime_Meter SHALL regenerate 2 Overtime points, capped at the current maximum capacity.
3. WHEN the player plays a card, THE Battle_Manager SHALL deduct the card's Overtime cost from the Overtime_Meter.
4. IF the player attempts to play a card whose Overtime cost exceeds the current Overtime_Meter value, THEN THE Battle_Manager SHALL reject the card play and display a rejection animation on the Card_Instance.
5. WHEN the player takes damage, THE Overtime_Meter SHALL gain 1 Overtime point for every 10 percent of maximum HP actually lost after Block absorption in that damage instance, rounded down.
6. WHEN the player takes damage from a status effect tick (such as Burn), THE Overtime_Meter SHALL gain at most 1 Overtime point per tick, regardless of the damage amount.
7. WHEN the Overtime_Meter gains points that would exceed the maximum capacity, THE Overflow_Buffer SHALL store the excess points.

### Requirement 3: Overflow and Rage Burst

**User Story:** As a player, I want excess Overtime points to accumulate and boost my next attack, so that taking damage creates a comeback mechanic.

#### Acceptance Criteria

1. THE Battle_Manager SHALL track the Overflow_Buffer as a non-negative integer value, initialized to 0 at the start of each Encounter.
2. WHEN the player plays an Attack card while the Overflow_Buffer is greater than 0, THE Battle_Manager SHALL consume all Overflow_Buffer points and apply a Rage_Burst damage bonus to that attack.
3. THE Battle_Manager SHALL calculate the Rage_Burst damage bonus using a diminishing-returns formula: 1 overflow point grants +20% bonus damage, 5 points grant +80%, 10 points grant +120%, and 20 points grant +140%.
4. WHEN the Overflow_Buffer is consumed by a Rage_Burst, THE Battle_Manager SHALL reset the Overflow_Buffer to 0.
5. THE Battle_Manager SHALL apply the Rage_Burst bonus only to Attack type cards.
6. WHEN a Rage_Burst activates on an Attack card with Target_Mode All_Enemies, THE Battle_Manager SHALL apply the bonus to the base damage value, and each Enemy_Combatant SHALL take the full boosted damage amount.

### Requirement 4: Card Data Model

**User Story:** As a developer, I want a comprehensive card data model, so that cards can express the full range of gameplay mechanics defined in the GDD.

#### Acceptance Criteria

1. THE Card_Data SHALL define the following fields: card name, Overtime cost, description, Card_Type, Card_Rarity, effect value, Target_Mode, and sprite reference.
2. THE Card_Data SHALL support the following Card_Type values: Attack, Defense, Effect, Utility, and Special.
3. THE Card_Data SHALL support the following Card_Rarity values: Common, Rare, Epic, Legendary, and Unknown.
4. THE Card_Data SHALL support the following Target_Mode values: Single_Enemy, All_Enemies, Self, and No_Target.
5. THE Battle_Manager SHALL allow the player to target any entity in the encounter with any card, including Jean-Guy himself and allied NPCs (such as the worker in the Suicidal_Worker_Encounter). Target_Mode defines the default targeting behavior, but the player may override it to target any valid entity.
6. WHERE a card has Card_Type Defense, THE Card_Data SHALL include a block value field representing the amount of Block granted when played.
7. WHERE a card has Card_Type Effect, THE Card_Data SHALL include a status effect identifier and a duration field.
8. WHERE a card has Card_Type Utility, THE Card_Data SHALL include a utility effect type field specifying the sub-type: Draw, Restore, Retrieve, Reorder, or Heal.

### Requirement 5: Card Play and Effect Resolution

**User Story:** As a player, I want cards to resolve their effects correctly based on their type and target, so that combat feels responsive and predictable.

#### Acceptance Criteria

1. WHEN the player plays an Attack card with Target_Mode Single_Enemy, THE Battle_Manager SHALL deal the card's effect value as damage to the selected Enemy_Combatant.
2. WHEN the player plays an Attack card with Target_Mode All_Enemies, THE Battle_Manager SHALL deal the card's effect value as damage to every Enemy_Combatant in the Encounter.
3. WHEN the player plays a Defense card, THE Battle_Manager SHALL add the card's block value to the player's current Block total.
4. WHEN the player plays an Effect card, THE Battle_Manager SHALL apply the specified Status_Effect to the target for the specified duration.
5. WHEN a card is played, THE Battle_Manager SHALL move the Card_Instance from the Hand to the Discard_Pile after resolving the card's effect.
6. WHEN a card is played, THE Battle_Manager SHALL raise a CardPlayedEvent on the BattleEventBus containing the Card_Data, source, and target.

### Requirement 6: Block Mechanic

**User Story:** As a player, I want to use Defense cards to gain Block that absorbs incoming damage, so that I have a way to mitigate enemy attacks.

#### Acceptance Criteria

1. THE Battle_Manager SHALL track the player's Block as a non-negative integer, initialized to 0 at the start of each Encounter.
2. WHEN the player takes damage, THE Battle_Manager SHALL reduce the Block value first before reducing HP.
3. IF the incoming damage exceeds the current Block value, THEN THE Battle_Manager SHALL reduce HP by the remaining damage after Block is fully consumed.
4. WHEN a new player turn begins, THE Battle_Manager SHALL reset the player's Block to 0.
5. THE Battle_Manager SHALL track Block for each Enemy_Combatant using the same mechanic as player Block. Enemy Block absorbs incoming damage before HP, and resets to 0 at the start of that enemy's turn in the Enemy_Phase, regardless of whether the enemy's action is skipped due to Stun.

### Requirement 7: Deck Cycling

**User Story:** As a player, I want my deck to reshuffle when the draw pile is empty, so that I can keep drawing cards throughout a long fight.

#### Acceptance Criteria

1. WHEN the Draw_Pile is empty and the player needs to draw a card, THE Deck_Manager SHALL shuffle the Discard_Pile and move all cards into the Draw_Pile.
2. IF both the Draw_Pile and the Discard_Pile are empty when a draw is attempted, THEN THE Deck_Manager SHALL draw no card and the draw action SHALL be skipped.
3. THE Deck_Manager SHALL use a Fisher-Yates shuffle algorithm when shuffling the Discard_Pile into the Draw_Pile.
4. THE Deck_Manager SHALL preserve the total number of cards across the Draw_Pile, Hand, and Discard_Pile throughout the Encounter.

### Requirement 8: Multi-Enemy Encounters

**User Story:** As a player, I want to fight multiple enemies at once on some floors, so that combat encounters have variety and tactical depth.

#### Acceptance Criteria

1. THE Battle_Manager SHALL support Encounters containing between 1 and 4 Enemy_Combatants.
2. THE Level_Generator SHALL support pre-defined multi-enemy encounter groups placed in specific rooms. When the player triggers a grouped encounter, all enemies in that group enter the same battle. Individual roaming enemies always trigger 1v1 encounters.
3. WHILE an Encounter has multiple Enemy_Combatants, THE Battle_Manager SHALL display all Enemy_Combatants on the battle screen simultaneously.
4. WHEN the player plays a card with Target_Mode Single_Enemy during a multi-enemy Encounter, THE CardTargetingManager SHALL require the player to select one Enemy_Combatant as the target.
5. WHEN the player plays a card with Target_Mode All_Enemies, THE Battle_Manager SHALL apply the card's effect to each Enemy_Combatant in the Encounter.
6. WHEN an Enemy_Combatant's HP reaches 0, THE Battle_Manager SHALL remove that Enemy_Combatant from the Encounter and play a death animation.
7. WHEN all Enemy_Combatants in an Encounter reach 0 HP, THE Battle_Manager SHALL end the Encounter as a player victory.
8. WHILE an Enemy_Combatant has 0 HP, THE Battle_Manager SHALL skip that Enemy_Combatant during the Enemy_Phase.

### Requirement 9: Enemy Turn Execution

**User Story:** As a player, I want enemies to execute their actions in a visible sequence, so that I can understand what happened during the enemy turn.

#### Acceptance Criteria

1. WHEN the Enemy_Phase begins, THE Battle_Manager SHALL execute each living Enemy_Combatant's action sequentially with a visible delay between actions.
2. WHEN an Enemy_Combatant deals damage to the player, THE Battle_Manager SHALL reduce the player's Block first, then HP for any remaining damage.
3. WHEN an Enemy_Combatant deals damage, THE Battle_Manager SHALL play an attack dash animation from the Enemy_Combatant toward the player and a hit shake animation on the player.
4. WHEN an Enemy_Combatant deals damage, THE Battle_Manager SHALL raise a DamageEvent on the BattleEventBus.
5. IF the player's HP reaches 0 during the Enemy_Phase, THEN THE Battle_Manager SHALL immediately end the Encounter as a player defeat and trigger the run reset.

### Requirement 10: Status Effects

**User Story:** As a player, I want status effects to modify combat over multiple turns, so that Effect cards and enemy abilities create strategic depth.

#### Acceptance Criteria

1. THE Battle_Manager SHALL maintain a list of active Status_Effects for the player and for each Enemy_Combatant.
2. WHEN a Status_Effect is applied to a target that already has the same Status_Effect, THE Battle_Manager SHALL refresh the duration to the new value rather than stacking a second instance.
3. WHEN a turn ends, THE Battle_Manager SHALL decrement the duration of each active Status_Effect by 1 and remove Status_Effects whose duration reaches 0.
4. WHEN a Burn Status_Effect is active on a target, THE Battle_Manager SHALL deal the Burn's damage value to that target at the start of that target's turn.
5. WHEN a Stun Status_Effect is active on an Enemy_Combatant, THE Battle_Manager SHALL skip that Enemy_Combatant's action during the Enemy_Phase.
6. WHEN a Bleed Status_Effect is active on a target, THE Battle_Manager SHALL deal additional damage equal to the Bleed's value each time that target takes damage from any source, including status effect ticks such as Burn.
7. WHEN a Stun Status_Effect is active on the player, THE Battle_Manager SHALL skip the Play_Phase for that turn — the player draws cards during the Draw_Phase but cannot play any cards, and the turn automatically advances to the Discard_Phase.
8. WHEN a Status_Effect is applied, THE Battle_Manager SHALL raise a StatusEffectEvent on the BattleEventBus.
9. WHEN a Status_Effect is removed, THE Battle_Manager SHALL raise a StatusEffectEvent on the BattleEventBus indicating removal.
10. THE Battle_Manager SHALL display active Status_Effect icons in a vertical stack behind each affected entity's sprite (behind the player sprite for player effects, behind each Enemy_Combatant sprite for enemy effects), with each icon showing the remaining duration number.
11. WHEN an Encounter ends (victory or defeat), THE Battle_Manager SHALL clear all active Status_Effects from the player. Enemy Status_Effects are implicitly cleared when Enemy_Combatants are destroyed.

### Requirement 11: Battle Transition

**User Story:** As a player, I want combat to start seamlessly when I encounter an enemy during exploration, so that the transition feels smooth.

#### Acceptance Criteria

1. WHEN the player collides with an enemy trigger in the exploration scene, THE SceneLoader SHALL load the battle scene with the Encounter data for that enemy.
2. WHEN the battle scene loads, THE Battle_Manager SHALL initialize all Enemy_Combatants, the player's HP, the Overtime_Meter, the Draw_Pile from the player's current deck, and the Block to 0.
3. WHEN an Encounter ends in player victory, THE SceneLoader SHALL return to the exploration scene with the defeated enemy removed and the player at the pre-battle position.
4. WHEN an Encounter ends in player defeat, THE SceneLoader SHALL trigger a full run reset, returning the player to the starting state.

### Requirement 12: Card Targeting and Selection UI

**User Story:** As a player, I want to select cards and choose targets intuitively, so that I can execute my strategy without UI friction.

#### Acceptance Criteria

1. WHILE the Turn_Phase is Play_Phase, THE CardTargetingManager SHALL allow the player to hover over cards to preview them and click to select a card.
2. WHEN the player hovers over a card, THE CardInteractionHandler SHALL display a tooltip showing the calculated effective values (damage after Rage Burst bonus and Tool modifiers, block after Tool modifiers, draw count, etc.) so the player can see the real output before committing. The tooltip reflects source-side modifiers only and does not account for target-specific effects such as Bleed, since the target has not been selected yet.
3. WHEN a card with Target_Mode Single_Enemy is selected, THE CardTargetingManager SHALL highlight valid Enemy_Combatant targets with an outline and wait for the player to click a target.
4. WHEN a card with Target_Mode Self or No_Target is selected, THE Battle_Manager SHALL play the card immediately without requiring target selection.
5. WHEN a card with Target_Mode All_Enemies is selected, THE CardTargetingManager SHALL highlight all Enemy_Combatants and play the card on confirmation click.
6. WHEN the player right-clicks or presses Escape while a card is selected, THE CardTargetingManager SHALL cancel the selection and return the card to the Hand.
7. THE CardInteractionHandler SHALL prevent card hover and selection interactions when the Turn_Phase is not Play_Phase.

### Requirement 13: HP and Resource UI Display

**User Story:** As a player, I want to see my HP, Overtime meter, Block, Overflow, and enemy HP at all times during combat, so that I can make informed decisions.

#### Acceptance Criteria

1. THE Battle_Manager SHALL display the player's current HP and maximum HP using the PlayerHPStack UI component.
2. THE Battle_Manager SHALL display the current Overtime_Meter value and maximum capacity in a visible UI element.
3. THE Battle_Manager SHALL display the current Overflow_Buffer value when the Overflow_Buffer is greater than 0.
4. THE Battle_Manager SHALL display the player's current Block value when Block is greater than 0.
5. THE Battle_Manager SHALL display each Enemy_Combatant's current HP and maximum HP using an EnemyHPBar UI component.
6. THE Battle_Manager SHALL display the current Draw_Pile count and Discard_Pile count.
7. THE Battle_Manager SHALL display the current turn number at the top-center of the battle screen.
8. WHEN the player clicks the Draw_Pile counter, THE Battle_Manager SHALL display a scrollable list of all cards remaining in the Draw_Pile (sorted alphabetically, without revealing draw order).
9. WHEN the player clicks the Discard_Pile counter, THE Battle_Manager SHALL display a scrollable list of all cards currently in the Discard_Pile.

### Requirement 14: Starting Deck Composition

**User Story:** As a player, I want to choose 8 cards at the start of a run, so that I have agency over my initial strategy.

#### Acceptance Criteria

1. WHEN a new run begins, THE Battle_Manager SHALL present the player with a selection of pre-built starting deck sets, each containing 8 cards with a distinct strategic identity.
2. THE Battle_Manager SHALL display the starting deck selection as a carousel view: the current deck set's name is shown at the top, all 8 cards are displayed below with their full card details (name, cost, type, effect, description), and left/right arrow buttons allow the player to browse between deck sets.
3. WHEN the player clicks the Select button at the bottom-center of the carousel, THE Battle_Manager SHALL initialize the Draw_Pile with the 8 cards from the selected set at the start of the first Encounter of a run.
4. THE Deck_Manager SHALL support adding new cards to the deck between Encounters during a run, up to the current maximum deck size (default 25, increased by the Filing Cabinet hub upgrade).
5. IF the player attempts to add a card that would exceed the maximum deck size (via Work_Box Keep, Bathroom_Shop purchase, or Break_Room_Trade), THE system SHALL reject the addition and display a feedback message indicating the deck is full.

### Requirement 15: Tool (Relic) Integration

**User Story:** As a player, I want passive Tool items to modify battle mechanics, so that collected relics feel impactful during combat.

#### Acceptance Criteria

1. THE Battle_Manager SHALL query the player's active Tool inventory at the start of each Encounter and apply passive modifiers.
2. WHERE the player has a Tool that modifies Overtime regeneration, THE Overtime_Meter SHALL adjust the per-turn regeneration value accordingly.
3. WHERE the player has a Tool that modifies Block, THE Battle_Manager SHALL adjust Block values granted by Defense cards accordingly.
4. WHERE the player has a Tool that modifies hand size, THE Battle_Manager SHALL adjust the base hand size for the Draw_Phase accordingly.
5. WHEN a Tool modifier affects a combat value, THE Battle_Manager SHALL reflect the modified value in the UI display.

### Requirement 16: Encounter Victory Rewards

**User Story:** As a player, I want to receive Hours currency after winning a battle, so that my run progresses and I can spend currency at shops.

#### Acceptance Criteria

1. WHEN an Encounter ends in player victory, THE Battle_Manager SHALL award the player Hours currency equal to the sum of each defeated Enemy_Combatant's individual Hours reward value.
2. WHEN a boss Encounter ends in player victory, THE Battle_Manager SHALL award the player Bad_Reviews meta-currency in addition to Hours.
3. WHEN an Encounter ends in player victory, THE Battle_Manager SHALL NOT offer card rewards. Cards are obtained exclusively through Work_Boxes, Bathroom_Shops, and Break_Room_Trades.
4. WHEN an Encounter ends in player victory, THE Battle_Manager SHALL display a victory splash screen showing a randomized victory verb (e.g., "Defeated", "Vanquished", "Dealt with", "Showed the door to") followed by the enemy name(s), and below it the Hours earned (and Bad_Reviews if a boss). The splash SHALL dismiss on player click or after a short delay.

### Requirement 17: Card Animation and Visual Feedback

**User Story:** As a player, I want cards to animate smoothly when drawn, played, and discarded, so that combat feels polished and readable.

#### Acceptance Criteria

1. WHEN cards are drawn during the Draw_Phase, THE CardAnimator SHALL play an entrance animation for each card with a staggered delay.
2. WHEN a card is played, THE CardAnimator SHALL play an exit animation moving the card toward the battlefield before resolving the effect.
3. WHEN a card play is rejected due to insufficient Overtime, THE CardAnimator SHALL play a horizontal shake rejection animation on the card.
4. WHEN an Attack card resolves, THE BattleAnimations SHALL play an attack dash animation from the attacker toward the target followed by a hit shake on the target.
5. WHEN an Enemy_Combatant is defeated, THE BattleAnimations SHALL play a death animation consisting of a backward tip and a downward sink.
6. WHEN the player takes damage exceeding 20% of maximum HP in a single hit, THE BattleAnimations SHALL play a screen shake effect proportional to the damage percentage.
7. WHEN a Rage_Burst activates (Overflow_Buffer consumed on an Attack card), THE BattleAnimations SHALL play a screen pulse or flash effect to emphasize the bonus damage.
8. WHILE the Overflow_Buffer is greater than 0, THE BattleAnimations SHALL apply a subtle visual indicator (such as a screen edge glow or color tint) to signal that Rage Burst is charged.

### Requirement 17a: Floating Combat Text

**User Story:** As a player, I want to see floating numbers when damage is dealt, block is gained, and resources change, so that I can quickly read what's happening without checking UI bars.

#### Acceptance Criteria

1. WHEN damage is dealt to any target, THE BattleAnimations SHALL display a floating damage number at the target's position that drifts upward and fades out.
2. WHEN Block is gained, THE BattleAnimations SHALL display a floating block number near the player's Block display.
3. WHEN Overtime points are spent, THE BattleAnimations SHALL display a floating cost number near the Overtime_Meter that drifts downward and fades out.
4. WHEN a Status_Effect is applied, THE BattleAnimations SHALL display a floating text label with the effect name at the target's position.
5. WHEN healing occurs, THE BattleAnimations SHALL display a floating heal number in a distinct color (green) at the target's position.
6. THE floating combat text SHALL use a visual style distinct from other deck-builder games, consistent with the office-supply pixel art aesthetic of Overtime.

### Requirement 18: First-Person Hands During Exploration

**User Story:** As a player, I want to see Jean-Guy's fists at the bottom of the screen during exploration, so that the first-person view feels grounded and immersive in the classic Doom/Fallout style.

#### Acceptance Criteria

1. WHILE the player is in the exploration scene, THE First_Person_Hands_Controller SHALL render two 2D pixel art hand sprites overlaid on the camera view, positioned at the bottom-left and bottom-right of the screen.
2. THE First_Person_Hands_Controller SHALL render the hand sprites using the same pixel art style and resolution as other 2D sprites in the game world.
3. WHILE the player is standing still or walking at normal speed, THE First_Person_Hands_Controller SHALL display the hands in a relaxed idle pose with a subtle bob animation synchronized to the player's movement speed.
4. WHILE the player is sprinting, THE First_Person_Hands_Controller SHALL play a pumping bob animation on the hand sprites to convey running speed.
5. WHEN the player interacts with an object (work box, toilet, NPC, or other interactable), THE First_Person_Hands_Controller SHALL play a reach-forward interaction animation on one hand sprite.
6. WHEN the interaction animation completes, THE First_Person_Hands_Controller SHALL return the hand sprites to the idle pose.
7. WHILE the player is not moving, THE First_Person_Hands_Controller SHALL play a subtle idle breathing animation on the hand sprites.
8. WHEN the player transitions from the exploration scene to the battle scene, THE First_Person_Hands_Controller SHALL hide the hand sprites before the scene transition begins.

### Requirement 19: Utility and Special Card Resolution

**User Story:** As a player, I want Utility and Special cards to resolve their unique effects, so that my deck can include cards beyond simple attack and defense.

#### Acceptance Criteria

1. WHEN the player plays a Utility card with a draw effect, THE Battle_Manager SHALL draw the specified number of additional cards from the Draw_Pile into the Hand.
2. WHEN the player plays a Utility card with a restore effect, THE Battle_Manager SHALL add the specified number of Overtime points to the Overtime_Meter, capped at the maximum capacity. Any excess points beyond the maximum capacity SHALL be routed to the Overflow_Buffer, consistent with damage-based Overtime gain.
3. WHEN the player plays a Utility card with a retrieve effect, THE Battle_Manager SHALL return N randomly selected cards from the Discard_Pile to the Hand, where N is the minimum of the card's effect value and the current Discard_Pile size. The player does not choose which cards are retrieved.
4. WHEN the player plays a Utility card with a reorder effect, THE Battle_Manager SHALL allow the player to view and rearrange the top N cards of the Draw_Pile where N is the minimum of the card's effect value and the current Draw_Pile size.
5. WHEN the player plays a Utility card with a heal effect, THE Battle_Manager SHALL restore the card's effect value as HP to the target, capped at the target's maximum HP.
6. WHEN the player plays a Special card, THE Battle_Manager SHALL execute the effect logic defined by that Special card's unique identifier.
7. THE Battle_Manager SHALL support registering custom effect logic for each Special card type so that new Special cards can be added without modifying the core resolution pipeline.
8. WHEN a Utility or Special card is played, THE Battle_Manager SHALL move the Card_Instance from the Hand to the Discard_Pile after resolving the card's effect.

### Requirement 20: Enemy Intent Display

**User Story:** As a player, I want to see what each enemy plans to do next turn, so that I can make informed decisions about blocking versus attacking.

#### Acceptance Criteria

1. WHEN an Encounter begins, THE Battle_Manager SHALL determine each Enemy_Combatant's first action based on that enemy's attack pattern and store the action as the Enemy_Intent.
2. WHILE the Turn_Phase is Play_Phase, THE Battle_Manager SHALL display an Enemy_Intent icon above each living Enemy_Combatant's sprite indicating the planned action type (attack, defend, buff, or special). The intent SHALL update in real-time as conditions change during the player's turn (e.g., if the player deals damage that pushes an enemy below an HP threshold, the displayed intent updates immediately to reflect the conditional pattern change).
3. WHERE an Enemy_Combatant's Enemy_Intent is an attack action, THE Battle_Manager SHALL display the intended damage number alongside the Enemy_Intent icon.
4. WHEN an Enemy_Phase completes, THE Battle_Manager SHALL update each living Enemy_Combatant's Enemy_Intent to reflect the action for the following turn.
5. WHEN an Enemy_Combatant is defeated, THE Battle_Manager SHALL remove that Enemy_Combatant's Enemy_Intent display.

### Requirement 21: Work Box (Chest) System

**User Story:** As a player, I want to find and open work boxes under desks to discover new cards with a rarity reveal mechanic, so that exploration rewards feel exciting and suspenseful.

#### Acceptance Criteria

1. THE Level_Generator SHALL spawn Work_Box objects only under work desk furniture within generated rooms.
2. THE Level_Generator SHALL assign Work_Box sizes using floor-based spawn rates: Floors 1-5 produce 100% Small; Floors 6-10 produce 90% Small and 10% Big; Floors 11 and above produce 70% Small, 25% Big, and 5% Huge.
3. THE Work_Box SHALL contain a number of cards based on size: Small contains 1 to 3 cards, Big contains 3 to 5 cards, and Huge contains 5 to 7 cards.
4. THE Work_Box SHALL assign card rarities using floor-based probability tables: Floors 1-3 use 70% Common, 20% Rare, 8% Epic, 2% Legendary, 0% Unknown; Floors 3-6 use 50% Common, 30% Rare, 12% Epic, 8% Legendary, 0% Unknown; scaling up to Floors 25 and above using 0% Common, 0% Rare, 1% Epic, 69% Legendary, 30% Unknown.
5. WHEN the player interacts with an unopened Work_Box, THE Work_Box SHALL play a shake animation before opening.
6. WHEN the Work_Box opens, THE Work_Box SHALL display an inventory screen showing all contained cards face-down as grey tiles.
7. WHEN the player clicks a grey tile, THE Work_Box SHALL begin the rarity reveal sequence: if the card is Common the tile reveals immediately; if the card is Rare or higher the tile shifts to blue; if the card is Epic or higher the tile shifts from blue to orange; if the card is Legendary or higher the tile shifts from orange to red; if the card is Unknown the tile shifts to black. Each click advances one step and triggers an animation, and the player must wait for the animation to complete before clicking again. The tile stops changing color at the card's true rarity.
8. WHEN the player clicks a tile that has reached its true rarity color, THE Work_Box SHALL reveal the full card detail including art, name, cost, and effect, and display a Keep button on the right and a Leave button on the left.
9. WHEN the player clicks Keep, THE Work_Box SHALL add the card to the player's deck at no cost.
10. WHEN the player clicks Leave, THE Work_Box SHALL discard the card and it is no longer available.
11. WHEN the player revisits a previously opened Work_Box, THE Work_Box SHALL skip the shake animation, display all remaining cards immediately with their true rarity colors visible, and skip the rarity reveal clicking sequence.
12. THE player SHALL be able to walk away from an open Work_Box at any time without keeping or leaving all cards. Unrevealed or undecided cards remain in the Work_Box for later revisit per criterion 11.

### Requirement 22: Bathroom Shop System

**User Story:** As a player, I want to visit bathroom shops to buy cards, purchase Tools, and remove unwanted cards from my deck, so that I can refine my build during a run.

#### Acceptance Criteria

1. THE Level_Generator SHALL place bathroom rooms on every floor, with a subset of bathrooms containing a Bathroom_Shop.
2. WHEN the player enters a Bathroom_Shop, THE Bathroom_Shop SHALL display available cards and Tools with their Hours prices.
3. WHEN the player purchases a card from the Bathroom_Shop, THE Bathroom_Shop SHALL deduct the card's Hours cost from the player's Hours balance and add the card to the player's deck.
4. IF the player attempts to purchase an item with insufficient Hours, THEN THE Bathroom_Shop SHALL reject the purchase and display a feedback message indicating insufficient Hours.
5. WHEN the player interacts with the toilet in a Bathroom_Shop, THE Bathroom_Shop SHALL display the player's current deck and allow the player to select one card to remove permanently from the deck for the current run at a cost in Hours.
6. WHEN the player confirms a card removal via the toilet, THE Bathroom_Shop SHALL deduct the removal cost in Hours and permanently remove the selected card from the player's deck for the remainder of the run.
7. THE Bathroom_Shop SHALL allow only one card removal per bathroom visit. After one removal, the toilet interaction SHALL be disabled for that bathroom.
8. THE Bathroom_Shop SHALL NOT allow card removal if the player's deck contains only 1 card. The minimum deck size is 1.
9. WHERE a Bathroom_Shop has Tools available, THE Bathroom_Shop SHALL display the Tools with their Hours prices and allow the player to purchase them.

### Requirement 23: Break Room NPC Trades

**User Story:** As a player, I want to encounter NPCs in break rooms who offer direct item trades, so that I have additional opportunities to improve my deck without spending currency.

#### Acceptance Criteria

1. THE Level_Generator SHALL place break rooms once every 2 floors, with a subset of break rooms containing trade NPCs.
2. WHEN the player interacts with a trade NPC, THE Break_Room_Trade SHALL display the offered trade showing the item the NPC wants and the item the NPC offers in return. The NPC SHALL only offer trades the player can fulfill based on their current inventory.
3. THE Break_Room_Trade SHALL support card-for-card and item-for-item trade types with no currency cost.
4. THE Break_Room_Trade SHALL generate trades that are either equal in value or unfavorable to the player — the NPCs are coworkers looking out for themselves, not offering charity. The player may occasionally get a fair deal, but never a clearly advantageous one.
5. WHEN the player accepts a trade, THE Break_Room_Trade SHALL remove the requested item from the player's inventory and add the offered item.
6. WHEN the player declines a trade, THE Break_Room_Trade SHALL close the trade interface with no penalty to the player.
7. THE Break_Room_Trade SHALL offer trades that are thematically consistent with the office-supply theme of the game.

### Requirement 24: Run State and Persistence

**User Story:** As a player, I want my in-run progress to reset on death while keeping meta-progression currency, so that each run feels fresh but long-term progress is preserved.

#### Acceptance Criteria

1. WHEN the player dies, THE Battle_Manager SHALL wipe all in-run state including the current deck, Hours balance, collected Tools, and all temporary modifiers.
2. THE Battle_Manager SHALL preserve the player's Bad_Reviews balance across deaths.
3. THE Battle_Manager SHALL preserve hub office upgrade state across deaths.
4. WHEN a new run begins, THE Battle_Manager SHALL initialize the player with zero Hours, no Tools, and prompt the player to choose a new 8-card starting deck.
5. WHEN the player defeats an Enemy_Combatant, THE Battle_Manager SHALL award Hours currency to the player.
6. WHEN the player defeats a boss Enemy_Combatant, THE Battle_Manager SHALL award Bad_Reviews meta-currency to the player.
7. THE Battle_Manager SHALL use Hours exclusively for Bathroom_Shop purchases and toilet card removal during a run.
8. THE Battle_Manager SHALL use Bad_Reviews exclusively for hub office upgrades, which can be purchased at any time but only take effect from the next run onward.

### Requirement 25: Boss Encounter Differentiation

**User Story:** As a player, I want boss encounters to feel distinct from normal encounters with unique dialogue and rewards, so that floor milestones are memorable.

#### Acceptance Criteria

1. THE Level_Generator SHALL place a boss Encounter every 3 floors at floors 3, 6, 9, and so on.
2. WHEN a boss Encounter begins, THE Battle_Manager SHALL display pre-fight dialogue from the boss before combat starts.
3. WHEN a boss Encounter ends in player victory, THE Battle_Manager SHALL display post-fight dialogue from the boss.
4. WHILE the current floor is 9 or below, THE Battle_Manager SHALL present boss dialogue written in corporate business language.
5. WHILE the current floor is 12 or above, THE Battle_Manager SHALL present boss dialogue written in an unnatural and unsettling tone.
6. WHEN a boss Encounter ends in player victory, THE Battle_Manager SHALL award Bad_Reviews meta-currency to the player.
7. THE Encounter data model SHALL include a flag distinguishing boss Encounters from normal Encounters.

### Requirement 26: Enemy Attack Pattern System

**User Story:** As a player, I want enemies to follow defined attack patterns rather than acting randomly, so that I can learn enemy behavior and plan my strategy.

#### Acceptance Criteria

1. THE Enemy_Combatant SHALL define an attack pattern as an ordered sequence of actions including attack, defend, buff, and special action types.
2. THE Enemy_Combatant SHALL execute actions from the attack pattern in order, cycling back to the beginning when the sequence completes.
3. WHERE an Enemy_Combatant executes a Defend action, THE Enemy_Combatant SHALL gain Block equal to the action's value. Enemy Block absorbs incoming damage before HP, consistent with the player Block mechanic (Req 6.5).
4. WHERE an Enemy_Combatant executes a Buff action, THE Enemy_Combatant SHALL apply a temporary modifier to itself (e.g., increased damage on next attack, or a status effect on itself such as a damage shield). The specific buff effect is defined per-enemy in the EnemyCombatantData.
5. WHERE an Enemy_Combatant has conditional logic in the attack pattern, THE Enemy_Combatant SHALL evaluate the condition each turn and select the appropriate action.
6. WHILE the current floor is in the early range of floors 1 through 5, THE Level_Generator SHALL weight enemy type selection toward coworker variant Enemy_Combatants.
7. WHILE the current floor is in the deep range of floors 11 and above, THE Level_Generator SHALL weight enemy type selection toward creature variant Enemy_Combatants.
8. THE Level_Generator SHALL assign more complex attack patterns to Enemy_Combatants on deeper floors.

### Requirement 27: Cutscene and Narrative Event Persistence

**User Story:** As a player, I want cutscenes and narrative events to only play once per run, so that reloading a saved game mid-run does not replay them.

#### Acceptance Criteria

1. WHEN the player starts a new run, THE Save_Manager SHALL reset all cutscene-seen flags for that run.
2. WHEN a narrative cutscene plays (opening boss dialogue, death screen, win cinematic, or the suicidal worker encounter), THE Save_Manager SHALL mark that cutscene as seen for the current run and persist the flag to the save file.
3. WHEN the player loads a saved game mid-run, THE Save_Manager SHALL check the cutscene-seen flags and skip any cutscene that has already been marked as seen.
4. WHEN the player begins a new run after death, THE Save_Manager SHALL clear all run-scoped cutscene-seen flags so that narrative events replay on the new run.
5. THE Save_Manager SHALL persist the current run state (floor, HP, deck, Hours, Tools, cutscene-seen flags) so that the player can quit and resume mid-run without losing progress.
6. WHEN the player quits mid-run and relaunches the game, THE Save_Manager SHALL restore the player to the exact floor and state they were in, skipping the opening dialogue and deck selection since those are run-start events.
7. WHEN the player quits mid-combat (during an active Encounter), THE Save_Manager SHALL save the run state as it was at the start of that Encounter (pre-combat snapshot). When the player resumes, they SHALL be placed back in the exploration scene at the pre-battle position and the enemy that triggered the encounter SHALL still be present, so the encounter restarts from the beginning if triggered again.

### Requirement 28: Hub Office Upgrade System

**User Story:** As a player, I want to visit Jean-Guy's personal office hub to spend Bad_Reviews on permanent upgrades, so that I feel long-term progression across runs.

#### Acceptance Criteria

1. THE Hub_Office SHALL be accessible from the main menu and during a run, but upgrades purchased during a run SHALL apply only from the next run onward.
2. THE Hub_Office SHALL render as a 2D diorama-style scene with 3D depth, using cursor-based interaction only with no WASD movement.
3. WHEN the player hovers over a furniture item in the Hub_Office, THE Hub_Office SHALL display the available upgrade options and their Bad_Reviews costs for that item.
4. WHEN the player clicks a furniture item with a purchasable upgrade, THE Hub_Office SHALL deduct the Bad_Reviews cost and apply the upgrade to the player's meta-progression state.
5. IF the player attempts to purchase an upgrade with insufficient Bad_Reviews, THEN THE Hub_Office SHALL reject the purchase and display a feedback message indicating insufficient Bad_Reviews.
6. WHERE the Computer upgrade is purchased, THE Hub_Office SHALL increase damage dealt by Technology-themed cards by 1 per upgrade level starting from the next run.
7. WHERE the Coffee Machine upgrade is purchased, THE Hub_Office SHALL increase the Overtime_Meter per-turn regeneration value starting from the next run.
8. WHERE the Desk Chair upgrade is purchased, THE Hub_Office SHALL increase the block value granted by Defense cards by 1 per upgrade level starting from the next run.
9. WHERE the Filing Cabinet upgrade is purchased, THE Hub_Office SHALL increase the starting hand size at early upgrade levels and increase the maximum deck size at later upgrade levels, with effects applying from the next run onward.
10. WHERE the Plant upgrade is purchased, THE Hub_Office SHALL increase Jean-Guy's base HP at early upgrade levels and add passive healing per floor at later upgrade levels, with effects applying from the next run onward. Passive healing triggers when the player uses the floor exit (stairs or elevator), not on floor entry.
11. THE Hub_Office SHALL visually update the appearance of furniture items as upgrades are purchased, reflecting the current upgrade level.
12. THE Hub_Office SHALL support multiple upgrade levels per furniture item, with each successive level requiring a higher Bad_Reviews cost.
13. WHERE the Whiteboard upgrade is purchased, THE Hub_Office SHALL unlock a floor minimap during exploration starting from the next run. The minimap reveals rooms the player has visited and shows icons for key room types (bathroom, break room, boss room, exit). Higher upgrade levels reveal unvisited rooms progressively.

### Requirement 29: Opening Dialogue Choice

**User Story:** As a player, I want the game to begin with a dialogue choice about overtime, so that the narrative premise is established and a joke ending is available.

#### Acceptance Criteria

1. WHEN a new run begins, THE Battle_Manager SHALL display an opening dialogue scene where the boss leans over the cubicle and asks Jean-Guy about overtime.
2. THE Battle_Manager SHALL present two dialogue choices: YES (Accept overtime) and NO (Refuse overtime).
3. WHEN the player selects YES, THE Battle_Manager SHALL fade the screen to black, display white text reading "YOU WORKED OVERTIME", and roll credits as a joke ending. After the credits finish, a "Back to Menu" button SHALL appear, allowing the player to return to the Main_Menu.
4. WHEN the player selects NO, THE Battle_Manager SHALL play an animation of Jean-Guy standing up, proceed to the run start with deck selection, and after deck selection transition to the Hub_Office so the player can review or purchase upgrades before starting the run.
5. WHEN the player loads a saved mid-run game, THE Battle_Manager SHALL skip the opening dialogue and resume the run at the saved state.

### Requirement 30: Death Screen

**User Story:** As a player, I want a death screen that reinforces the office horror premise when Jean-Guy reaches 0 HP, so that dying feels narratively meaningful rather than arbitrary.

#### Acceptance Criteria

1. WHEN Jean-Guy's HP reaches 0, THE Battle_Manager SHALL trigger a death screen sequence showing Jean-Guy being dragged back to his desk.
2. THE Battle_Manager SHALL display the death screen with imagery and tone reinforcing the office horror premise that Jean-Guy cannot escape by dying and simply returns to work.
3. WHEN the death screen sequence completes, THE Battle_Manager SHALL wipe all in-run state including the current deck, Hours balance, collected Tools, and all temporary modifiers.
4. THE Battle_Manager SHALL preserve the player's Bad_Reviews balance and hub office upgrade state through the death sequence.
5. WHEN the death screen sequence completes, THE Battle_Manager SHALL proceed to the Game_Over_Screen defined in Requirement 36.

### Requirement 31: Win Cinematic

**User Story:** As a player, I want a quiet cinematic after defeating the final boss showing Jean-Guy returning home, so that the ending feels earned and human.

#### Acceptance Criteria

1. WHEN the player defeats the final boss on the final floor, THE Battle_Manager SHALL spawn a door object in the boss room. The final floor number is a configurable constant (default 75, must be a multiple of 3 to align with boss floor placement).
2. WHEN the player interacts with the door, THE Battle_Manager SHALL trigger the win cinematic sequence.
3. THE Battle_Manager SHALL play a cinematic showing Jean-Guy walking out of the office building, arriving home, and seeing his wife and child.
4. THE Battle_Manager SHALL present the win cinematic with a quiet and human tone, without dialogue or triumphant fanfare.
5. WHEN the win cinematic completes, THE Battle_Manager SHALL return the player to the Main_Menu.

### Requirement 32: Suicidal Worker Encounter (Floor 5)

**User Story:** As a player, I want a unique non-combat encounter on floor 5 where I must intervene to save a coworker, so that the game explores its darker themes with emotional weight.

#### Acceptance Criteria

1. THE Level_Generator SHALL place exactly one Suicidal_Worker_Encounter per run, on floor 5 only.
2. THE Suicidal_Worker_Encounter SHALL present the worker as a non-hostile NPC who does not chase or attack the player.
3. WHEN the player encounters the worker, THE Suicidal_Worker_Encounter SHALL enter a special turn-based encounter view where the player uses their normal run deck and hand (same draw pile, discard pile, and Overtime Meter as regular combat) and acts first each turn.
4. THE Suicidal_Worker_Encounter SHALL initialize the worker with a small HP pool (10-15 HP). The worker deals fixed self-damage at the end of each turn after the player's action.
5. WHILE the Suicidal_Worker_Encounter is active and the player has not intervened, THE Suicidal_Worker_Encounter SHALL show the worker harming himself at the end of each turn after the player's action, reducing the worker's HP.
6. WHEN the player plays a Defense card targeting the worker, THE Suicidal_Worker_Encounter SHALL apply Block to the worker, absorbing the worker's self-damage, interrupting the self-harm, de-escalating the situation, and resolving the encounter as a success (Shield resolution).
7. WHEN the player causes Jean-Guy to take self-damage before the worker acts in a given turn (e.g., by playing an Attack card targeting Jean-Guy via the free targeting system in Req 4.5), THE Suicidal_Worker_Encounter SHALL cause the worker to question his actions and walk away, resolving the encounter as a success (Empathy resolution).
8. IF the worker's HP reaches 0 from self-harm before the player intervenes, THEN THE Suicidal_Worker_Encounter SHALL resolve as a failure with the worker dying, awarding no reward, and playing a narrative consequence.
9. WHEN the Suicidal_Worker_Encounter resolves via Shield resolution, THE Suicidal_Worker_Encounter SHALL award the player a specific Tool reward distinct from the Empathy resolution reward.
10. WHEN the Suicidal_Worker_Encounter resolves via Empathy resolution, THE Suicidal_Worker_Encounter SHALL award the player a different Tool reward distinct from the Shield resolution reward.
11. THE Suicidal_Worker_Encounter SHALL treat animation, sound design, and dialogue with the highest level of care as the emotional core of the game's darker themes.

### Requirement 33: Procedural Floor Generation

**User Story:** As a player, I want each floor to be procedurally generated with context-aware spawn rules, so that every run feels different while maintaining structural consistency.

#### Acceptance Criteria

1. THE Level_Generator SHALL procedurally generate each floor per run so that no two runs produce identical floor layouts.
2. THE Level_Generator SHALL spawn Work_Box objects only under work desk furniture within generated rooms.
3. THE Level_Generator SHALL place bathroom rooms on every floor, with a subset of bathrooms containing a Bathroom_Shop.
4. THE Level_Generator SHALL place break rooms once every 2 floors, with a subset of break rooms containing trade NPCs.
5. THE Level_Generator SHALL weight enemy type selection by floor depth, favoring coworker variant Enemy_Combatants on floors 1 through 5 and creature variant Enemy_Combatants on floors 11 and above.
6. THE Level_Generator SHALL place a boss Encounter room every 3 floors as a fixed anchor point in the floor layout.
7. THE Level_Generator SHALL place exactly one Suicidal_Worker_Encounter on floor 5 in every run.
8. THE Level_Generator SHALL support the following room types: Office floor (present on every floor, containing enemies, desks, and Work_Boxes), Bathroom (present on every floor, containing a toilet and an optional Bathroom_Shop), Break room (present every 2 floors, containing an optional trade NPC), and Boss room (present every 3 floors, containing a boss Encounter and boss currency reward).
9. THE Level_Generator SHALL use constrained procedural generation rather than purely random placement, ensuring spawn rules and room type frequencies are respected.

### Requirement 34: Battle Scene Background

**User Story:** As a player, I want the 3D office environment to remain visible behind the 2D battle UI during combat, so that I maintain a sense of place and immersion.

#### Acceptance Criteria

1. WHEN combat is triggered, THE Battle_Manager SHALL snap the view to a 2D battle screen while keeping the 3D exploration environment visible in the background.
2. THE Battle_Manager SHALL render the 3D exploration scene as a blurred or dimmed background behind the 2D battle UI elements to maintain focus on the battle.
3. THE Battle_Manager SHALL display the card hand, player HP, Overtime_Meter, Block, and Enemy_Combatant stats as 2D UI elements overlaid on the background.
4. THE Battle_Manager SHALL render the background as a view of the 3D exploration scene from the position where combat was triggered.

### Requirement 35: Main Menu

**User Story:** As a player, I want a main menu with clear navigation options, so that I can start a new game, continue a saved run, adjust settings, and view achievements.

#### Acceptance Criteria

1. WHEN the game launches, THE Main_Menu SHALL be the first screen displayed to the player.
2. THE Main_Menu SHALL display the following options: New Game, Continue, Settings, Achievements, and Quit.
3. WHEN the player selects New Game, THE Main_Menu SHALL start a new run beginning with the opening dialogue choice.
4. WHEN the player selects Continue and a mid-run save exists, THE Main_Menu SHALL load the saved run state and resume the player at the exact floor and state they left off.
5. IF the player selects Continue and no mid-run save exists, THEN THE Main_Menu SHALL grey out or disable the Continue option.
6. WHEN the player selects Settings, THE Main_Menu SHALL open a settings panel with options for audio volume, display resolution, controls, and other configurable preferences.
7. WHEN the player selects Achievements, THE Main_Menu SHALL display a list of achievements with their unlock status and descriptions.
8. WHEN the player selects Quit, THE Main_Menu SHALL close the application.
9. THE Main_Menu SHALL be accessible after the death screen sequence and after the win cinematic completes.

### Requirement 36: Game Over Screen

**User Story:** As a player, I want a game over screen after dying that shows my run stats before resetting, so that I can reflect on how far I got.

#### Acceptance Criteria

1. WHEN the death screen sequence from Requirement 30 completes, THE Game_Over_Screen SHALL display a summary of the completed run.
2. THE Game_Over_Screen SHALL display the floor reached, enemies defeated, Hours earned during the run, and Bad_Reviews earned during the run.
3. THE Game_Over_Screen SHALL display a button to start a New Run and a button to return to the Main_Menu.
4. WHEN the player selects New Run, THE Game_Over_Screen SHALL proceed directly to the opening dialogue choice for a new run.
5. WHEN the player selects Main Menu, THE Game_Over_Screen SHALL return the player to the Main_Menu.

### Requirement 37: Enemy Exploration Behavior

**User Story:** As a player, I want enemies to roam the floor and chase me when I get close, so that exploration feels tense and encounters feel organic.

#### Acceptance Criteria

1. WHILE an Enemy_Combatant is in the exploration scene, THE Enemy_Combatant SHALL roam freely within the floor using patrol logic.
2. THE Enemy_Combatant SHALL NOT enter bathroom rooms or break rooms during roaming.
3. WHEN the player enters an aggressive Enemy_Combatant's proximity range, THE Enemy_Combatant SHALL begin chasing the player.
4. WHEN a chasing Enemy_Combatant reaches the entrance of a bathroom or break room while pursuing the player, THE Enemy_Combatant SHALL stop at the doorway and not enter. After a short timer (approximately 5 seconds), the Enemy_Combatant SHALL give up the chase and resume patrol behavior.
5. WHEN the player first interacts with an NPC in a bathroom or break room while enemies are present on the floor, THE NPC SHALL deliver a contextual line indicating the room is safe (e.g., "Relax, they can't reach you in here").
6. WHEN a chasing Enemy_Combatant makes contact with the player, THE SceneLoader SHALL trigger a combat Encounter with only that Enemy_Combatant. Other chasing enemies SHALL remain on the floor and resume patrol or chase behavior after the encounter ends.
7. THE Level_Generator SHALL support both aggressive Enemy_Combatants that chase on proximity and passive Enemy_Combatants that do not chase.

### Requirement 38: Floor Progression

**User Story:** As a player, I want to progress through floors with boss floors acting as mandatory gates, so that I can choose my battles on normal floors but must face every boss.

#### Acceptance Criteria

1. THE Level_Generator SHALL place an exit (stairs or elevator) on each floor that leads to the next floor.
2. ON non-boss floors, THE player SHALL be able to use the exit at any time, even if enemies remain alive on the floor.
3. ON boss floors (every 3 floors), THE player SHALL NOT be able to progress to the next floor until the boss is defeated. The player MAY freely explore the boss floor (visiting bathrooms, break rooms, and shops) before engaging the boss.
4. IF the player attempts to leave a boss floor without defeating the boss, THE boss SHALL intercept the player and force-trigger the boss Encounter.
5. WHEN the player defeats the boss on a boss floor, THE Level_Generator SHALL unlock the exit to the next floor. The player MAY continue exploring the boss floor after the boss is defeated (visiting shops, Work Boxes, etc.) and leave when ready.

### Requirement 39: Pause Menu

**User Story:** As a player, I want to pause the game at any time during exploration or combat, so that I can access the hub, change settings, or quit without losing progress.

#### Acceptance Criteria

1. WHEN the player presses the Escape key during exploration or combat, THE Pause_Menu SHALL pause the game and display a pause overlay.
2. THE Pause_Menu SHALL display the following options: Resume, Hub Office, Settings, and Quit to Main Menu.
3. WHEN the player selects Resume, THE Pause_Menu SHALL close the overlay and unpause the game.
4. WHEN the player selects Hub Office, THE Pause_Menu SHALL open the Hub_Office scene while keeping the current run state preserved.
5. WHEN the player selects Settings, THE Pause_Menu SHALL open the settings panel with the same options as the Main_Menu settings.
6. WHEN the player selects Quit to Main Menu, THE Pause_Menu SHALL save the current run state via the Save_Manager and return the player to the Main_Menu.
7. WHILE the Pause_Menu is open, THE game SHALL freeze all gameplay logic including enemy movement, turn timers, and animations.
8. THE Pause_Menu SHALL include a View Deck option that displays all cards currently in the player's deck in a scrollable grid view.
9. THE Pause_Menu SHALL include a View Tools option that displays all Tools currently collected in the run, showing each Tool's name, sprite, and description of its passive effects.

### Requirement 40: First-Run Tutorial

**User Story:** As a new player, I want a guided tutorial on my first run, so that I understand the unique mechanics of Overtime before being thrown into the deep end.

#### Acceptance Criteria

1. WHEN the player starts their very first run (no previous runs recorded in MetaState), THE Save_Manager SHALL flag the run as a tutorial run.
2. WHEN the tutorial run begins (after the opening dialogue choice and deck selection), THE Tutorial_NPC (a coworker) SHALL appear in the Hub_Office scene and greet Jean-Guy with dialogue framing the tutorial as a casual new employee orientation: "Come on, I'm gonna show you how to work overtime."
3. THE Tutorial_NPC SHALL walk the player through the Hub_Office scene, explaining each furniture item and how Bad_Reviews upgrades work as part of the orientation, using mundane office language (e.g., "This is your desk, you can upgrade your stuff here").
4. THE Tutorial_NPC SHALL NOT explain combat mechanics while in the Hub_Office. Combat is not a known or expected part of the job — it should feel like a surprise when it first happens.
5. WHEN the Hub_Office tutorial walkthrough completes, THE Tutorial_NPC SHALL casually direct the player to start their first shift on floor 1, treating it as a normal workday.
6. WHEN the player encounters their first enemy on floor 1, THE Tutorial_NPC SHALL react with surprise and confusion (e.g., "What the — that guy looks pissed. Uh... I think you gotta fight him?"), establishing that combat is absurd and unexpected even to the NPCs.
7. DURING the first combat encounter of the tutorial run, THE Battle_Manager SHALL display contextual UI prompts highlighting key battle screen elements: the card hand, Overtime Meter, Block display, enemy HP bars, enemy intent icons, and the End Turn button. The prompts SHALL use confused, improvised language rather than formal tutorial text (e.g., "I guess these are your... cards? Try dragging one?" rather than "Play a card by clicking it").
8. THE Tutorial_NPC SHALL explain Work_Box interaction when the player first encounters a Work_Box on floor 1, reacting as if discovering it for the first time (e.g., "Wait, there's stuff under the desks?").
9. WHEN the tutorial sequence completes (after the first combat encounter), THE Tutorial_NPC SHALL say farewell and disappear, allowing the player to continue the run normally.
10. THE Save_Manager SHALL persist the tutorial-completed flag in MetaState so the tutorial never replays on subsequent runs.
11. THE tutorial prompts SHALL be non-blocking contextual tooltips that the player can dismiss, not forced dialogue that interrupts gameplay flow.

### Requirement 41: Floor Minimap (Hub Upgrade)

**User Story:** As a player, I want to unlock a floor minimap through hub upgrades, so that I can navigate procedurally generated floors more effectively as I progress.

#### Acceptance Criteria

1. WHEN the Whiteboard hub upgrade is purchased, THE exploration scene SHALL display a minimap in the corner of the screen starting from the next run.
2. AT upgrade level 1, THE minimap SHALL show only rooms the player has already visited.
3. AT upgrade level 2, THE minimap SHALL additionally show icons for key room types (bathroom, break room, boss room, exit) in visited rooms.
4. AT upgrade level 3, THE minimap SHALL reveal the full floor layout including unvisited rooms and their types.
5. IF the player has not purchased the Whiteboard upgrade, THE exploration scene SHALL NOT display any minimap.

### Requirement 42: Water Cooler Rest Stop

**User Story:** As a player, I want to find water coolers between floors to restore some HP, so that long runs don't feel hopeless when I'm low on health.

#### Acceptance Criteria

1. THE Level_Generator SHALL place a water cooler rest stop once every 2 floors (on floors 2, 4, 6, 8, etc.). The transition between floors is presented as an elevator ride with a UI prompt — when a water cooler is available, the interaction appears as a prompt during the elevator sequence.
2. WHEN the player interacts with a water cooler, THE water cooler SHALL restore 35% of Jean-Guy's maximum HP, rounded down.
3. THE water cooler SHALL be a one-time-use interaction per occurrence — once used, it is consumed and cannot be used again.
4. THE water cooler SHALL display the amount of HP that will be restored before the player confirms the interaction.
5. WHERE the player has the Plant hub upgrade with passive healing, THE Plant healing SHALL trigger first when the player uses the floor exit, and the water cooler interaction SHALL occur afterward in the transition area. Both healing effects are independent and can stack.

### Requirement 43: Bathroom Shop Inventory Generation

**User Story:** As a player, I want bathroom shops to have varied and floor-appropriate inventory, so that shopping feels rewarding and scales with progression.

#### Acceptance Criteria

1. WHEN a Bathroom_Shop is generated, THE Level_Generator SHALL stock it with 3 to 5 cards and 0 to 2 Tools.
2. THE Bathroom_Shop SHALL assign card rarities using the same floor-based probability tables as Work_Boxes (defined in Requirement 21.4).
3. THE Bathroom_Shop SHALL price cards based on rarity: Common costs 10 Hours, Rare costs 25 Hours, Epic costs 50 Hours, Legendary costs 100 Hours, and Unknown costs 75 Hours.
4. THE Bathroom_Shop SHALL price Tools based on rarity: Common costs 30 Hours, Rare costs 60 Hours, Epic costs 120 Hours, and Legendary costs 200 Hours.
5. THE Bathroom_Shop SHALL price toilet card removal at 25 Hours, increasing by 10 Hours for each card previously removed during the current run.
6. THE Bathroom_Shop SHALL guarantee at least one Tool is available in shops on boss floors (floors 3, 6, 9, etc.).
7. THE Bathroom_Shop inventory SHALL be generated once when the floor is created and remain fixed for the duration of that floor visit.
