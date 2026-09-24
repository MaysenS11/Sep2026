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
        }

        public override List<BoardEffect> TakeDamage(int damage, TileOccupant source = null)
        {
            var effects = base.TakeDamage(damage, source);

            if (IsDead)
            {
                OnPropDestroyed?.Invoke(this);
            }

            return effects;
        }
    }
}
