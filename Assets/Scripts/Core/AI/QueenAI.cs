using Core.Board;
using UnityEngine;

namespace Core.AI
{
    /// AI strategy for Queen archetype.
    /// Combines Rook (cardinal) and Bishop (diagonal) raycasting across all 8 directions.
    /// Prioritizes attacking the player if clear line of sight; handles clear push vs blocked push recoil appropriately.
    public class QueenAI : LinePieceAI
    {
        protected override Vector2Int[] AllowedDirections => BoardCoordinate.AllDirections;
    }
}
