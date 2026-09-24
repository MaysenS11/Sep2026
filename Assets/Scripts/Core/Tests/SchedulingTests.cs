using System;
using System.Collections.Generic;
using Core.Actions;
using Core.AI;
using Core.Board;
using Core.Effects;
using Core.Occupants;
using Core.Scheduling;
using UnityEngine;

namespace Core.Tests
{
    /// Pure C# unit tests and verification suite for Phase 4: Turn Batching & Dependency Scheduler.
    /// Verifies path disjointness, vacated tile ordering, blocker arrival and departure precedence,
    /// time-slice duration division, and Knight jump independence.
    public static class SchedulingTests
    {
        public static void RunAllTests()
        {
            Debug.Log("[SchedulingTests] Starting Phase 4 Scheduling test suite...");

            TestIndependentEnemiesGroupedInSameBatch();
            TestIntersectingPathsSeparatedIntoConsecutiveBatches();
            TestVacatedTileOrdering();
            TestBlockerArrivalPrecedesBlockedPushRecoil();
            TestBlockerDeparturePrecedesPushIntoVacatedTile();
            TestTimeSliceDurationDivision();
            TestEmptyActionsProducesEmptyBatches();
            TestKnightJumpingOverStraightLinePath();
            TestAnimationBatchHelperMethods();
            TestMultipleIndependentPairsBatchedTogether();

            Debug.Log("[SchedulingTests] All 10 Phase 4 test cases passed successfully!");
        }

        private static void Assert(bool condition, string testName)
        {
            if (!condition)
            {
                throw new Exception($"[SchedulingTests] Assertion failed in {testName}!");
            }
        }

        /// Test 1: Two distant enemies with disjoint paths placed together in Batch 0.
        public static void TestIndependentEnemiesGroupedInSameBatch()
        {
            var scheduler = new TurnBatchScheduler();

            var enemy1 = new EnemyOccupant(EnemyArchetype.Pawn, initialPosition: new Vector2Int(0, 0));
            var intent1 = new EnemyIntent(enemy1, IntentType.Move, new Vector2Int(0, 2), new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2) });
            var action1 = new EnactedEnemyAction(enemy1, intent1);

            var enemy2 = new EnemyOccupant(EnemyArchetype.Pawn, initialPosition: new Vector2Int(5, 5));
            var intent2 = new EnemyIntent(enemy2, IntentType.Move, new Vector2Int(5, 7), new List<Vector2Int> { new Vector2Int(5, 5), new Vector2Int(5, 6), new Vector2Int(5, 7) });
            var action2 = new EnactedEnemyAction(enemy2, intent2);

            var batches = scheduler.BuildBatches(new List<EnactedEnemyAction> { action1, action2 }, totalTurnDuration: 1.0f);

