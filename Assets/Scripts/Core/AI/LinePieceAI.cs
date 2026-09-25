using System.Collections.Generic;
using Core.Actions;
using Core.Board;
using Core.Occupants;
using UnityEngine;

namespace Core.AI
{
    /// Base class for straight-line raycasting chess pieces (Bishop, Rook, Queen).
    /// Implements raycast attack targeting with blocked-push 1-step-short recoil,
    /// and multi-step line advancement toward the player.
    public abstract class LinePieceAI : BaseEnemyAI
    {
        /// The raycast directions permitted for this archetype.
        protected abstract Vector2Int[] AllowedDirections { get; }

        protected override EnemyIntent DecideIntent(GameBoard board, EnemyOccupant enemy, PlayerOccupant player)
        {
            Vector2Int enemyPos = enemy.GridPosition;
            Vector2Int playerPos = player.GridPosition;

            foreach (var dir in AllowedDirections)
            {
                var rayPath = new List<Vector2Int> { enemyPos };
                Vector2Int current = enemyPos + dir;

                while (board.IsInBounds(current) && !board.IsWall(current))
                {
                    rayPath.Add(current);

                    if (current == playerPos)
                    {
                        Vector2Int pushDir = dir;

                        Vector2Int recoilTile = (rayPath.Count <= 2) ? enemyPos : (playerPos - dir);
                        var blockedPath = (rayPath.Count <= 2)
                            ? new List<Vector2Int> { enemyPos }
                            : rayPath.GetRange(0, rayPath.Count - 1);

                        return CreateAttackOrBlockedPushIntent(
                            board: board,
                            enemy: enemy,
                            player: player,
                            pushDir: pushDir,
                            attackPath: rayPath,
                            blockedRecoilTile: recoilTile,
                            blockedPath: blockedPath
                        );
                    }

                    if (board.IsOccupied(current))
                    {
                        break;
                    }

                    current += dir;
                }
            }

            int maxSteps = enemy.MaxLineSteps > 0 ? enemy.MaxLineSteps : 1;
            Vector2Int bestDestination = enemyPos;
            List<Vector2Int> bestPath = null;
            float bestScore = float.MaxValue;

            foreach (var dir in AllowedDirections)
            {
                var currentPath = new List<Vector2Int> { enemyPos };
                Vector2Int current = enemyPos + dir;

                for (int step = 1; step <= maxSteps; step++)
                {
                    if (!board.CanEnter(current))
                    {
                        break;
                    }

                    currentPath.Add(current);
                    float score = ScoreCandidateTile(current, playerPos, enemy);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestDestination = current;
                        bestPath = new List<Vector2Int>(currentPath);
                    }

                    current += dir;
                }
            }

            if (bestPath != null && bestDestination != enemyPos)
            {
                var action = new MoveAction(enemy, bestPath.ToArray());
                return new EnemyIntent(
                    enemy: enemy,
                    intentType: IntentType.Move,
                    targetPosition: bestDestination,
                    path: bestPath,
                    pushDirection: Vector2Int.zero,
                    generatedAction: action
                );
            }

            return EnemyIntent.CreateWait(enemy);
        }

        /// <summary>
        /// Scores a candidate move destination for line pieces. Lower score is preferred.
        /// Overridden by Smart pieces (Rook, Queen) to prioritize establishing line-of-sight.
        /// </summary>
        protected virtual float ScoreCandidateTile(Vector2Int candidate, Vector2Int playerPos, EnemyOccupant enemy)
        {
            return BoardCoordinate.EuclideanDistance(candidate, playerPos);
        }
    }
}
