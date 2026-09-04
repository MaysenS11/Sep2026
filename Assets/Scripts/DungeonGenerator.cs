using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum RoomType { Start, Normal, Chest, Boss }
public enum RoomShape { Rectangle, LShape, TShape, UShape }
public enum LRotation { TopRight, TopLeft, BottomRight, BottomLeft }

public class DungeonGenerator : MonoBehaviour
{
    #region Nested Types
    public class Room
    {
        public int Index;
        public Vector2Int MacroPos;
        public RoomType Type;
        public RoomShape Shape;
        public LRotation LRot;
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

    private struct RectBounds
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
    #endregion

    #region Inspector Fields
    [Header("Layer Tilemaps")]
    [SerializeField] private Tilemap roofTilemap;
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField] private Tilemap borderFloorTilemap;
    [SerializeField] private Tilemap fillFloorTilemap;
    [SerializeField] private Tilemap objectTilemap;
    [SerializeField] private Tilemap debugTilemap;

    [Header("Rule Tile Assets")]
    [SerializeField] private TileBase roofRuleTile;
    [SerializeField] private TileBase wallRuleTile;
    [SerializeField] private TileBase borderFloorRuleTile;
    [SerializeField] private TileBase fillFloorRuleTile;

    [Header("Door Assets")]
    [SerializeField] private TileBase entranceDoorTile;
    [SerializeField] private TileBase exitDoorTile;
    [SerializeField] private TileBase specialEntranceDoorTile;
    [SerializeField] private TileBase specialExitDoorTile;

    [Header("Debug Settings")]
    [SerializeField] private TileBase debugPathTile;

    [Header("Dungeon Settings")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private int minRooms = 6;
    [SerializeField] private int maxRooms = 9;
    [SerializeField] private Vector2Int macroCellSize = new Vector2Int(25, 20);
    [SerializeField] private float minDoorDistance = 5.0f;

    [Header("Room Size Settings")]
    [SerializeField] private Vector2Int minNormalRoomSize = new Vector2Int(15, 12);
    [SerializeField] private Vector2Int maxNormalRoomSize = new Vector2Int(20, 16);
    [SerializeField] private Vector2Int fixedBossRoomSize = new Vector2Int(18, 16);
    [SerializeField] private Vector2Int fixedChestRoomSize = new Vector2Int(12, 12);
    #endregion

    #region Properties & Fields
    public List<Room> GeneratedRooms { get; private set; } = new List<Room>();

    private static readonly Vector2Int[] outerPerimeter = new Vector2Int[]
    {
        new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0),
        new Vector2Int(3, 1),
        new Vector2Int(3, 2), new Vector2Int(2, 2), new Vector2Int(1, 2), new Vector2Int(0, 2),
        new Vector2Int(0, 1)
    };

    private static readonly Vector2Int[] innerChestCells = new Vector2Int[]
    {
        new Vector2Int(1, 1),
        new Vector2Int(2, 1)
    };

    private readonly List<Vector2Int> _validFloorTilesBuffer = new List<Vector2Int>(256);
    private readonly List<Vector2Int> _exitCandidatesBuffer = new List<Vector2Int>(256);
    private readonly List<Room> _candidateParentRoomsBuffer = new List<Room>(16);
    private readonly Dictionary<Vector2Int, DoorTriggerData> _doorMap = new Dictionary<Vector2Int, DoorTriggerData>();

    private DoorTriggerData _activeDoor;
    #endregion

    public struct DoorTriggerData
    {
        public Vector2Int Position;
        public Vector2Int TargetPosition;
        public bool IsExitDoor;
        public Room CurrentRoom;
        public Room TargetRoom;
    }

    #region Unity Lifecycle
    private void Awake()
    {
        CachePlayerTransform();
    }

    private void Start()
    {
        GenerateAndBuildDungeon();
    }

