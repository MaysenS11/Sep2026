using Core.Board;
using UnityEngine;

namespace Core.AI
{
    /// AI strategy for Rook archetype.
    /// Moves and attacks along the 4 cardinal directions.
    /// Slides into player tile on clear push; stops 1 tile short taking recoil on blocked push.
    public class RookAI : LinePieceAI
    {
        protected override Vector2Int[] AllowedDirections => BoardCoordinate.CardinalDirections;
    }
}
