using System;
using System.Collections.Generic;
using UnityEngine;
using Core.Board;
using Core.Effects;
using Core.Occupants;
using Infrastructure;
using Presentation.Board;
using Presentation.Effects;
using Presentation.Entities;

namespace Core.Tests
{
    /// <summary>
    /// Test suite for Plan D: Multi-Tile Pillars & Barrels.
    /// Verifies 1x1, 1x2, 2x2 pillar registration on GameBoard, unified visual GameObject,
    /// grid-based transparency calculations and lerp, and barrel destruction with direct heart restore.
    /// </summary>
    public static class PlanDTests
    {
        public static void RunAllTests()
        {
            Debug.Log("[PlanDTests] Starting Plan D Multi-Tile Pillars & Barrels test suite...");

            RunNamedTest("TestPillar_1x1_Registration", TestPillar_1x1_Registration);
            RunNamedTest("TestPillar_1x2_Registration", TestPillar_1x2_Registration);
            RunNamedTest("TestPillar_2x2_Registration", TestPillar_2x2_Registration);
            RunNamedTest("TestPillar_GridBasedTransparency_1x1", TestPillar_GridBasedTransparency_1x1);
            RunNamedTest("TestPillar_GridBasedTransparency_2x2", TestPillar_GridBasedTransparency_2x2);
            RunNamedTest("TestBarrel_DestructibleObstacleAndBlocking", TestBarrel_DestructibleObstacleAndBlocking);
            RunNamedTest("TestBarrel_DirectHeartRestoreSimulation", TestBarrel_DirectHeartRestoreSimulation);
            RunNamedTest("TestFlyingHeartParticle_ProceduralSpriteAndSpawn", TestFlyingHeartParticle_ProceduralSpriteAndSpawn);

            Debug.Log("[PlanDTests] All 8 Plan D test cases passed successfully!");
        }

