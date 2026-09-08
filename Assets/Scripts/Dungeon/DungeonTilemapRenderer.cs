using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dungeon
{
    public class DungeonTilemapRenderer
    {
        public struct RectBounds
        {
            public Vector2Int Origin;
            public Vector2Int Size;

            public RectBounds(Vector2Int origin, Vector2Int size)
            {
                Origin = origin;
                Size = size;
            }

            public RectBounds Expand(int amount)
            {
                int margin = amount / 2;
                return new RectBounds(
                    Origin - new Vector2Int(margin, margin),
                    Size + new Vector2Int(amount, amount)
                );
            }
        }

        private readonly List<RectBounds> _subRectsBuffer = new List<RectBounds>(8);
        private TileBase[] _tileBuffer = new TileBase[1024];
        private readonly List<GameManager.RoomData> _candidateParentRoomsBuffer = new List<GameManager.RoomData>(16);

        public void RenderRooms(
            List<GameManager.RoomData> rooms,
            Tilemap roofTilemap,
            Tilemap wallTilemap,
            Tilemap borderFloorTilemap,
            Tilemap fillFloorTilemap,
            TileBase roofRuleTile,
            TileBase wallRuleTile,
            TileBase borderFloorRuleTile,
            TileBase fillFloorRuleTile)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                GameManager.RoomData room = rooms[i];

                DrawDecomposedLayer(room, fillFloorTilemap, fillFloorRuleTile, -2);
                DrawDecomposedLayer(room, borderFloorTilemap, borderFloorRuleTile, 0);
                DrawDecomposedLayer(room, wallTilemap, wallRuleTile, 2);
                DrawDecomposedLayer(room, roofTilemap, roofRuleTile, 4);
            }

            roofTilemap.RefreshAllTiles();
            wallTilemap.RefreshAllTiles();
            borderFloorTilemap.RefreshAllTiles();
            fillFloorTilemap.RefreshAllTiles();
        }

        public void DrawDebugConnections(
            List<GameManager.RoomData> rooms,
            Tilemap debugTilemap,
            TileBase debugPathTile)
        {
            if (debugTilemap == null || debugPathTile == null) return;
            debugTilemap.ClearAllTiles();

            _candidateParentRoomsBuffer.Clear();
            List<GameManager.RoomData> chestRooms = new List<GameManager.RoomData>();

            for (int i = 0; i < rooms.Count; i++)
            {
                GameManager.RoomData room = rooms[i];
                if (room.Type != RoomType.Chest) _candidateParentRoomsBuffer.Add(room);
                else chestRooms.Add(room);
            }

            for (int i = 0; i < _candidateParentRoomsBuffer.Count - 1; i++)
            {
                DrawOrthogonalPath(debugTilemap, debugPathTile, _candidateParentRoomsBuffer[i].CenterTile, _candidateParentRoomsBuffer[i + 1].CenterTile);
            }

            for (int i = 0; i < chestRooms.Count; i++)
            {
                GameManager.RoomData chest = chestRooms[i];
                if (chest.ParentRoomIndex != -1 && GameManager.Instance != null && GameManager.Instance.DungeonDictionary.TryGetValue(chest.ParentRoomIndex, out var parentRoom))
                {
                    DrawOrthogonalPath(debugTilemap, debugPathTile, parentRoom.CenterTile, chest.CenterTile);
                }
            }
        }

        private void DrawDecomposedLayer(GameManager.RoomData room, Tilemap tilemap, TileBase tile, int diameterOffset)
        {
            PopulateRoomSubRectangles(room, _subRectsBuffer);

            for (int i = 0; i < _subRectsBuffer.Count; i++)
            {
                RectBounds expanded = _subRectsBuffer[i].Expand(diameterOffset);
                DrawRectangle(tilemap, tile, expanded.Origin, expanded.Size);
            }
        }

        private void PopulateRoomSubRectangles(GameManager.RoomData room, List<RectBounds> results)
        {
            results.Clear();
            Vector2Int origin = room.WorldOriginTile;
            Vector2Int size = room.Size;

            int halfW = size.x / 2;
            int halfH = size.y / 2;

            switch (room.Shape)
            {
                case RoomShape.Rectangle:
                    results.Add(new RectBounds(origin, size));
                    break;

                case RoomShape.LShape:
                    switch (room.LRot)
                    {
                        case LRotation.TopRight:
                            results.Add(new RectBounds(origin, new Vector2Int(size.x, halfH)));
                            results.Add(new RectBounds(origin, new Vector2Int(halfW, size.y)));
                            break;

                        case LRotation.TopLeft:
                            results.Add(new RectBounds(origin, new Vector2Int(size.x, halfH)));
                            results.Add(new RectBounds(new Vector2Int(origin.x + halfW, origin.y), new Vector2Int(size.x - halfW, size.y)));
                            break;

                        case LRotation.BottomRight:
                            results.Add(new RectBounds(new Vector2Int(origin.x, origin.y + halfH), new Vector2Int(size.x, size.y - halfH)));
                            results.Add(new RectBounds(origin, new Vector2Int(halfW, size.y)));
                            break;

                        case LRotation.BottomLeft:
                            results.Add(new RectBounds(new Vector2Int(origin.x, origin.y + halfH), new Vector2Int(size.x, size.y - halfH)));
                            results.Add(new RectBounds(new Vector2Int(origin.x + halfW, origin.y), new Vector2Int(size.x - halfW, size.y)));
                            break;
                    }
                    break;

                case RoomShape.TShape:
                    results.Add(new RectBounds(origin, new Vector2Int(size.x, halfH)));
                    results.Add(new RectBounds(new Vector2Int(origin.x + (size.x - 5) / 2, origin.y), new Vector2Int(5, size.y)));
                    break;

                case RoomShape.UShape:
                    results.Add(new RectBounds(origin, new Vector2Int(size.x, 5)));
                    results.Add(new RectBounds(origin, new Vector2Int(5, size.y)));
                    results.Add(new RectBounds(new Vector2Int(origin.x + size.x - 5, origin.y), new Vector2Int(5, size.y)));
                    break;
            }
        }

        private void DrawRectangle(Tilemap tilemap, TileBase tile, Vector2Int origin, Vector2Int size)
        {
            if (size.x <= 0 || size.y <= 0) return;

            int totalTiles = size.x * size.y;
            if (_tileBuffer.Length < totalTiles)
            {
                _tileBuffer = new TileBase[totalTiles];
            }

            for (int i = 0; i < totalTiles; i++) _tileBuffer[i] = tile;

            tilemap.SetTilesBlock(new BoundsInt(new Vector3Int(origin.x, origin.y, 0), new Vector3Int(size.x, size.y, 1)), _tileBuffer);
        }

        private void DrawOrthogonalPath(Tilemap debugTilemap, TileBase debugPathTile, Vector2Int start, Vector2Int end)
        {
            int currentX = start.x;
            int currentY = start.y;

            int stepX = start.x < end.x ? 1 : -1;
            while (currentX != end.x)
            {
                debugTilemap.SetTile(new Vector3Int(currentX, currentY, 0), debugPathTile);
                currentX += stepX;
            }

            int stepY = start.y < end.y ? 1 : -1;
            while (currentY != end.y)
            {
                debugTilemap.SetTile(new Vector3Int(currentX, currentY, 0), debugPathTile);
                currentY += stepY;
            }

            debugTilemap.SetTile(new Vector3Int(end.x, end.y, 0), debugPathTile);
        }
    }
}
