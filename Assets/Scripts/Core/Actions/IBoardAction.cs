using System.Collections.Generic;
using Core.Board;
using Core.Effects;

namespace Core.Actions
{
    /// Contract for an authoritative simulation action applied to the GameBoard.
    /// Mutates board state immediately and outputs a sequence of immutable visual BoardEffects.
    public interface IBoardAction
    {
        /// Executes the action against the given GameBoard.
        /// Mutates board state and returns the list of generated visual BoardEffects.
        List<BoardEffect> Execute(GameBoard board);
    }
}
