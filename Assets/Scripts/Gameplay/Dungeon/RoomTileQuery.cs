using System.Collections.Generic;
using UnityEngine;

namespace Dungeon
{
    public enum RoomEdge
    {
        Top,
        Left,
        Right
    }

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

        public static bool IsInsideRoomFloor(GameManager.RoomData room, int localX, int localY)
        {
            if (localX < 1 || localX >= room.Size.x - 1 || localY < 1 || localY >= room.Size.y - 1)
            {
                return false;
            }

            int halfWidth = room.Size.x / 2;
            int halfHeight = room.Size.y / 2;

            if (room.Shape == RoomShape.LShape)
            {
                if (room.LRot == LRotation.TopRight && localX >= halfWidth && localY >= halfHeight) return false;
                if (room.LRot == LRotation.TopLeft && localX < halfWidth && localY >= halfHeight) return false;
                if (room.LRot == LRotation.BottomRight && localX >= halfWidth && localY < halfHeight) return false;
                if (room.LRot == LRotation.BottomLeft && localX < halfWidth && localY < halfHeight) return false;
            }
            else if (room.Shape == RoomShape.TShape)
            {
                if (localY >= halfHeight && (localX < (room.Size.x - 5) / 2 || localX >= (room.Size.x + 5) / 2)) return false;
            }
            else if (room.Shape == RoomShape.UShape)
            {
                if (localY >= 5 && (localX >= 5 && localX < room.Size.x - 5)) return false;
            }

            return true;
        }

        public static bool IsInsideBorderFloor(GameManager.RoomData room, int localX, int localY)
        {
            if (localX < 0 || localX >= room.Size.x || localY < 0 || localY >= room.Size.y)
            {
                return false;
            }

            int halfWidth = room.Size.x / 2;
            int halfHeight = room.Size.y / 2;

            if (room.Shape == RoomShape.LShape)
            {
                if (room.LRot == LRotation.TopRight && localX >= halfWidth && localY >= halfHeight) return false;
                if (room.LRot == LRotation.TopLeft && localX < halfWidth && localY >= halfHeight) return false;
                if (room.LRot == LRotation.BottomRight && localX >= halfWidth && localY < halfHeight) return false;
                if (room.LRot == LRotation.BottomLeft && localX < halfWidth && localY < halfHeight) return false;
            }
            else if (room.Shape == RoomShape.TShape)
            {
                if (localY >= halfHeight && (localX < (room.Size.x - 5) / 2 || localX >= (room.Size.x + 5) / 2)) return false;
            }
            else if (room.Shape == RoomShape.UShape)
            {
                if (localY >= 5 && (localX >= 5 && localX < room.Size.x - 5)) return false;
            }

            return true;
        }

        public void GetValidInteriorFloorTiles(GameManager.RoomData room, List<Vector2Int> results, bool onlyUnoccupied = false)
        {
            results.Clear();

            for (int x = 1; x < room.Size.x - 1; x++)
            {
                for (int y = 1; y < room.Size.y - 1; y++)
                {
                    if (!IsInsideRoomFloor(room, x, y)) continue;

                    Vector2Int tile = new Vector2Int(room.WorldOriginTile.x + x, room.WorldOriginTile.y + y);
                    if (!onlyUnoccupied || !_occupiedTiles.Contains(tile))
                    {
                        results.Add(tile);
                    }
                }
            }
        }

        public void GetBorderFloorTiles(GameManager.RoomData room, RoomEdge edge, List<Vector2Int> results, bool onlyUnoccupied = false)
        {
            results.Clear();

            for (int x = 0; x < room.Size.x; x++)
            {
                for (int y = 0; y < room.Size.y; y++)
                {
                    if (!IsInsideBorderFloor(room, x, y)) continue;
                    if (IsInsideRoomFloor(room, x, y)) continue;

                    bool isBorder = false;
                    switch (edge)
                    {
                        case RoomEdge.Top:
                            isBorder = !IsInsideBorderFloor(room, x, y + 1);
                            break;
                        case RoomEdge.Left:
                            isBorder = !IsInsideBorderFloor(room, x - 1, y);
                            break;
                        case RoomEdge.Right:
                            isBorder = !IsInsideBorderFloor(room, x + 1, y);
                            break;
                    }

                    if (!isBorder) continue;

                    Vector2Int tile = new Vector2Int(room.WorldOriginTile.x + x, room.WorldOriginTile.y + y);
                    if (!onlyUnoccupied || !_occupiedTiles.Contains(tile))
                    {
                        results.Add(tile);
                    }
                }
            }
        }

        public static bool IsHallwayTile(GameManager.RoomData room, int localX, int localY)
        {
            if (!IsInsideRoomFloor(room, localX, localY)) return false;

            int halfWidth = room.Size.x / 2;
            int halfHeight = room.Size.y / 2;

            switch (room.Shape)
            {
                case RoomShape.TShape:
                    return localY >= halfHeight;

                case RoomShape.UShape:
                    return localY >= 5;

                case RoomShape.LShape:
                    switch (room.LRot)
                    {
                        case LRotation.TopRight:
                            return (localX >= halfWidth && localY < halfHeight) || (localX < halfWidth && localY >= halfHeight);
                        case LRotation.TopLeft:
                            return (localX < halfWidth && localY < halfHeight) || (localX >= halfWidth && localY >= halfHeight);
                        case LRotation.BottomRight:
                            return (localX >= halfWidth && localY >= halfHeight) || (localX < halfWidth && localY < halfHeight);
                        case LRotation.BottomLeft:
                            return (localX < halfWidth && localY >= halfHeight) || (localX >= halfWidth && localY < halfHeight);
                    }
                    break;
            }

            return false;
        }

        public static bool IsNearHallwayTile(GameManager.RoomData room, int localX, int localY, int radius = 1)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (IsHallwayTile(room, localX + dx, localY + dy))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool IsDistanceValidFromWalls(GameManager.RoomData room, Vector2Int footprintOrigin, Vector2Int footprintSize, int minDistanceToWalls)
        {
            if (minDistanceToWalls <= 0) return true;

            int minLocalX = footprintOrigin.x - room.WorldOriginTile.x;
            int minLocalY = footprintOrigin.y - room.WorldOriginTile.y;
            int maxLocalX = minLocalX + footprintSize.x - 1;
            int maxLocalY = minLocalY + footprintSize.y - 1;

            for (int lx = minLocalX - minDistanceToWalls; lx <= maxLocalX + minDistanceToWalls; lx++)
            {
                for (int ly = minLocalY - minDistanceToWalls; ly <= maxLocalY + minDistanceToWalls; ly++)
                {
                    if (!IsInsideRoomFloor(room, lx, ly))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}

