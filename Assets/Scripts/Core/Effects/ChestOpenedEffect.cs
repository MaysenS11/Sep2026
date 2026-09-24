using UnityEngine;

namespace Core.Effects
{
    /// Visual effect emitted when a chest is opened by player interaction or attack.
    /// Drives lid opening animations, sparkle particle bursts, and floating loot rewards.
    public class ChestOpenedEffect : BoardEffect
    {
        public int ChestOccupantId => OccupantId;
        public Vector2Int TilePos { get; }
        public string LootInfo { get; }

        public ChestOpenedEffect(
            int chestOccupantId,
            Vector2Int tilePos,
            string lootInfo = null,
            float timestamp = 0f,
            int sequenceIndex = 0)
            : base(chestOccupantId, timestamp, sequenceIndex)
        {
            TilePos = tilePos;
            LootInfo = lootInfo ?? string.Empty;
        }

        public override string ToString()
        {
            return $"[ChestOpenedEffect #{SequenceIndex}] Chest {ChestOccupantId} opened at {TilePos} (Loot: {LootInfo})";
        }
    }
}
