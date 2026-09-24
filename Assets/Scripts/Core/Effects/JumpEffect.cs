using UnityEngine;

namespace Core.Effects
{
    /// Visual effect commanding a parabolic arc trajectory across the grid.
    /// Used for non-linear movement leaps (e.g. Knight leaping over intervening obstacles/units).
    public class JumpEffect : BoardEffect
    {
        public Vector2Int StartPos { get; }
        public Vector2Int EndPos { get; }
        public float ArcHeight { get; }

        public JumpEffect(
            int occupantId,
            Vector2Int startPos,
            Vector2Int endPos,
            float arcHeight = 1.0f,
            float timestamp = 0f,
            int sequenceIndex = 0)
            : base(occupantId, timestamp, sequenceIndex)
        {
            StartPos = startPos;
            EndPos = endPos;
            ArcHeight = arcHeight;
        }

        public override string ToString()
        {
            return $"[JumpEffect #{SequenceIndex}] Occupant {OccupantId}: {StartPos} -> {EndPos} (Arc: {ArcHeight:F1})";
        }
    }
}
