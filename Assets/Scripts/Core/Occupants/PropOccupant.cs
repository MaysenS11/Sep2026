using Core.Board;
using UnityEngine;

namespace Core.Occupants
{
    public class PropOccupant : DestructiblePropOccupant
    {
        public PropOccupant(
            DestructiblePropType propType = DestructiblePropType.Barrel,
            int maxHealth = 1,
            Vector2Int initialPosition = default,
            int id = 0,
            string name = null)
            : base(propType, maxHealth, initialPosition, id, name)
        {
        }
    }
}
