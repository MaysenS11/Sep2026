using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapDungeonDebugger : MonoBehaviour
{
    [Header("Tilemap References")]
    [SerializeField] private Tilemap debugTilemap;

    [Header("Debug Tile Assets")]
    [SerializeField] private TileBase startFloorTile;
    [SerializeField] private TileBase normalFloorTile;
    [SerializeField] private TileBase lShapeFloorTile;
    [SerializeField] private TileBase secretFloorTile;
    [SerializeField] private TileBase bossFloorTile;
    [SerializeField] private TileBase doorTile;
    [SerializeField] private TileBase wallTile;

    [Header("Layout Configuration")]
    [SerializeField] private int minRooms = 5;
    [SerializeField] private int maxRooms = 8;
    
    // Size of each room cell region in world grid units
    [SerializeField] private Vector2Int macroCellDimension = new Vector2Int(16, 16);

    // 8-Cell Perimeter Array (Outer Ring of 3x3 Macro Grid)
    private readonly Vector2Int[] outerPerimeter = new Vector2Int[]
    {
        new Vector2Int(0, 0), // Top-Left
        new Vector2Int(1, 0), // Top-Mid
        new Vector2Int(2, 0), // Top-Right
        new Vector2Int(2, 1), // Mid-Right
        new Vector2Int(2, 2), // Bottom-Right
        new Vector2Int(1, 2), // Bottom-Mid
        new Vector2Int(0, 2), // Bottom-Left
        new Vector2Int(0, 1)  // Mid-Left
    };

    private readonly Vector2Int centerBossMacroPos = new Vector2Int(1, 1);

    public class GeneratedRoom
    {
        public int Index;
        public Vector2Int MacroPos;
        public RoomType Type;
        public bool IsLShape;
        public Vector2Int LocalSize;
        public Vector2Int RoomOriginTilePos;
        public Vector2Int ExitDoorTilePos;
        public Vector2Int? SecretDoorTilePos;
    }

    public enum RoomType { Start, Normal, Secret, Boss }

    private List<GeneratedRoom> activeRooms = new List<GeneratedRoom>();

    private void Start()
    {
        GenerateAndDrawDungeon();
    }


    [ContextMenu("Re-Generate Dungeon")]
    public void GenerateAndDrawDungeon()
    {
        debugTilemap.ClearAllTiles();
        activeRooms.Clear();

        int roomCount = Random.Range(minRooms, maxRooms + 1);
        int startPerimeterIdx = Random.Range(0, outerPerimeter.Length);

        // 1. Build Perimeter Rooms
        for (int i = 0; i < roomCount; i++)
        {
            int pIdx = (startPerimeterIdx + i) % outerPerimeter.Length;
            Vector2Int macroPos = outerPerimeter[pIdx];

            GeneratedRoom room = new GeneratedRoom
            {
                Index = i,
                MacroPos = macroPos,
                Type = (i == 0) ? RoomType.Start : RoomType.Normal,
                IsLShape = (i > 0 && Random.value < 0.35f), // Random L-shapes
                LocalSize = new Vector2Int(Random.Range(8, 13), Random.Range(8, 13)),
                RoomOriginTilePos = GetMacroWorldTileOrigin(macroPos)
            };

            // 30% Chance for Secret Chest Room attachment on normal rooms
            if (room.Type == RoomType.Normal && Random.value < 0.30f)
            {
                room.SecretDoorTilePos = CalculateSecretDoorPosition(room);
            }

            activeRooms.Add(room);
        }

        // 2. Add Center Boss Room
        GeneratedRoom bossRoom = new GeneratedRoom
        {
            Index = activeRooms.Count,
            MacroPos = centerBossMacroPos,
            Type = RoomType.Boss,
            IsLShape = false,
            LocalSize = new Vector2Int(12, 12),
            RoomOriginTilePos = GetMacroWorldTileOrigin(centerBossMacroPos)
        };
        activeRooms.Add(bossRoom);

        // 3. Draw Rooms & Doors onto Tilemap
        DrawAllRoomsToTilemap();
        DrawConnectingDoors();
    }

    private Vector2Int GetMacroWorldTileOrigin(Vector2Int macroPos)
    {
        // Offsets the 3x3 macro coordinates so each room sits cleanly in space
        return new Vector2Int(macroPos.x * macroCellDimension.x, macroPos.y * macroCellDimension.y);
    }

    private void DrawAllRoomsToTilemap()
    {
        foreach (var room in activeRooms)
        {
            TileBase chosenTile = room.Type switch
            {
                RoomType.Start => startFloorTile,
                RoomType.Boss => bossFloorTile,
                _ => room.IsLShape ? lShapeFloorTile : normalFloorTile
            };

            // Fill base room footprint
            for (int x = 0; x < room.LocalSize.x; x++)
            {
                for (int y = 0; y < room.LocalSize.y; y++)
                {
                    // If L-shaped room, skip top-right quadrant tiles
                    if (room.IsLShape && x >= room.LocalSize.x / 2 && y >= room.LocalSize.y / 2)
                        continue;

                    Vector3Int tilePos = new Vector3Int(
                        room.RoomOriginTilePos.x + x,
                        room.RoomOriginTilePos.y + y,
                        0
                    );

                    debugTilemap.SetTile(tilePos, chosenTile);
                }
            }

            // Draw Secret Chest Room extension if applicable
            if (room.SecretDoorTilePos.HasValue)
            {
                DrawSecretChestRoom(room.SecretDoorTilePos.Value);
            }
        }
    }

    private Vector2Int CalculateSecretDoorPosition(GeneratedRoom room)
    {
        // Place secret door along outer boundary of the current room
        return new Vector2Int(
            room.RoomOriginTilePos.x + room.LocalSize.x,
            room.RoomOriginTilePos.y + (room.LocalSize.y / 2)
        );
    }

    private void DrawSecretChestRoom(Vector2Int doorPos)
    {
        // Draw 4x4 secret chest room
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                Vector3Int tilePos = new Vector3Int(doorPos.x + x, doorPos.y - 1 + y, 0);
                debugTilemap.SetTile(tilePos, secretFloorTile);
            }
        }
        // Place Door Tile at entrance
        debugTilemap.SetTile(new Vector3Int(doorPos.x, doorPos.y, 0), doorTile);
    }

    private void DrawConnectingDoors()
    {
        for (int i = 0; i < activeRooms.Count - 1; i++)
        {
            GeneratedRoom current = activeRooms[i];
            GeneratedRoom next = activeRooms[i + 1];

            // Calculate midpoint door tile between current and next room center
            Vector2Int currentCenter = current.RoomOriginTilePos + (current.LocalSize / 2);
            Vector2Int nextCenter = next.RoomOriginTilePos + (next.LocalSize / 2);

            Vector2Int doorPos = new Vector2Int(
                (currentCenter.x + nextCenter.x) / 2,
                (currentCenter.y + nextCenter.y) / 2
            );

            current.ExitDoorTilePos = doorPos;
            debugTilemap.SetTile(new Vector3Int(doorPos.x, doorPos.y, 0), doorTile);
        }
    }
}
