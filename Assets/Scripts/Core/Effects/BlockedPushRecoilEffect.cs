using UnityEngine;

namespace Core.Effects
{
    /// Visual effect representing a failed push attack: clash impact, spark/recoil animation,
    /// recoil damage taken, and rebound bounce back to the recoil end tile.
    public class BlockedPushRecoilEffect : BoardEffect
    {
        public int AttackerId => OccupantId;
        public Vector2Int AttackerStartPos { get; }
        public Vector2Int PlayerTile { get; }
        public Vector2Int RecoilEndTile { get; }
        public int RecoilDamage { get; }
        public Vector2Int BlockerPos { get; }
        public int BlockerOccupantId { get; }

        public BlockedPushRecoilEffect(
            int attackerId,
            Vector2Int attackerStartPos,
            Vector2Int playerTile,
            Vector2Int recoilEndTile,
            int recoilDamage = 1,
            Vector2Int blockerPos = default,
            int blockerOccupantId = 0,
            float timestamp = 0f,
            int sequenceIndex = 0)
            : base(attackerId, timestamp, sequenceIndex)
        {
            AttackerStartPos = attackerStartPos;
            PlayerTile = playerTile;
            RecoilEndTile = recoilEndTile;
            RecoilDamage = recoilDamage;
            BlockerPos = blockerPos;
            BlockerOccupantId = blockerOccupantId;
        }

        public override string ToString()
        {
            return $"[BlockedPushRecoilEffect #{SequenceIndex}] Attacker {AttackerId} from {AttackerStartPos} clashed at {PlayerTile}, recoiled to {RecoilEndTile} (Dmg: {RecoilDamage}, Blocker at {BlockerPos} ID:{BlockerOccupantId})";
        }
    }
}
