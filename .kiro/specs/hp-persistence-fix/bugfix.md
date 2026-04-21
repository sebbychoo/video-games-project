# Bugfix Requirements Document

## Introduction

Player HP resets to maximum at the start of every battle encounter instead of carrying over from the previous encounter. After taking damage and winning a fight, the next fight begins with full HP. This removes a core roguelike tension — damage taken should persist across encounters within a run. The `RunState` already has `playerHP` and `playerMaxHP` fields designed for this purpose, but they are never written on victory and never read on battle initialization.

## Bug Analysis

### Current Behavior (Defect)

1.1 WHEN a battle starts after the player has already completed a previous encounter with reduced HP THEN the system resets `playerHealth.currentHealth` to `maxHP` in `InitializePlayerHP()`, discarding the player's actual remaining HP.

1.2 WHEN the player wins a battle THEN the system does not save the player's current HP to `RunState.playerHP` before returning to exploration, so the surviving HP value is lost.

1.3 WHEN `OnBattleVictory()` is called in `SceneLoader.cs` THEN the system updates hours, bad reviews, and enemies defeated on `RunState` but does not persist the player's current HP or max HP.

### Expected Behavior (Correct)

2.1 WHEN a battle starts and `RunState.playerHP` is greater than zero (indicating a previous encounter has been completed) THEN the system SHALL initialize `playerHealth.currentHealth` from `RunState.playerHP` and `playerHealth.maxHealth` from `RunState.playerMaxHP` instead of resetting to the base max HP.

2.2 WHEN the player wins a battle THEN the system SHALL save `playerHealth.currentHealth` to `RunState.playerHP` and `playerHealth.maxHealth` to `RunState.playerMaxHP` before transitioning back to exploration.

2.3 WHEN a new run begins (first battle, no prior encounter data) and `RunState.playerHP` is zero or unset THEN the system SHALL initialize player HP to the full base max HP (existing behavior for the first encounter of a run).

### Unchanged Behavior (Regression Prevention)

3.1 WHEN the player loses a battle and the run is wiped via `SaveManager.WipeRun()` THEN the system SHALL CONTINUE TO reset `RunState` to defaults (playerHP = 0), ensuring the next run starts fresh with full HP.

3.2 WHEN the player wins a battle THEN the system SHALL CONTINUE TO award hours, bad reviews, and increment enemies defeated on `RunState` exactly as before.

3.3 WHEN the player wins a battle THEN the system SHALL CONTINUE TO persist `persistentBloodLevel` and `persistentOTLevel` to `RunState` exactly as before.

3.4 WHEN the HP UI elements (PlayerHPStack, EnemyHPBar badge, PlayerBadgeHP) are initialized at battle start THEN the system SHALL CONTINUE TO display the correct current and max HP values.
