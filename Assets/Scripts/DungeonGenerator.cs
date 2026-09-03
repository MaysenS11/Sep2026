using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum RoomType { Start, Normal, Chest, Boss }
public enum RoomShape { Rectangle, LShape }

[ExecuteAlways]
public class DungeonGenerator : MonoBehaviour
{
    [Header("Layer Tilemaps")]
    [SerializeField] private Tilemap roofTilemap;
    [FormerlySerializedAs("layer1WallTilemap")]
    [SerializeField] private Tilemap wallTilemap;
    [FormerlySerializedAs("floorBorderTilemap")]
    [SerializeField] private Tilemap borderFloorTilemap;
    [FormerlySerializedAs("floorTilemap")]
    [SerializeField] private Tilemap fillFloorTilemap;
    [SerializeField] private Tilemap objectTilemap;
    [SerializeField] private Tilemap debugTilemap;

    [Header("Rule Tile Assets")]
    [SerializeField] private TileBase roofRuleTile;
    [FormerlySerializedAs("wallRuleTile")]
    [SerializeField] private TileBase wallRuleTile;
    [FormerlySerializedAs("floorBorderRuleTile")]
    [SerializeField] private TileBase borderFloorRuleTile;
    [FormerlySerializedAs("floorRuleTile")]
    [SerializeField] private TileBase fillFloorRuleTile;

    [Header("Standard Door Assets")]
    [SerializeField] private TileBase entranceDoorTile; 
    [SerializeField] private TileBase exitDoorTile;     

    [Header("Special Door Assets")]
    [SerializeField] private TileBase specialEntranceDoorTile; 
    [SerializeField] private TileBase specialExitDoorTile;     

    [Header("Debug Settings")]
    [SerializeField] private bool showDebugPaths = true;
    [SerializeField] private TileBase debugPathTile; 

