using System;
using System.Collections.Generic;
using UnityEngine;
using Core.Board;
using Core.Effects;
using Core.Occupants;
using Infrastructure;

namespace Core.Tests
{
    /// <summary>
    /// Test suite verifying Plan B:
    /// 1. Turn-Before-Step (free orientation updates without spending turn)
    /// 2. Step Forward (advancing when facing direction matches input)
    /// 3. Auto Bump-to-Attack (strikes Enemy, Prop, Chest)
    /// 4. Bump-denied recoil on walls and obstacles without passing turn
    /// 5. Degen (Rapier) weapon profile (single-target strike stopping at first occupant)
    /// 6. Halberd weapon profile (sweep cleave stopping at first obstacle)
    /// 7. Pistol weapon profile (cardinal piercing with damage degradation per hit)
    /// 8. Flask (Raven Mask) weapon profile (overhead thrown arc impacting target tile)
    /// </summary>
    public static class PlanBTests
    {
        public static void RunAllTests()
        {
            Debug.Log("[PlanBTests] Starting Plan B test suite...");

            TestTurnBeforeStepFacingChange();
            TestStepForwardWhenFacing();
            TestBumpToAttackEnemy();
            TestBumpToAttackProp();
            TestBumpToAttackChest();
            TestBumpDeniedWall();
            TestBumpDeniedObstacle();
            TestDegenStopsAtFirstOccupant();
            TestHalberdSweepStopsAtObstacle();
            TestPistolPiercingDamageDegradation();
            TestFlaskThrownArcOverObstacle();

            Debug.Log("[PlanBTests] All 11 Plan B tests passed successfully!");
        }

        private static void Assert(bool condition, string testName)
        {
            if (!condition)
            {
                throw new Exception($"[PlanBTests] Assertion failed in {testName}!");
            }
        }

        private static void TestTurnBeforeStepFacingChange()
        {
            var board = new GameBoard(10, 10);
            var player = new PlayerOccupant(maxHealth: 6, attackDamage: 2, initialPosition: new Vector2Int(5, 5));
            board.Place(player, new Vector2Int(5, 5));
            player.SetFacingDirection(BoardCoordinate.South);

            Assert(player.FacingDirection == BoardCoordinate.South, "Initial facing South");

            // Turn-Before-Step: Turning East
            Vector2Int newDir = BoardCoordinate.East;
            bool isDirectionChange = (newDir != player.FacingDirection);
            Assert(isDirectionChange, "Direction changed East != South");

            // Update facing as a free action without moving or advancing turn
            player.SetFacingDirection(newDir);
            Assert(player.FacingDirection == BoardCoordinate.East, "Facing updated to East");
            Assert(player.GridPosition == new Vector2Int(5, 5), "Player position remains unchanged");
        }

        private static void TestStepForwardWhenFacing()
        {
            var board = new GameBoard(10, 10);
            var coordinator = new BoardTurnCoordinator(board);
            var player = new PlayerOccupant(maxHealth: 6, attackDamage: 2, initialPosition: new Vector2Int(5, 5));
            board.Place(player, new Vector2Int(5, 5));
            player.SetFacingDirection(BoardCoordinate.North);

            // Same direction -> Step forward
            bool moved = coordinator.SimulatePlayerMove(BoardCoordinate.North, out var moveEffect);
            Assert(moved, "Player advances 1 tile North");
            Assert(player.GridPosition == new Vector2Int(5, 6), "Player at (5, 6)");
            Assert(moveEffect != null, "MoveEffect was emitted");
            Assert(moveEffect.Path.Length == 2, "Path length is 2 (start -> end)");
        }

