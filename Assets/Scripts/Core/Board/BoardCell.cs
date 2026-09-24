using System;
using UnityEngine;

namespace Core.Board
{
    /// Categorizes the physical terrain structure of a single grid cell.
    public enum TerrainType
    {
        Floor = 0,
        Wall = 1,
        Void = 2
    }

    /// Represents static tile data on the board grid, independent of dynamic occupants.
    public class BoardCell
    {
        public Vector2Int Position { get; }
        public TerrainType Terrain { get; set; }

        /// True if the terrain itself is naturally walkable.
        public bool IsWalkableTerrain => Terrain == TerrainType.Floor;

        public BoardCell(Vector2Int position, TerrainType terrain = TerrainType.Floor)
        {
            Position = position;
            Terrain = terrain;
        }

        public override string ToString()
        {
            return $"BoardCell({Position.x}, {Position.y}, Terrain={Terrain})";
        }
    }
}
