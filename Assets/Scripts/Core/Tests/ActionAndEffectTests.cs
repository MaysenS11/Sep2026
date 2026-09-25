using System;
using System.Collections.Generic;
using Core.Actions;
using Core.Board;
using Core.Effects;
using Core.Occupants;
using UnityEngine;

namespace Core.Tests
{
    /// Pure C# unit tests and verification suite for Phase 2: Action and Effect Primitives.
    /// Verifies MoveAction, JumpAction, AttackPushAction, and BlockedPushAction recoil mechanics.
    public static class ActionAndEffectTests
    {
        public static void RunAllTests()
        {
            Debug.Log("[ActionAndEffectTests] Starting test suite...");

            TestStandardMoveAction();
            TestMultiStepMoveActionAndWallBlocking();
            TestStandardJumpAction();
            TestJumpActionBlockedDestination();
            TestAttackPushAction();
            TestBlockedPushActionStandardEnemy();
            TestBlockedPushActionDistantEnemyRepositioning();
            TestBlockedPushActionKnightOriginRebound();
            TestBlockedPushRecoilDestroysEnemy();
            TestOpenChestBlockerRecoil();
            TestDestructiblePropBlockerRecoil();

            Debug.Log("[ActionAndEffectTests] All 11 test cases passed successfully!");
        }

        private static void Assert(bool condition, string testName)
        {
            if (!condition)
            {
                throw new Exception($"[ActionAndEffectTests] Assertion failed in {testName}!");
            }
        }

        private static void TestStandardMoveAction()
        {
            var board = new GameBoard(5, 5);
            var pawn = new EnemyOccupant(EnemyArchetype.Pawn, initialPosition: new Vector2Int(1, 1));
            board.Place(pawn, new Vector2Int(1, 1));

            var moveAction = new MoveAction(pawn, new Vector2Int(1, 2));
            var effects = moveAction.Execute(board);

            Assert(board.GetOccupant(new Vector2Int(1, 2)) == pawn, "Pawn should be at destination (1,2)");
            Assert(!board.IsOccupied(new Vector2Int(1, 1)), "Original tile (1,1) should be vacant");
            Assert(pawn.GridPosition == new Vector2Int(1, 2), "Pawn GridPosition updated");

            var moveEffect = effects.Find(e => e is MoveEffect) as MoveEffect;
            Assert(moveEffect != null, "MoveEffect should be emitted");
            Assert(moveEffect.OccupantId == pawn.Id, "MoveEffect has correct occupant ID");
            Assert(moveEffect.StartPos == new Vector2Int(1, 1), "MoveEffect startPos is (1,1)");
            Assert(moveEffect.EndPos == new Vector2Int(1, 2), "MoveEffect endPos is (1,2)");
        }

        private static void TestMultiStepMoveActionAndWallBlocking()
        {
            var board = new GameBoard(5, 5);
            var rook = new EnemyOccupant(EnemyArchetype.Rook, initialPosition: new Vector2Int(0, 0));
            board.Place(rook, new Vector2Int(0, 0));

            // Valid 3-step slide
            var moveAction = new MoveAction(rook, new Vector2Int(0, 3));
            var effects = moveAction.Execute(board);

            Assert(board.GetOccupant(new Vector2Int(0, 3)) == rook, "Rook reached (0,3)");
            Assert(!board.IsOccupied(new Vector2Int(0, 0)), "Original tile vacated");
            var moveEffect = effects.Find(e => e is MoveEffect) as MoveEffect;
            Assert(moveEffect != null, "MoveEffect emitted for slide");
            Assert(moveEffect.Path.Length == 4, "Path has 4 waypoints: (0,0) to (0,3)");

            // Add wall at (0, 2) blocking return slide
            board.SetWall(new Vector2Int(0, 2), true);
            var blockedMove = new MoveAction(rook, new Vector2Int(0, 0));
            var blockedEffects = blockedMove.Execute(board);

            Assert(blockedEffects.Count == 0, "Blocked move returns no effects");
            Assert(board.GetOccupant(new Vector2Int(0, 3)) == rook, "Rook stays at (0,3) due to wall at (0,2)");
        }

