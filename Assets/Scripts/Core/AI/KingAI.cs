using Core.Board;
using Core.Occupants;

namespace Core.AI
{
    public class KingAI : BaseEnemyAI
    {
        public override EnemyIntent EvaluateIntent(GameBoard board, EnemyOccupant enemy)
        {
            return EnemyIntent.CreateWait(enemy);
        }

        protected override EnemyIntent DecideIntent(GameBoard board, EnemyOccupant enemy, PlayerOccupant player)
        {
            return EnemyIntent.CreateWait(enemy);
        }
    }
}
