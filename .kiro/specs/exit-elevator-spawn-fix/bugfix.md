# Bugfix Requirements Document

## Introduction

On floor 2+ during procedural level generation, the exit (working) elevator spawns near the player's spawn position instead of at the furthest spawn point from the player. The `PlaceElevator()` method in `LevelGenerator.cs` calculates the exit elevator position as the farthest `ElevatorSpawn` point from the arrival elevator, rather than from the player's actual spawn position. This means the exit elevator can end up right next to where the player spawns, breaking level flow. Additionally, when `hasCustomSpawn` is false, the arrival point defaults to `points[0]` with no distance logic, making exit elevator placement essentially arbitrary.

## Bug Analysis

### Current Behavior (Defect)

1.1 WHEN the player is on floor 2+ with `hasCustomSpawn = true` and there are 2+ ElevatorSpawn points THEN the system picks the exit elevator as the farthest point from the arrival elevator position instead of from the player's spawn position, which can place the exit elevator near or at the player's spawn location

1.2 WHEN the player is on floor 2+ with `hasCustomSpawn = false` and there are 2+ ElevatorSpawn points THEN the system defaults `arrivalPoint` to `points[0]` without any distance calculation and picks the exit elevator farthest from that arbitrary point, resulting in effectively random exit elevator placement relative to the player

### Expected Behavior (Correct)

2.1 WHEN the player is on floor 2+ with `hasCustomSpawn = true` and there are 2+ ElevatorSpawn points THEN the system SHALL pick the exit elevator as the ElevatorSpawn point farthest from the player's saved spawn position (`spawnX`, `spawnZ`), ensuring the exit elevator is always placed as far as possible from where the player starts

2.2 WHEN the player is on floor 2+ with `hasCustomSpawn = false` and there are 2+ ElevatorSpawn points THEN the system SHALL use the arrival elevator position (closest to `points[0]` fallback) as the reference for exit elevator distance calculation, and the exit elevator SHALL be placed at the farthest ElevatorSpawn point from that reference, maintaining consistent behavior even without a custom spawn

### Unchanged Behavior (Regression Prevention)

3.1 WHEN the player is on floor 1 or there is only 1 ElevatorSpawn point THEN the system SHALL CONTINUE TO place the elevator at the farthest point from the PlayerSpawn marker (or at the single available point)

3.2 WHEN the player is on floor 2+ THEN the system SHALL CONTINUE TO place a closed/broken elevator at the arrival point (the ElevatorSpawn closest to the player's spawn position)

3.3 WHEN the floor is a boss floor THEN the system SHALL CONTINUE TO attach the BossFloorGate component to the exit elevator

3.4 WHEN the player is on floor 2+ THEN the system SHALL CONTINUE TO store the exit elevator position in `ElevatorSpawnPosition` for use as the next floor's spawn point
