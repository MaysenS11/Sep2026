namespace Core.Effects
{
    /// Visual effect emitted when an occupant takes damage.
    /// Drives floating combat text, hurt flashes, and localized hit sparks.
    public class DamageTakenEffect : BoardEffect
    {
        public int DamageAmount { get; }
        public int RemainingHealth { get; }
        public int SourceOccupantId { get; }

        public DamageTakenEffect(
            int occupantId,
            int damageAmount,
            int remainingHealth,
            int sourceOccupantId = 0,
            float timestamp = 0f,
            int sequenceIndex = 0)
            : base(occupantId, timestamp, sequenceIndex)
        {
            DamageAmount = damageAmount;
            RemainingHealth = remainingHealth;
            SourceOccupantId = sourceOccupantId;
        }

        public override string ToString()
        {
            return $"[DamageTakenEffect #{SequenceIndex}] Occupant {OccupantId} took {DamageAmount} dmg (Remaining HP: {RemainingHealth}, Source: {SourceOccupantId})";
        }
    }
}
