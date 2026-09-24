using System;

namespace Core.Effects
{
    /// Base class for all immutable board visual effects emitted during simulation.
    /// Effects are consumed by the presentation layer (EffectsQueueRunner) to drive animations.
    public abstract class BoardEffect
    {
        /// Optional identifier of the primary unit or occupant associated with this effect.
        public int OccupantId { get; protected set; }

        protected BoardEffect(int occupantId = 0)
        {
            OccupantId = occupantId;
        }
    }
}
