using System.Collections.Generic;
using Core.Actions;
using Core.Board;
using Core.Occupants;
using UnityEngine;

namespace Core.AI
{
    /// AI strategy for Knight archetype.
    /// Can leap over intervening obstacles and units.
    /// Evaluates 8 L-shaped jump offsets to attack or move closest to player.
    /// Rebounds to start tile on blocked push attack.
    public class KnightAI : BaseEnemyAI
    {
        protected override EnemyIntent DecideIntent(GameBoard board, EnemyOccupant enemy, PlayerOccupant player)
        {
            Vector2Int enemyPos = enemy.GridPosition;
            Vector2Int playerPos = player.GridPosition;
            Vector2Int delta = playerPos - enemyPos;

            if (BoardCoordinate.IsKnightOffset(delta))
            {
                Vector2Int pushDir = BoardCoordinate.GetDominantCardinalDirection(enemyPos, playerPos);
                var attackPath = new List<Vector2Int> { enemyPos, playerPos };

                return CreateAttackOrBlockedPushIntent(
                    board: board,
                    enemy: enemy,
                    player: player,
                    pushDir: pushDir,
                    attackPath: attackPath,
                    blockedRecoilTile: enemyPos,
                    blockedPath: new List<Vector2Int> { enemyPos }
                );
            }

            Vector2Int bestTile = enemyPos;
            float bestDist = float.MaxValue;
            bool foundCandidate = false;

            foreach (var offset in BoardCoordinate.KnightOffsets)
            {
                Vector2Int candidate = enemyPos + offset;
                if (board.CanEnter(candidate))
                {
                    float dist = BoardCoordinate.EuclideanDistance(candidate, playerPos);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestTile = candidate;
                        foundCandidate = true;
                    }
                }
            }

            if (foundCandidate)
            {
                var jumpPath = new List<Vector2Int> { enemyPos, bestTile };
                var action = new JumpAction(enemy, bestTile);
                return new EnemyIntent(
                    enemy: enemy,
                    intentType: IntentType.Jump,
                    targetPosition: bestTile,
                    path: jumpPath,
                    pushDirection: Vector2Int.zero,
                    generatedAction: action
                );
            }

            return EnemyIntent.CreateWait(enemy);
        }
    }
}