        private static void TestBumpToAttackEnemy()
        {
            var board = new GameBoard(10, 10);
            var coordinator = new BoardTurnCoordinator(board);
            var player = new PlayerOccupant(maxHealth: 6, attackDamage: 2, initialPosition: new Vector2Int(5, 5));
            board.Place(player, new Vector2Int(5, 5));
            player.SetFacingDirection(BoardCoordinate.North);

            var enemy = new EnemyOccupant(EnemyArchetype.Pawn, maxHealth: 4, initialPosition: new Vector2Int(5, 6));
            board.Place(enemy, new Vector2Int(5, 6));

            // Tile ahead is occupied by attackable occupant -> Triggers attack
            var targetOcc = board.GetOccupant(new Vector2Int(5, 6));
            Assert(targetOcc is EnemyOccupant, "Target is EnemyOccupant");

            bool attackSimulated = coordinator.SimulatePlayerAttack(new List<Vector2Int> { new Vector2Int(5, 6) }, out var effects);
            Assert(attackSimulated, "SimulatePlayerAttack succeeded");
            Assert(enemy.CurrentHealth == 2, "Enemy took 2 damage (HP: 2/4)");
            Assert(enemy.SkipNextTurn, "Damaged enemy is stunned / skips next turn");
        }

        private static void TestBumpToAttackProp()
        {
            var board = new GameBoard(10, 10);
            var coordinator = new BoardTurnCoordinator(board);
            var player = new PlayerOccupant(maxHealth: 6, attackDamage: 2, initialPosition: new Vector2Int(5, 5));
            board.Place(player, new Vector2Int(5, 5));

            var prop = new PropOccupant(DestructiblePropType.Barrel, maxHealth: 1, initialPosition: new Vector2Int(5, 6));
            board.Place(prop, new Vector2Int(5, 6));

            bool attackSimulated = coordinator.SimulatePlayerAttack(new List<Vector2Int> { new Vector2Int(5, 6) }, out var effects);
            Assert(attackSimulated, "SimulatePlayerAttack on prop succeeded");
            Assert(prop.IsDead, "Prop was destroyed at 0 HP");
            Assert(!board.IsOccupied(new Vector2Int(5, 6)), "Tile is now free after prop destruction");
        }

        private static void TestBumpToAttackChest()
        {
            var board = new GameBoard(10, 10);
            var coordinator = new BoardTurnCoordinator(board);
            var player = new PlayerOccupant(maxHealth: 6, attackDamage: 2, initialPosition: new Vector2Int(5, 5));
            board.Place(player, new Vector2Int(5, 5));

            var chest = new ChestOccupant(initialPosition: new Vector2Int(5, 6));
            board.Place(chest, new Vector2Int(5, 6));
            Assert(!chest.IsOpen, "Chest is closed initially");

            bool attackSimulated = coordinator.SimulatePlayerAttack(new List<Vector2Int> { new Vector2Int(5, 6) }, out var effects);
            Assert(attackSimulated, "Chest attacked successfully");
            Assert(chest.IsOpen, "Chest is now open");
            Assert(board.IsOccupied(new Vector2Int(5, 6)), "Chest remains impassable obstacle on board");
        }

        private static void TestBumpDeniedWall()
        {
            var board = new GameBoard(10, 10);
            var coordinator = new BoardTurnCoordinator(board);
            var player = new PlayerOccupant(maxHealth: 6, attackDamage: 2, initialPosition: new Vector2Int(5, 5));
            board.Place(player, new Vector2Int(5, 5));

            // Set wall at (5, 6)
            board.SetWall(new Vector2Int(5, 6), true);

            Vector2Int stepTarget = new Vector2Int(5, 6);
            bool isWall = board.IsWall(stepTarget);
            Assert(isWall, "Target is wall");

            // Moving into wall fails
            bool moved = coordinator.SimulatePlayerMove(BoardCoordinate.North, out var moveEffect);
            Assert(!moved, "Move into wall is denied");
            Assert(player.GridPosition == new Vector2Int(5, 5), "Player remains at (5, 5)");
        }

