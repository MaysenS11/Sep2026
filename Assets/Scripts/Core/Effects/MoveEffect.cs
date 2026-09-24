using System;
using UnityEngine;

namespace Core.Effects
{
    /// Visual effect commanding continuous linear path translation across grid cells.
    /// Used for standard movement (Pawns, Rooks, Bishops, Queens, and Player).
    public class MoveEffect : BoardEffect
    {
        /// Sequence of discrete grid coordinates forming the movement trajectory.
        public Vector2Int[] Path { get; }

        public Vector2Int StartPos => Path != null && Path.Length > 0 ? Path[0] : Vector2Int.zero;
        public Vector2Int EndPos => Path != null && Path.Length > 0 ? Path[Path.Length - 1] : Vector2Int.zero;

        public MoveEffect(int occupantId, Vector2Int[] path, float timestamp = 0f, int sequenceIndex = 0)
            : base(occupantId, timestamp, sequenceIndex)
        {
            Path = path ?? Array.Empty<Vector2Int>();
        }

        public MoveEffect(int occupantId, Vector2Int startPos, Vector2Int endPos, float timestamp = 0f, int sequenceIndex = 0)
            : base(occupantId, timestamp, sequenceIndex)
        {
            Path = new[] { startPos, endPos };
        }

        public override string ToString()
        {
            return $"[MoveEffect #{SequenceIndex}] Occupant {OccupantId}: {StartPos} -> {EndPos} ({Path?.Length ?? 0} points)";
        }
    }
}
