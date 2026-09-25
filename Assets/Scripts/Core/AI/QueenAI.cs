using System;
using Core.Board;
using Core.Occupants;
using UnityEngine;

namespace Core.AI
{
    /// <summary>
    /// AI strategy for Queen archetype.
    /// Combines Rook (cardinal) and Bishop (diagonal) raycasting across all 8 directions.
    /// Smart intelligence: Relentless high-priority tracking across cardinal and diagonal axes.
    /// Prioritizes attacking the player if clear line of sight; handles clear push vs blocked push recoil appropriately.
    /// </summary>
    public class QueenAI : LinePieceAI
    {
        protected override Vector2Int[] AllowedDirections => BoardCoordinate.AllDirections;

        protected override float ScoreCandidateTile(Vector2Int candidate, Vector2Int playerPos, EnemyOccupant enemy)
        {
            if (enemy.IntelligenceLevel == EnemySmartness.Smart)
            {
                // Cardinal line-of-sight distance
                int cardinalDist = Math.Min(Math.Abs(candidate.x - playerPos.x), Math.Abs(candidate.y - playerPos.y));
                // Diagonal line-of-sight distance
                int diagonalDist = Math.Abs(Math.Abs(candidate.x - playerPos.x) - Math.Abs(candidate.y - playerPos.y));
                // Distance to any line-of-sight
                int minLOS = Math.Min(cardinalDist, diagonalDist);
                float dist = BoardCoordinate.EuclideanDistance(candidate, playerPos);

                // Priority: 1. Minimizing steps to establish line-of-sight, 2. Shortest distance to player
                return (minLOS * 1000f) + dist;
            }

            return base.ScoreCandidateTile(candidate, playerPos, enemy);
        }
    }
}
