using Core.Board;
using UnityEngine;

namespace Core.AI
{
    public class BishopAI : LinePieceAI
    {
        protected override Vector2Int[] AllowedDirections => BoardCoordinate.DiagonalDirections;
    }
}
