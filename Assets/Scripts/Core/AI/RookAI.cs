using System;
using Core.Board;
using Core.Occupants;
using UnityEngine;

namespace Core.AI
{
    public class RookAI : LinePieceAI
    {
        protected override Vector2Int[] AllowedDirections => BoardCoordinate.CardinalDirections;

        protected override float ScoreCandidateTile(Vector2Int candidate, Vector2Int playerPos, EnemyOccupant enemy)
        {
            if (enemy.IntelligenceLevel == EnemySmartness.Smart)
            {
                int distToOrthogonalLOS = Math.Min(Math.Abs(candidate.x - playerPos.x), Math.Abs(candidate.y - playerPos.y));
                int manhattanDist = BoardCoordinate.ManhattanDistance(candidate, playerPos);

                return (distToOrthogonalLOS * 1000f) + manhattanDist;
            }

            return base.ScoreCandidateTile(candidate, playerPos, enemy);
        }
    }
}
