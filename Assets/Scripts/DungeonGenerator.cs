using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class DungeonGenerator : MonoBehaviour
{
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

    public Tilemap DoorTilemap => objectTilemap;
    public TileBase EntranceDoorTile => entranceDoorTile;
    public TileBase ExitDoorTile => exitDoorTile;
    public TileBase SpecialEntranceDoorTile => specialEntranceDoorTile;
    public TileBase SpecialExitDoorTile => specialExitDoorTile;

    [Header("Debug Settings")]
    [SerializeField] private TileBase debugPathTile;

    [Header("Dungeon Settings")]
    [SerializeField] private int minRooms = 6;
    [SerializeField] private int maxRooms = 9;
    [SerializeField] private Vector2Int macroCellSize = new Vector2Int(25, 20);
    [SerializeField] private float minDoorDistance = 5.0f;

    [Header("Room Size Settings")]
    [SerializeField] private Vector2Int minNormalRoomSize = new Vector2Int(15, 12);
    [SerializeField] private Vector2Int maxNormalRoomSize = new Vector2Int(20, 16);
    [SerializeField] private Vector2Int fixedBossRoomSize = new Vector2Int(18, 16);
    [SerializeField] private Vector2Int fixedChestRoomSize = new Vector2Int(12, 12);

    public List<GameManager.RoomData> GeneratedRooms { get; private set; } = new List<GameManager.RoomData>();

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
    private readonly List<GameManager.RoomData> _candidateParentRoomsBuffer = new List<GameManager.RoomData>(16);
    private readonly List<RectBounds> _subRectsBuffer = new List<RectBounds>(8);
    private TileBase[] _tileBuffer = new TileBase[1024];

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

        if (GameManager.Instance == null) return;
        
        GameManager.Instance.ConfigureDoorTiles
        (
            objectTilemap,
            entranceDoorTile,
            exitDoorTile,
            specialEntranceDoorTile,
            specialExitDoorTile
        );

        ClearDungeonTiles();
        GeneratedRooms.Clear();

        int totalRooms = Mathf.Clamp(Random.Range(minRooms, maxRooms + 1), 4, outerPerimeter.Length);
        int startPerimeterIdx = Random.Range(0, outerPerimeter.Length);

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
                )
            };

            GeneratedRooms.Add(room);
        }

        TryInsertChestRooms();
        CalculateInteriorDoors();
        RenderDungeonTiles();
        ApplyDoorsToTilemap();
        PublishDungeonData();

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
    }

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
            GameManager.RoomData parentRoom = _candidateParentRoomsBuffer[parentIdx];
            _candidateParentRoomsBuffer.RemoveAt(parentIdx);

            Vector2Int padding = macroCellSize - fixedChestRoomSize;
            Vector2Int centeredOffset = new Vector2Int(padding.x / 2, padding.y / 2);

            GameManager.RoomData chestRoom = new GameManager.RoomData
            {
                RoomIndex = GeneratedRooms.Count,
                ParentRoomIndex = parentRoom.RoomIndex,
                MacroPos = selectedChestMacro.Value,
                Type = RoomType.Chest,
                Shape = RoomShape.Rectangle,
                Size = fixedChestRoomSize,
                WorldOriginTile = new Vector2Int(
                    (selectedChestMacro.Value.x * macroCellSize.x) + centeredOffset.x,
                    (selectedChestMacro.Value.y * macroCellSize.y) + centeredOffset.y
                )
            };

            GeneratedRooms.Add(chestRoom);
        }
    }

    private void CalculateInteriorDoors()
    {
        float minDoorDistanceSqr = minDoorDistance * minDoorDistance;

        for (int i = 0; i < GeneratedRooms.Count; i++)
        {
            GameManager.RoomData room = GeneratedRooms[i];
            GetValidInteriorFloorTiles(room, _validFloorTilesBuffer);

            if (_validFloorTilesBuffer.Count < 2) continue;

            if (room.Type == RoomType.Start)
            {
                room.EntranceDoorTile = null;
                room.ExitDoorTile = _validFloorTilesBuffer[Random.Range(0, _validFloorTilesBuffer.Count)];
            }
            else if (room.Type == RoomType.Boss)
            {
                room.EntranceDoorTile = _validFloorTilesBuffer[Random.Range(0, _validFloorTilesBuffer.Count)];
                room.ExitDoorTile = null;
            }
            else
            {
                Vector2Int entrance = _validFloorTilesBuffer[Random.Range(0, _validFloorTilesBuffer.Count)];
                room.EntranceDoorTile = entrance;

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
                    room.ExitDoorTile = _exitCandidatesBuffer[Random.Range(0, _exitCandidatesBuffer.Count)];
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
                    room.ExitDoorTile = farthestTile;
                }
            }
        }
    }

    private void PublishDungeonData()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.DungeonDictionary.Clear();
        for (int i = 0; i < GeneratedRooms.Count; i++)
        {
            GameManager.RoomData room = GeneratedRooms[i];

            room.EntryDoorPosition = GetWorldPosition(room.EntranceDoorTile);
            room.ExitDoorPosition = GetWorldPosition(room.ExitDoorTile);

            room.CenterPosition = new Vector3(
                room.WorldOriginTile.x + (room.Size.x / 2f),
                room.WorldOriginTile.y + (room.Size.y / 2f),
                0f
            );

            room.CenterTile = new Vector2Int(
                room.WorldOriginTile.x + (room.Size.x / 2),
                room.WorldOriginTile.y + (room.Size.y / 2)
            );
            room.CenterTilePosition = GetWorldPosition(room.CenterTile);

            if (room.ParentRoomIndex != -1 && GameManager.Instance.DungeonDictionary.TryGetValue(room.ParentRoomIndex, out var parentRoom))
            {
                room.HasSpecialChestRoom = true;
                room.SpecialChestRoomIndex = parentRoom.RoomIndex;
                room.SpecialEntryDoorPosition = GetWorldPosition(room.EntranceDoorTile);
                room.SpecialExitDoorPosition = GetWorldPosition(parentRoom.ExitDoorTile);

                parentRoom.HasSpecialChestRoom = true;
                parentRoom.SpecialChestRoomIndex = room.RoomIndex;
                parentRoom.SpecialExitDoorPosition = GetWorldPosition(parentRoom.ExitDoorTile);
            }

            GameManager.Instance.DungeonDictionary[room.RoomIndex] = room;
        }

        GameManager.Instance.CurrentRoomIndex = GeneratedRooms.Count > 0 ? GeneratedRooms[0].RoomIndex : 0;
        if (GeneratedRooms.Count > 0)
        {
            GameManager.NotifyNewRoomEntered(GameManager.Instance.DungeonDictionary[GameManager.Instance.CurrentRoomIndex]);
        }
    }

    private Vector3 GetWorldPosition(Vector2Int tilePosition)
    {
        return fillFloorTilemap.GetCellCenterWorld(new Vector3Int(tilePosition.x, tilePosition.y, 0));
    }

    private Vector3? GetWorldPosition(Vector2Int? tilePosition)
    {
        if (!tilePosition.HasValue || fillFloorTilemap == null) return null;

        Vector2Int position = tilePosition.Value;
        return fillFloorTilemap.GetCellCenterWorld(new Vector3Int(position.x, position.y, 0));
    }

    private void RenderDungeonTiles()
    {
        for (int i = 0; i < GeneratedRooms.Count; i++)
        {
            GameManager.RoomData room = GeneratedRooms[i];

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

    private void ApplyDoorsToTilemap()
    {
        if (objectTilemap == null) return;

        for (int i = 0; i < GeneratedRooms.Count; i++)
        {
            GameManager.RoomData room = GeneratedRooms[i];
            TileBase inTile = room.Type == RoomType.Chest && specialEntranceDoorTile != null ? specialEntranceDoorTile : entranceDoorTile;
            TileBase outTile = room.ParentRoomIndex != -1 && specialExitDoorTile != null ? specialExitDoorTile : exitDoorTile;

            if (room.EntranceDoorTile.HasValue && inTile != null)
            {
                Vector3Int pos = new Vector3Int(room.EntranceDoorTile.Value.x, room.EntranceDoorTile.Value.y, 0);
                fillFloorTilemap.SetTile(pos, fillFloorRuleTile);
                objectTilemap.SetTile(pos, inTile);
            }

            if (room.ExitDoorTile.HasValue && outTile != null)
            {
                Vector3Int pos = new Vector3Int(room.ExitDoorTile.Value.x, room.ExitDoorTile.Value.y, 0);
                fillFloorTilemap.SetTile(pos, fillFloorRuleTile);
                objectTilemap.SetTile(pos, outTile);
            }
        }
    }

    private void DrawDebugRoomConnections()
    {
        if (debugTilemap == null || debugPathTile == null)
        {
            return;
        }
        debugTilemap.ClearAllTiles();

        _candidateParentRoomsBuffer.Clear();
        List<GameManager.RoomData> chestRooms = new List<GameManager.RoomData>();

        for (int i = 0; i < GeneratedRooms.Count; i++)
        {
            GameManager.RoomData room = GeneratedRooms[i];
            if (room.Type != RoomType.Chest) _candidateParentRoomsBuffer.Add(room);
            else chestRooms.Add(room);
        }

        for (int i = 0; i < _candidateParentRoomsBuffer.Count - 1; i++)
        {
            DrawOrthogonalPath(_candidateParentRoomsBuffer[i].CenterTile, _candidateParentRoomsBuffer[i + 1].CenterTile);
        }

        for (int i = 0; i < chestRooms.Count; i++)
        {
            GameManager.RoomData chest = chestRooms[i];
            if (chest.ParentRoomIndex != -1 && GameManager.Instance.DungeonDictionary.TryGetValue(chest.ParentRoomIndex, out var parentRoom))
            {
                DrawOrthogonalPath(parentRoom.CenterTile, chest.CenterTile);
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

    private void GetValidInteriorFloorTiles(GameManager.RoomData room, List<Vector2Int> results)
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