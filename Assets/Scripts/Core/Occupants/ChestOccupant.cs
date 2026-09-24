using System;
using System.Collections.Generic;
using Core.Effects;
using UnityEngine;

namespace Core.Occupants
{
    /// Pure C# authoritative data model for a Chest on the GameBoard.
    public class ChestOccupant : TileOccupant
    {
        public bool IsOpen { get; private set; }
        public bool IsChestRoom { get; set; }

        public event Action<ChestOccupant> OnOpened;

        public ChestOccupant(
            Vector2Int initialPosition = default,
            bool isChestRoom = false,
            int id = 0,
            string name = "Chest")
            : base(maxHealth: 1, initialPosition, id, name)
        {
            IsOpen = false;
            IsChestRoom = isChestRoom;
            IsPushable = false;
        }

        /// Attacking a chest opens it.
        /// Once opened, it remains on the board indefinitely as an impassable obstacle.
        public override List<BoardEffect> TakeDamage(int damage, TileOccupant source = null)
        {
            var effects = new List<BoardEffect>();
            if (IsOpen || damage <= 0) return effects;

            IsOpen = true;
            CurrentHealth = 0;

            OnOpened?.Invoke(this);

            return effects;
        }

        public override string ToString()
        {
            return $"{Name} [ID:{Id}] at {GridPosition} (Open: {IsOpen}, ChestRoom: {IsChestRoom})";
        }
    }
}
