using System.Collections.Generic;
using Core.Board;
using Core.Effects;
using UnityEngine;

namespace Core.Occupants
{
    /// Pure C# authoritative data model for the Player unit on the GameBoard.
    /// 
    /// Key Property:
    /// Player is the ONLY TileOccupant in the entire game with IsPushable = true.
    /// All other occupants (enemies, chests, props, obstacles) have IsPushable = false.
    public class PlayerOccupant : TileOccupant
    {
        public Vector2Int FacingDirection { get; set; } = BoardCoordinate.North;
        public int AttackDamage { get; set; }

        public PlayerOccupant(
            int maxHealth = 6,
            int attackDamage = 2,
            Vector2Int initialPosition = default,
            int id = 0,
            string name = "Player")
            : base(maxHealth, initialPosition, id, name)
        {
            AttackDamage = attackDamage;
            // CRITICAL: Player is the sole pushable occupant on the board!
            IsPushable = true;
        }

        /// Updates the player's facing direction based on movement or manual turning.
        public void SetFacingDirection(Vector2Int direction)
        {
            if (direction != Vector2Int.zero)
            {
                FacingDirection = BoardCoordinate.GetSignVector(direction);
            }
        }
    }
}
