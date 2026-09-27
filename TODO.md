# Master Development TODO List

> **Project Scope Updates & Architectural Notes:**
> - **Stamina & Dash:** Cut from project scope. Speed stat remains removed.
> - **King Boss AI:** Cut from project scope. The boss room is authored/pre-designed in `BossRoom_18x18.prefab`. The King is positioned at an unreachable coordinate and serves a purely visual/cosmetic role (animations: Idle, Damage, Decapitate). A pseudo script (`KingBossController`) manages the King's health/healthbar linked to minion deaths in the room.
> - **Special Chest Rooms (1–2 per dungeon):** Must spawn as locked child rooms branched from normal parent rooms with distinct special door visuals, key requirement, and override/custom floor rule tiles.
> - **Multi-Tile Pillars:** Fix board grid registration and overlap issues, and implement an ASCII board printer utility for debugging authoritative board state.

---

## 1. Board Debugging & Pillar Registration Fixes

- [ ] **Authoritative Board ASCII Printer Utility**
  - Implement a debug/editor tool (`BoardPrinter.cs` or CLI eval command) that dumps the current `GameBoard` as an ASCII grid to the Unity console or a text file.
  - Map characters: `.` = Empty floor, `#` = Wall, `P` = Player, `E` = Enemy, `B` = Barrel, `C` = Chest, `∏` = Pillar, `D` = Door (`D_in`, `D_out`, `D_spec`).
  - Print occupant coordinate lists with IDs and dimensions to verify cell boundaries.

- [ ] **Fix Multi-Tile Pillar Footprint & Overlap Conflicts**
  - In `PropSpawner.cs`, verify that candidate footprint checks (`tileQuery.IsAreaClear(candidate, propSize)`) strictly prevent pillars from overlapping each other or clipping into wall boundaries.
  - Fix overlapping 3x3 pillar spawns (e.g., overlapping occupants overwriting cells at coordinates like `(72,12)`).
  - Ensure pillar prefabs match designated dimensions (`1x1`, `1x2`, `2x2`, `3x3`) and register each tile individually as `PillarOccupant` referencing the single shared `PillarTileObject` GameObject.
  - Remove or initialize unlinked static `Pillar` GameObjects in `Level.unity` scene that currently register zero occupant IDs.

- [ ] **Verify Pillar Transparency & Blocking**
  - Ensure `PillarTileObject.EvaluateBehindState` correctly fades sprite alpha when player or enemies walk on tiles directly behind the pillar.
  - Verify player bump-denied recoil correctly triggers against every registered tile of multi-tile pillars without ending the player's turn.

---

## 2. Child Chest Rooms & Special Door System

- [ ] **Restore Child Chest Room Spawning (1–2 per Dungeon)**
  - In `DungeonLayoutPlanner.cs` (`TryInsertChestRooms`), fix parent room linkage:
    - Set `rooms[parentIdx].HasSpecialChestRoom = true;`
    - Set `rooms[parentIdx].SpecialChestRoomIndex = chestRoom.RoomIndex;`
    - Verify 1 to 2 child chest rooms are guaranteed to generate from valid normal parent rooms without macro cell collisions.
  - In `DungeonManager.cs`, ensure `fixedChestRoomSize` (currently configured in Inspector) is passed correctly to `DungeonLayoutPlanner`.

- [ ] **Special Locked Door Generation in Parent Room**
  - In `DoorSpawner.cs`, when a room has `HasSpecialChestRoom == true`:
    - Pick a valid wall/floor perimeter tile in the parent room for the special door.
    - Store position in `room.SpecialExitDoorTile`.
    - Paint tile on `ObjectTilemap` using `specialExitDoorTile` sprite (distinguishable from normal doors).
    - Initialize the door state as locked (`room.IsLocked = true`).

- [ ] **Child Chest Room Exit Door Generation**
  - In `DoorSpawner.cs`, for `RoomType.Chest`:
    - Spawn `EntranceDoorTile` using `specialEntranceDoorTile` sprite at the room entry tile.
    - Spawn `ExitDoorTile` using `specialExitDoorTile` sprite leading back out to `parentRoom.SpecialExitDoorTile`.

