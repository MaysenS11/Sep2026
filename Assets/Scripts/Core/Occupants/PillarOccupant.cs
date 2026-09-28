using UnityEngine;

namespace Core.Occupants
{
    public class PillarOccupant : ObstacleOccupant
    {
        public Vector2Int FootprintOrigin { get; }
        public Vector2Int FootprintSize { get; }
        public Vector2Int LocalOffset { get; }
        public GameObject VisualRoot { get; }

        public PillarOccupant(
            Vector2Int gridPosition,
            Vector2Int footprintOrigin,
            Vector2Int footprintSize,
            int id = 0,
            string name = null,
            GameObject visualRoot = null)
            : base(
                ObstacleType.Pillar,
                gridPosition,
                id,
                name ?? $"Pillar_{footprintSize.x}x{footprintSize.y}_({gridPosition.x},{gridPosition.y})")
        {
            FootprintOrigin = footprintOrigin;
            FootprintSize = footprintSize;
            LocalOffset = gridPosition - footprintOrigin;
            VisualRoot = visualRoot;
            IsPushable = false;
        }

        public override string ToString()
        {
            return $"{Name} [ID:{Id}] at {GridPosition} (Footprint: {FootprintSize.x}x{FootprintSize.y} at {FootprintOrigin})";
        }
    }
}
