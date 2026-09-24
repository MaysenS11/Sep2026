using System;
using System.Collections.Generic;
using Core.Actions;
using Core.AI;
using Core.Board;
using Core.Occupants;
using UnityEngine;

namespace Core.Tests
{
    /// Pure C# unit tests and verification suite for Phase 3: Enemy AI Strategies.
    /// Verifies Pawn, Knight, Bishop, Rook, Queen pathing, attack selection,
    /// Knight origin-rebound, sliding pieces stopping 1 step short, stunned turn skips,
    /// and detection range rules.
    public static class EnemyAITests
    {
        public static void RunAllTests()
        {
            Debug.Log("[EnemyAITests] Starting Phase 3 AI test suite...");

            TestPawnMovementAndAttackSelection();
            TestPawnBlockedPushRecoil();
            TestDiagonalPawnAttack();
            TestKnightJumpMovementAndAttack();
            TestKnightBlockedPushOriginRebound();
            TestRookAttackAndBlockedPush();
            TestRookAdvancementWhenNotAligned();
            TestBishopAttackAndBlockedPush();
            TestBishopAdvancementWhenNotAligned();
            TestQueenAttackCardinalAndDiagonal();
            TestStunnedEnemySkipsTurnAndClearsStun();
            TestDetectionRange();
            TestBlockedLineOfSightDoesNotAttackThroughObstacle();

            Debug.Log("[EnemyAITests] All 13 Phase 3 test cases passed successfully!");
        }

        private static void Assert(bool condition, string testName)
        {
            if (!condition)
            {
                throw new Exception($"[EnemyAITests] Assertion failed in {testName}!");
            }
        }

        private static void TestPawnMovementAndAttackSelection()
        {
            var board = new GameBoard(6, 6);
            var player = new PlayerOccupant(maxHealth: 6, initialPosition: new Vector2Int(1, 4));
            board.Place(player, new Vector2Int(1, 4));

            // 1. Pawn distant from player: moves 1 step North along primary Y axis
            var distantPawn = new EnemyOccupant(EnemyArchetype.Pawn, initialPosition: new Vector2Int(1, 1));
            board.Place(distantPawn, new Vector2Int(1, 1));

            var moveIntent = EnemyAIFactory.EvaluateEnemy(board, distantPawn);
            Assert(moveIntent.IntentType == IntentType.Move, "Distant pawn intends to move");
            Assert(moveIntent.TargetPosition == new Vector2Int(1, 2), "Pawn targets (1,2) along primary axis");
            Assert(moveIntent.GeneratedAction is MoveAction, "Pawn generates MoveAction");

            moveIntent.GeneratedAction.Execute(board);
            Assert(distantPawn.GridPosition == new Vector2Int(1, 2), "Pawn moved to (1,2)");
            board.Remove(distantPawn);

            // 2. Pawn cardinally adjacent to player: attacks and pushes player North
            var adjacentPawn = new EnemyOccupant(EnemyArchetype.Pawn, attackDamage: 2, initialPosition: new Vector2Int(1, 3));
            board.Place(adjacentPawn, new Vector2Int(1, 3));

            var attackIntent = EnemyAIFactory.EvaluateEnemy(board, adjacentPawn);
            Assert(attackIntent.IntentType == IntentType.AttackPush, "Adjacent pawn intends to AttackPush");
            Assert(attackIntent.TargetPosition == new Vector2Int(1, 4), "Pawn targets player tile (1,4)");
            Assert(attackIntent.PushDirection == BoardCoordinate.North, "Push direction is North");
            Assert(attackIntent.GeneratedAction is AttackPushAction, "Generates AttackPushAction");

            attackIntent.GeneratedAction.Execute(board);
            Assert(player.GridPosition == new Vector2Int(1, 5), "Player pushed to (1,5)");
            Assert(adjacentPawn.GridPosition == new Vector2Int(1, 4), "Pawn moved into former player tile (1,4)");
            Assert(player.CurrentHealth == 4, "Player took 2 damage (HP: 4/6)");
        }

