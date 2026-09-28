using System;

namespace Core.Effects
{
    /// Base class for all immutable board visual effects emitted during simulation.
    /// Effects are consumed by the presentation layer (EffectsQueueRunner) to drive animations.
    public abstract class BoardEffect
    {
        private static int _nextSequenceIndex = 1;
        public int SequenceIndex { get; set; }
        public float Timestamp { get; set; }
        public int OccupantId { get; protected set; }
        public int TargetOccupantId => OccupantId;

        protected BoardEffect(int occupantId = 0, float timestamp = 0f, int sequenceIndex = 0)
        {
            OccupantId = occupantId;
            Timestamp = timestamp;
            SequenceIndex = sequenceIndex > 0 ? sequenceIndex : _nextSequenceIndex++;
        }

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
