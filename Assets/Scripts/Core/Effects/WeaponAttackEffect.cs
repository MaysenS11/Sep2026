using System.Collections.Generic;
using UnityEngine;

namespace Core.Effects
{
    public enum WeaponType
    {
        Degen,
        Halberd,
        Pistol,
        Flask
    }

    /// <summary>
    /// Dedicated visual effect data describing a weapon attack sequence.
    /// Emitted by simulation and interpreted by EffectsQueueRunner to spawn travel and impact particles.
    /// </summary>
    public class WeaponAttackEffect : BoardEffect
    {
        public int AttackerId => OccupantId;
        public WeaponType Weapon { get; }
        public Vector2Int OriginPos { get; }
        public Vector2Int Direction { get; }
        public Vector2Int PrimaryTarget { get; }
        public List<Vector2Int> TargetTiles { get; }
        public List<Vector2Int> HitTiles { get; }

        public WeaponAttackEffect(
            int attackerId,
            WeaponType weapon,
            Vector2Int originPos,
            Vector2Int direction,
            Vector2Int primaryTarget,
            List<Vector2Int> targetTiles,
            List<Vector2Int> hitTiles = null,
            float timestamp = 0f,
            int sequenceIndex = 0)
            : base(attackerId, timestamp, sequenceIndex)
        {
            Weapon = weapon;
            OriginPos = originPos;
            Direction = direction;
            PrimaryTarget = primaryTarget;
            TargetTiles = targetTiles != null ? new List<Vector2Int>(targetTiles) : new List<Vector2Int>();
            HitTiles = hitTiles != null ? new List<Vector2Int>(hitTiles) : new List<Vector2Int>();
        }

        public override string ToString()
        {
            return $"[WeaponAttackEffect #{SequenceIndex}] {Weapon} from {OriginPos} (Dir: {Direction}, Targets: {TargetTiles.Count}, Hits: {HitTiles.Count})";
        }
    }
}