        private static void TestPawnBlockedPushRecoil()
        {
            var board = new GameBoard(6, 6);
            var player = new PlayerOccupant(maxHealth: 6, initialPosition: new Vector2Int(1, 4));
            board.Place(player, new Vector2Int(1, 4));

            // Wall directly behind player at (1, 5)
            board.SetWall(new Vector2Int(1, 5), true);

            var pawn = new EnemyOccupant(EnemyArchetype.Pawn, maxHealth: 4, initialPosition: new Vector2Int(1, 3));
            board.Place(pawn, new Vector2Int(1, 3));

            var intent = EnemyAIFactory.EvaluateEnemy(board, pawn);
            Assert(intent.IntentType == IntentType.BlockedPush, "Pawn intends BlockedPush when wall blocks push");
            Assert(intent.TargetPosition == new Vector2Int(1, 3), "Pawn target is origin tile (1,3)");
            Assert(intent.GeneratedAction is BlockedPushAction, "Generates BlockedPushAction");

            intent.GeneratedAction.Execute(board);
            Assert(player.GridPosition == new Vector2Int(1, 4), "Player remains at (1,4)");
            Assert(pawn.GridPosition == new Vector2Int(1, 3), "Pawn remains at origin (1,3)");
            Assert(pawn.CurrentHealth == 3, "Pawn took 1 recoil damage");
            Assert(pawn.SkipNextTurn, "Pawn is stunned for next turn");
        }

        private static void TestDiagonalPawnAttack()
        {
            var board = new GameBoard(6, 6);
            var player = new PlayerOccupant(maxHealth: 6, initialPosition: new Vector2Int(2, 2));
            board.Place(player, new Vector2Int(2, 2));

            // Pawn configured with UsesDiagonalAttack = true at (1, 1)
            var diagPawn = new EnemyOccupant(EnemyArchetype.Pawn, usesDiagonalAttack: true, initialPosition: new Vector2Int(1, 1));
            board.Place(diagPawn, new Vector2Int(1, 1));

            var intent = EnemyAIFactory.EvaluateEnemy(board, diagPawn);
            Assert(intent.IntentType == IntentType.AttackPush, "Diagonal pawn intends AttackPush on diagonal player");
            Assert(intent.TargetPosition == new Vector2Int(2, 2), "Target is player tile (2,2)");
            Assert(intent.PushDirection == new Vector2Int(1, 1), "Push direction is diagonal NorthEast (1,1)");

            intent.GeneratedAction.Execute(board);
            Assert(player.GridPosition == new Vector2Int(3, 3), "Player pushed diagonally to (3,3)");
            Assert(diagPawn.GridPosition == new Vector2Int(2, 2), "Pawn moved to (2,2)");
        }

        private static void TestKnightJumpMovementAndAttack()
        {
            var board = new GameBoard(7, 7);
            var player = new PlayerOccupant(maxHealth: 6, initialPosition: new Vector2Int(4, 4));
            board.Place(player, new Vector2Int(4, 4));

            // 1. Knight distant at (0, 0): cannot attack, jumps to valid L-jump closest to (4, 4)
            var knight = new EnemyOccupant(EnemyArchetype.Knight, initialPosition: new Vector2Int(0, 0));
            board.Place(knight, new Vector2Int(0, 0));

            var moveIntent = EnemyAIFactory.EvaluateEnemy(board, knight);
            Assert(moveIntent.IntentType == IntentType.Jump, "Knight intends Jump");
            Assert(moveIntent.GeneratedAction is JumpAction, "Generates JumpAction");
            // Offsets (1,2) and (2,1) are closest to (4,4)
            Assert(moveIntent.TargetPosition == new Vector2Int(1, 2) || moveIntent.TargetPosition == new Vector2Int(2, 1),
                "Knight jumped to offset closest to player");

            moveIntent.GeneratedAction.Execute(board);
            board.Remove(knight);

            // 2. Knight at (2, 3), player at (4, 4): offset (2, 1) lands on player!
            var attackingKnight = new EnemyOccupant(EnemyArchetype.Knight, initialPosition: new Vector2Int(2, 3));
            board.Place(attackingKnight, new Vector2Int(2, 3));

            var attackIntent = EnemyAIFactory.EvaluateEnemy(board, attackingKnight);
            Assert(attackIntent.IntentType == IntentType.AttackPush, "Knight on offset attacks player");
            Assert(attackIntent.TargetPosition == new Vector2Int(4, 4), "Target is player tile (4,4)");
            Assert(attackIntent.PushDirection == BoardCoordinate.East, "Dominant push direction is East (+2 X axis)");

            attackIntent.GeneratedAction.Execute(board);
            Assert(player.GridPosition == new Vector2Int(5, 4), "Player pushed East to (5,4)");
            Assert(attackingKnight.GridPosition == new Vector2Int(4, 4), "Knight landed on (4,4)");
        }

