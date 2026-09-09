using System.Collections.Generic;
using UnityEngine;

namespace Dungeon
{
    public class DungeonLayoutPlanner
    {
        private static readonly Vector2Int[] OuterPerimeter = new Vector2Int[]
        {
            new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0),
            new Vector2Int(3, 1),
            new Vector2Int(3, 2), new Vector2Int(2, 2), new Vector2Int(1, 2), new Vector2Int(0, 2),
            new Vector2Int(0, 1)
        };

        private static readonly Vector2Int[] InnerChestCells = new Vector2Int[]
        {
            new Vector2Int(1, 1),
            new Vector2Int(2, 1)
        };

        private readonly List<GameManager.RoomData> _candidateParentRoomsBuffer = new List<GameManager.RoomData>(16);

        public List<GameManager.RoomData> GenerateLayout(
            int minRooms,
            int maxRooms,
            Vector2Int macroCellSize,
            Vector2Int minNormalRoomSize,
            Vector2Int maxNormalRoomSize,
            Vector2Int fixedBossRoomSize,
            Vector2Int fixedChestRoomSize)
        {
            var generatedRooms = new List<GameManager.RoomData>();

            int totalRooms = Mathf.Clamp(Random.Range(minRooms, maxRooms + 1), 4, OuterPerimeter.Length);
            int startPerimeterIdx = Random.Range(0, OuterPerimeter.Length);

            for (int i = 0; i < totalRooms; i++)
            {
                int pIdx = (startPerimeterIdx + i) % OuterPerimeter.Length;
                Vector2Int macroPos = OuterPerimeter[pIdx];

                RoomType type = RoomType.Normal;
                if (i == 0) type = RoomType.Start;
                else if (i == totalRooms - 1) type = RoomType.Boss;

                Vector2Int size = CalculateRoomSize(type, minNormalRoomSize, maxNormalRoomSize, fixedBossRoomSize, fixedChestRoomSize);
                Vector2Int padding = macroCellSize - size;
                Vector2Int centeredOffset = new Vector2Int(padding.x / 2, padding.y / 2);

                RoomShape shape = RoomShape.Rectangle;
                if (type == RoomType.Normal)
                {
                    float shapeRoll = Random.value;
                    if (shapeRoll < 0.35f) shape = RoomShape.LShape;
                    else if (shapeRoll < 0.55f) shape = RoomShape.TShape;
                    else if (shapeRoll < 0.70f) shape = RoomShape.UShape;
                }

                GameManager.RoomData room = new GameManager.RoomData
                {
                    RoomIndex = i,
                    MacroPos = macroPos,
                    Type = type,
                    Shape = shape,
                    LRot = (LRotation)Random.Range(0, 4),
                    Size = size,
                    WorldOriginTile = new Vector2Int(
                        (macroPos.x * macroCellSize.x) + centeredOffset.x,
                        (macroPos.y * macroCellSize.y) + centeredOffset.y
                    ),
                    CenterTile = new Vector2Int(
                        (macroPos.x * macroCellSize.x) + centeredOffset.x + (size.x / 2),
                        (macroPos.y * macroCellSize.y) + centeredOffset.y + (size.y / 2)
                    )
                };

                generatedRooms.Add(room);
            }

            TryInsertChestRooms(generatedRooms, macroCellSize, fixedChestRoomSize);

            return generatedRooms;
        }

        private Vector2Int CalculateRoomSize(
            RoomType type,
            Vector2Int minNormal,
            Vector2Int maxNormal,
            Vector2Int bossSize,
            Vector2Int chestSize)
        {
            if (type == RoomType.Boss) return bossSize;
            if (type == RoomType.Chest) return chestSize;

            int width = Random.Range(minNormal.x, maxNormal.x + 1);
            int height = Random.Range(minNormal.y, maxNormal.y + 1);

            width = Mathf.Max(width, 12);
            height = Mathf.Max(height, 12);

            return new Vector2Int(width, height);
        }

        private void TryInsertChestRooms(
            List<GameManager.RoomData> rooms,
            Vector2Int macroCellSize,
            Vector2Int fixedChestRoomSize)
        {
            _candidateParentRoomsBuffer.Clear();
            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i].Type == RoomType.Normal)
                    _candidateParentRoomsBuffer.Add(rooms[i]);
            }

            if (_candidateParentRoomsBuffer.Count == 0) return;

            int chestRoomCount = Random.Range(1, 3);

            for (int c = 0; c < chestRoomCount; c++)
            {
                if (_candidateParentRoomsBuffer.Count == 0) break;

                Vector2Int? selectedChestMacro = null;
                for (int i = 0; i < InnerChestCells.Length; i++)
                {
                    Vector2Int cell = InnerChestCells[i];
                    bool isOccupied = false;
                    for (int r = 0; r < rooms.Count; r++)
                    {
                        if (rooms[r].MacroPos == cell)
                        {
                            isOccupied = true;
                            break;
                        }
                    }

                    if (!isOccupied)
                    {
                        selectedChestMacro = cell;
                        break;
                    }
                }

                if (!selectedChestMacro.HasValue) break;

                int parentIdx = Random.Range(0, _candidateParentRoomsBuffer.Count);
                GameManager.RoomData parentRoom = _candidateParentRoomsBuffer[parentIdx];
                _candidateParentRoomsBuffer.RemoveAt(parentIdx);

                Vector2Int padding = macroCellSize - fixedChestRoomSize;
                Vector2Int centeredOffset = new Vector2Int(padding.x / 2, padding.y / 2);

                GameManager.RoomData chestRoom = new GameManager.RoomData
                {
                    RoomIndex = rooms.Count,
                    ParentRoomIndex = parentRoom.RoomIndex,
                    MacroPos = selectedChestMacro.Value,
                    Type = RoomType.Chest,
                    Shape = RoomShape.Rectangle,
                    Size = fixedChestRoomSize,
                    WorldOriginTile = new Vector2Int(
                        (selectedChestMacro.Value.x * macroCellSize.x) + centeredOffset.x,
                        (selectedChestMacro.Value.y * macroCellSize.y) + centeredOffset.y
                    ),
                    CenterTile = new Vector2Int(
                        (selectedChestMacro.Value.x * macroCellSize.x) + centeredOffset.x + (fixedChestRoomSize.x / 2),
                        (selectedChestMacro.Value.y * macroCellSize.y) + centeredOffset.y + (fixedChestRoomSize.y / 2)
                    )
                };

                rooms.Add(chestRoom);
            }
        }
    }
}