        private static void TestStandardJumpAction()
        {
            var board = new GameBoard(5, 5);
            var knight = new EnemyOccupant(EnemyArchetype.Knight, initialPosition: new Vector2Int(1, 1));
            board.Place(knight, new Vector2Int(1, 1));

            // Place an intervening obstacle at (1, 2) and wall at (2, 2)
            var obstacle = new ObstacleOccupant(ObstacleType.Pillar, initialPosition: new Vector2Int(1, 2));
            board.Place(obstacle, new Vector2Int(1, 2));
            board.SetWall(new Vector2Int(2, 2), true);

            // Knight jumps over (1,2) and (2,2) to empty (2, 3)
            var jumpAction = new JumpAction(knight, new Vector2Int(2, 3), arcHeight: 1.5f);
            var effects = jumpAction.Execute(board);

            Assert(board.GetOccupant(new Vector2Int(2, 3)) == knight, "Knight landed at (2,3)");
            Assert(!board.IsOccupied(new Vector2Int(1, 1)), "Knight start tile is vacant");
            Assert(board.GetOccupant(new Vector2Int(1, 2)) == obstacle, "Intervening obstacle untouched");

            var jumpEffect = effects.Find(e => e is JumpEffect) as JumpEffect;
            Assert(jumpEffect != null, "JumpEffect should be emitted");
            Assert(jumpEffect.OccupantId == knight.Id, "JumpEffect has knight ID");
            Assert(jumpEffect.StartPos == new Vector2Int(1, 1), "JumpEffect startPos is (1,1)");
            Assert(jumpEffect.EndPos == new Vector2Int(2, 3), "JumpEffect endPos is (2,3)");
            Assert(Math.Abs(jumpEffect.ArcHeight - 1.5f) < 0.001f, "JumpEffect arcHeight is 1.5");
        }

        private static void TestJumpActionBlockedDestination()
        {
            var board = new GameBoard(5, 5);
            var knight = new EnemyOccupant(EnemyArchetype.Knight, initialPosition: new Vector2Int(1, 1));
            board.Place(knight, new Vector2Int(1, 1));

            // Target destination is a wall
            board.SetWall(new Vector2Int(2, 3), true);

            var jumpAction = new JumpAction(knight, new Vector2Int(2, 3));
            var effects = jumpAction.Execute(board);

            Assert(effects.Count == 0, "Blocked jump emits no effects");
            Assert(board.GetOccupant(new Vector2Int(1, 1)) == knight, "Knight remains at origin");
        }

        private static void TestAttackPushAction()
        {
            var board = new GameBoard(5, 5);
            var player = new PlayerOccupant(maxHealth: 6, attackDamage: 2, initialPosition: new Vector2Int(2, 2));
            board.Place(player, new Vector2Int(2, 2));

            var enemy = new EnemyOccupant(EnemyArchetype.Pawn, maxHealth: 4, attackDamage: 2, initialPosition: new Vector2Int(1, 2));
            board.Place(enemy, new Vector2Int(1, 2));

            // Push direction is East (1, 0). Push destination is (3, 2), which is clear.
            var attackPush = new AttackPushAction(enemy, player, BoardCoordinate.East, damage: 2);
            var effects = attackPush.Execute(board);

            // Player pushed to (3, 2)
            Assert(board.GetOccupant(new Vector2Int(3, 2)) == player, "Player pushed to (3,2)");
            Assert(player.GridPosition == new Vector2Int(3, 2), "Player position updated to (3,2)");
            Assert(player.CurrentHealth == 4, "Player took 2 damage (HP: 4/6)");

            // Enemy moved into Player's former tile (2, 2)
            Assert(board.GetOccupant(new Vector2Int(2, 2)) == enemy, "Enemy moved into (2,2)");
            Assert(enemy.GridPosition == new Vector2Int(2, 2), "Enemy position updated to (2,2)");
            Assert(!board.IsOccupied(new Vector2Int(1, 2)), "Enemy start tile (1,2) is vacant");

            // Verify effects
            var damageEffect = effects.Find(e => e is DamageTakenEffect) as DamageTakenEffect;
            Assert(damageEffect != null, "DamageTakenEffect emitted");
            Assert(damageEffect.OccupantId == player.Id, "Damage taken by player");
            Assert(damageEffect.DamageAmount == 2, "Damage amount is 2");
            Assert(damageEffect.RemainingHealth == 4, "Remaining health is 4");

            var pushEffect = effects.Find(e => e is PushDisplacementEffect) as PushDisplacementEffect;
            Assert(pushEffect != null, "PushDisplacementEffect emitted");
            Assert(pushEffect.PushedOccupantId == player.Id, "Pushed occupant is player");
            Assert(pushEffect.StartPos == new Vector2Int(2, 2), "Push start is (2,2)");
            Assert(pushEffect.EndPos == new Vector2Int(3, 2), "Push end is (3,2)");
            Assert(pushEffect.PushDirection == BoardCoordinate.East, "Push dir is East");

            var lungeEffect = effects.Find(e => e is AttackLungeEffect) as AttackLungeEffect;
            Assert(lungeEffect != null, "AttackLungeEffect emitted");
            Assert(lungeEffect.AttackerId == enemy.Id, "Attacker is enemy");
            Assert(lungeEffect.TargetPos == new Vector2Int(2, 2), "Lunge target is (2,2)");
        }

