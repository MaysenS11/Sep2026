using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core.Actions;
using Core.AI;
using Core.Board;
using Core.Effects;
using Core.Occupants;
using Core.Scheduling;
using Infrastructure;
using Presentation.Board;
using Presentation.Entities;

namespace Core.Tests
{
    /// Integration verification suite for Phase 6: GameManager & Spawner Integration.
    /// Verifies the full two-speed turn loop from Player Input -> Board Mutation -> Enemy AI ->
    /// Scheduling -> Effects Queue dispatch, plus chest attacks, prop breaking, recoil, and entity factory binding.
    public static class IntegrationTurnLoopTests
    {
        public static void RunAllTests()
        {
            Debug.Log("[IntegrationTurnLoopTests] Starting Phase 6 Integration test suite...");

            RunNamedTest("TestPlayerMoveTurnLoop_PureSimulation", TestPlayerMoveTurnLoop_PureSimulation);
            RunNamedTest("TestPlayerAttackEnemy_StunAndSkipNextTurn", TestPlayerAttackEnemy_StunAndSkipNextTurn);
            RunNamedTest("TestPlayerAttackEnemy_DestructionRemovesFromBoard", TestPlayerAttackEnemy_DestructionRemovesFromBoard);
            RunNamedTest("TestPlayerAttackChest_OpensAndRemainsImpassable", TestPlayerAttackChest_OpensAndRemainsImpassable);
            RunNamedTest("TestPlayerAttackDestructibleProp_BreaksAndClearsTile", TestPlayerAttackDestructibleProp_BreaksAndClearsTile);
            RunNamedTest("TestEnemyBlockedPushRecoilInTurnLoop", TestEnemyBlockedPushRecoilInTurnLoop);
            RunNamedTest("TestBoardEntityFactory_PhysicsStrippingAndPresenterBinding", TestBoardEntityFactory_PhysicsStrippingAndPresenterBinding);
            RunNamedTest("TestConcurrentBatchingExecutionWithRunner", TestConcurrentBatchingExecutionWithRunner);
            RunNamedTest("TestFullTurnLoopCoroutineExecution", TestFullTurnLoopCoroutineExecution);

            Debug.Log("[IntegrationTurnLoopTests] All 9 Phase 6 test cases passed successfully!");
        }

