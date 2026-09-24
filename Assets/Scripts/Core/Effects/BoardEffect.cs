using System;

namespace Core.Effects
{
    /// Base class for all immutable board visual effects emitted during simulation.
    /// Effects are consumed by the presentation layer (EffectsQueueRunner) to drive animations.
    public abstract class BoardEffect
    {
        private static int _nextSequenceIndex = 1;

        /// Monotonically increasing sequence index preserving chronological emission order.
        public int SequenceIndex { get; set; }

        /// Simulation timestamp (or relative time offset) associated with this effect.
        public float Timestamp { get; set; }

        /// Identifier of the primary occupant or unit associated with/targeted by this effect.
        public int OccupantId { get; protected set; }

        /// Alias for OccupantId representing the target occupant.
        public int TargetOccupantId => OccupantId;

        protected BoardEffect(int occupantId = 0, float timestamp = 0f, int sequenceIndex = 0)
        {
            OccupantId = occupantId;
            Timestamp = timestamp;
            SequenceIndex = sequenceIndex > 0 ? sequenceIndex : _nextSequenceIndex++;
        }

        /// Resets the global sequence index counter (useful for unit tests and clean level initialization).
        public static void ResetSequenceCounter(int startingIndex = 1)
        {
            _nextSequenceIndex = startingIndex;
        }

        public override string ToString()
        {
            return $"[{GetType().Name} #{SequenceIndex}] Occupant ID: {OccupantId}, Time: {Timestamp:F2}";
        }
    }
}