        private static void TestKnightBlockedPushOriginRebound()
        {
            var board = new GameBoard(6, 6);
            var player = new PlayerOccupant(maxHealth: 6, initialPosition: new Vector2Int(2, 2));
            board.Place(player, new Vector2Int(2, 2));

            // Wall directly East of player at (3, 2)
            board.SetWall(new Vector2Int(3, 2), true);

            // Knight at (0, 1): jump offset (+2, +1) lands on player at (2, 2), push East is blocked by wall!
            var knight = new EnemyOccupant(EnemyArchetype.Knight, maxHealth: 4, initialPosition: new Vector2Int(0, 1));
            board.Place(knight, new Vector2Int(0, 1));

            var intent = EnemyAIFactory.EvaluateEnemy(board, knight);
            Assert(intent.IntentType == IntentType.BlockedPush, "Knight intends BlockedPush when player push is blocked");
            Assert(intent.TargetPosition == new Vector2Int(0, 1), "CRITICAL: Knight recoil target is origin tile (0,1)");

            intent.GeneratedAction.Execute(board);
            Assert(player.GridPosition == new Vector2Int(2, 2), "Player stays at (2,2)");
            Assert(knight.GridPosition == new Vector2Int(0, 1), "Knight rebounds to starting tile (0,1)");
            Assert(knight.CurrentHealth == 3, "Knight took 1 recoil damage");
            Assert(knight.SkipNextTurn, "Knight is stunned from recoil");
        }

        private static void TestRookAttackAndBlockedPush()
        {
            var board = new GameBoard(8, 8);
            var player = new PlayerOccupant(maxHealth: 6, initialPosition: new Vector2Int(3, 2));
            board.Place(player, new Vector2Int(3, 2));

            // 1. Clear attack: Rook at (0, 2), player at (3, 2), push East to (4, 2) is clear
            var rook = new EnemyOccupant(EnemyArchetype.Rook, maxHealth: 4, initialPosition: new Vector2Int(0, 2));
            board.Place(rook, new Vector2Int(0, 2));

            var clearIntent = EnemyAIFactory.EvaluateEnemy(board, rook);
            Assert(clearIntent.IntentType == IntentType.AttackPush, "Rook on cardinal line attacks player");
            Assert(clearIntent.TargetPosition == new Vector2Int(3, 2), "Rook targets player tile (3,2)");
            Assert(clearIntent.Path.Count == 4, "Rook path has 4 waypoints: (0,2), (1,2), (2,2), (3,2)");

            // 2. Blocked push: add wall at (4, 2)
            board.SetWall(new Vector2Int(4, 2), true);

            var blockedIntent = EnemyAIFactory.EvaluateEnemy(board, rook);
            Assert(blockedIntent.IntentType == IntentType.BlockedPush, "Rook intends BlockedPush when push tile is wall");
            Assert(blockedIntent.TargetPosition == new Vector2Int(2, 2), "CRITICAL: Rook stops 1 tile short of player at (2,2)");
            Assert(blockedIntent.Path.Count == 3, "Rook blocked path ends at (2,2)");

            blockedIntent.GeneratedAction.Execute(board);
            Assert(player.GridPosition == new Vector2Int(3, 2), "Player remains at (3,2)");
            Assert(rook.GridPosition == new Vector2Int(2, 2), "Rook moved to (2,2) 1 step short of player");
            Assert(rook.CurrentHealth == 3, "Rook took 1 recoil damage");
        }

        private static void TestRookAdvancementWhenNotAligned()
        {
            var board = new GameBoard(8, 8);
            var player = new PlayerOccupant(initialPosition: new Vector2Int(4, 5));
            board.Place(player, new Vector2Int(4, 5));

            // Rook at (0, 0): not on row 5 or col 4, advances along clear orthogonal line
            var rook = new EnemyOccupant(EnemyArchetype.Rook, maxLineSteps: 3, initialPosition: new Vector2Int(0, 0));
            board.Place(rook, new Vector2Int(0, 0));

            var intent = EnemyAIFactory.EvaluateEnemy(board, rook);
            Assert(intent.IntentType == IntentType.Move, "Non-aligned Rook intends Move");
            Assert(intent.GeneratedAction is MoveAction, "Generates MoveAction");
            // Should advance 3 steps North or East toward player
            Assert(intent.TargetPosition == new Vector2Int(0, 3) || intent.TargetPosition == new Vector2Int(3, 0),
                "Rook advances 3 steps along clear cardinal line");

            intent.GeneratedAction.Execute(board);
            Assert(rook.GridPosition == intent.TargetPosition, "Rook position updated after execute");
        }

