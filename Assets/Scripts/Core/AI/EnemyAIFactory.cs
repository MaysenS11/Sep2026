using System;
using System.Collections.Generic;
using Core.Board;
using Core.Occupants;

namespace Core.AI
{
    /// Factory and dispatcher for retrieving and executing piece AI strategies.
    /// Manages strategy instances and coordinates turn evaluation.
    public static class EnemyAIFactory
    {
        private static readonly Dictionary<EnemyArchetype, IEnemyAI> _strategies = new Dictionary<EnemyArchetype, IEnemyAI>
        {
            { EnemyArchetype.Pawn, new PawnAI() },
            { EnemyArchetype.Knight, new KnightAI() },
            { EnemyArchetype.Bishop, new BishopAI() },
            { EnemyArchetype.Rook, new RookAI() },
            { EnemyArchetype.Queen, new QueenAI() }
        };

        /// Retrieves the AI strategy implementation for a given piece archetype.
        public static IEnemyAI GetAI(EnemyArchetype archetype)
        {
            if (_strategies.TryGetValue(archetype, out var strategy))
            {
                return strategy;
            }

            throw new ArgumentOutOfRangeException(nameof(archetype), $"No AI strategy registered for archetype: {archetype}");
        }

        /// Evaluates turn intent for an enemy occupant against the authoritative GameBoard.
        /// Enforces stun/turn skip rules and player detection range checks.
        public static EnemyIntent EvaluateEnemy(GameBoard board, EnemyOccupant enemy)
        {
            if (enemy == null) return null;

            var strategy = GetAI(enemy.Archetype);
            return strategy.EvaluateIntent(board, enemy);
        }
    }
}
