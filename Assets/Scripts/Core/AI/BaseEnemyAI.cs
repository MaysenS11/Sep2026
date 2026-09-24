using System.Collections.Generic;
using Core.Actions;
using Core.Board;
using Core.Occupants;
using UnityEngine;

namespace Core.AI
{
    /// Abstract base strategy for enemy piece AI.
    /// Handles common lifecycle concerns including stun turn skipping, player detection range,
    /// and standard attack push vs blocked push resolution.
    public abstract class BaseEnemyAI : IEnemyAI
    {
        public virtual EnemyIntent EvaluateIntent(GameBoard board, EnemyOccupant enemy)
        {
            if (enemy == null) return null;

            if (enemy.SkipNextTurn)
            {
                enemy.SkipNextTurn = false;
                return EnemyIntent.CreateWait(enemy);
            }

            if (board == null)
            {
                return EnemyIntent.CreateWait(enemy);
            }

            var player = board.FindPlayer();
            if (player == null || player.IsDead)
            {
                return EnemyIntent.CreateWait(enemy);
            }

            if (enemy.DetectionRange > 0)
            {
                int distance = BoardCoordinate.ChebyshevDistance(enemy.GridPosition, player.GridPosition);
                if (distance > enemy.DetectionRange)
                {
                    return EnemyIntent.CreateWait(enemy);
                }
            }

            return DecideIntent(board, enemy, player);
        }

        /// Archetype-specific decision algorithm when player is detected and enemy is active.
        protected abstract EnemyIntent DecideIntent(GameBoard board, EnemyOccupant enemy, PlayerOccupant player);

        /// Evaluates whether an attack against the player pushes them to a clear tile or triggers blocked push recoil.
        /// Generates either AttackPushAction or BlockedPushAction accordingly.
        protected EnemyIntent CreateAttackOrBlockedPushIntent(
            GameBoard board,
            EnemyOccupant enemy,
            PlayerOccupant player,
            Vector2Int pushDir,
            List<Vector2Int> attackPath,
            Vector2Int blockedRecoilTile,
            List<Vector2Int> blockedPath = null)
        {
            Vector2Int playerTile = player.GridPosition;
            Vector2Int pushDest = playerTile + pushDir;

            bool canPush = board.CanEnter(pushDest);

            if (canPush)
            {
                var action = new AttackPushAction(enemy, player, pushDir, enemy.AttackDamage);
                return new EnemyIntent(
                    enemy: enemy,
                    intentType: IntentType.AttackPush,
                    targetPosition: playerTile,
                    path: attackPath,
                    pushDirection: pushDir,
                    generatedAction: action
                );
            }
            else
            {
                var action = new BlockedPushAction(enemy, player, pushDir, recoilDamage: 1, blockerPos: pushDest);
                var path = blockedPath ?? new List<Vector2Int> { enemy.GridPosition };
                return new EnemyIntent(
                    enemy: enemy,
                    intentType: IntentType.BlockedPush,
                    targetPosition: blockedRecoilTile,
                    path: path,
                    pushDirection: pushDir,
                    generatedAction: action
                );
            }
        }
    }
}