        private static void TestBlockedPushActionStandardEnemy()
        {
            var board = new GameBoard(5, 5);
            var player = new PlayerOccupant(maxHealth: 6, initialPosition: new Vector2Int(2, 2));
            board.Place(player, new Vector2Int(2, 2));

            // Wall directly behind player at (3, 2)
            board.SetWall(new Vector2Int(3, 2), true);

            var pawn = new EnemyOccupant(EnemyArchetype.Pawn, maxHealth: 4, initialPosition: new Vector2Int(1, 2));
            board.Place(pawn, new Vector2Int(1, 2));

            // Pawn attacks pushing East into wall at (3, 2)
            var blockedPush = new BlockedPushAction(pawn, player, BoardCoordinate.East);
            var effects = blockedPush.Execute(board);

            // 1. Player does NOT move
            Assert(board.GetOccupant(new Vector2Int(2, 2)) == player, "Player must remain at (2,2)");
            Assert(player.GridPosition == new Vector2Int(2, 2), "Player GridPosition unchanged");
            Assert(player.CurrentHealth == 5, "Player took 1 incoming attack damage (HP: 5/6)");

            // 2. Attacking enemy CANNOT enter player's tile, stays at origin
            Assert(board.GetOccupant(new Vector2Int(1, 2)) == pawn, "Pawn remains at origin (1,2)");
            Assert(pawn.GridPosition == new Vector2Int(1, 2), "Pawn GridPosition unchanged");

            // 3. Attacking enemy takes 1 recoil damage
            Assert(pawn.CurrentHealth == 3, "Pawn took 1 recoil damage (HP: 3/4)");
            Assert(pawn.SkipNextTurn, "Pawn is stunned (SkipNextTurn = true)");

            // 4. BlockedPushRecoilEffect telemetry
            var recoilEffect = effects.Find(e => e is BlockedPushRecoilEffect) as BlockedPushRecoilEffect;
            Assert(recoilEffect != null, "BlockedPushRecoilEffect emitted");
            Assert(recoilEffect.AttackerId == pawn.Id, "Attacker ID matches");
            Assert(recoilEffect.AttackerStartPos == new Vector2Int(1, 2), "Start pos is (1,2)");
            Assert(recoilEffect.PlayerTile == new Vector2Int(2, 2), "Player tile is (2,2)");
            Assert(recoilEffect.RecoilEndTile == new Vector2Int(1, 2), "Recoil end tile is origin (1,2)");
            Assert(recoilEffect.RecoilDamage == 1, "Recoil damage is 1");
            Assert(recoilEffect.BlockerPos == new Vector2Int(3, 2), "Blocker pos is wall at (3,2)");

            var pawnDamageEffect = effects.Find(e => e is DamageTakenEffect d && d.OccupantId == pawn.Id) as DamageTakenEffect;
            Assert(pawnDamageEffect != null, "DamageTakenEffect emitted for recoil");
            Assert(pawnDamageEffect.OccupantId == pawn.Id, "Damage taken by pawn");
            Assert(pawnDamageEffect.DamageAmount == 1, "Recoil damage amount is 1");

            var playerDamageEffect = effects.Find(e => e is DamageTakenEffect d && d.OccupantId == player.Id) as DamageTakenEffect;
            Assert(playerDamageEffect != null, "DamageTakenEffect emitted for player incoming attack damage");
            Assert(playerDamageEffect.OccupantId == player.Id, "Damage taken by player");
            Assert(playerDamageEffect.DamageAmount == 1, "Player incoming damage amount is 1");
        }

