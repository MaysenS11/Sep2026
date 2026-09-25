using UnityEngine;

namespace Core.Occupants
{
    /// <summary>
    /// Pure C# authoritative simulation data model for an individual grid tile of a pillar obstacle.
    /// Indestructible, impassable, and never pushable.
    /// Can be a single 1x1 pillar or part of a multi-tile (1x2, 2x2, etc.) pillar
    /// that shares a single unified visual GameObject.
    /// </summary>
    public class PillarOccupant : ObstacleOccupant
    {
        /// <summary>
        /// Bottom-left origin tile of the pillar footprint.
        /// </summary>
        public Vector2Int FootprintOrigin { get; }

        /// <summary>
        /// Total dimensions in grid tiles (e.g. 1x1, 1x2, 2x2).
        /// </summary>
        public Vector2Int FootprintSize { get; }

        /// <summary>
        /// Offset of this specific tile relative to FootprintOrigin.
        /// </summary>
        public Vector2Int LocalOffset { get; }

        /// <summary>
        /// Reference to the shared visual presentation GameObject.
        /// </summary>
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
