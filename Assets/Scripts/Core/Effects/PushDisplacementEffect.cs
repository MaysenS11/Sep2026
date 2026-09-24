using UnityEngine;

namespace Core.Effects
{
    /// Visual effect commanding forced displacement of an occupant (specifically the Player) knocked back by an attack.
    public class PushDisplacementEffect : BoardEffect
    {
        public int PushedOccupantId => OccupantId;
        public Vector2Int StartPos { get; }
        public Vector2Int EndPos { get; }
        public Vector2Int PushDirection { get; }

        public PushDisplacementEffect(
            int pushedOccupantId,
            Vector2Int startPos,
            Vector2Int endPos,
            Vector2Int pushDirection,
            float timestamp = 0f,
            int sequenceIndex = 0)
            : base(pushedOccupantId, timestamp, sequenceIndex)
        {
            StartPos = startPos;
            EndPos = endPos;
            PushDirection = pushDirection;
        }

        public override string ToString()
        {
            return $"[PushDisplacementEffect #{SequenceIndex}] Pushed {PushedOccupantId}: {StartPos} -> {EndPos} (Dir: {PushDirection})";
        }
    }
}
