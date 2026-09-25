using System;
using System.Collections.Generic;
using Core.Effects;
using UnityEngine;

namespace Core.Occupants
{
    /// Type of destructible prop on the board.
    public enum DestructiblePropType
    {
        Barrel,
        Crate,
        Urn
    }

    /// Pure C# authoritative data model for destructible props (e.g. barrels, crates).
    /// Typically has 1-2 HP. Decrements health when attacked; at 0 HP is destroyed and removed from board.
    /// NEVER pushable.
    public class DestructiblePropOccupant : TileOccupant
    {
        public DestructiblePropType PropType { get; }

        /// <summary>
        /// Whether destroying this prop triggers a direct heart restore to the player without a ground drop.
        /// Guaranteed true for Barrels by default.
        /// </summary>
        public bool RestoresHeartOnDestruction { get; set; }

        /// <summary>
        /// Drop chance for heart restore on destruction (0.0 to 1.0). Default 1.0f (guaranteed).
        /// </summary>
        public float HeartDropChance { get; set; } = 1.0f;

        public event Action<DestructiblePropOccupant> OnPropDestroyed;

        public DestructiblePropOccupant(
            DestructiblePropType propType = DestructiblePropType.Barrel,
            int maxHealth = 1,
            Vector2Int initialPosition = default,
            int id = 0,
            string name = null)
            : base(maxHealth, initialPosition, id, name ?? propType.ToString())
        {
            PropType = propType;
            IsPushable = false;
            // Barrels restore hearts on destruction by default
            RestoresHeartOnDestruction = (propType == DestructiblePropType.Barrel);
        }

        public override List<BoardEffect> TakeDamage(int damage, TileOccupant source = null)
        {
            var effects = base.TakeDamage(damage, source);

            if (IsDead)
            {
                if (RestoresHeartOnDestruction && (HeartDropChance >= 1.0f || UnityEngine.Random.value <= HeartDropChance))
                {
                    if (source is PlayerOccupant player)
                    {
                        player.Heal(1);
                        effects.Add(new HeartRestoreEffect(Id, GridPosition, player.GridPosition, 1, player.Id));
                    }
                    else
                    {
                        effects.Add(new HeartRestoreEffect(Id, GridPosition, GridPosition, 1, 0));
                    }
                }

                OnPropDestroyed?.Invoke(this);
            }

            return effects;
        }
    }
}
