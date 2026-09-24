using System.Collections.Generic;
using Core.Effects;
using UnityEngine;

namespace Core.Occupants
{
    /// Types of permanent, indestructible physical obstacles placed onto the board grid.
    public enum ObstacleType
    {
        Pillar,
        IndestructibleWall,
        VoidPit,
        Statue
    }

    /// Pure C# authoritative data model for permanent, indestructible obstacles (e.g. pillars, internal stone blocks).
    /// Cannot be destroyed, cannot be pushed, cannot be traversed.
    public class ObstacleOccupant : TileOccupant
    {
        public ObstacleType ObstacleType { get; }

        public override bool IsDead => false;

        public ObstacleOccupant(
            ObstacleType obstacleType = ObstacleType.Pillar,
            Vector2Int initialPosition = default,
            int id = 0,
            string name = null)
            : base(maxHealth: 1, initialPosition, id, name ?? obstacleType.ToString())
        {
            ObstacleType = obstacleType;
            IsPushable = false;
        }

        /// Obstacles are indestructible; attacks inflict no damage.
        public override List<BoardEffect> TakeDamage(int damage, TileOccupant source = null)
        {
            // Indestructible: absorbs attacks with no effect
            return new List<BoardEffect>();
        }
    }
}