        private static void TestBlockedPushActionDistantEnemyRepositioning()
        {
            var board = new GameBoard(8, 8);
            var player = new PlayerOccupant(maxHealth: 6, initialPosition: new Vector2Int(3, 2));
            board.Place(player, new Vector2Int(3, 2));

            // Wall directly behind player at (4, 2)
            board.SetWall(new Vector2Int(4, 2), true);

            // Rook starts at (0, 2) and approaches player at (3, 2)
            var rook = new EnemyOccupant(EnemyArchetype.Rook, maxHealth: 4, initialPosition: new Vector2Int(0, 2));
            board.Place(rook, new Vector2Int(0, 2));

            var blockedPush = new BlockedPushAction(rook, player, BoardCoordinate.East);
            var effects = blockedPush.Execute(board);

            // Player does not move
            Assert(board.GetOccupant(new Vector2Int(3, 2)) == player, "Player remains at (3,2)");

            // Rook stops 1 tile short of the player along its approach path: (2, 2)!
            Assert(board.GetOccupant(new Vector2Int(2, 2)) == rook, "Rook stops at (2,2) 1 tile short of player");
            Assert(!board.IsOccupied(new Vector2Int(0, 2)), "Rook start tile (0,2) is vacant");
            Assert(rook.GridPosition == new Vector2Int(2, 2), "Rook position is (2,2)");

            // Rook took 1 recoil damage
            Assert(rook.CurrentHealth == 3, "Rook took 1 recoil damage (HP: 3/4)");

            var recoilEffect = effects.Find(e => e is BlockedPushRecoilEffect) as BlockedPushRecoilEffect;
            Assert(recoilEffect != null, "BlockedPushRecoilEffect emitted");
            Assert(recoilEffect.AttackerStartPos == new Vector2Int(0, 2), "Start pos is (0,2)");
            Assert(recoilEffect.RecoilEndTile == new Vector2Int(2, 2), "Recoil end tile is 1 tile short: (2,2)");
        }

        private static void TestBlockedPushActionKnightOriginRebound()
        {
            var board = new GameBoard(5, 5);
            var player = new PlayerOccupant(maxHealth: 6, initialPosition: new Vector2Int(2, 2));
            board.Place(player, new Vector2Int(2, 2));

            // Wall behind player at (3, 2)
            board.SetWall(new Vector2Int(3, 2), true);

            // Knight at (1, 0) leaps to attack player at (2, 2)
            var knight = new EnemyOccupant(EnemyArchetype.Knight, maxHealth: 4, initialPosition: new Vector2Int(1, 0));
            board.Place(knight, new Vector2Int(1, 0));

            var blockedPush = new BlockedPushAction(knight, player, BoardCoordinate.East);
            var effects = blockedPush.Execute(board);

            // Player does not move
            Assert(board.GetOccupant(new Vector2Int(2, 2)) == player, "Player remains at (2,2)");

            // Knight CANNOT enter player tile (2, 2) and bounces back to ORIGINAL STARTING TILE (1, 0)
            Assert(board.GetOccupant(new Vector2Int(1, 0)) == knight, "Knight bounced back to origin (1,0)");
            Assert(knight.GridPosition == new Vector2Int(1, 0), "Knight position is origin (1,0)");
            Assert(knight.CurrentHealth == 3, "Knight took 1 recoil damage (HP: 3/4)");

            var recoilEffect = effects.Find(e => e is BlockedPushRecoilEffect) as BlockedPushRecoilEffect;
            Assert(recoilEffect != null, "BlockedPushRecoilEffect emitted");
            Assert(recoilEffect.AttackerStartPos == new Vector2Int(1, 0), "Start pos is (1,0)");
            Assert(recoilEffect.RecoilEndTile == new Vector2Int(1, 0), "Knight rebound tile is origin (1,0)");
        }

        private static void TestBlockedPushRecoilDestroysEnemy()
        {
            var board = new GameBoard(5, 5);
            var player = new PlayerOccupant(maxHealth: 6, initialPosition: new Vector2Int(2, 2));
            board.Place(player, new Vector2Int(2, 2));
            board.SetWall(new Vector2Int(3, 2), true);

            // Enemy with only 1 HP
            var weakEnemy = new EnemyOccupant(EnemyArchetype.Pawn, maxHealth: 1, initialPosition: new Vector2Int(1, 2));
            board.Place(weakEnemy, new Vector2Int(1, 2));

            var blockedPush = new BlockedPushAction(weakEnemy, player, BoardCoordinate.East);
            var effects = blockedPush.Execute(board);

            // Enemy took 1 recoil damage and died
            Assert(weakEnemy.IsDead, "Enemy is dead from recoil damage");
            Assert(!board.IsOccupied(new Vector2Int(1, 2)), "Dead enemy is removed from board");

            var deathEffect = effects.Find(e => e is OccupantDestroyedEffect) as OccupantDestroyedEffect;
            Assert(deathEffect != null, "OccupantDestroyedEffect emitted");
            Assert(deathEffect.OccupantId == weakEnemy.Id, "Destroyed occupant ID matches");
        }