        private static void RunNamedTest(string name, Action testAction)
        {
            try
            {
                testAction();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IntegrationTurnLoopTests] FAILED in {name}: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private static void Assert(bool condition, string testName)
        {
            if (!condition)
            {
                throw new Exception($"[IntegrationTurnLoopTests] Assertion failed in {testName}!");
            }
        }

        private static void TestPlayerMoveTurnLoop_PureSimulation()
        {
            var board = new GameBoard(15, 15);
            var coordinator = new BoardTurnCoordinator(board);

            var player = new PlayerOccupant(maxHealth: 6, attackDamage: 2, initialPosition: new Vector2Int(5, 5), id: 1);
            board.Place(player, new Vector2Int(5, 5));

            var pawn = new EnemyOccupant(EnemyArchetype.Pawn, maxHealth: 4, attackDamage: 1, movePriority: 0, detectionRange: 10, initialPosition: new Vector2Int(5, 8), id: 2);
            board.Place(pawn, new Vector2Int(5, 8));

            // 1. Player moves North to (5, 6)
            bool moveSuccess = coordinator.SimulatePlayerMove(Vector2Int.up, out var moveEffect);
            Assert(moveSuccess, "SimulatePlayerMove should succeed");
            Assert(player.GridPosition == new Vector2Int(5, 6), "Player should be at (5, 6)");
            Assert(!board.IsOccupied(new Vector2Int(5, 5)), "Former tile (5, 5) should be vacant");
            Assert(moveEffect != null && moveEffect.Path[1] == new Vector2Int(5, 6), "MoveEffect should target (5, 6)");

            // 2. Enemy turn simulation
            var enactedActions = coordinator.SimulateEnemyTurn(out var batches);
            Assert(enactedActions.Count == 1, "Pawn should have enacted 1 action");
            Assert(pawn.GridPosition == new Vector2Int(5, 7), "Pawn should move 1 step South towards player to (5, 7)");
            Assert(batches.Count == 1, "Should generate 1 animation batch");
        }

        private static void TestPlayerAttackEnemy_StunAndSkipNextTurn()
        {
            var board = new GameBoard(15, 15);
            var coordinator = new BoardTurnCoordinator(board);

            var player = new PlayerOccupant(maxHealth: 6, attackDamage: 2, initialPosition: new Vector2Int(5, 5), id: 1);
            board.Place(player, new Vector2Int(5, 5));

            var pawn = new EnemyOccupant(EnemyArchetype.Pawn, maxHealth: 4, attackDamage: 1, movePriority: 0, detectionRange: 10, initialPosition: new Vector2Int(5, 6), id: 2);
            board.Place(pawn, new Vector2Int(5, 6));

            // Player attacks cell (5, 6)
            bool attackSuccess = coordinator.SimulatePlayerAttack(new List<Vector2Int> { new Vector2Int(5, 6) }, out var effects);
            Assert(attackSuccess, "SimulatePlayerAttack should succeed");
            Assert(pawn.CurrentHealth == 2, "Pawn HP should be reduced to 2");
            Assert(pawn.SkipNextTurn == true, "Damaged enemy should have SkipNextTurn = true");

            // Run enemy turn: enemy should be stunned and skip turn
            var enacted = coordinator.SimulateEnemyTurn(out var batches);
            Assert(enacted.Count == 0, "Stunned enemy should enact no actions");
            Assert(pawn.GridPosition == new Vector2Int(5, 6), "Enemy should remain stationary at (5, 6)");
            Assert(pawn.SkipNextTurn == false, "Turn skip should be consumed for the next turn");
        }

        private static void TestPlayerAttackEnemy_DestructionRemovesFromBoard()
        {
            var board = new GameBoard(15, 15);
            var coordinator = new BoardTurnCoordinator(board);

            var player = new PlayerOccupant(maxHealth: 6, attackDamage: 3, initialPosition: new Vector2Int(5, 5), id: 1);
            board.Place(player, new Vector2Int(5, 5));

            var pawn = new EnemyOccupant(EnemyArchetype.Pawn, maxHealth: 2, attackDamage: 1, initialPosition: new Vector2Int(5, 6), id: 2);
            board.Place(pawn, new Vector2Int(5, 6));

            // Player attacks with 3 damage against 2 HP enemy
            coordinator.SimulatePlayerAttack(new List<Vector2Int> { new Vector2Int(5, 6) }, out var effects);
            Assert(pawn.IsDead, "Pawn should be dead");
            Assert(!board.IsOccupied(new Vector2Int(5, 6)), "Dead enemy must be removed from GameBoard");
            Assert(board.CanEnter(new Vector2Int(5, 6)), "Cell (5, 6) must now be clear to enter");
        }

        private static void TestPlayerAttackChest_OpensAndRemainsImpassable()
        {
            var board = new GameBoard(15, 15);
            var coordinator = new BoardTurnCoordinator(board);

            var player = new PlayerOccupant(maxHealth: 6, attackDamage: 2, initialPosition: new Vector2Int(5, 5), id: 1);
            board.Place(player, new Vector2Int(5, 5));

            var chest = new ChestOccupant(initialPosition: new Vector2Int(5, 6), isChestRoom: false, id: 10);
            board.Place(chest, new Vector2Int(5, 6));

            Assert(!chest.IsOpen, "Chest should initially be closed");
            Assert(!board.CanEnter(new Vector2Int(5, 6)), "Chest tile should be impassable");

            // Attack chest
            coordinator.SimulatePlayerAttack(new List<Vector2Int> { new Vector2Int(5, 6) }, out var effects);

            Assert(chest.IsOpen, "Chest should now be open");
            Assert(board.GetOccupant(new Vector2Int(5, 6)) == chest, "Chest must remain on the board");
            Assert(!board.CanEnter(new Vector2Int(5, 6)), "Open chest must remain strictly impassable on the board");

            bool hasOpenedEffect = false;
            foreach (var eff in effects)
            {
                if (eff is ChestOpenedEffect) hasOpenedEffect = true;
            }
            Assert(hasOpenedEffect, "ChestOpenedEffect must be emitted");
        }

        private static void TestPlayerAttackDestructibleProp_BreaksAndClearsTile()
        {
            var board = new GameBoard(15, 15);
            var coordinator = new BoardTurnCoordinator(board);

            var player = new PlayerOccupant(maxHealth: 6, attackDamage: 2, initialPosition: new Vector2Int(5, 5), id: 1);
            board.Place(player, new Vector2Int(5, 5));

            var barrel = new DestructiblePropOccupant(DestructiblePropType.Barrel, maxHealth: 1, initialPosition: new Vector2Int(5, 6), id: 20);
            board.Place(barrel, new Vector2Int(5, 6));

            Assert(board.IsOccupied(new Vector2Int(5, 6)), "Barrel tile is occupied");

            // Attack barrel
            coordinator.SimulatePlayerAttack(new List<Vector2Int> { new Vector2Int(5, 6) }, out var effects);

            Assert(barrel.IsDead, "Barrel should be destroyed");
            Assert(!board.IsOccupied(new Vector2Int(5, 6)), "Destroyed barrel must be removed from GameBoard");
            Assert(board.CanEnter(new Vector2Int(5, 6)), "Destroyed barrel tile must now be free to enter");
        }

        private static void TestEnemyBlockedPushRecoilInTurnLoop()
        {
            var board = new GameBoard(15, 15);
            var coordinator = new BoardTurnCoordinator(board);

            // Wall directly behind player to block push
            board.SetWall(new Vector2Int(5, 4), true);

            var player = new PlayerOccupant(maxHealth: 6, attackDamage: 2, initialPosition: new Vector2Int(5, 5), id: 1);
            board.Place(player, new Vector2Int(5, 5));

            // Knight at (6, 7) - L-shape jump to (5, 5) with push direction South (0, -1) to (5, 4) which is wall
            var knight = new EnemyOccupant(EnemyArchetype.Knight, maxHealth: 4, attackDamage: 2, movePriority: 0, detectionRange: 10, initialPosition: new Vector2Int(6, 7), id: 3);
            board.Place(knight, new Vector2Int(6, 7));

            // Run enemy turn
            var enacted = coordinator.SimulateEnemyTurn(out var batches);
            Assert(enacted.Count == 1, "Knight should act");

            var action = enacted[0];
            Assert(action.Intent.IntentType == IntentType.BlockedPush, "Action should be BlockedPush");
            Assert(player.GridPosition == new Vector2Int(5, 5), "Player position must NOT move when push is blocked");
            Assert(knight.GridPosition == new Vector2Int(6, 7), "Knight must rebound back to origin tile (6, 7)");
            Assert(knight.CurrentHealth == 3, "Knight must take 1 recoil damage");
        }

        private static void TestBoardEntityFactory_PhysicsStrippingAndPresenterBinding()
        {
            var board = new GameBoard(10, 10);
            GameObject testGo = new GameObject("TestEnemyWithPhysics");
            GameObject runnerGo = new GameObject("TestRunner");

            try
            {
                // Attach legacy physics components
                testGo.AddComponent<Rigidbody2D>();
                testGo.AddComponent<BoxCollider2D>();
                testGo.AddComponent<SpriteRenderer>();

                EffectsQueueRunner runner = runnerGo.AddComponent<EffectsQueueRunner>();

                var enemyOccupant = BoardEntityFactory.CreateEnemy(
                    testGo,
                    new Vector2Int(3, 4),
                    board,
                    EnemyArchetype.Rook,
                    data: null,
                    runner: runner
                );

                // Verify physics stripping
                Assert(testGo.GetComponent<Rigidbody2D>() == null, "Rigidbody2D must be stripped");
                Assert(testGo.GetComponent<Collider2D>() == null, "Collider2D must be stripped");

                // Verify presenter binding
                var presenter = testGo.GetComponent<EnemyTileObject>();
                Assert(presenter != null, "EnemyTileObject presenter must be attached");
                Assert(presenter.OccupantId == enemyOccupant.Id, "OccupantId must match between presenter and model");
                Assert(presenter.CurrentGridPosition == new Vector2Int(3, 4), "CurrentGridPosition snapped to (3, 4)");

                // Verify registry in runner
                Assert(runner.GetTileObject(enemyOccupant.Id) == presenter, "Presenter must be registered in EffectsQueueRunner");
                Assert(board.GetOccupant(new Vector2Int(3, 4)) == enemyOccupant, "Enemy must be placed on board at (3, 4)");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(testGo);
                UnityEngine.Object.DestroyImmediate(runnerGo);
            }
        }

        private static void TestConcurrentBatchingExecutionWithRunner()
        {
            var board = new GameBoard(20, 20);
            var scheduler = new TurnBatchScheduler();

            // Two independent enemies far apart
            var enemy1 = new EnemyOccupant(EnemyArchetype.Pawn, maxHealth: 4, initialPosition: new Vector2Int(2, 2), id: 101);
            var enemy2 = new EnemyOccupant(EnemyArchetype.Pawn, maxHealth: 4, initialPosition: new Vector2Int(15, 15), id: 102);

            var intent1 = new EnemyIntent(enemy1, IntentType.Move, new Vector2Int(2, 3), new List<Vector2Int> { new Vector2Int(2, 2), new Vector2Int(2, 3) }, Vector2Int.up);
            var intent2 = new EnemyIntent(enemy2, IntentType.Move, new Vector2Int(15, 14), new List<Vector2Int> { new Vector2Int(15, 15), new Vector2Int(15, 14) }, Vector2Int.down);

            var action1 = new EnactedEnemyAction(enemy1, intent1, new List<BoardEffect> { new MoveEffect(101, new[] { new Vector2Int(2, 2), new Vector2Int(2, 3) }) });
            var action2 = new EnactedEnemyAction(enemy2, intent2, new List<BoardEffect> { new MoveEffect(102, new[] { new Vector2Int(15, 15), new Vector2Int(15, 14) }) });

            var batches = scheduler.BuildBatches(new List<EnactedEnemyAction> { action1, action2 }, totalTurnDuration: 1.0f);
            Assert(batches.Count == 1, "Independent actions must be grouped into single concurrent batch");
            Assert(batches[0].Actions.Count == 2, "Batch 0 must contain both actions");
            Assert(Mathf.Approximately(batches[0].Duration, 1.0f), "Single batch gets full 1.0s duration");
        }

        private static void TestFullTurnLoopCoroutineExecution()
        {
            var board = new GameBoard(15, 15);
            GameObject runnerGo = new GameObject("TurnLoopRunner");

            try
            {
                EffectsQueueRunner runner = runnerGo.AddComponent<EffectsQueueRunner>();
                var coordinator = new BoardTurnCoordinator(board, new TurnBatchScheduler(), runner);
                coordinator.PlayerMoveDuration = 0.01f;
                coordinator.EnemyTurnTotalDuration = 0.01f;

                var player = new PlayerOccupant(maxHealth: 6, attackDamage: 2, initialPosition: new Vector2Int(5, 5), id: 1);
                board.Place(player, new Vector2Int(5, 5));

                var pawn = new EnemyOccupant(EnemyArchetype.Pawn, maxHealth: 4, initialPosition: new Vector2Int(5, 8), id: 2);
                board.Place(pawn, new Vector2Int(5, 8));

                bool inputLocked = false;
                coordinator.OnInputLockChanged += locked => inputLocked = locked;

                // Execute player move North
                bool started = coordinator.TryExecutePlayerMove(Vector2Int.up, runner);
                Assert(started, "TryExecutePlayerMove should return true");
                Assert(inputLocked, "Input should be locked when turn starts");

                // Execute coroutine step
                IEnumerator routine = coordinator.RunEnemyTurnPhase();
                while (routine.MoveNext()) { }

                Assert(!coordinator.IsTurnInProgress, "Turn must complete and unlock input");
                Assert(!coordinator.IsPlayerInputLocked, "IsPlayerInputLocked must be false after turn ends");
                Assert(pawn.GridPosition == new Vector2Int(5, 7), "Pawn must have moved towards player");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runnerGo);
            }
        }
    }
}