        private static void RunNamedTest(string name, Action testAction)
        {
            try
            {
                testAction();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlanDTests] FAILED in {name}: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private static void Assert(bool condition, string testName)
        {
            if (!condition)
            {
                throw new Exception($"[PlanDTests] Assertion failed in {testName}!");
            }
        }

        public static void TestPillar_1x1_Registration()
        {
            var board = new GameBoard(10, 10);
            var go = new GameObject("Pillar_1x1");
            try
            {
                Vector2Int origin = new Vector2Int(2, 2);
                Vector2Int size = new Vector2Int(1, 1);

                var pillars = BoardEntityFactory.CreatePillar(go, origin, size, board);

                Assert(pillars.Count == 1, "1x1 pillar creates 1 occupant");
                Assert(board.IsOccupied(origin), "Origin (2,2) is occupied");
                Assert(!board.CanEnter(origin), "Cannot enter (2,2)");

                TileOccupant occ = board.GetOccupant(origin);
                Assert(occ is PillarOccupant, "Occupant is PillarOccupant");
                var pOcc = (PillarOccupant)occ;
                Assert(pOcc.FootprintSize == size, "Footprint size is (1,1)");
                Assert(pOcc.VisualRoot == go, "Visual root is GameObject");
                Assert(!pOcc.IsPushable, "Pillar is not pushable");
                Assert(!pOcc.IsDead, "Pillar is indestructible (not dead)");

                PillarTileObject presenter = go.GetComponent<PillarTileObject>();
                Assert(presenter != null, "PillarTileObject is attached to GameObject");
                Assert(presenter.Size == size, "Presenter size is 1x1");
                Assert(presenter.OccupantIds.Count == 1, "Presenter tracks 1 occupant ID");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void TestPillar_1x2_Registration()
        {
            var board = new GameBoard(10, 10);
            var go = new GameObject("Pillar_1x2");
            try
            {
                Vector2Int origin = new Vector2Int(4, 4);
                Vector2Int size = new Vector2Int(1, 2);

                var pillars = BoardEntityFactory.CreatePillar(go, origin, size, board);

                Assert(pillars.Count == 2, "1x2 pillar creates 2 occupants");

                // Both (4, 4) and (4, 5) must be occupied and blocked
                Vector2Int tile1 = new Vector2Int(4, 4);
                Vector2Int tile2 = new Vector2Int(4, 5);

                Assert(board.IsOccupied(tile1), "Tile (4,4) is occupied");
                Assert(board.IsOccupied(tile2), "Tile (4,5) is occupied");
                Assert(!board.CanEnter(tile1), "Cannot enter (4,4)");
                Assert(!board.CanEnter(tile2), "Cannot enter (4,5)");

                // Surrounding tiles must remain passable
                Assert(board.CanEnter(new Vector2Int(4, 3)), "Can enter south tile (4,3)");
                Assert(board.CanEnter(new Vector2Int(4, 6)), "Can enter north tile (4,6)");
                Assert(board.CanEnter(new Vector2Int(3, 4)), "Can enter west tile (3,4)");
                Assert(board.CanEnter(new Vector2Int(5, 4)), "Can enter east tile (5,4)");

                // Both occupants are distinct but share the same visual GameObject
                TileOccupant occ1 = board.GetOccupant(tile1);
                TileOccupant occ2 = board.GetOccupant(tile2);
                Assert(occ1 != occ2, "Occupants have distinct instances");
                Assert(occ1.Id != occ2.Id, "Occupants have distinct unique IDs");

                var pOcc1 = (PillarOccupant)occ1;
                var pOcc2 = (PillarOccupant)occ2;
                Assert(pOcc1.VisualRoot == go && pOcc2.VisualRoot == go, "Both occupants share single visual GameObject");

                PillarTileObject presenter = go.GetComponent<PillarTileObject>();
                Assert(presenter.OccupantIds.Count == 2, "Presenter tracks both occupant IDs");
                Assert(presenter.IsOccupiedTile(tile1) && presenter.IsOccupiedTile(tile2), "Presenter tracks both covered tiles");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void TestPillar_2x2_Registration()
        {
            var board = new GameBoard(15, 15);
            var go = new GameObject("Pillar_2x2");
            try
            {
                Vector2Int origin = new Vector2Int(8, 8);
                Vector2Int size = new Vector2Int(2, 2);

                var pillars = BoardEntityFactory.CreatePillar(go, origin, size, board);

                Assert(pillars.Count == 4, "2x2 pillar creates 4 occupants");

                var expectedCells = new Vector2Int[]
                {
                    new Vector2Int(8, 8),
                    new Vector2Int(9, 8),
                    new Vector2Int(8, 9),
                    new Vector2Int(9, 9)
                };

                var ids = new HashSet<int>();
                for (int i = 0; i < expectedCells.Length; i++)
                {
                    Vector2Int cell = expectedCells[i];
                    Assert(board.IsOccupied(cell), $"Cell {cell} is occupied");
                    Assert(!board.CanEnter(cell), $"Cell {cell} cannot be entered");

                    TileOccupant occ = board.GetOccupant(cell);
                    Assert(occ is PillarOccupant, $"Cell {cell} occupant is PillarOccupant");
                    Assert(ids.Add(occ.Id), $"Cell {cell} ID is unique");

                    var pOcc = (PillarOccupant)occ;
                    Assert(pOcc.VisualRoot == go, $"Cell {cell} shares single visual GameObject");
                    Assert(pOcc.FootprintOrigin == origin, $"Cell {cell} footprint origin matches");
                    Assert(pOcc.FootprintSize == size, $"Cell {cell} footprint size matches");
                }

                PillarTileObject presenter = go.GetComponent<PillarTileObject>();
                Assert(presenter.OccupantIds.Count == 4, "Presenter tracks 4 occupant IDs");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void TestPillar_GridBasedTransparency_1x1()
        {
            var board = new GameBoard(10, 10);
            var go = new GameObject("Pillar_1x1_Transp");
            try
            {
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.color = Color.white;

                Vector2Int origin = new Vector2Int(5, 5);
                Vector2Int size = new Vector2Int(1, 1);
                BoardEntityFactory.CreatePillar(go, origin, size, board);

                PillarTileObject presenter = go.GetComponent<PillarTileObject>();
                presenter.BehindTileDepth = 2;

                // Behind tiles relative to camera looking from south: (5, 6) and (5, 7)
                Assert(presenter.IsBehindTile(new Vector2Int(5, 6)), "Tile (5,6) is directly behind pillar");
                Assert(presenter.IsBehindTile(new Vector2Int(5, 7)), "Tile (5,7) is directly behind pillar");
                Assert(!presenter.IsBehindTile(new Vector2Int(5, 4)), "Tile (5,4) in front is not behind");
                Assert(!presenter.IsBehindTile(new Vector2Int(6, 6)), "Tile (6,6) to the side is not behind");

                // Initially, no one is behind
                bool isBehind = presenter.EvaluateBehindState(board);
                Assert(!isBehind, "Initial behind state is false");
                Assert(Mathf.Approximately(presenter.CurrentAlpha, 1.0f), "Initial alpha is 1.0");

                // Place a player occupant behind the pillar at (5, 6)
                var player = new PlayerOccupant(6, 2, new Vector2Int(5, 6));
                board.Place(player, new Vector2Int(5, 6));

                isBehind = presenter.EvaluateBehindState(board);
                Assert(isBehind, "Behind state is true when player is at (5,6)");

                // Lerp transparency forward
                presenter.UpdateTransparency(0.1f, board);
                Assert(presenter.CurrentAlpha < 1.0f, "Alpha is lerping towards transparent");

                // Full fade completion
                presenter.UpdateTransparency(1.0f, board);
                Assert(Mathf.Approximately(presenter.CurrentAlpha, presenter.TransparentAlpha), "Alpha reaches transparentAlpha");

                // Move player away to (5, 4) in front of pillar
                board.Move(player, new Vector2Int(5, 4));

                isBehind = presenter.EvaluateBehindState(board);
                Assert(!isBehind, "Behind state is false when player moves away");

                // Lerp back towards opaque
                presenter.UpdateTransparency(1.0f, board);
                Assert(Mathf.Approximately(presenter.CurrentAlpha, presenter.OpaqueAlpha), "Alpha restores to opaqueAlpha (1.0)");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void TestPillar_GridBasedTransparency_2x2()
        {
            var board = new GameBoard(15, 15);
            var go = new GameObject("Pillar_2x2_Transp");
            try
            {
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.color = Color.white;

                Vector2Int origin = new Vector2Int(6, 6);
                Vector2Int size = new Vector2Int(2, 2);
                BoardEntityFactory.CreatePillar(go, origin, size, board);

                PillarTileObject presenter = go.GetComponent<PillarTileObject>();
                presenter.BehindTileDepth = 2;

                // For 2x2 at (6,6), top row is y=7. Behind tiles:
                // x=6: (6, 8), (6, 9)
                // x=7: (7, 8), (7, 9)
                Assert(presenter.IsBehindTile(new Vector2Int(6, 8)), "(6,8) is behind");
                Assert(presenter.IsBehindTile(new Vector2Int(7, 8)), "(7,8) is behind");
                Assert(presenter.IsBehindTile(new Vector2Int(6, 9)), "(6,9) is behind");
                Assert(presenter.IsBehindTile(new Vector2Int(7, 9)), "(7,9) is behind");
                Assert(!presenter.IsBehindTile(new Vector2Int(8, 8)), "(8,8) is not behind");

                // Place an enemy occupant at (7, 8)
                var enemy = new EnemyOccupant(EnemyArchetype.Pawn, initialPosition: new Vector2Int(7, 8));
                board.Place(enemy, new Vector2Int(7, 8));

                bool isBehind = presenter.EvaluateBehindState(board);
                Assert(isBehind, "Behind state is true when enemy is at (7,8)");

                presenter.UpdateTransparency(1.0f, board);
                Assert(Mathf.Approximately(presenter.CurrentAlpha, presenter.TransparentAlpha), "Alpha lerps to transparent for enemy behind 2x2 pillar");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static void TestBarrel_DestructibleObstacleAndBlocking()
        {
            var board = new GameBoard(6, 6);
            var barrel = new DestructiblePropOccupant(DestructiblePropType.Barrel, maxHealth: 1, initialPosition: new Vector2Int(3, 3));
            board.Place(barrel, new Vector2Int(3, 3));

            Assert(board.IsOccupied(new Vector2Int(3, 3)), "Barrel occupies tile");
            Assert(!board.CanEnter(new Vector2Int(3, 3)), "Barrel blocks entry");
            Assert(!barrel.IsPushable, "Barrel is NOT pushable");
            Assert(!barrel.IsDead, "Barrel is not dead initially");
            Assert(barrel.RestoresHeartOnDestruction, "Barrel restores heart on destruction by default");
        }

        public static void TestBarrel_DirectHeartRestoreSimulation()
        {
            var board = new GameBoard(6, 6);
            var player = new PlayerOccupant(maxHealth: 6, attackDamage: 2, initialPosition: new Vector2Int(2, 2));
            board.Place(player, new Vector2Int(2, 2));

            // Set player health to 3 / 6 (damaged)
            player.TakeDamage(3);
            Assert(player.CurrentHealth == 3, "Player starts test at 3 HP");

            var barrel = new DestructiblePropOccupant(DestructiblePropType.Barrel, maxHealth: 1, initialPosition: new Vector2Int(2, 3));
            board.Place(barrel, new Vector2Int(2, 3));
            barrel.HeartDropChance = 1.0f; // Guaranteed

            var coordinator = new BoardTurnCoordinator(board);
            var targetTiles = new List<Vector2Int> { new Vector2Int(2, 3) };

            bool attackSimulated = coordinator.SimulatePlayerAttack(targetTiles, out var emittedEffects);

            Assert(attackSimulated, "SimulatePlayerAttack succeeded");
            Assert(barrel.IsDead, "Barrel is destroyed");
            Assert(!board.IsOccupied(new Vector2Int(2, 3)), "Barrel was removed from GameBoard");
            Assert(board.CanEnter(new Vector2Int(2, 3)), "Tile is now free to enter");

            // Direct heart restore: player health must increase by 1 HP (no ground pickup step)
            Assert(player.CurrentHealth == 4, "Player health restored by 1 HP to 4/6 HP");

            // Verify HeartRestoreEffect was emitted in the effect stream
            bool foundHeartEffect = false;
            for (int i = 0; i < emittedEffects.Count; i++)
            {
                if (emittedEffects[i] is HeartRestoreEffect heartEffect)
                {
                    foundHeartEffect = true;
                    Assert(heartEffect.HealAmount == 1, "Heart restore effect restores 1 HP");
                    Assert(heartEffect.SourcePos == new Vector2Int(2, 3), "Source position is barrel position");
                    Assert(heartEffect.TargetPos == new Vector2Int(2, 2), "Target position is player position");
                }
            }
            Assert(foundHeartEffect, "HeartRestoreEffect was emitted upon barrel destruction");
        }

        public static void TestFlyingHeartParticle_ProceduralSpriteAndSpawn()
        {
            Sprite heartSprite = FlyingHeartParticle.GetOrCreateHeartSprite();
            Assert(heartSprite != null, "Procedural heart sprite created");
            Assert(heartSprite.rect.width == 16f && heartSprite.rect.height == 16f, "Heart sprite is 16x16");

            var targetObj = new GameObject("TargetPlayer");
            FlyingHeartParticle particle = null;
            try
            {
                var stats = targetObj.AddComponent<PlayerStats>();
                int hpBefore = stats.CurrentHealth;

                particle = FlyingHeartParticle.Spawn(Vector3.zero, targetObj.transform, duration: 0.05f, restoreAmount: 1);
                Assert(particle != null, "FlyingHeartParticle spawned");
            }
            finally
            {
                if (particle != null && particle.gameObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(particle.gameObject);
                }
                UnityEngine.Object.DestroyImmediate(targetObj);
            }
        }
    }
}
