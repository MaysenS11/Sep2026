using Core.Board;
using Core.Occupants;

namespace Core.AI
{
    /// <summary>
    /// AI strategy for King Boss archetype.
    /// King Boss is stationary on his throne and executes NO movement or attack loops.
    /// Always returns Wait intent.
    /// </summary>
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
