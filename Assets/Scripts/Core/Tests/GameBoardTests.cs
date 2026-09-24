using System;
using Core.Board;
using Core.Occupants;
using UnityEngine;

namespace Core.Tests
{
    /// Pure C# unit tests and self-verification suite for the Core Simulation Layer (GameBoard and TileOccupants).
    /// Can be executed in-editor, in test runners, or at startup without Unity Physics or Scene dependencies.
    public static class GameBoardTests
    {
        public static void RunAllTests()
        {
            Debug.Log("[GameBoardTests] Starting test suite...");

            TestBoundsChecking();
            TestWallAndTerrainQueries();
            TestOccupantPlacementAndBlocking();
            TestOccupantMovement();
            TestOccupantRemoval();
            TestPushableRules();
            TestChestOpenAndImpassability();
            TestPropDestruction();
            TestRaycasting();

            Debug.Log("[GameBoardTests] All 9 test cases passed successfully!");
        }

        private static void Assert(bool condition, string testName)
        {
            if (!condition)
            {
                throw new Exception($"[GameBoardTests] Assertion failed in {testName}!");
            }
        }

        private static void TestBoundsChecking()
        {
            var board = new GameBoard(10, 10);
            Assert(board.IsInBounds(new Vector2Int(0, 0)), "Bounds (0,0)");
            Assert(board.IsInBounds(new Vector2Int(9, 9)), "Bounds (9,9)");
            Assert(!board.IsInBounds(new Vector2Int(-1, 0)), "Bounds (-1,0) should fail");
            Assert(!board.IsInBounds(new Vector2Int(10, 5)), "Bounds (10,5) should fail");
        }

        private static void TestWallAndTerrainQueries()
        {
            var board = new GameBoard(5, 5);
            Assert(!board.IsWall(new Vector2Int(2, 2)), "Initial floor should not be wall");

            board.SetWall(new Vector2Int(2, 2), true);
            Assert(board.IsWall(new Vector2Int(2, 2)), "Marked wall should be wall");
            Assert(!board.CanEnter(new Vector2Int(2, 2)), "Wall cannot be entered");

            board.SetCell(new Vector2Int(1, 1), TerrainType.Void);
            Assert(board.IsWall(new Vector2Int(1, 1)), "Void should act as impassable wall");
        }

        private static void TestOccupantPlacementAndBlocking()
        {
            var board = new GameBoard(5, 5);
            var pawn = new EnemyOccupant(EnemyArchetype.Pawn, initialPosition: new Vector2Int(2, 2));

            bool placed = board.Place(pawn, new Vector2Int(2, 2));
            Assert(placed, "Pawn placement should succeed");
            Assert(board.IsOccupied(new Vector2Int(2, 2)), "Tile should be occupied");
            Assert(board.GetOccupant(new Vector2Int(2, 2)) == pawn, "Occupant lookup should return pawn");
            Assert(!board.CanEnter(new Vector2Int(2, 2)), "Occupied tile cannot be entered by others");

            // Attempting to place another occupant on the same tile must fail
            var rook = new EnemyOccupant(EnemyArchetype.Rook);
            bool secondPlaced = board.Place(rook, new Vector2Int(2, 2));
            Assert(!secondPlaced, "Second occupant cannot be placed on occupied tile");
        }

        private static void TestOccupantMovement()
        {
            var board = new GameBoard(5, 5);
            var player = new PlayerOccupant(initialPosition: new Vector2Int(1, 1));
            board.Place(player, new Vector2Int(1, 1));

            bool moved = board.Move(player, new Vector2Int(1, 2));
            Assert(moved, "Valid move should succeed");
            Assert(player.GridPosition == new Vector2Int(1, 2), "Player GridPosition should update");
            Assert(!board.IsOccupied(new Vector2Int(1, 1)), "Old tile should be vacant");
            Assert(board.IsOccupied(new Vector2Int(1, 2)), "New tile should be occupied");
        }

        private static void TestOccupantRemoval()
        {
            var board = new GameBoard(5, 5);
            var barrel = new DestructiblePropOccupant(DestructiblePropType.Barrel, initialPosition: new Vector2Int(3, 3));
            board.Place(barrel, new Vector2Int(3, 3));

            Assert(board.IsOccupied(new Vector2Int(3, 3)), "Barrel occupies tile");
            board.Remove(barrel);
            Assert(!board.IsOccupied(new Vector2Int(3, 3)), "Tile is free after removal");
            Assert(board.CanEnter(new Vector2Int(3, 3)), "Tile can now be entered");
        }

        private static void TestPushableRules()
        {
            var player = new PlayerOccupant();
            var enemy = new EnemyOccupant(EnemyArchetype.Knight);
            var chest = new ChestOccupant();
            var prop = new DestructiblePropOccupant();
            var obstacle = new ObstacleOccupant();

            Assert(player.IsPushable, "Player MUST be pushable");
            Assert(!enemy.IsPushable, "Enemy MUST NOT be pushable");
            Assert(!chest.IsPushable, "Chest MUST NOT be pushable");
            Assert(!prop.IsPushable, "Prop MUST NOT be pushable");
            Assert(!obstacle.IsPushable, "Obstacle MUST NOT be pushable");
        }

        private static void TestChestOpenAndImpassability()
        {
            var board = new GameBoard(5, 5);
            var chest = new ChestOccupant(initialPosition: new Vector2Int(2, 2));
            board.Place(chest, new Vector2Int(2, 2));

            Assert(!chest.IsOpen, "Chest starts closed");
            Assert(board.IsOccupied(new Vector2Int(2, 2)), "Chest occupies tile");

            // Attack chest
            chest.TakeDamage(1);
            Assert(chest.IsOpen, "Chest should be open after attack");

            // CRITICAL RULE: Open chest remains an impassable obstacle on the board!
            Assert(board.IsOccupied(new Vector2Int(2, 2)), "Open chest remains on tile");
            Assert(!board.CanEnter(new Vector2Int(2, 2)), "Open chest cell remains impassable");
        }

        private static void TestPropDestruction()
        {
            var prop = new DestructiblePropOccupant(DestructiblePropType.Crate, maxHealth: 2);
            bool destroyedEventFired = false;
            prop.OnPropDestroyed += _ => destroyedEventFired = true;

            prop.TakeDamage(1);
            Assert(prop.CurrentHealth == 1, "Prop takes 1 damage");
            Assert(!prop.IsDead, "Prop not dead at 1 HP");
            Assert(!destroyedEventFired, "Destroy event not fired yet");

            prop.TakeDamage(1);
            Assert(prop.CurrentHealth == 0, "Prop reaches 0 HP");
            Assert(prop.IsDead, "Prop is dead at 0 HP");
            Assert(destroyedEventFired, "Destroy event fired");
        }

        private static void TestRaycasting()
        {
            var board = new GameBoard(10, 10);
            var wallPos = new Vector2Int(5, 2);
            board.SetWall(wallPos, true);

            var ray = board.Raycast(new Vector2Int(2, 2), BoardCoordinate.East, maxSteps: 5);
            // Should hit (3,2), (4,2) and stop before/at wall (5,2)
            Assert(ray.Count == 2, "Raycast should yield exactly 2 floor tiles before wall");
            Assert(ray[0] == new Vector2Int(3, 2), "Ray step 1");
            Assert(ray[1] == new Vector2Int(4, 2), "Ray step 2");
        }
    }
}
