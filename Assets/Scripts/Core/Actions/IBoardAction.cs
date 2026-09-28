using System.Collections.Generic;
using Core.Board;
using Core.Effects;

namespace Core.Actions
{
    public interface IBoardAction
    {
        List<BoardEffect> Execute(GameBoard board);
    }
}