        private static void TestBishopAttackAndBlockedPush()
        {
            var board = new GameBoard(8, 8);
            var player = new PlayerOccupant(maxHealth: 6, initialPosition: new Vector2Int(4, 4));
            board.Place(player, new Vector2Int(4, 4));

            // Bishop at (1, 1): diagonal line of sight to player at (4, 4)
            var bishop = new EnemyOccupant(EnemyArchetype.Bishop, maxHealth: 4, initialPosition: new Vector2Int(1, 1));
            board.Place(bishop, new Vector2Int(1, 1));

            // 1. Clear attack
            var clearIntent = EnemyAIFactory.EvaluateEnemy(board, bishop);
            Assert(clearIntent.IntentType == IntentType.AttackPush, "Bishop attacks on diagonal line");
            Assert(clearIntent.TargetPosition == new Vector2Int(4, 4), "Bishop targets player tile (4,4)");
            Assert(clearIntent.PushDirection == new Vector2Int(1, 1), "Push direction is NorthEast (1,1)");

            // 2. Blocked push: wall at (5, 5)
            board.SetWall(new Vector2Int(5, 5), true);

            var blockedIntent = EnemyAIFactory.EvaluateEnemy(board, bishop);
            Assert(blockedIntent.IntentType == IntentType.BlockedPush, "Bishop blocked push when (5,5) is wall");
            Assert(blockedIntent.TargetPosition == new Vector2Int(3, 3), "CRITICAL: Bishop stops 1 tile short at (3,3)");

            blockedIntent.GeneratedAction.Execute(board);
            Assert(player.GridPosition == new Vector2Int(4, 4), "Player remains at (4,4)");
            Assert(bishop.GridPosition == new Vector2Int(3, 3), "Bishop stopped at (3,3) 1 tile short");
            Assert(bishop.CurrentHealth == 3, "Bishop took 1 recoil damage");
        }

        private static void TestBishopAdvancementWhenNotAligned()
        {
            var board = new GameBoard(8, 8);
            var player = new PlayerOccupant(initialPosition: new Vector2Int(2, 5));
            board.Place(player, new Vector2Int(2, 5));

            // Bishop at (1, 0): not on same diagonal, advances along clear diagonal line
            var bishop = new EnemyOccupant(EnemyArchetype.Bishop, maxLineSteps: 2, initialPosition: new Vector2Int(1, 0));
            board.Place(bishop, new Vector2Int(1, 0));

            var intent = EnemyAIFactory.EvaluateEnemy(board, bishop);
            Assert(intent.IntentType == IntentType.Move, "Non-aligned Bishop advances toward player");
            Assert(intent.TargetPosition != new Vector2Int(1, 0), "Bishop target moved closer");

            intent.GeneratedAction.Execute(board);
            Assert(bishop.GridPosition == intent.TargetPosition, "Bishop position updated");
        }

        private static void TestQueenAttackCardinalAndDiagonal()
        {
            var board = new GameBoard(8, 8);
            var player = new PlayerOccupant(maxHealth: 6, initialPosition: new Vector2Int(4, 4));
            board.Place(player, new Vector2Int(4, 4));

            // 1. Cardinal attack
            var queenCardinal = new EnemyOccupant(EnemyArchetype.Queen, initialPosition: new Vector2Int(4, 1));
            board.Place(queenCardinal, new Vector2Int(4, 1));

            var cardinalIntent = EnemyAIFactory.EvaluateEnemy(board, queenCardinal);
            Assert(cardinalIntent.IntentType == IntentType.AttackPush, "Queen attacks along cardinal line");
            Assert(cardinalIntent.PushDirection == BoardCoordinate.North, "Push direction is North");
            board.Remove(queenCardinal);

            // 2. Diagonal attack with blocked recoil
            var queenDiag = new EnemyOccupant(EnemyArchetype.Queen, maxHealth: 4, initialPosition: new Vector2Int(1, 1));
            board.Place(queenDiag, new Vector2Int(1, 1));
            board.SetWall(new Vector2Int(5, 5), true); // Block push tile

            var diagIntent = EnemyAIFactory.EvaluateEnemy(board, queenDiag);
            Assert(diagIntent.IntentType == IntentType.BlockedPush, "Queen blocked push on diagonal attack");
            Assert(diagIntent.TargetPosition == new Vector2Int(3, 3), "Queen stops 1 tile short at (3,3)");

            diagIntent.GeneratedAction.Execute(board);
            Assert(queenDiag.GridPosition == new Vector2Int(3, 3), "Queen positioned 1 tile short at (3,3)");
            Assert(queenDiag.CurrentHealth == 3, "Queen took 1 recoil damage");
        }

