using UnityEngine;

namespace Core.Effects
{
    /// <summary>
    /// Visual effect emitted when a destructible barrel is shattered, releasing a healing heart
    /// that flies directly into the player to restore health without any ground drop or pickup step.
    /// </summary>
    public class HeartRestoreEffect : BoardEffect
    {
        public Vector2Int SourcePos { get; }
        public Vector2Int TargetPos { get; }
        public int HealAmount { get; }
        public int TargetOccupantId { get; }

        public HeartRestoreEffect(
            int sourceOccupantId,
            Vector2Int sourcePos,
            Vector2Int targetPos,
            int healAmount = 1,
            int targetOccupantId = 0)
            : base(sourceOccupantId)
        {
            SourcePos = sourcePos;
            TargetPos = targetPos;
            HealAmount = healAmount;
            TargetOccupantId = targetOccupantId;
        }

        public override string ToString()
        {
            return $"[HeartRestoreEffect] From {SourcePos} -> To {TargetPos} (+{HealAmount} HP to Occupant {TargetOccupantId})";
        }
    }
}
