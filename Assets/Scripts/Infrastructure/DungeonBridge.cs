using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Core.Board;

namespace Infrastructure
{
    /// Converts procedural dungeon room layout into an authoritative simulation GameBoard
    /// with floor terrain, wall boundaries, and door connections.
    public static class DungeonBridge
    {
        public static GameBoard BuildBoardFromDungeon(
            List<GameManager.RoomData> rooms,
            int margin = 10,
            Tilemap floorTilemap = null,
            Tilemap borderFloorTilemap = null)
        {
            if (rooms == null || rooms.Count == 0)
            {
                return new GameBoard(100, 100, new Vector2Int(-50, -50));
            }

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            for (int i = 0; i < rooms.Count; i++)
            {
                var r = rooms[i];
                minX = Math.Min(minX, r.WorldOriginTile.x);
                minY = Math.Min(minY, r.WorldOriginTile.y);
                maxX = Math.Max(maxX, r.WorldOriginTile.x + r.Size.x);
                maxY = Math.Max(maxY, r.WorldOriginTile.y + r.Size.y);
            }

            // Include tilemap bounds if available
            if (floorTilemap != null && floorTilemap.cellBounds.size.x > 0)
            {
                minX = Math.Min(minX, floorTilemap.cellBounds.xMin);
                minY = Math.Min(minY, floorTilemap.cellBounds.yMin);
                maxX = Math.Max(maxX, floorTilemap.cellBounds.xMax);
                maxY = Math.Max(maxY, floorTilemap.cellBounds.yMax);
            }
            if (borderFloorTilemap != null && borderFloorTilemap.cellBounds.size.x > 0)
            {
                minX = Math.Min(minX, borderFloorTilemap.cellBounds.xMin);
                minY = Math.Min(minY, borderFloorTilemap.cellBounds.yMin);
                maxX = Math.Max(maxX, borderFloorTilemap.cellBounds.xMax);
                maxY = Math.Max(maxY, borderFloorTilemap.cellBounds.yMax);
            }

            minX -= margin;
            minY -= margin;
            maxX += margin;
            maxY += margin;

            int width = Math.Max(1, maxX - minX);
            int height = Math.Max(1, maxY - minY);
            var origin = new Vector2Int(minX, minY);

            var board = new GameBoard(width, height, origin);

            // Initialize all cells on the board as Wall terrain by default
            for (int x = origin.x; x < origin.x + width; x++)
            {
                for (int y = origin.y; y < origin.y + height; y++)
                {
                    board.SetCell(new Vector2Int(x, y), TerrainType.Wall);
                }
            }

            // 1. Carve floor cells for each room's walkable area (both fill and border floor)
            for (int i = 0; i < rooms.Count; i++)
            {
                var r = rooms[i];
                for (int rx = r.WorldOriginTile.x; rx < r.WorldOriginTile.x + r.Size.x; rx++)
                {
                    for (int ry = r.WorldOriginTile.y; ry < r.WorldOriginTile.y + r.Size.y; ry++)
                    {
                        int localX = rx - r.WorldOriginTile.x;
                        int localY = ry - r.WorldOriginTile.y;
                        if (Dungeon.RoomTileQuery.IsInsideBorderFloor(r, localX, localY))
                        {
                            board.SetCell(new Vector2Int(rx, ry), TerrainType.Floor);
                        }
                    }
                }

                // Ensure door connection tiles and their landing areas are walkable floors
                if (r.EntranceDoorTile.HasValue)
                {
                    board.SetCell(r.EntranceDoorTile.Value, TerrainType.Floor);
                    board.SetCell(new Vector2Int(r.EntranceDoorTile.Value.x, r.EntranceDoorTile.Value.y - 1), TerrainType.Floor);
                    board.SetCell(new Vector2Int(r.EntranceDoorTile.Value.x, r.EntranceDoorTile.Value.y + 1), TerrainType.Floor);
                    board.SetCell(new Vector2Int(r.EntranceDoorTile.Value.x - 1, r.EntranceDoorTile.Value.y), TerrainType.Floor);
                    board.SetCell(new Vector2Int(r.EntranceDoorTile.Value.x + 1, r.EntranceDoorTile.Value.y), TerrainType.Floor);
                }

                if (r.ExitDoorTile.HasValue)
                {
                    board.SetCell(r.ExitDoorTile.Value, TerrainType.Floor);
                    board.SetCell(new Vector2Int(r.ExitDoorTile.Value.x, r.ExitDoorTile.Value.y - 1), TerrainType.Floor);
                    board.SetCell(new Vector2Int(r.ExitDoorTile.Value.x, r.ExitDoorTile.Value.y + 1), TerrainType.Floor);
                    board.SetCell(new Vector2Int(r.ExitDoorTile.Value.x - 1, r.ExitDoorTile.Value.y), TerrainType.Floor);
                    board.SetCell(new Vector2Int(r.ExitDoorTile.Value.x + 1, r.ExitDoorTile.Value.y), TerrainType.Floor);
                }
            }

            // 2. Read directly from floor Tilemaps if rendered in scene
            if (floorTilemap != null)
            {
                foreach (var pos in floorTilemap.cellBounds.allPositionsWithin)
                {
                    if (floorTilemap.HasTile(pos))
                    {
                        board.SetCell(new Vector2Int(pos.x, pos.y), TerrainType.Floor);
                    }
                }
            }

            if (borderFloorTilemap != null)
            {
                foreach (var pos in borderFloorTilemap.cellBounds.allPositionsWithin)
                {
                    if (borderFloorTilemap.HasTile(pos))
                    {
                        board.SetCell(new Vector2Int(pos.x, pos.y), TerrainType.Floor);
                    }
                }
            }

            return board;
        }
    }
}
