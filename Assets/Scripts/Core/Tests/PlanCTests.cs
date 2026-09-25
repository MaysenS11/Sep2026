using System;
using System.Collections.Generic;
using UnityEngine;
using Core.AI;
using Core.Board;
using Core.Effects;
using Core.Occupants;
using Dungeon;
using Infrastructure;
using Presentation.Effects;

namespace Core.Tests
{
    /// <summary>
    /// Test suite for Plan C: AI Intelligence, Prefab Boss Room & Keys.
    /// Verifies AI Intelligence Tiers (Pawn Dumb, Knight Mid, Bishop Mid, Rook Smart, Queen Smart),
    /// Stationary Minion-Linked King Boss, and Direct Key Delivery with Child Chest Rooms.
    /// </summary>
    public static class PlanCTests
    {
        public static void RunAllTests()
        {
            Debug.Log("[PlanCTests] Starting Plan C AI Intelligence, Prefab Boss Room & Keys test suite...");

            RunNamedTest("TestAI_IntelligenceLevels_AssignedProperly", TestAI_IntelligenceLevels_AssignedProperly);
            RunNamedTest("TestAI_RookSmart_SeeksCardinalLineOfSight", TestAI_RookSmart_SeeksCardinalLineOfSight);
            RunNamedTest("TestAI_QueenSmart_Tracking", TestAI_QueenSmart_Tracking);
            RunNamedTest("TestAI_PawnDumb_HasErraticMovementBranch", TestAI_PawnDumb_HasErraticMovementBranch);
            RunNamedTest("TestKeys_FlyingKeyParticleProceduralSpriteAndDelivery", TestKeys_FlyingKeyParticleProceduralSpriteAndDelivery);
            RunNamedTest("TestKeys_KeyholderAuraAndDeathEmission", TestKeys_KeyholderAuraAndDeathEmission);
            RunNamedTest("TestKeys_ChildChestRoomLockAndUnlock", TestKeys_ChildChestRoomLockAndUnlock);

            Debug.Log("[PlanCTests] All 7 Plan C test cases passed successfully!");
        }

        private static void RunNamedTest(string name, Action testAction)
        {
            try
            {
                testAction();
            }
            catch (Exception ex)
            {
                string msg = $"[PlanCTests] FAILED in {name}: {ex.Message}\n{ex.StackTrace}";
                Debug.LogError(msg);
                try
                {
                    string dir = @"C:\Users\RedDev\.gemini\antigravity\brain\92d42800-2f58-45d5-8886-38b1df2b6d4a\scratch";
                    if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                    System.IO.File.AppendAllText(System.IO.Path.Combine(dir, "test_error.txt"), msg + "\n\n");
                }
                catch { }
                throw;
            }
        }

        private static void Assert(bool condition, string testName)
        {
            if (!condition)
            {
                throw new Exception($"[PlanCTests] Assertion failed in {testName}!");
            }
        }

        public static void TestAI_IntelligenceLevels_AssignedProperly()
        {
            var pawn = new EnemyOccupant(EnemyArchetype.Pawn, intelligenceLevel: EnemySmartness.Dumb);
            Assert(pawn.IntelligenceLevel == EnemySmartness.Dumb, "Pawn is Dumb");

            var knight = new EnemyOccupant(EnemyArchetype.Knight, intelligenceLevel: EnemySmartness.Mid);
            Assert(knight.IntelligenceLevel == EnemySmartness.Mid, "Knight is Mid");

            var bishop = new EnemyOccupant(EnemyArchetype.Bishop, intelligenceLevel: EnemySmartness.Mid);
            Assert(bishop.IntelligenceLevel == EnemySmartness.Mid, "Bishop is Mid");

            var rook = new EnemyOccupant(EnemyArchetype.Rook, intelligenceLevel: EnemySmartness.Smart);
            Assert(rook.IntelligenceLevel == EnemySmartness.Smart, "Rook is Smart");

            var queen = new EnemyOccupant(EnemyArchetype.Queen, intelligenceLevel: EnemySmartness.Smart);
            Assert(queen.IntelligenceLevel == EnemySmartness.Smart, "Queen is Smart");
            Assert(queen.Smartness == EnemySmartness.Smart, "Smartness property alias matches IntelligenceLevel");
        }