        private static void TestOpenChestBlockerRecoil()
        {
            var board = new GameBoard(5, 5);
            var player = new PlayerOccupant(maxHealth: 6, initialPosition: new Vector2Int(2, 2));
            board.Place(player, new Vector2Int(2, 2));

            // Chest placed at (3, 2)
            var chest = new ChestOccupant(initialPosition: new Vector2Int(3, 2));
            board.Place(chest, new Vector2Int(3, 2));

            // Attack chest to open it
            var openEffects = chest.TakeDamage(1);
            Assert(chest.IsOpen, "Chest is now open");
            Assert(openEffects.Exists(e => e is ChestOpenedEffect), "ChestOpenedEffect emitted");

            // CRITICAL VERIFICATION: Open chest remains on board as impassable blocker!
            Assert(board.IsOccupied(new Vector2Int(3, 2)), "Open chest occupies (3,2)");
            Assert(!board.CanEnter(new Vector2Int(3, 2)), "Open chest cell is impassable");

            // Enemy attacks player, push destination is open chest at (3, 2)
            var enemy = new EnemyOccupant(EnemyArchetype.Pawn, maxHealth: 4, initialPosition: new Vector2Int(1, 2));
            board.Place(enemy, new Vector2Int(1, 2));

            var blockedPush = new BlockedPushAction(enemy, player, BoardCoordinate.East);
            var effects = blockedPush.Execute(board);

            // Blocked by open chest
            Assert(board.GetOccupant(new Vector2Int(2, 2)) == player, "Player blocked by open chest");
            Assert(enemy.CurrentHealth == 3, "Enemy takes recoil damage");

            var recoilEffect = effects.Find(e => e is BlockedPushRecoilEffect) as BlockedPushRecoilEffect;
            Assert(recoilEffect != null, "BlockedPushRecoilEffect emitted");
            Assert(recoilEffect.BlockerPos == new Vector2Int(3, 2), "Blocker pos is chest at (3,2)");
            Assert(recoilEffect.BlockerOccupantId == chest.Id, "Blocker ID matches chest ID");
        }

        private static void TestDestructiblePropBlockerRecoil()
        {
            var board = new GameBoard(5, 5);
            var player = new PlayerOccupant(maxHealth: 6, initialPosition: new Vector2Int(2, 2));
            board.Place(player, new Vector2Int(2, 2));

            // Barrel placed at (3, 2)
            var barrel = new DestructiblePropOccupant(DestructiblePropType.Barrel, maxHealth: 2, initialPosition: new Vector2Int(3, 2));
            board.Place(barrel, new Vector2Int(3, 2));

            var enemy = new EnemyOccupant(EnemyArchetype.Pawn, maxHealth: 4, initialPosition: new Vector2Int(1, 2));
            board.Place(enemy, new Vector2Int(1, 2));

            var blockedPush = new BlockedPushAction(enemy, player, BoardCoordinate.East);
            var effects = blockedPush.Execute(board);

            // Blocked by barrel
            Assert(board.GetOccupant(new Vector2Int(2, 2)) == player, "Player blocked by barrel");
            Assert(enemy.CurrentHealth == 3, "Enemy takes recoil damage");
            Assert(barrel.CurrentHealth == 2, "Barrel remains undamaged");

            var recoilEffect = effects.Find(e => e is BlockedPushRecoilEffect) as BlockedPushRecoilEffect;
            Assert(recoilEffect != null, "BlockedPushRecoilEffect emitted");
            Assert(recoilEffect.BlockerPos == new Vector2Int(3, 2), "Blocker pos is barrel at (3,2)");
            Assert(recoilEffect.BlockerOccupantId == barrel.Id, "Blocker ID matches barrel ID");
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/Tests/Run Action and Effect Tests")]
        public static void RunFromEditorMenu()
        {
            RunAllTests();
        }
#endif
    }
}
