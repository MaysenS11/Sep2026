using Core.Board;
using UnityEngine;

namespace Core.AI
{
    /// AI strategy for Bishop archetype.
    /// Moves and attacks along the 4 diagonal directions.
    /// Slides into player tile on clear push; stops 1 tile short taking recoil on blocked push.
    public class BishopAI : LinePieceAI
    {
        protected override Vector2Int[] AllowedDirections => BoardCoordinate.DiagonalDirections;
    }
}
