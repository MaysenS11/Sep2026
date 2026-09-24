using UnityEngine;

namespace Core.Effects
{
    /// Visual effect commanding a fast strike lunge into/toward a target tile along an attack direction.
    /// Used by attacking units to present physical attack impact.
    public class AttackLungeEffect : BoardEffect
    {
        public int AttackerId => OccupantId;
        public Vector2Int TargetPos { get; }
        public Vector2Int AttackDirection { get; }
        public Vector2Int StartPos { get; }

        public AttackLungeEffect(
            int attackerId,
            Vector2Int targetPos,
            Vector2Int attackDirection,
            Vector2Int startPos = default,
            float timestamp = 0f,
            int sequenceIndex = 0)
            : base(attackerId, timestamp, sequenceIndex)
        {
            TargetPos = targetPos;
            AttackDirection = attackDirection;
            StartPos = startPos;
        }

        public override string ToString()
        {
            return $"[AttackLungeEffect #{SequenceIndex}] Attacker {AttackerId} -> Target {TargetPos} (Dir: {AttackDirection})";
        }
    }
}
