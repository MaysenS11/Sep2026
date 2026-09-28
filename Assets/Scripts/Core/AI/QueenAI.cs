using System;
using Core.Board;
using Core.Occupants;
using UnityEngine;

namespace Core.AI
{
    public class QueenAI : LinePieceAI
    {
        protected override Vector2Int[] AllowedDirections => BoardCoordinate.AllDirections;

        protected override float ScoreCandidateTile(Vector2Int candidate, Vector2Int playerPos, EnemyOccupant enemy)
        {
            if (enemy.IntelligenceLevel == EnemySmartness.Smart)
            {
                int cardinalDist = Math.Min(Math.Abs(candidate.x - playerPos.x), Math.Abs(candidate.y - playerPos.y));
                
                int diagonalDist = Math.Abs(Math.Abs(candidate.x - playerPos.x) - Math.Abs(candidate.y - playerPos.y));
                
                int minLOS = Math.Min(cardinalDist, diagonalDist);
                float dist = BoardCoordinate.EuclideanDistance(candidate, playerPos);

                
                return (minLOS * 1000f) + dist;
            }

            return base.ScoreCandidateTile(candidate, playerPos, enemy);
        }
    }
}
