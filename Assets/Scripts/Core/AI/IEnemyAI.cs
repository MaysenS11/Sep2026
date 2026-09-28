using Core.Board;
using Core.Occupants;

namespace Core.AI
{
    public interface IEnemyAI
    {
        EnemyIntent EvaluateIntent(GameBoard board, EnemyOccupant enemy);
    }
}