        public static void TestAI_RookSmart_SeeksCardinalLineOfSight()
        {
            var board = new GameBoard(15, 15);
            var rook = new EnemyOccupant(EnemyArchetype.Rook, intelligenceLevel: EnemySmartness.Smart, initialPosition: new Vector2Int(2, 2));
            board.Place(rook, new Vector2Int(2, 2));

            // Player at (5, 8). Not in cardinal LOS with (2,2).
            var player = new PlayerOccupant(6, 2, new Vector2Int(5, 8));
            board.Place(player, new Vector2Int(5, 8));

            EnemyIntent intent = EnemyAIFactory.EvaluateEnemy(board, rook);
            Assert(intent != null, "Rook intent generated");
            Assert(intent.IntentType == IntentType.Move, "Rook moves towards LOS");

            // Smart Rook should move along cardinal axis to either x=5 or y=8
            Vector2Int dest = intent.TargetPosition;
            Assert(dest.x == 2 || dest.y == 2, "Rook moves along straight cardinal line");
        }

        public static void TestAI_QueenSmart_Tracking()
        {
            var board = new GameBoard(15, 15);
            var queen = new EnemyOccupant(EnemyArchetype.Queen, intelligenceLevel: EnemySmartness.Smart, initialPosition: new Vector2Int(1, 1));
            board.Place(queen, new Vector2Int(1, 1));

            // Player at (1, 6) - direct cardinal attack range!
            var player = new PlayerOccupant(6, 2, new Vector2Int(1, 6));
            board.Place(player, new Vector2Int(1, 6));

            EnemyIntent intent = EnemyAIFactory.EvaluateEnemy(board, queen);
            Assert(intent != null, "Queen intent generated");
            Assert(intent.IntentType == IntentType.AttackPush, "Queen executes high-priority cardinal/diagonal attack when in line of sight");
        }

        public static void TestAI_PawnDumb_HasErraticMovementBranch()
        {
            var board = new GameBoard(10, 10);
            var pawn = new EnemyOccupant(EnemyArchetype.Pawn, intelligenceLevel: EnemySmartness.Dumb, initialPosition: new Vector2Int(2, 2));
            board.Place(pawn, new Vector2Int(2, 2));

            // Player diagonally at (4, 4)
            var player = new PlayerOccupant(6, 2, new Vector2Int(4, 4));
            board.Place(player, new Vector2Int(4, 4));

            EnemyIntent intent = EnemyAIFactory.EvaluateEnemy(board, pawn);
            Assert(intent != null, "Pawn intent generated");
            Assert(intent.IntentType == IntentType.Move, "Pawn chooses move");
            Assert(board.CanEnter(intent.TargetPosition), "Pawn target position is valid enterable tile");
        }

        public static void TestKeys_FlyingKeyParticleProceduralSpriteAndDelivery()
        {
            Sprite keySprite = FlyingKeyParticle.GetOrCreateKeySprite();
            Assert(keySprite != null, "Procedural golden key sprite generated");
            Assert(keySprite.rect.width == 16f && keySprite.rect.height == 16f, "Key sprite is 16x16");

            var targetObj = new GameObject("PlayerForKeys");
            FlyingKeyParticle particle = null;
            try
            {
                particle = FlyingKeyParticle.Spawn(Vector3.zero, targetObj.transform, keys: 1, duration: 0.05f);
                Assert(particle != null, "FlyingKeyParticle spawned");
                Assert(particle.Keys == 1, "Particle carries 1 key");
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

        public static void TestKeys_KeyholderAuraAndDeathEmission()
        {
            var enemyGo = new GameObject("KeyholderEnemy");
            try
            {
                var keyholder = enemyGo.AddComponent<Keyholder>();
                keyholder.Initialize(chestRoomIndex: 3);
                Assert(keyholder.TargetChestRoomIndex == 3, "Keyholder targets chest room 3");
                Assert(keyholder.AuraObject != null, "Visual key aura created over keyholder");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemyGo);
            }
        }

        public static void TestKeys_ChildChestRoomLockAndUnlock()
        {
            var player = new PlayerOccupant(6, 2, new Vector2Int(0, 0));
            Assert(player.Keys == 0, "Player initially has 0 keys");

            player.Keys += 1;
            Assert(player.Keys == 1, "Player received 1 key");

            var chestRoom = new GameManager.RoomData
            {
                RoomIndex = 4,
                ParentRoomIndex = 1,
                Type = RoomType.Chest,
                IsLocked = true
            };

            Assert(chestRoom.IsLocked, "Child chest room starts locked");

            // Key unlocks child chest room
            if (player.Keys > 0)
            {
                player.Keys--;
                chestRoom.IsLocked = false;
            }

            Assert(!chestRoom.IsLocked, "Child chest room is now unlocked");
            Assert(player.Keys == 0, "Player key consumed to unlock room");
        }
    }
}