    private void Update()
    {
        if (Application.isPlaying && _activeDoor.CurrentRoom != null && Input.GetKeyDown(interactKey))
        {
            PerformDoorTransition(_activeDoor);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Vector3Int cellPos = fillFloorTilemap.WorldToCell(other.transform.position);
        Vector2Int tilePos = new Vector2Int(cellPos.x, cellPos.y);

        if (_doorMap.TryGetValue(tilePos, out DoorTriggerData doorData))
        {
            _activeDoor = doorData;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Vector3Int cellPos = fillFloorTilemap.WorldToCell(other.transform.position);
        Vector2Int tilePos = new Vector2Int(cellPos.x, cellPos.y);

        if (_doorMap.ContainsKey(tilePos))
        {
            _activeDoor = default;
        }
    }

    private void OnGUI()
    {
        if (Application.isPlaying && _activeDoor.CurrentRoom != null)
        {
            GUIStyle style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            string actionText = _activeDoor.IsExitDoor ? "Enter Next Room" : "Return to Previous Room";
            GUI.Box(new Rect(Screen.width / 2 - 150, Screen.height - 80, 300, 40), $"Press [{interactKey}] to {actionText}", style);
        }
    }
    #endregion

    #region Public Interface
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
        _doorMap.Clear();

        int totalRooms = Mathf.Clamp(Random.Range(minRooms, maxRooms + 1), 4, outerPerimeter.Length);
        int startPerimeterIdx = Random.Range(0, outerPerimeter.Length);

        // 1. Generate Main Path Rooms
        for (int i = 0; i < totalRooms; i++)
        {
            int pIdx = (startPerimeterIdx + i) % outerPerimeter.Length;
            Vector2Int macroPos = outerPerimeter[pIdx];

            RoomType type = RoomType.Normal;
            if (i == 0) type = RoomType.Start;
            else if (i == totalRooms - 1) type = RoomType.Boss;

            Vector2Int size = GetRoomSize(type);
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

            Room room = new Room
            {
                Index = i,
                MacroPos = macroPos,
                Type = type,
                Shape = shape,
                LRot = (LRotation)Random.Range(0, 4),
                LocalSize = size,
                WorldOriginTile = new Vector2Int(
                    (macroPos.x * macroCellSize.x) + centeredOffset.x,
                    (macroPos.y * macroCellSize.y) + centeredOffset.y
                )
            };

            GeneratedRooms.Add(room);
        }

        // 2. Generate Special Chest Rooms
        TryInsertChestRooms();

        // 3. Doors, Layout, and Teleport Mappings
        CalculateInteriorDoors();
        LinkDoorTriggers();
        RenderDungeonTiles();
        ApplyDoorsToTilemap();
        PlacePlayerAtFirstRoom();

        if (debugTilemap != null && debugPathTile != null)
        {
            DrawDebugRoomConnections();
        }

        MarkTilemapsDirtyInEditor();
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
        _doorMap.Clear();
    }
    #endregion

    #region Generation Pipeline Steps
    private Vector2Int GetRoomSize(RoomType type)
    {
        if (type == RoomType.Boss) return fixedBossRoomSize;
        if (type == RoomType.Chest) return fixedChestRoomSize;

        int width = Random.Range(minNormalRoomSize.x, maxNormalRoomSize.x + 1);
        int height = Random.Range(minNormalRoomSize.y, maxNormalRoomSize.y + 1);

        width = Mathf.Max(width, 12);
        height = Mathf.Max(height, 12);

        return new Vector2Int(width, height);
    }

    private void TryInsertChestRooms()
    {
        _candidateParentRoomsBuffer.Clear();
        for (int i = 0; i < GeneratedRooms.Count; i++)
        {
            if (GeneratedRooms[i].Type == RoomType.Normal)
                _candidateParentRoomsBuffer.Add(GeneratedRooms[i]);
        }

        if (_candidateParentRoomsBuffer.Count == 0) return;

        int chestRoomCount = Random.Range(1, 3);

        for (int c = 0; c < chestRoomCount; c++)
        {
            if (_candidateParentRoomsBuffer.Count == 0) break;

            Vector2Int? selectedChestMacro = null;
            for (int i = 0; i < innerChestCells.Length; i++)
            {
                Vector2Int cell = innerChestCells[i];
                bool isOccupied = false;
                for (int r = 0; r < GeneratedRooms.Count; r++)
                {
                    if (GeneratedRooms[r].MacroPos == cell)
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
            Room parentRoom = _candidateParentRoomsBuffer[parentIdx];
            _candidateParentRoomsBuffer.RemoveAt(parentIdx);

            Vector2Int padding = macroCellSize - fixedChestRoomSize;
            Vector2Int centeredOffset = new Vector2Int(padding.x / 2, padding.y / 2);

            Room chestRoom = new Room
            {
                Index = GeneratedRooms.Count,
                MacroPos = selectedChestMacro.Value,
                Type = RoomType.Chest,
                Shape = RoomShape.Rectangle,
                LocalSize = fixedChestRoomSize,
                WorldOriginTile = new Vector2Int(
                    (selectedChestMacro.Value.x * macroCellSize.x) + centeredOffset.x,
                    (selectedChestMacro.Value.y * macroCellSize.y) + centeredOffset.y
                ),
                ParentRoom = parentRoom
            };

            GeneratedRooms.Add(chestRoom);
        }
    }

    private void CalculateInteriorDoors()
    {
        float minDoorDistanceSqr = minDoorDistance * minDoorDistance;

        for (int i = 0; i < GeneratedRooms.Count; i++)
        {
            Room room = GeneratedRooms[i];
            GetValidInteriorFloorTiles(room, _validFloorTilesBuffer);

            if (_validFloorTilesBuffer.Count < 2) continue;

            if (room.Type == RoomType.Start)
            {
                room.EntranceDoorPos = null;
                room.ExitDoorPos = _validFloorTilesBuffer[Random.Range(0, _validFloorTilesBuffer.Count)];
            }
            else if (room.Type == RoomType.Boss)
            {
                room.EntranceDoorPos = _validFloorTilesBuffer[Random.Range(0, _validFloorTilesBuffer.Count)];
                room.ExitDoorPos = null;
            }
            else
            {
                Vector2Int entrance = _validFloorTilesBuffer[Random.Range(0, _validFloorTilesBuffer.Count)];
                room.EntranceDoorPos = entrance;

                _exitCandidatesBuffer.Clear();
                for (int t = 0; t < _validFloorTilesBuffer.Count; t++)
                {
                    Vector2Int tile = _validFloorTilesBuffer[t];
                    if ((tile - entrance).sqrMagnitude >= minDoorDistanceSqr)
                    {
                        _exitCandidatesBuffer.Add(tile);
                    }
                }

                if (_exitCandidatesBuffer.Count > 0)
                {
                    room.ExitDoorPos = _exitCandidatesBuffer[Random.Range(0, _exitCandidatesBuffer.Count)];
                }
                else
                {
                    int maxSqrDist = -1;
                    Vector2Int farthestTile = _validFloorTilesBuffer[0];

                    for (int t = 0; t < _validFloorTilesBuffer.Count; t++)
                    {
                        Vector2Int tile = _validFloorTilesBuffer[t];
                        int sqrDist = (tile - entrance).sqrMagnitude;
                        if (sqrDist > maxSqrDist)
                        {
                            maxSqrDist = sqrDist;
                            farthestTile = tile;
                        }
                    }
                    room.ExitDoorPos = farthestTile;
                }
            }
        }
    }

    private void LinkDoorTriggers()
    {
        _doorMap.Clear();

        for (int i = 0; i < GeneratedRooms.Count; i++)
        {
            Room currentRoom = GeneratedRooms[i];
            if (currentRoom.Type == RoomType.Chest) continue;

            if (currentRoom.ExitDoorPos.HasValue && i + 1 < GeneratedRooms.Count)
            {
                Room nextRoom = GeneratedRooms[i + 1];
                if (nextRoom.EntranceDoorPos.HasValue)
                {
                    _doorMap[currentRoom.ExitDoorPos.Value] = new DoorTriggerData
                    {
                        Position = currentRoom.ExitDoorPos.Value,
                        TargetPosition = nextRoom.EntranceDoorPos.Value,
                        IsExitDoor = true,
                        CurrentRoom = currentRoom,
                        TargetRoom = nextRoom
                    };

                    _doorMap[nextRoom.EntranceDoorPos.Value] = new DoorTriggerData
                    {
                        Position = nextRoom.EntranceDoorPos.Value,
                        TargetPosition = currentRoom.ExitDoorPos.Value,
                        IsExitDoor = false,
                        CurrentRoom = nextRoom,
                        TargetRoom = currentRoom
                    };
                }
            }
        }

        for (int i = 0; i < GeneratedRooms.Count; i++)
        {
            Room chest = GeneratedRooms[i];
            if (chest.Type != RoomType.Chest || chest.ParentRoom == null) continue;

            Room host = chest.ParentRoom;
            if (chest.EntranceDoorPos.HasValue && host.ExitDoorPos.HasValue)
            {
                _doorMap[host.ExitDoorPos.Value] = new DoorTriggerData
                {
                    Position = host.ExitDoorPos.Value,
                    TargetPosition = chest.EntranceDoorPos.Value,
                    IsExitDoor = true,
                    CurrentRoom = host,
                    TargetRoom = chest
                };

                _doorMap[chest.EntranceDoorPos.Value] = new DoorTriggerData
                {
                    Position = chest.EntranceDoorPos.Value,
                    TargetPosition = host.ExitDoorPos.Value,
                    IsExitDoor = false,
                    CurrentRoom = chest,
                    TargetRoom = host
                };
            }
        }
    }

    private void PerformDoorTransition(DoorTriggerData door)
    {
        CachePlayerTransform();
        if (playerTransform == null) return;

        Vector3Int targetCell = new Vector3Int(door.TargetPosition.x, door.TargetPosition.y, 0);
        Vector3 targetWorld = fillFloorTilemap.GetCellCenterWorld(targetCell);
        targetWorld.z = playerTransform.position.z;

        playerTransform.position = targetWorld;
        _activeDoor = default;
    }

    private void PlacePlayerAtFirstRoom()
    {
        if (GeneratedRooms.Count == 0) return;

        CachePlayerTransform();
        if (playerTransform == null) return;

        Room firstRoom = GeneratedRooms[0];
        Vector3Int spawnCell = new Vector3Int(firstRoom.WorldCenterTile.x, firstRoom.WorldCenterTile.y, 0);

        Vector3 center = fillFloorTilemap.GetCellCenterWorld(spawnCell);
        center.z = playerTransform.position.z;
        playerTransform.position = center;
    }
    #endregion

    #region Rendering & Sub-Rectangle Layering Logic
    private void RenderDungeonTiles()
    {
        for (int i = 0; i < GeneratedRooms.Count; i++)
        {
            Room room = GeneratedRooms[i];

            // 1. Draw Fill Layer FIRST (-2)
            DrawDecomposedLayer(room, fillFloorTilemap, fillFloorRuleTile, -2);

            // 2. Draw Border Floor Layer (0)
            DrawDecomposedLayer(room, borderFloorTilemap, borderFloorRuleTile, 0);

            // 3. Draw Wall Layer (+2)
            DrawDecomposedLayer(room, wallTilemap, wallRuleTile, 2);

            // 4. Draw Roof Layer (+4)
            DrawDecomposedLayer(room, roofTilemap, roofRuleTile, 4);
        }

        roofTilemap.RefreshAllTiles();
        wallTilemap.RefreshAllTiles();
        borderFloorTilemap.RefreshAllTiles();
        fillFloorTilemap.RefreshAllTiles();
    }

    private void DrawDecomposedLayer(Room room, Tilemap tilemap, TileBase tile, int diameterOffset)
    {
        List<RectBounds> subRects = GetRoomSubRectangles(room);

        for (int i = 0; i < subRects.Count; i++)
        {
            RectBounds expanded = subRects[i].Expand(diameterOffset);
            DrawRectangle(tilemap, tile, expanded.Origin, expanded.Size);
        }
    }

    private List<RectBounds> GetRoomSubRectangles(Room room)
    {
        List<RectBounds> rects = new List<RectBounds>();
        Vector2Int origin = room.WorldOriginTile;
        Vector2Int size = room.LocalSize;

        int halfW = size.x / 2;
        int halfH = size.y / 2;

        switch (room.Shape)
        {
            case RoomShape.Rectangle:
                rects.Add(new RectBounds(origin, size));
                break;

            case RoomShape.LShape:
                switch (room.LRot)
                {
                    case LRotation.TopRight: // Cut Top-Right
                        rects.Add(new RectBounds(origin, new Vector2Int(size.x, halfH)));
                        rects.Add(new RectBounds(origin, new Vector2Int(halfW, size.y)));
                        break;

                    case LRotation.TopLeft: // Cut Top-Left
                        rects.Add(new RectBounds(origin, new Vector2Int(size.x, halfH)));
                        rects.Add(new RectBounds(new Vector2Int(origin.x + halfW, origin.y), new Vector2Int(size.x - halfW, size.y)));
                        break;

                    case LRotation.BottomRight: // Cut Bottom-Right
                        rects.Add(new RectBounds(new Vector2Int(origin.x, origin.y + halfH), new Vector2Int(size.x, size.y - halfH)));
                        rects.Add(new RectBounds(origin, new Vector2Int(halfW, size.y)));
                        break;

                    case LRotation.BottomLeft: // Cut Bottom-Left
                        rects.Add(new RectBounds(new Vector2Int(origin.x, origin.y + halfH), new Vector2Int(size.x, size.y - halfH)));
                        rects.Add(new RectBounds(new Vector2Int(origin.x + halfW, origin.y), new Vector2Int(size.x - halfW, size.y)));
                        break;
                }
                break;

            case RoomShape.TShape: // Horizontal cross bar + Vertical stem
                rects.Add(new RectBounds(origin, new Vector2Int(size.x, halfH)));
                rects.Add(new RectBounds(new Vector2Int(origin.x + (size.x - 5) / 2, origin.y), new Vector2Int(5, size.y)));
                break;

            case RoomShape.UShape: // Bottom bar + Left/Right vertical arms
                rects.Add(new RectBounds(origin, new Vector2Int(size.x, 5)));
                rects.Add(new RectBounds(origin, new Vector2Int(5, size.y)));
                rects.Add(new RectBounds(new Vector2Int(origin.x + size.x - 5, origin.y), new Vector2Int(5, size.y)));
                break;
        }

        return rects;
    }

    private void DrawRectangle(Tilemap tilemap, TileBase tile, Vector2Int origin, Vector2Int size)
    {
        if (size.x <= 0 || size.y <= 0) return;

        int totalTiles = size.x * size.y;
        TileBase[] tileArray = new TileBase[totalTiles];

        for (int i = 0; i < totalTiles; i++) tileArray[i] = tile;

        tilemap.SetTilesBlock(new BoundsInt(new Vector3Int(origin.x, origin.y, 0), new Vector3Int(size.x, size.y, 1)), tileArray);
    }

    private void ApplyDoorsToTilemap()
    {
        if (objectTilemap == null) return;

        for (int i = 0; i < GeneratedRooms.Count; i++)
        {
            Room room = GeneratedRooms[i];
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

    private void DrawDebugRoomConnections()
    {
        debugTilemap.ClearAllTiles();

        _candidateParentRoomsBuffer.Clear();
        List<Room> chestRooms = new List<Room>();

        for (int i = 0; i < GeneratedRooms.Count; i++)
        {
            Room room = GeneratedRooms[i];
            if (room.Type != RoomType.Chest) _candidateParentRoomsBuffer.Add(room);
            else chestRooms.Add(room);
        }

        for (int i = 0; i < _candidateParentRoomsBuffer.Count - 1; i++)
        {
            DrawOrthogonalPath(_candidateParentRoomsBuffer[i].WorldCenterTile, _candidateParentRoomsBuffer[i + 1].WorldCenterTile);
        }

        for (int i = 0; i < chestRooms.Count; i++)
        {
            Room chest = chestRooms[i];
            if (chest.ParentRoom != null)
            {
                DrawOrthogonalPath(chest.ParentRoom.WorldCenterTile, chest.WorldCenterTile);
            }
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
    #endregion

    #region Helper Methods
    private void CachePlayerTransform()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }
    }

    private void GetValidInteriorFloorTiles(Room room, List<Vector2Int> results)
    {
        results.Clear();
        int halfWidth = room.LocalSize.x / 2;
        int halfHeight = room.LocalSize.y / 2;

        for (int x = 1; x < room.LocalSize.x - 1; x++)
        {
            for (int y = 1; y < room.LocalSize.y - 1; y++)
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
                    if (y >= halfHeight && (x < (room.LocalSize.x - 5) / 2 || x >= (room.LocalSize.x + 5) / 2)) continue;
                }
                else if (room.Shape == RoomShape.UShape)
                {
                    if (y >= 5 && (x >= 5 && x < room.LocalSize.x - 5)) continue;
                }

                results.Add(new Vector2Int(room.WorldOriginTile.x + x, room.WorldOriginTile.y + y));
            }
        }
    }

    private void MarkTilemapsDirtyInEditor()
    {
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
    #endregion
}

#region Editor Extensions
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
#endregion