- [ ] **Door Lock Gatekeeping & Key Consumption**
  - In `GameManager.cs` (`TransitionThroughDoor`):
    - Check if the destination room or door is locked (`targetRoom.IsLocked`).
    - If locked:
      - If `PlayerOccupant.Keys > 0`: consume 1 key (`player.Keys--`), unlock room (`targetRoom.IsLocked = false`), update HUD key counter, play `openLockSound` FMOD event, and proceed with room transition.
      - If `PlayerOccupant.Keys == 0`: deny entry, play locked feedback / recoil, and cancel transition.

- [ ] **Distinct Rule Tile / Override Rule Tile for Chest Rooms**
  - In `DungeonTilemapRenderer.cs`:
    - Add support for `chestFloorRuleTile` / `chestOverrideRuleTile`.
    - When rendering rooms (`RenderRooms`), if `room.Type == RoomType.Chest`, paint the floor using the chest room override rule tile instead of the default dungeon rule tile.

---

## 3. Pre-Authored Boss Room & Cosmetic King Pseudo-Script

- [ ] **Boss Room Entity Registration (Remove Skip Filter)**
  - In `BossRoom.cs` (`ScanAndRegisterBossEntities`), remove the King exclusion filter (`if (enemy.name.ToLower().Contains("king")) continue;`).
  - Register all pre-placed minion pieces in `BossRoom_18x18.prefab` to the `GameBoard`.
  - Ensure the King GameObject is recognized as a non-moving cosmetic entity outside player reach.

- [ ] **Cosmetic King Pseudo-Script (`KingBossController`)**
  - Create `KingBossController.cs` on the King GameObject:
    - Manage total King Boss Health and expose public health telemetry.
    - Listen for minion death events in the boss room.
    - Apply damage to the King when minions die based on archetype damage values (`PawnBossDamage`, `KnightBossDamage`, `BishopBossDamage`, `RookBossDamage`, `QueenBossDamage`).
    - Play King hit reactions (`animator.SetTrigger("TakeDamage")` and damage SFX) upon minion death.
    - When King health reaches 0:
      - Trigger the King decapitation animation (`animator.SetTrigger("Decapitate")`).
      - Play `winMusic` / `SFX_WinOverKing`.
      - Award Mask Key on first defeat per mask in `PlayerPrefs` (`first_king_defeat_{maskId}`).
      - Display the Victory/Win screen.

- [ ] **Boss Healthbar UI**
  - Bind a Boss Healthbar slider/HUD element under `Canvas` in `Level.unity` that activates upon entering `RoomType.Boss` and reflects the King's pseudo-health.

---

## 4. UI Scene Wiring & Overlays (Level.unity)

- [ ] **HUD Key Counter**
  - Add `KeyContainer` with icon and TMP `KeyCount` text under `Canvas/IngamePanel` in `Level.unity`.
  - Connect with `UIManager.UpdateKeyUI` so keys collected from keyholders are visually updated.

- [ ] **Minimap UI Scene Wiring**
  - Attach `MinimapUI` component to `Canvas/IngamePanel/map` in `Level.unity`.
  - Ensure room icons (`Map_Normal`, `Map_Chest`, `Map_Boss`, `Map_Curr`) and corridor connectors render dynamically on room entry and auto-discover child chest rooms.

- [ ] **Tab Threat Overlay Scene Wiring**
  - Add a GameObject with `ThreatOverlay` component to `Level.unity`.
  - Verify holding `Tab` renders move (yellow) and attack (red) threat tiles for active enemies.

- [ ] **Game Over & Win UI Scene Wiring**
  - Add `GameOverWinUI` panel hierarchy under `Canvas` in `Level.unity`.
  - Wire buttons for `RestartGame` (re-loads run with selected mask) and `LoadMainMenu`.
  - Ensure best completion time (`best_time_{maskId}`) and Mask Key unlock notifications display on victory.

- [ ] **In-Game Options / Pause Menu**
  - Ensure `OptionsMenuUI` is present or accessible via Escape/Pause in `Level.unity`, allowing in-game master/SFX volume adjustment and key rebinding validation.
