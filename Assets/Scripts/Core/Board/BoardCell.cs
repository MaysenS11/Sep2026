using System;
using UnityEngine;

namespace Core.Board
{
    public enum TerrainType
    {
        Floor = 0,
        Wall = 1,
        Void = 2
    }

    public class BoardCell
    {
        public Vector2Int Position { get; }
        public TerrainType Terrain { get; set; }

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