        private static void TestStunnedEnemySkipsTurnAndClearsStun()
        {
            var board = new GameBoard(6, 6);
            var player = new PlayerOccupant(initialPosition: new Vector2Int(3, 3));
            board.Place(player, new Vector2Int(3, 3));

            var enemy = new EnemyOccupant(EnemyArchetype.Rook, initialPosition: new Vector2Int(3, 1));
            enemy.SkipNextTurn = true; // Stunned!
            board.Place(enemy, new Vector2Int(3, 1));

            // Turn 1: Stunned enemy must wait and clear stun flag
            var intent = EnemyAIFactory.EvaluateEnemy(board, enemy);
            Assert(intent.IntentType == IntentType.Wait, "Stunned enemy must return IntentType.Wait");
            Assert(intent.GeneratedAction == null, "Stunned enemy generates no movement action");
            Assert(!enemy.SkipNextTurn, "SkipNextTurn flag must be cleared back to false after turn skip");

            // Turn 2: Stun is now cleared, enemy acts normally and attacks player
            var nextIntent = EnemyAIFactory.EvaluateEnemy(board, enemy);
            Assert(nextIntent.IntentType == IntentType.AttackPush, "After stun clears, enemy acts normally");
        }

        private static void TestDetectionRange()
        {
            var board = new GameBoard(10, 10);
            var player = new PlayerOccupant(initialPosition: new Vector2Int(0, 6));
            board.Place(player, new Vector2Int(0, 6));

            // Rook with DetectionRange = 4 at (0, 0). Player is 6 tiles away (> 4)
            var rook = new EnemyOccupant(EnemyArchetype.Rook, detectionRange: 4, initialPosition: new Vector2Int(0, 0));
            board.Place(rook, new Vector2Int(0, 0));

            var intentOut = EnemyAIFactory.EvaluateEnemy(board, rook);
            Assert(intentOut.IntentType == IntentType.Wait, "Enemy beyond detection range must Wait");

            // Move player to (0, 4), distance is now 4 (within detection range)
            board.Move(player, new Vector2Int(0, 4));

            var intentIn = EnemyAIFactory.EvaluateEnemy(board, rook);
            Assert(intentIn.IntentType == IntentType.AttackPush, "Enemy within detection range acts on player");
        }

        private static void TestBlockedLineOfSightDoesNotAttackThroughObstacle()
        {
            var board = new GameBoard(8, 8);
            var player = new PlayerOccupant(initialPosition: new Vector2Int(0, 5));
            board.Place(player, new Vector2Int(0, 5));

            var rook = new EnemyOccupant(EnemyArchetype.Rook, initialPosition: new Vector2Int(0, 0));
            board.Place(rook, new Vector2Int(0, 0));

            // Place an impassable pillar at (0, 3) blocking the line of sight between Rook and Player
            var pillar = new ObstacleOccupant(ObstacleType.Pillar, initialPosition: new Vector2Int(0, 3));
            board.Place(pillar, new Vector2Int(0, 3));

            var intent = EnemyAIFactory.EvaluateEnemy(board, rook);
            // Must NOT attack the player through the pillar!
            Assert(intent.IntentType != IntentType.AttackPush && intent.IntentType != IntentType.BlockedPush,
                "Rook cannot attack through intervening pillar");
            // Rook should advance up to pillar or choose another path
            Assert(intent.TargetPosition.y < 3, "Rook target position stops before the pillar");
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/Tests/Run Enemy AI Tests")]
        public static void RunFromEditorMenu()
        {
            RunAllTests();
        }
#endif
    }
}
