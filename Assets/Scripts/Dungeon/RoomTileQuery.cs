using System.Collections.Generic;
using UnityEngine;

namespace Dungeon
{
    public class RoomTileQuery
    {
        private readonly HashSet<Vector2Int> _occupiedTiles = new HashSet<Vector2Int>();

        public void ClearOccupancy()
        {
            _occupiedTiles.Clear();
        }

        public bool IsOccupied(Vector2Int tile)
        {
            return _occupiedTiles.Contains(tile);
        }

        public void MarkOccupied(Vector2Int tile)
        {
            _occupiedTiles.Add(tile);
        }

        public void MarkOccupied(Vector2Int tile, int radius)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    _occupiedTiles.Add(new Vector2Int(tile.x + x, tile.y + y));
                }
            }
        }

        public void GetValidInteriorFloorTiles(GameManager.RoomData room, List<Vector2Int> results, bool onlyUnoccupied = false)
        {
            results.Clear();
            int halfWidth = room.Size.x / 2;
            int halfHeight = room.Size.y / 2;

            for (int x = 1; x < room.Size.x - 1; x++)
            {
                for (int y = 1; y < room.Size.y - 1; y++)
                {
                    if (room.Shape == RoomShape.LShape)
                    {
                        if (room.LRot == LRotation.TopRight && x >= halfWidth && y >= halfHeight) continue;
                        if (room.LRot == LRotation.TopLeft && x < halfWidth && y >= halfHeight) continue;
                        if (room.LRot == LRotation.BottomRight && x >= halfWidth && y < halfHeight) continue;
                        if (room.LRot == LRotation.BottomLeft && x < halfWidth && y < halfHeight) continue;
                    }
                    else if (room.Shape == RoomShape.TShape)
                    {
                        if (y >= halfHeight && (x < (room.Size.x - 5) / 2 || x >= (room.Size.x + 5) / 2)) continue;
                    }
                    else if (room.Shape == RoomShape.UShape)
                    {
                        if (y >= 5 && (x >= 5 && x < room.Size.x - 5)) continue;
                    }

                    Vector2Int tile = new Vector2Int(room.WorldOriginTile.x + x, room.WorldOriginTile.y + y);
                    if (!onlyUnoccupied || !_occupiedTiles.Contains(tile))
                    {
                        results.Add(tile);
                    }
                }
            }
        }
    }
}