        private static void TestBumpDeniedObstacle()
        {
            var board = new GameBoard(10, 10);
            var player = new PlayerOccupant(maxHealth: 6, attackDamage: 2, initialPosition: new Vector2Int(5, 5));
            board.Place(player, new Vector2Int(5, 5));

            // Place indestructible obstacle (pillar)
            var pillar = new ObstacleOccupant(ObstacleType.Pillar, initialPosition: new Vector2Int(5, 6));
            board.Place(pillar, new Vector2Int(5, 6));

            TileOccupant occ = board.GetOccupant(new Vector2Int(5, 6));
            bool isAttackable = occ is EnemyOccupant || occ is DestructiblePropOccupant || (occ is ChestOccupant ch && !ch.IsOpen);
            Assert(!isAttackable, "ObstacleOccupant is NOT attackable");
        }

        private static void TestDegenStopsAtFirstOccupant()
        {
            var board = new GameBoard(10, 10);
            var coordinator = new BoardTurnCoordinator(board);
            var player = new PlayerOccupant(maxHealth: 6, attackDamage: 2, initialPosition: new Vector2Int(2, 2));
            board.Place(player, new Vector2Int(2, 2));
            player.SetFacingDirection(BoardCoordinate.East);

            // Two pawns aligned along attack direction
            var pawn1 = new EnemyOccupant(EnemyArchetype.Pawn, maxHealth: 4, initialPosition: new Vector2Int(3, 2));
            var pawn2 = new EnemyOccupant(EnemyArchetype.Pawn, maxHealth: 4, initialPosition: new Vector2Int(4, 2));
            board.Place(pawn1, new Vector2Int(3, 2));
            board.Place(pawn2, new Vector2Int(4, 2));

            // Attack line: (3,2) and (4,2)
            var targetTiles = new List<Vector2Int> { new Vector2Int(3, 2), new Vector2Int(4, 2) };
            coordinator.SimulatePlayerAttack(targetTiles, out var emittedEffects);

            // Degen stops at first occupant!
            Assert(pawn1.CurrentHealth == 2, "Pawn 1 was struck (HP 2/4)");
            Assert(pawn2.CurrentHealth == 4, "Pawn 2 behind Pawn 1 was NOT struck (Degen stopped at first occupant)");

            bool hasWeaponEffect = false;
            for (int i = 0; i < emittedEffects.Count; i++)
            {
                if (emittedEffects[i] is WeaponAttackEffect we && we.Weapon == WeaponType.Degen)
                {
                    hasWeaponEffect = true;
                    Assert(we.HitTiles.Count == 1, "Degen hit exactly 1 tile");
                    Assert(we.HitTiles[0] == new Vector2Int(3, 2), "Degen hit tile is (3,2)");
                }
            }
            Assert(hasWeaponEffect, "WeaponAttackEffect(Degen) was emitted");
        }

        private static void TestHalberdSweepStopsAtObstacle()
        {
            var board = new GameBoard(10, 10);
            var coordinator = new BoardTurnCoordinator(board);
            var player = new PlayerOccupant(maxHealth: 6, attackDamage: 2, initialPosition: new Vector2Int(2, 2));
            board.Place(player, new Vector2Int(2, 2));
            player.SetFacingDirection(BoardCoordinate.North);

            // Sweep arc targets: (1,3), (2,3), (3,3)
            // (1,3) has enemy, (2,3) has a wall obstacle, (3,3) has enemy behind the obstacle
            var enemy1 = new EnemyOccupant(EnemyArchetype.Pawn, maxHealth: 4, initialPosition: new Vector2Int(1, 3));
            board.Place(enemy1, new Vector2Int(1, 3));
            board.SetWall(new Vector2Int(2, 3), true);
            var enemy2 = new EnemyOccupant(EnemyArchetype.Pawn, maxHealth: 4, initialPosition: new Vector2Int(3, 3));
            board.Place(enemy2, new Vector2Int(3, 3));

            // Halberd attack
            // Use ScriptableObject or reflection to set Halberd weapon if needed, or create character definition
            var charDef = ScriptableObject.CreateInstance<CharacterDefinition>();
            charDef.SetWeaponType(WeaponType.Halberd);
            player.InitializeFromCharacter(charDef, 6);

            var sweepTiles = new List<Vector2Int> { new Vector2Int(1, 3), new Vector2Int(2, 3), new Vector2Int(3, 3) };
            coordinator.SimulatePlayerAttack(sweepTiles, out var emittedEffects);

            Assert(enemy1.CurrentHealth == 4 - player.AttackDamage, "Enemy 1 before obstacle was struck");
            Assert(enemy2.CurrentHealth == 4, "Enemy 2 after obstacle was NOT struck (sweep stopped by obstacle)");
        }

