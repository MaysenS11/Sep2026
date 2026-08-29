using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum RoomType { Start, Normal, Chest, Boss }
public enum RoomShape { Rectangle, LShape }

[ExecuteAlways]
public class PerimeterDungeonGenerator : MonoBehaviour
{
    [Header("Tilemap References")]
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField] private Tilemap debugTilemap;

    [Header("Rule Tile Assets")]
    [SerializeField] private TileBase floorRuleTile;
    [SerializeField] private TileBase wallRuleTile;
    
    [Header("Standard Door Tile Assets")]
    [SerializeField] private TileBase entranceDoorTile; 
    [SerializeField] private TileBase exitDoorTile;     

    [Header("Special / Chest Room Door Assets")]
    [SerializeField] private TileBase specialEntranceDoorTile; 
    [SerializeField] private TileBase specialExitDoorTile;     

    [Header("Debug Settings")]
    [SerializeField] private bool showDebugPaths = true;
    [SerializeField] private TileBase debugPathTile; 

    [Header("Dungeon Settings")]
    [SerializeField] private int minRooms = 6;
    [SerializeField] private int maxRooms = 9;
    [SerializeField] private Vector2Int macroCellSize = new Vector2Int(16, 16);
    [SerializeField] private float minDoorDistance = 4.0f; 

    private readonly Vector2Int[] outerPerimeter = new Vector2Int[]
    {
        new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0),
        new Vector2Int(3, 1),
        new Vector2Int(3, 2), new Vector2Int(2, 2), new Vector2Int(1, 2), new Vector2Int(0, 2),
        new Vector2Int(0, 1)
    };

    public class Room
    {
        public int Index;
        public Vector2Int MacroPos;
        public RoomType Type;
        public RoomShape Shape;
        public Vector2Int LocalSize;
        public Vector2Int WorldOriginTile;
        public Room ParentRoom;
        
        public Vector2Int? EntranceDoorPos; 
        public Vector2Int? ExitDoorPos;     

        public Vector2Int WorldCenterTile => new Vector2Int(
            WorldOriginTile.x + (LocalSize.x / 2),
            WorldOriginTile.y + (LocalSize.y / 2)
        );
    }

    public List<Room> GeneratedRooms { get; private set; } = new List<Room>();

    private void Start()
    {
        GenerateAndBuildDungeon();
    }

    [ContextMenu("Generate Dungeon in Editor")]
    public void GenerateAndBuildDungeon()
    {
        if (floorTilemap == null || wallTilemap == null)
        {
            Debug.LogWarning("Assign FloorTilemap and WallTilemap in Inspector!");
            return;
        }

        ClearDungeonTiles();
        GeneratedRooms.Clear();

        int totalRooms = Random.Range(minRooms, maxRooms + 1);
        int startPerimeterIdx = Random.Range(0, outerPerimeter.Length);

        // 1. Generate Main Path Rooms
        for (int i = 0; i < totalRooms; i++)
        {
            int pIdx = (startPerimeterIdx + i) % outerPerimeter.Length;
            Vector2Int macroPos = outerPerimeter[pIdx];

            RoomType type = RoomType.Normal;
            if (i == 0) type = RoomType.Start;
            else if (i == totalRooms - 1) type = RoomType.Boss;

            Room room = new Room
            {
                Index = i,
                MacroPos = macroPos,
                Type = type,
                Shape = (type == RoomType.Normal && Random.value < 0.35f) ? RoomShape.LShape : RoomShape.Rectangle,
                LocalSize = (type == RoomType.Boss) ? new Vector2Int(14, 14) : new Vector2Int(Random.Range(9, 14), Random.Range(9, 14)),
                WorldOriginTile = new Vector2Int(macroPos.x * macroCellSize.x, macroPos.y * macroCellSize.y)
            };

            GeneratedRooms.Add(room);
        }

        // 2. Insert Chest Room & Link Parent
        TryInsertChestRoom();

        // 3. Position Doors
        CalculateInteriorDoors();

        // 4. Render Floor, Walls, and Custom Doors
        RenderDungeonTiles();
        ApplyDoorsToTilemap();

        // 5. Draw Border/Cell Connections on Debug Tilemap
        if (showDebugPaths && debugTilemap != null && debugPathTile != null)
        {
            DrawDebugRoomConnections();
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(floorTilemap);
            EditorUtility.SetDirty(wallTilemap);
            if (debugTilemap != null) EditorUtility.SetDirty(debugTilemap);
        }
#endif
    }

    private void TryInsertChestRoom()
    {
        List<Room> normalRooms = GeneratedRooms.FindAll(r => r.Type == RoomType.Normal);
        if (normalRooms.Count == 0) return;

        Room parentRoom = normalRooms[Random.Range(0, normalRooms.Count)];
        List<Vector2Int> innerCells = new List<Vector2Int> { new Vector2Int(1, 1), new Vector2Int(2, 1) };
        Vector2Int? selectedChestMacro = null;

        foreach (var cell in innerCells)
        {
            if (!GeneratedRooms.Exists(r => r.MacroPos == cell))
            {
                selectedChestMacro = cell;
                break;
            }
        }

        if (selectedChestMacro.HasValue)
        {
            Room chestRoom = new Room
            {
                Index = GeneratedRooms.Count,
                MacroPos = selectedChestMacro.Value,
                Type = RoomType.Chest,
                Shape = RoomShape.Rectangle,
                LocalSize = new Vector2Int(8, 8),
                WorldOriginTile = new Vector2Int(selectedChestMacro.Value.x * macroCellSize.x, selectedChestMacro.Value.y * macroCellSize.y),
                ParentRoom = parentRoom
            };

            GeneratedRooms.Add(chestRoom);
        }
    }

    [ContextMenu("Clear Dungeon Tiles")]
    public void ClearDungeonTiles()
    {
        if (floorTilemap != null) floorTilemap.ClearAllTiles();
        if (wallTilemap != null) wallTilemap.ClearAllTiles();
        if (debugTilemap != null) debugTilemap.ClearAllTiles();
    }

    private void CalculateInteriorDoors()
    {
        foreach (var room in GeneratedRooms)
        {
            List<Vector2Int> validFloorTiles = GetValidInteriorFloorTiles(room);
            if (validFloorTiles.Count < 2) continue;

            if (room.Type == RoomType.Start)
            {
                room.EntranceDoorPos = null;
                room.ExitDoorPos = validFloorTiles[Random.Range(0, validFloorTiles.Count)];
            }
            else if (room.Type == RoomType.Boss)
            {
                room.EntranceDoorPos = validFloorTiles[Random.Range(0, validFloorTiles.Count)];
                room.ExitDoorPos = null;
            }
            else
            {
                Vector2Int entrance = validFloorTiles[Random.Range(0, validFloorTiles.Count)];
                room.EntranceDoorPos = entrance;

                List<Vector2Int> validExitCandidates = validFloorTiles.FindAll(tile => 
                    Vector2Int.Distance(tile, entrance) >= minDoorDistance
                );

                if (validExitCandidates.Count > 0)
                {
                    room.ExitDoorPos = validExitCandidates[Random.Range(0, validExitCandidates.Count)];
                }
                else
                {
                    validFloorTiles.Sort((a, b) => Vector2Int.Distance(b, entrance).CompareTo(Vector2Int.Distance(a, entrance)));
                    room.ExitDoorPos = validFloorTiles[0];
                }
            }
        }
    }

    private List<Vector2Int> GetValidInteriorFloorTiles(Room room)
    {
        List<Vector2Int> floorTiles = new List<Vector2Int>();

        for (int x = 1; x < room.LocalSize.x - 1; x++)
        {
            for (int y = 1; y < room.LocalSize.y - 1; y++)
            {
                if (room.Shape == RoomShape.LShape && x >= room.LocalSize.x / 2 && y >= room.LocalSize.y / 2)
                    continue;

                floorTiles.Add(new Vector2Int(room.WorldOriginTile.x + x, room.WorldOriginTile.y + y));
            }
        }

        return floorTiles;
    }

    private void RenderDungeonTiles()
    {
        foreach (var room in GeneratedRooms)
        {
            for (int x = 0; x < room.LocalSize.x; x++)
            {
                for (int y = 0; y < room.LocalSize.y; y++)
                {
                    if (room.Shape == RoomShape.LShape && x >= room.LocalSize.x / 2 && y >= room.LocalSize.y / 2)
                        continue;

                    Vector3Int tilePos = new Vector3Int(room.WorldOriginTile.x + x, room.WorldOriginTile.y + y, 0);

                    if (x == 0 || x == room.LocalSize.x - 1 || y == 0 || y == room.LocalSize.y - 1)
                    {
                        wallTilemap.SetTile(tilePos, wallRuleTile);
                    }
                    else
                    {
                        floorTilemap.SetTile(tilePos, floorRuleTile);
                    }
                }
            }
        }
    }

    private void ApplyDoorsToTilemap()
    {
        foreach (var room in GeneratedRooms)
        {
            TileBase inTile = (room.Type == RoomType.Chest && specialEntranceDoorTile != null) ? specialEntranceDoorTile : entranceDoorTile;
            TileBase outTile = (room.Type == RoomType.Chest && specialExitDoorTile != null) ? specialExitDoorTile : exitDoorTile;

            if (room.EntranceDoorPos.HasValue && inTile != null)
            {
                Vector3Int pos = new Vector3Int(room.EntranceDoorPos.Value.x, room.EntranceDoorPos.Value.y, 0);
                floorTilemap.SetTile(pos, inTile);
            }

            if (room.ExitDoorPos.HasValue && outTile != null)
            {
                Vector3Int pos = new Vector3Int(room.ExitDoorPos.Value.x, room.ExitDoorPos.Value.y, 0);
                floorTilemap.SetTile(pos, outTile);
            }
        }
    }

    private void DrawDebugRoomConnections()
    {
        debugTilemap.ClearAllTiles();

        // 1. Draw sequential main path connections
        List<Room> mainPathRooms = GeneratedRooms.FindAll(r => r.Type != RoomType.Chest);
        for (int i = 0; i < mainPathRooms.Count - 1; i++)
        {
            DrawOrthogonalPath(mainPathRooms[i].WorldCenterTile, mainPathRooms[i + 1].WorldCenterTile);
        }

        // 2. Draw branch connection from Chest room -> Parent Room
        Room chestRoom = GeneratedRooms.Find(r => r.Type == RoomType.Chest);
        if (chestRoom != null && chestRoom.ParentRoom != null)
        {
            DrawOrthogonalPath(chestRoom.ParentRoom.WorldCenterTile, chestRoom.WorldCenterTile);
        }
    }

    private void DrawOrthogonalPath(Vector2Int start, Vector2Int end)
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

#if UNITY_EDITOR
[CustomEditor(typeof(PerimeterDungeonGenerator))]
public class PerimeterDungeonGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PerimeterDungeonGenerator generator = (PerimeterDungeonGenerator)target;

        EditorGUILayout.Space(10);
        if (GUILayout.Button("Generate Dungeon (In Editor)", GUILayout.Height(30)))
        {
            generator.GenerateAndBuildDungeon();
        }

        if (GUILayout.Button("Clear All Tiles", GUILayout.Height(25)))
        {
            generator.ClearDungeonTiles();
        }
    }
}
#endif