            Assert(batches != null, "Batches should not be null");
            Assert(batches.Count == 1, $"Expected 1 batch for independent enemies, got {batches.Count}");
            Assert(batches[0].BatchIndex == 0, "Batch index should be 0");
            Assert(batches[0].Actions.Count == 2, $"Batch 0 should contain both enemies, got {batches[0].Actions.Count}");
            Assert(batches[0].ContainsEnemy(enemy1.Id), "Batch 0 should contain enemy1");
            Assert(batches[0].ContainsEnemy(enemy2.Id), "Batch 0 should contain enemy2");
        }

        /// Test 2: Two enemies whose paths cross placed in Batch 0 and Batch 1.
        public static void TestIntersectingPathsSeparatedIntoConsecutiveBatches()
        {
            var scheduler = new TurnBatchScheduler();

            // Enemy 1 moves horizontally across (2, 2): (0, 2) -> (4, 2)
            var enemy1 = new EnemyOccupant(EnemyArchetype.Rook, initialPosition: new Vector2Int(0, 2));
            var intent1 = new EnemyIntent(enemy1, IntentType.Move, new Vector2Int(4, 2),
                new List<Vector2Int> { new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2), new Vector2Int(3, 2), new Vector2Int(4, 2) });
            var action1 = new EnactedEnemyAction(enemy1, intent1);

            // Enemy 2 moves vertically across (2, 2): (2, 0) -> (2, 4)
            var enemy2 = new EnemyOccupant(EnemyArchetype.Rook, initialPosition: new Vector2Int(2, 0));
            var intent2 = new EnemyIntent(enemy2, IntentType.Move, new Vector2Int(2, 4),
                new List<Vector2Int> { new Vector2Int(2, 0), new Vector2Int(2, 1), new Vector2Int(2, 2), new Vector2Int(2, 3), new Vector2Int(2, 4) });
            var action2 = new EnactedEnemyAction(enemy2, intent2);

            Assert(TurnBatchScheduler.HasSpatialConflict(action1, action2), "Actions 1 and 2 should have spatial conflict at (2, 2)");

            var batches = scheduler.BuildBatches(new List<EnactedEnemyAction> { action1, action2 }, totalTurnDuration: 1.0f);

            Assert(batches.Count == 2, $"Expected 2 batches for intersecting paths, got {batches.Count}");
            Assert(batches[0].BatchIndex == 0, "First batch index should be 0");
            Assert(batches[1].BatchIndex == 1, "Second batch index should be 1");
            Assert(batches[0].Actions.Count == 1 && batches[0].ContainsEnemy(enemy1.Id), "Batch 0 should contain enemy 1");
            Assert(batches[1].Actions.Count == 1 && batches[1].ContainsEnemy(enemy2.Id), "Batch 1 should contain enemy 2");
        }

        /// Test 3: Enemy B moving into tile vacated by Enemy A -> A in Batch 0, B in Batch 1.
        public static void TestVacatedTileOrdering()
        {
            var scheduler = new TurnBatchScheduler();

            // Enemy A vacates (2, 2) by moving to (2, 3)
            var enemyA = new EnemyOccupant(EnemyArchetype.Pawn, initialPosition: new Vector2Int(2, 2));
            var intentA = new EnemyIntent(enemyA, IntentType.Move, new Vector2Int(2, 3),
                new List<Vector2Int> { new Vector2Int(2, 2), new Vector2Int(2, 3) });
            var actionA = new EnactedEnemyAction(enemyA, intentA);

            // Enemy B moves from (2, 1) into (2, 2) (the tile A just vacated)
            var enemyB = new EnemyOccupant(EnemyArchetype.Pawn, initialPosition: new Vector2Int(2, 1));
            var intentB = new EnemyIntent(enemyB, IntentType.Move, new Vector2Int(2, 2),
                new List<Vector2Int> { new Vector2Int(2, 1), new Vector2Int(2, 2) });
            var actionB = new EnactedEnemyAction(enemyB, intentB);

            Assert(TurnBatchScheduler.MustPrecede(actionA, actionB), "Action A must precede Action B due to vacated tile chaining");

            var batches = scheduler.BuildBatches(new List<EnactedEnemyAction> { actionA, actionB }, totalTurnDuration: 1.0f);

            Assert(batches.Count == 2, $"Expected 2 batches for vacated tile chaining, got {batches.Count}");
            Assert(batches[0].ContainsEnemy(enemyA.Id), "Batch 0 MUST contain vacating enemy A");
            Assert(batches[1].ContainsEnemy(enemyB.Id), "Batch 1 MUST contain entering enemy B");
        }

        /// Test 4: Blocker Arrival Precedes Blocked Push Recoil.
        /// Unit X moves to (5, 6). Enemy E attacks player at (5, 5), attempting to push player to (5, 6)
        /// which is blocked by Unit X. Blocker Unit X MUST be in Batch 0, and Enemy E's blocked push recoil in Batch 1!
        public static void TestBlockerArrivalPrecedesBlockedPushRecoil()
        {
            var board = new GameBoard(10, 10);
            var player = new PlayerOccupant(maxHealth: 10, initialPosition: new Vector2Int(5, 5));
            board.Place(player, new Vector2Int(5, 5));

            // Unit X moves from (4, 6) to (5, 6)
            var unitX = new EnemyOccupant(EnemyArchetype.Pawn, initialPosition: new Vector2Int(4, 6));
            board.Place(unitX, new Vector2Int(4, 6));

            var moveActionX = new MoveAction(unitX, new Vector2Int(5, 6));
            var effectsX = moveActionX.Execute(board);
            var intentX = new EnemyIntent(unitX, IntentType.Move, new Vector2Int(5, 6),
                new List<Vector2Int> { new Vector2Int(4, 6), new Vector2Int(5, 6) }, generatedAction: moveActionX);
            var actionX = new EnactedEnemyAction(unitX, intentX, effectsX, startTile: new Vector2Int(4, 6), endTile: new Vector2Int(5, 6));

            // Enemy E at (5, 4) attacks player at (5, 5), push direction (0, 1), push destination (5, 6)
            // Push destination is blocked by Unit X at (5, 6)
            var enemyE = new EnemyOccupant(EnemyArchetype.Pawn, initialPosition: new Vector2Int(5, 4));
            board.Place(enemyE, new Vector2Int(5, 4));

            var blockedPushActionE = new BlockedPushAction(enemyE, player, pushDirection: new Vector2Int(0, 1));
            var effectsE = blockedPushActionE.Execute(board);
            var intentE = new EnemyIntent(enemyE, IntentType.BlockedPush, new Vector2Int(5, 5),
                new List<Vector2Int> { new Vector2Int(5, 4) }, pushDirection: new Vector2Int(0, 1), generatedAction: blockedPushActionE);

            var actionE = new EnactedEnemyAction(enemyE, intentE, effectsE,
                startTile: new Vector2Int(5, 4),
                endTile: new Vector2Int(5, 4),
                blockedTile: new Vector2Int(5, 6),
                blockerOccupantId: unitX.Id);

            Assert(actionE.BlockedTile == new Vector2Int(5, 6), "Action E blocked tile should be (5,6)");
            Assert(actionE.BlockerOccupantId == unitX.Id, "Action E blocker occupant ID should match Unit X");
            Assert(TurnBatchScheduler.MustPrecede(actionX, actionE), "Unit X move MUST precede Enemy E blocked push recoil!");

            var scheduler = new TurnBatchScheduler();
            var batches = scheduler.BuildBatches(new List<EnactedEnemyAction> { actionX, actionE }, totalTurnDuration: 1.0f);

            Assert(batches.Count == 2, $"Expected 2 batches for blocker arrival dependency, got {batches.Count}");
            Assert(batches[0].BatchIndex == 0, "First batch index should be 0");
            Assert(batches[1].BatchIndex == 1, "Second batch index should be 1");
            Assert(batches[0].ContainsEnemy(unitX.Id), "Batch 0 MUST contain arriving blocker Unit X");
            Assert(batches[1].ContainsEnemy(enemyE.Id), "Batch 1 MUST contain recoiling attacker Enemy E");

            // Verify effects present in batch 1
            var recoilEffect = batches[1].AllEffects.Find(e => e is BlockedPushRecoilEffect) as BlockedPushRecoilEffect;
            Assert(recoilEffect != null, "Batch 1 AllEffects must contain BlockedPushRecoilEffect");
            Assert(recoilEffect.BlockerPos == new Vector2Int(5, 6), "Recoil effect blocker pos is (5,6)");
            Assert(recoilEffect.BlockerOccupantId == unitX.Id, "Recoil effect blocker occupant ID is unitX.Id");
        }

        /// Test 5: Blocker Departure Precedes Push Into Vacated Tile.
        /// Blocker Unit Y starts at (5, 6) and moves to (5, 7).
        /// Enemy E attacks player at (5, 5), pushing player to (5, 6) (now valid because Unit Y moves away).
        /// Verify that Unit Y's departure is scheduled in Batch 0, and Enemy E's push attack is scheduled in Batch 1.
        public static void TestBlockerDeparturePrecedesPushIntoVacatedTile()
        {
            var board = new GameBoard(10, 10);
            var player = new PlayerOccupant(maxHealth: 10, initialPosition: new Vector2Int(5, 5));
            board.Place(player, new Vector2Int(5, 5));

            // Blocker Unit Y starts at (5, 6) and moves away to (5, 7)
            var unitY = new EnemyOccupant(EnemyArchetype.Pawn, initialPosition: new Vector2Int(5, 6));
            board.Place(unitY, new Vector2Int(5, 6));

            var moveActionY = new MoveAction(unitY, new Vector2Int(5, 7));
            var effectsY = moveActionY.Execute(board);
            var intentY = new EnemyIntent(unitY, IntentType.Move, new Vector2Int(5, 7),
                new List<Vector2Int> { new Vector2Int(5, 6), new Vector2Int(5, 7) }, generatedAction: moveActionY);
            var actionY = new EnactedEnemyAction(unitY, intentY, effectsY,
                startTile: new Vector2Int(5, 6), endTile: new Vector2Int(5, 7));

            // Enemy E at (5, 4) attacks player at (5, 5), pushing player into (5, 6)
            var enemyE = new EnemyOccupant(EnemyArchetype.Pawn, initialPosition: new Vector2Int(5, 4));
            board.Place(enemyE, new Vector2Int(5, 4));

            var pushActionE = new AttackPushAction(enemyE, player, pushDirection: new Vector2Int(0, 1));
            var effectsE = pushActionE.Execute(board);
            var intentE = new EnemyIntent(enemyE, IntentType.AttackPush, new Vector2Int(5, 5),
                new List<Vector2Int> { new Vector2Int(5, 4) }, pushDirection: new Vector2Int(0, 1), generatedAction: pushActionE);
            var actionE = new EnactedEnemyAction(enemyE, intentE, effectsE,
                startTile: new Vector2Int(5, 4), endTile: new Vector2Int(5, 5));

            Assert(TurnBatchScheduler.MustPrecede(actionY, actionE),
                "Blocker departure action Y must precede attack push action E into vacated tile!");

            var scheduler = new TurnBatchScheduler();
            var batches = scheduler.BuildBatches(new List<EnactedEnemyAction> { actionY, actionE }, totalTurnDuration: 1.0f);

            Assert(batches.Count == 2, $"Expected 2 batches for blocker departure, got {batches.Count}");
            Assert(batches[0].BatchIndex == 0, "First batch index should be 0");
            Assert(batches[1].BatchIndex == 1, "Second batch index should be 1");
            Assert(batches[0].ContainsEnemy(unitY.Id), "Batch 0 MUST contain departing blocker Unit Y");
            Assert(batches[1].ContainsEnemy(enemyE.Id), "Batch 1 MUST contain attacker Enemy E pushing player into vacated tile");

            // Verify effects present in batch 1
            var pushEffect = batches[1].AllEffects.Find(e => e is PushDisplacementEffect) as PushDisplacementEffect;
            Assert(pushEffect != null, "Batch 1 AllEffects must contain PushDisplacementEffect");
            Assert(pushEffect.StartPos == new Vector2Int(5, 5), "Push effect start pos is (5,5)");
            Assert(pushEffect.EndPos == new Vector2Int(5, 6), "Push effect end pos is (5,6)");
        }

        /// Test 6: Verify 1.0s total duration over 3 batches assigns exactly 0.3333s to each batch.
        public static void TestTimeSliceDurationDivision()
        {
            var scheduler = new TurnBatchScheduler();

            // Create 3 chain-dependent actions: A vacates for B, B vacates for C
            var enemyA = new EnemyOccupant(EnemyArchetype.Pawn, initialPosition: new Vector2Int(1, 3));
            var actionA = new EnactedEnemyAction(enemyA, new EnemyIntent(enemyA, IntentType.Move, new Vector2Int(1, 4),
                new List<Vector2Int> { new Vector2Int(1, 3), new Vector2Int(1, 4) }));

            var enemyB = new EnemyOccupant(EnemyArchetype.Pawn, initialPosition: new Vector2Int(1, 2));
            var actionB = new EnactedEnemyAction(enemyB, new EnemyIntent(enemyB, IntentType.Move, new Vector2Int(1, 3),
                new List<Vector2Int> { new Vector2Int(1, 2), new Vector2Int(1, 3) }));

            var enemyC = new EnemyOccupant(EnemyArchetype.Pawn, initialPosition: new Vector2Int(1, 1));
            var actionC = new EnactedEnemyAction(enemyC, new EnemyIntent(enemyC, IntentType.Move, new Vector2Int(1, 2),
                new List<Vector2Int> { new Vector2Int(1, 1), new Vector2Int(1, 2) }));

            var batches = scheduler.BuildBatches(new List<EnactedEnemyAction> { actionA, actionB, actionC }, totalTurnDuration: 1.0f);

            Assert(batches.Count == 3, $"Expected exactly 3 batches, got {batches.Count}");
            float expectedDuration = 1.0f / 3.0f;

            for (int i = 0; i < batches.Count; i++)
            {
                Assert(batches[i].BatchIndex == i, $"Batch index should match loop index {i}");
                Assert(Math.Abs(batches[i].Duration - expectedDuration) < 0.001f,
                    $"Batch {i} duration should be ~{expectedDuration:F4}s, got {batches[i].Duration:F4}s");
            }
        }

        /// Test 7: Verify empty actions list produces 0 batches.
        public static void TestEmptyActionsProducesEmptyBatches()
        {
            var scheduler = new TurnBatchScheduler();

            var emptyBatches = scheduler.BuildBatches(new List<EnactedEnemyAction>(), totalTurnDuration: 1.0f);
            Assert(emptyBatches != null, "Empty batches list should not be null");
            Assert(emptyBatches.Count == 0, "Empty action list should produce 0 batches");

            var nullBatches = scheduler.BuildBatches(null, totalTurnDuration: 1.0f);
            Assert(nullBatches != null, "Null action list should produce non-null empty list");
            Assert(nullBatches.Count == 0, "Null action list should produce 0 batches");
        }

        /// Test 8: Knight jumping over a Rook's linear movement path has disjoint traversed tiles
        /// and CAN be scheduled concurrently in the same batch (Batch 0).
        public static void TestKnightJumpingOverStraightLinePath()
        {
            var scheduler = new TurnBatchScheduler();

            // Rook moves horizontally along row 2: (0, 2) -> (4, 2)
            var rook = new EnemyOccupant(EnemyArchetype.Rook, initialPosition: new Vector2Int(0, 2));
            var rookIntent = new EnemyIntent(rook, IntentType.Move, new Vector2Int(4, 2),
                new List<Vector2Int> { new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2), new Vector2Int(3, 2), new Vector2Int(4, 2) });
            var rookAction = new EnactedEnemyAction(rook, rookIntent);

            // Knight jumps from (1, 1) to (3, 3) (leaping over row 2 at (2, 2))
            var knight = new EnemyOccupant(EnemyArchetype.Knight, initialPosition: new Vector2Int(1, 1));
            var knightIntent = new EnemyIntent(knight, IntentType.Jump, new Vector2Int(3, 3),
                new List<Vector2Int> { new Vector2Int(1, 1), new Vector2Int(3, 3) });
            var knightAction = new EnactedEnemyAction(knight, knightIntent);

            // Traversed tiles of Knight should only be (1, 1) and (3, 3)
            Assert(knightAction.TraversedTiles.Count == 2, $"Knight should only have 2 traversed tiles, got {knightAction.TraversedTiles.Count}");
            Assert(!knightAction.TraversedTiles.Contains(new Vector2Int(2, 2)), "Knight must not traverse intermediate cell (2, 2)");
            Assert(!TurnBatchScheduler.HasSpatialConflict(rookAction, knightAction), "Knight leap should NOT conflict with Rook linear path");

            var batches = scheduler.BuildBatches(new List<EnactedEnemyAction> { rookAction, knightAction }, totalTurnDuration: 1.0f);

            Assert(batches.Count == 1, $"Knight jumping over Rook should share Batch 0, got {batches.Count} batches");
            Assert(batches[0].ContainsEnemy(rook.Id), "Batch 0 should contain Rook");
            Assert(batches[0].ContainsEnemy(knight.Id), "Batch 0 should contain Knight");
        }

        /// Test 9: Tests AnimationBatch helper methods (AffectsTile, GetAllAffectedTiles, etc.).
        public static void TestAnimationBatchHelperMethods()
        {
            var batch = new AnimationBatch(batchIndex: 0, duration: 0.5f);

            var enemy = new EnemyOccupant(EnemyArchetype.Pawn, initialPosition: new Vector2Int(1, 1));
            var intent = new EnemyIntent(enemy, IntentType.Move, new Vector2Int(1, 3),
                new List<Vector2Int> { new Vector2Int(1, 1), new Vector2Int(1, 2), new Vector2Int(1, 3) });
            var moveEffect = new MoveEffect(enemy.Id, new[] { new Vector2Int(1, 1), new Vector2Int(1, 2), new Vector2Int(1, 3) });
            var action = new EnactedEnemyAction(enemy, intent, new List<BoardEffect> { moveEffect });

            batch.AddAction(action);

            Assert(batch.ContainsEnemy(enemy.Id), "Batch should contain enemy ID");
            Assert(batch.ContainsEnemy(enemy), "Batch should contain enemy occupant");
            Assert(batch.AffectsTile(new Vector2Int(1, 1)), "Batch affects start tile (1,1)");
            Assert(batch.AffectsTile(new Vector2Int(1, 2)), "Batch affects intermediate tile (1,2)");
            Assert(batch.AffectsTile(new Vector2Int(1, 3)), "Batch affects end tile (1,3)");
            Assert(!batch.AffectsTile(new Vector2Int(5, 5)), "Batch does not affect unrelated tile (5,5)");

            var affectedTiles = batch.GetAllAffectedTiles();
            Assert(affectedTiles.Contains(new Vector2Int(1, 1)), "GetAllAffectedTiles contains (1,1)");
            Assert(affectedTiles.Contains(new Vector2Int(1, 2)), "GetAllAffectedTiles contains (1,2)");
            Assert(affectedTiles.Contains(new Vector2Int(1, 3)), "GetAllAffectedTiles contains (1,3)");

            var affectedOccupants = batch.GetAllAffectedOccupantIds();
            Assert(affectedOccupants.Contains(enemy.Id), "GetAllAffectedOccupantIds contains acting enemy ID");

            Assert(batch.AllEffects.Count == 1, $"AllEffects should have 1 effect, got {batch.AllEffects.Count}");
            Assert(batch.AllEffects[0] == moveEffect, "AllEffects contains moveEffect");
        }

        /// Test 10: Two independent pairs, each with an internal dependency, grouping the first parts
        /// in Batch 0 and the second parts in Batch 1.
        public static void TestMultipleIndependentPairsBatchedTogether()
        {
            var scheduler = new TurnBatchScheduler();

            // Pair 1 at X=1: A1 moves (1, 2) -> (1, 3); A2 enters (1, 1) -> (1, 2)
            var enemyA1 = new EnemyOccupant(EnemyArchetype.Pawn, initialPosition: new Vector2Int(1, 2));
            var actionA1 = new EnactedEnemyAction(enemyA1, new EnemyIntent(enemyA1, IntentType.Move, new Vector2Int(1, 3),
                new List<Vector2Int> { new Vector2Int(1, 2), new Vector2Int(1, 3) }));

            var enemyA2 = new EnemyOccupant(EnemyArchetype.Pawn, initialPosition: new Vector2Int(1, 1));
            var actionA2 = new EnactedEnemyAction(enemyA2, new EnemyIntent(enemyA2, IntentType.Move, new Vector2Int(1, 2),
                new List<Vector2Int> { new Vector2Int(1, 1), new Vector2Int(1, 2) }));

            // Pair 2 at X=8: B1 moves (8, 2) -> (8, 3); B2 enters (8, 1) -> (8, 2)
            var enemyB1 = new EnemyOccupant(EnemyArchetype.Pawn, initialPosition: new Vector2Int(8, 2));
            var actionB1 = new EnactedEnemyAction(enemyB1, new EnemyIntent(enemyB1, IntentType.Move, new Vector2Int(8, 3),
                new List<Vector2Int> { new Vector2Int(8, 2), new Vector2Int(8, 3) }));

            var enemyB2 = new EnemyOccupant(EnemyArchetype.Pawn, initialPosition: new Vector2Int(8, 1));
            var actionB2 = new EnactedEnemyAction(enemyB2, new EnemyIntent(enemyB2, IntentType.Move, new Vector2Int(8, 2),
                new List<Vector2Int> { new Vector2Int(8, 1), new Vector2Int(8, 2) }));

            var batches = scheduler.BuildBatches(new List<EnactedEnemyAction> { actionA1, actionA2, actionB1, actionB2 }, totalTurnDuration: 1.0f);

            Assert(batches.Count == 2, $"Expected exactly 2 batches for two independent pairs, got {batches.Count}");
            Assert(batches[0].ContainsEnemy(enemyA1.Id) && batches[0].ContainsEnemy(enemyB1.Id),
                "Batch 0 should contain both leading enemies A1 and B1");
            Assert(batches[1].ContainsEnemy(enemyA2.Id) && batches[1].ContainsEnemy(enemyB2.Id),
                "Batch 1 should contain both following enemies A2 and B2");
        }
    }
}
