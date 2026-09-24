using Core.Board;
using Core.Occupants;

namespace Core.AI
{
    /// Pure AI strategy contract for enemy piece decision making.
    /// Evaluates the authoritative GameBoard state and outputs an EnemyIntent.
    public interface IEnemyAI
    {
        /// Evaluates board state and determines the next action intent for the given enemy.
        EnemyIntent EvaluateIntent(GameBoard board, EnemyOccupant enemy);
    }
}
