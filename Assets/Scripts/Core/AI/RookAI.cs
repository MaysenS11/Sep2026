using System;
using Core.Board;
using Core.Occupants;
using UnityEngine;

namespace Core.AI
{
    /// <summary>
    /// AI strategy for Rook archetype.
    /// Smart intelligence: Computes shortest Manhattan path to establish orthogonal line-of-sight to player.
    /// Moves and attacks along the 4 cardinal directions.
    /// Slides into player tile on clear push; stops 1 tile short taking recoil on blocked push.
    /// </summary>
    public class RookAI : LinePieceAI
    {
        protected override Vector2Int[] AllowedDirections => BoardCoordinate.CardinalDirections;

        protected override float ScoreCandidateTile(Vector2Int candidate, Vector2Int playerPos, EnemyOccupant enemy)
        {
            if (enemy.IntelligenceLevel == EnemySmartness.Smart)
            {
                // Smart: Computes shortest Manhattan path to establish orthogonal line-of-sight
                // Orthogonal LOS is established if candidate is on the same column (x == px) or same row (y == py)
                int distToOrthogonalLOS = Math.Min(Math.Abs(candidate.x - playerPos.x), Math.Abs(candidate.y - playerPos.y));
                int manhattanDist = BoardCoordinate.ManhattanDistance(candidate, playerPos);

                // Priority: 1. Minimum steps to establish orthogonal LOS, 2. Shortest Manhattan distance to player
                return (distToOrthogonalLOS * 1000f) + manhattanDist;
            }

            return base.ScoreCandidateTile(candidate, playerPos, enemy);
        }
    }
}