    [Header("Dungeon Settings")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private int minRooms = 6;
    [SerializeField] private int maxRooms = 9;
    [SerializeField] private Vector2Int macroCellSize = new Vector2Int(25, 20);
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

    public bool TryGetRoomCenter(Vector2Int macroPosition, out Vector3 center)
    {
        Room room = GeneratedRooms.Find(candidate => candidate.MacroPos == macroPosition);
        if (room == null)
        {
            center = default;
            return false;
        }

        center = new Vector3(
            room.WorldOriginTile.x + room.LocalSize.x / 2f,
            room.WorldOriginTile.y + room.LocalSize.y / 2f,
            0f);
        return true;
    }

    private void Start()
    {
        GenerateAndBuildDungeon();
    }

    [ContextMenu("Generate Dungeon in Editor")]
    public void GenerateAndBuildDungeon()
    {
        if (roofTilemap == null || wallTilemap == null || borderFloorTilemap == null || fillFloorTilemap == null)
        {
            Debug.LogWarning("Assign all four layer tilemaps in the Inspector!");
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

            Vector2Int size = (type == RoomType.Boss) 
                ? new Vector2Int(18, 16) 
                : new Vector2Int(Random.Range(15, 20), Random.Range(10, 15));

            Vector2Int padding = macroCellSize - size;
            Vector2Int centeredOffset = new Vector2Int(padding.x / 2, padding.y / 2);

            Room room = new Room
            {
                Index = i,
                MacroPos = macroPos,
                Type = type,
                Shape = (type == RoomType.Normal && Random.value < 0.35f) ? RoomShape.LShape : RoomShape.Rectangle,
                LocalSize = size,
                WorldOriginTile = new Vector2Int(
                    (macroPos.x * macroCellSize.x) + centeredOffset.x, 
                    (macroPos.y * macroCellSize.y) + centeredOffset.y
                )
            };

            GeneratedRooms.Add(room);
        }

        // 2. Insert Chest Room & Link Parent
        TryInsertChestRoom();

        // 3. Position Doors
        CalculateInteriorDoors();

        // 4. Render the centered roof, wall, border, and fill footprints
        RenderDungeonTiles();
        ApplyDoorsToTilemap();
        PlacePlayerAtFirstRoom();

        // 5. Draw Debug Paths
        if (showDebugPaths && debugTilemap != null && debugPathTile != null)
        {
            DrawDebugRoomConnections();
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(roofTilemap);
            EditorUtility.SetDirty(wallTilemap);
            EditorUtility.SetDirty(borderFloorTilemap);
            EditorUtility.SetDirty(fillFloorTilemap);
            if (objectTilemap != null) EditorUtility.SetDirty(objectTilemap);
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
            Vector2Int size = new Vector2Int(12, 12);
            Vector2Int padding = macroCellSize - size;
            Vector2Int centeredOffset = new Vector2Int(padding.x / 2, padding.y / 2);

            Room chestRoom = new Room
            {
                Index = GeneratedRooms.Count,
                MacroPos = selectedChestMacro.Value,
                Type = RoomType.Chest,
                Shape = RoomShape.Rectangle,
                LocalSize = size,
                WorldOriginTile = new Vector2Int(
                    (selectedChestMacro.Value.x * macroCellSize.x) + centeredOffset.x,
                    (selectedChestMacro.Value.y * macroCellSize.y) + centeredOffset.y
                ),
                ParentRoom = parentRoom
            };

            GeneratedRooms.Add(chestRoom);
        }
    }

    [ContextMenu("Clear Dungeon Tiles")]
    public void ClearDungeonTiles()
    {
        if (roofTilemap != null) roofTilemap.ClearAllTiles();
        if (wallTilemap != null) wallTilemap.ClearAllTiles();
        if (borderFloorTilemap != null) borderFloorTilemap.ClearAllTiles();
        if (fillFloorTilemap != null) fillFloorTilemap.ClearAllTiles();
        if (debugTilemap != null) debugTilemap.ClearAllTiles();
        if (objectTilemap != null) objectTilemap.ClearAllTiles();
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

    private bool IsInsideRoomShape(Room room, int localX, int localY)
    {
        if (localX < 0 || localX >= room.LocalSize.x || localY < 0 || localY >= room.LocalSize.y)
            return false;

        if (room.Shape == RoomShape.LShape && localX >= room.LocalSize.x / 2 && localY >= room.LocalSize.y / 2)
            return false;

        return true;
    }

    private void RenderDungeonTiles()
    {
        foreach (var room in GeneratedRooms)
        {
            DrawRoomLayer(room, roofTilemap, roofRuleTile, 4);
            DrawRoomLayer(room, wallTilemap, wallRuleTile, 2);
            DrawRoomLayer(room, borderFloorTilemap, borderFloorRuleTile, 0);
            DrawRoomLayer(room, fillFloorTilemap, fillFloorRuleTile, -2);
        }

        roofTilemap.RefreshAllTiles();
        wallTilemap.RefreshAllTiles();
        borderFloorTilemap.RefreshAllTiles();
        fillFloorTilemap.RefreshAllTiles();
    }

    private void DrawRoomLayer(Room room, Tilemap tilemap, TileBase tile, int diameterOffset)
    {
        Vector2Int layerSize = room.LocalSize + new Vector2Int(diameterOffset, diameterOffset);
        if (layerSize.x <= 0 || layerSize.y <= 0)
            return;

        int margin = diameterOffset / 2;
        Vector2Int layerOrigin = room.WorldOriginTile - new Vector2Int(margin, margin);

        if (room.Shape != RoomShape.LShape)
        {
            DrawRectangle(tilemap, tile, layerOrigin, layerSize);
            return;
        }

        // Expand each rectangle from the original L shape. Recomputing the
        // cutout from layerSize would change the shape at every ring size.
        int horizontalHeight = room.LocalSize.y / 2;
        int verticalWidth = room.LocalSize.x / 2;
        DrawRectangle(tilemap, tile,
            layerOrigin,
            new Vector2Int(layerSize.x, horizontalHeight + diameterOffset));
        DrawRectangle(tilemap, tile,
            layerOrigin,
            new Vector2Int(verticalWidth + diameterOffset, layerSize.y));
    }

    private void DrawRectangle(Tilemap tilemap, TileBase tile, Vector2Int origin, Vector2Int size)
    {
        if (size.x <= 0 || size.y <= 0)
            return;

        for (int x = 0; x < size.x; x++)
        for (int y = 0; y < size.y; y++)
        {
            tilemap.SetTile(new Vector3Int(origin.x + x, origin.y + y, 0), tile);
        }
    }

    private void ApplyDoorsToTilemap()
    {
        if (objectTilemap == null) return;

        foreach (var room in GeneratedRooms)
        {
            TileBase inTile = (room.Type == RoomType.Chest && specialEntranceDoorTile != null) ? specialEntranceDoorTile : entranceDoorTile;
            TileBase outTile = (room.Type == RoomType.Chest && specialExitDoorTile != null) ? specialExitDoorTile : exitDoorTile;

            if (room.EntranceDoorPos.HasValue && inTile != null)
            {
                Vector3Int pos = new Vector3Int(room.EntranceDoorPos.Value.x, room.EntranceDoorPos.Value.y, 0);
                fillFloorTilemap.SetTile(pos, fillFloorRuleTile);
                objectTilemap.SetTile(pos, inTile);
            }

            if (room.ExitDoorPos.HasValue && outTile != null)
            {
                Vector3Int pos = new Vector3Int(room.ExitDoorPos.Value.x, room.ExitDoorPos.Value.y, 0);
                fillFloorTilemap.SetTile(pos, fillFloorRuleTile);
                objectTilemap.SetTile(pos, outTile);
            }
        }
    }

    private void PlacePlayerAtFirstRoom()
    {
        if (GeneratedRooms.Count == 0)
            return;

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }

        if (playerTransform == null)
            return;

        Room firstRoom = GeneratedRooms[0];
        Vector3 center = new Vector3(
            firstRoom.WorldOriginTile.x + firstRoom.LocalSize.x / 2f,
            firstRoom.WorldOriginTile.y + firstRoom.LocalSize.y / 2f,
            playerTransform.position.z);
        playerTransform.position = center;
    }

    private void DrawDebugRoomConnections()
    {
        debugTilemap.ClearAllTiles();

        List<Room> mainPathRooms = GeneratedRooms.FindAll(r => r.Type != RoomType.Chest);
        for (int i = 0; i < mainPathRooms.Count - 1; i++)
        {
            DrawOrthogonalPath(mainPathRooms[i].WorldCenterTile, mainPathRooms[i + 1].WorldCenterTile);
        }

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
[CustomEditor(typeof(DungeonGenerator))]
public class DungeonGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DungeonGenerator generator = (DungeonGenerator)target;

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