using UnityEngine;

namespace Core.Effects
{
    /// Visual effect emitted when an occupant's health reaches zero and it is destroyed or killed.
    /// Drives death animations, prop shattering particles, and presenter GameObject cleanup.
    public class OccupantDestroyedEffect : BoardEffect
    {
        public Vector2Int DeathPosition { get; }
        public string OccupantType { get; }

        public OccupantDestroyedEffect(
            int occupantId,
            Vector2Int deathPosition,
            string occupantType = null,
            float timestamp = 0f,
            int sequenceIndex = 0)
            : base(occupantId, timestamp, sequenceIndex)
        {
            DeathPosition = deathPosition;
            OccupantType = occupantType ?? string.Empty;
        }

        public override string ToString()
        {
            return $"[OccupantDestroyedEffect #{SequenceIndex}] Occupant {OccupantId} ({OccupantType}) destroyed at {DeathPosition}";
        }
    }
}