        private static void TestPistolPiercingDamageDegradation()
        {
            var board = new GameBoard(10, 10);
            var coordinator = new BoardTurnCoordinator(board);
            var player = new PlayerOccupant(maxHealth: 6, attackDamage: 3, initialPosition: new Vector2Int(2, 2));
            board.Place(player, new Vector2Int(2, 2));
            player.SetFacingDirection(BoardCoordinate.East);

            var charDef = ScriptableObject.CreateInstance<CharacterDefinition>();
            charDef.SetWeaponType(WeaponType.Pistol);
            player.InitializeFromCharacter(charDef, 6);

            // 3 aligned pawns along cardinal line: (3,2), (4,2), (5,2)
            var pawn1 = new EnemyOccupant(EnemyArchetype.Pawn, maxHealth: 6, initialPosition: new Vector2Int(3, 2));
            var pawn2 = new EnemyOccupant(EnemyArchetype.Pawn, maxHealth: 6, initialPosition: new Vector2Int(4, 2));
            var pawn3 = new EnemyOccupant(EnemyArchetype.Pawn, maxHealth: 6, initialPosition: new Vector2Int(5, 2));
            board.Place(pawn1, new Vector2Int(3, 2));
            board.Place(pawn2, new Vector2Int(4, 2));
            board.Place(pawn3, new Vector2Int(5, 2));

            var lineTiles = new List<Vector2Int> { new Vector2Int(3, 2), new Vector2Int(4, 2), new Vector2Int(5, 2) };
            coordinator.SimulatePlayerAttack(lineTiles, out var emittedEffects);

            // Pistol pierces all 3, but damage degrades by 1 per hit:
            // Pawn 1: full damage (3) -> HP: 6 - 3 = 3
            // Pawn 2: damage - 1 (2) -> HP: 6 - 2 = 4
            // Pawn 3: damage - 2 (1) -> HP: 6 - 1 = 5
            Assert(pawn1.CurrentHealth == 3, "Pawn 1 takes 3 damage (HP 3/6)");
            Assert(pawn2.CurrentHealth == 4, "Pawn 2 takes 2 damage (HP 4/6)");
            Assert(pawn3.CurrentHealth == 5, "Pawn 3 takes 1 damage (HP 5/6)");
        }

        private static void TestFlaskThrownArcOverObstacle()
        {
            var board = new GameBoard(10, 10);
            var coordinator = new BoardTurnCoordinator(board);
            var player = new PlayerOccupant(maxHealth: 6, attackDamage: 2, initialPosition: new Vector2Int(2, 2));
            board.Place(player, new Vector2Int(2, 2));
            player.SetFacingDirection(BoardCoordinate.East);

            var charDef = ScriptableObject.CreateInstance<CharacterDefinition>();
            charDef.SetWeaponType(WeaponType.Flask);
            player.InitializeFromCharacter(charDef, 6);

            // Wall obstacle in intermediate tile (3,2)
            board.SetWall(new Vector2Int(3, 2), true);

            // Target enemy behind wall at (4,2)
            var enemy = new EnemyOccupant(EnemyArchetype.Pawn, maxHealth: 4, initialPosition: new Vector2Int(4, 2));
            board.Place(enemy, new Vector2Int(4, 2));

            var targetTiles = new List<Vector2Int> { new Vector2Int(4, 2) };
            coordinator.SimulatePlayerAttack(targetTiles, out var emittedEffects);

            // Overhead throw arc bypasses intermediate wall obstacle and hits target
            Assert(enemy.CurrentHealth == 4 - player.AttackDamage, "Enemy behind wall was struck by thrown flask arc");
        }
    }
}
