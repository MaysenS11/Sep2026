using System;
using System.Collections.Generic;
using Core.Actions;
using Core.Board;
using Core.Occupants;
using UnityEngine;

namespace Core.AI
{
    /// AI strategy for Pawn archetype.
    /// Moves 1 step toward the player along primary axis if clear.
    /// Attacks the player if adjacent (cardinal or diagonal, matching enemy.UsesDiagonalAttack).
    public class PawnAI : BaseEnemyAI
    {
        protected override EnemyIntent DecideIntent(GameBoard board, EnemyOccupant enemy, PlayerOccupant player)
        {
            Vector2Int enemyPos = enemy.GridPosition;
            Vector2Int playerPos = player.GridPosition;
            Vector2Int delta = playerPos - enemyPos;

            int dx = delta.x;
            int dy = delta.y;
            int absDx = Math.Abs(dx);
            int absDy = Math.Abs(dy);

            bool isAdjacent = (absDx == 1 && absDy == 1);

            if (isAdjacent)
            {
                Vector2Int pushDir = BoardCoordinate.GetSignVector(delta);
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

            Vector2Int primaryStep;
            Vector2Int secondaryStep = Vector2Int.zero;

            if (absDx >= absDy)
            {
                primaryStep = new Vector2Int(Math.Sign(dx), 0);
                if (dy != 0)
                {
                    secondaryStep = new Vector2Int(0, Math.Sign(dy));
                }
            }
            else
            {
                primaryStep = new Vector2Int(0, Math.Sign(dy));
                if (dx != 0)
                {
                    secondaryStep = new Vector2Int(Math.Sign(dx), 0);
                }
            }

            // Dumb Intelligence: Inefficient, erratic path selection when multiple axes are open
            if (enemy.IntelligenceLevel == EnemySmartness.Dumb && dx != 0 && dy != 0)
            {
                // Erratic preference: alternates secondary axis based on position hash
                bool erratic = ((enemyPos.x * 3 + enemyPos.y * 7 + enemy.Id) % 2) == 0;
                if (erratic && secondaryStep != Vector2Int.zero && board.CanEnter(enemyPos + secondaryStep))
                {
                    var temp = primaryStep;
                    primaryStep = secondaryStep;
                    secondaryStep = temp;
                }
            }

            Vector2Int targetStep = Vector2Int.zero;
            if (board.CanEnter(enemyPos + primaryStep))
            {
                targetStep = enemyPos + primaryStep;
            }
            else if (secondaryStep != Vector2Int.zero && board.CanEnter(enemyPos + secondaryStep))
            {
                targetStep = enemyPos + secondaryStep;
            }

            if (targetStep != Vector2Int.zero)
            {
                var movePath = new List<Vector2Int> { enemyPos, targetStep };
                var action = new MoveAction(enemy, targetStep);
                return new EnemyIntent(
                    enemy: enemy,
                    intentType: IntentType.Move,
                    targetPosition: targetStep,
                    path: movePath,
                    pushDirection: Vector2Int.zero,
                    generatedAction: action
                );
            }

            return EnemyIntent.CreateWait(enemy);
        }
    }
}
