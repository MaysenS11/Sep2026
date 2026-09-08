using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Dungeon.Spawning;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dungeon
{
    [ExecuteAlways]
    public class DungeonManager : MonoBehaviour
    {
        public static DungeonManager Instance { get; private set; }

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

        [Header("Door Assets (Legacy Reference)")]
        [SerializeField] private TileBase entranceDoorTile;
        [SerializeField] private TileBase exitDoorTile;
        [SerializeField] private TileBase specialEntranceDoorTile;
        [SerializeField] private TileBase specialExitDoorTile;

        public Tilemap DoorTilemap => objectTilemap;
        public TileBase EntranceDoorTile => doorSpawner.EntranceDoorTile;
        public TileBase ExitDoorTile => doorSpawner.ExitDoorTile;
        public TileBase SpecialEntranceDoorTile => doorSpawner.SpecialEntranceDoorTile;
        public TileBase SpecialExitDoorTile => doorSpawner.SpecialExitDoorTile;

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

        [Header("Spawners")]
        [SerializeField] private DoorSpawner doorSpawner = new DoorSpawner();
        [SerializeField] private EnemySpawner enemySpawner = new EnemySpawner();
        [SerializeField] private PropSpawner propSpawner = new PropSpawner();

        public List<GameManager.RoomData> GeneratedRooms { get; private set; } = new List<GameManager.RoomData>();

        private readonly DungeonLayoutPlanner _layoutPlanner = new DungeonLayoutPlanner();
        private readonly DungeonTilemapRenderer _tilemapRenderer = new DungeonTilemapRenderer();
        private readonly RoomTileQuery _tileQuery = new RoomTileQuery();
        private readonly List<IDungeonSpawner> _spawners = new List<IDungeonSpawner>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SyncLegacyFields();
            RegisterDefaultSpawners();
        }

        private void OnEnable()
        {
            EventBus<GenerateDungeonEvent>.Subscribe(OnGenerateDungeonEvent);
        }

        private void OnDisable()
        {
            EventBus<GenerateDungeonEvent>.Unsubscribe(OnGenerateDungeonEvent);
        }

        private void Start()
        {
            // Do not generate a new dungeon on Start.
            // If the dungeon was generated in editor, ensure data is published to GameManager.
            if (GeneratedRooms != null && GeneratedRooms.Count > 0)
            {
                PublishDungeonData();
            }
        }

        private void OnGenerateDungeonEvent(GenerateDungeonEvent evt)
        {
            if (ScreenFadeTransition.Instance != null && Application.isPlaying)
            {
                StartCoroutine(ScreenFadeTransition.Instance.PlayTransition(() =>
                {
                    GenerateAndBuildDungeon();
                }));
            }
            else
            {
                GenerateAndBuildDungeon();
            }
        }

        private void SyncLegacyFields()
        {
            if (entranceDoorTile != null && doorSpawner.EntranceDoorTile == null) doorSpawner.EntranceDoorTile = entranceDoorTile;
            if (exitDoorTile != null && doorSpawner.ExitDoorTile == null) doorSpawner.ExitDoorTile = exitDoorTile;
            if (specialEntranceDoorTile != null && doorSpawner.SpecialEntranceDoorTile == null) doorSpawner.SpecialEntranceDoorTile = specialEntranceDoorTile;
            if (specialExitDoorTile != null && doorSpawner.SpecialExitDoorTile == null) doorSpawner.SpecialExitDoorTile = specialExitDoorTile;
            doorSpawner.MinDoorDistance = minDoorDistance;
        }

        private void RegisterDefaultSpawners()
        {
            _spawners.Clear();
            doorSpawner.Initialize(objectTilemap, fillFloorRuleTile);
            _spawners.Add(doorSpawner);
            _spawners.Add(enemySpawner);
            _spawners.Add(propSpawner);
        }

        public void RegisterSpawner(IDungeonSpawner spawner)
        {
            if (spawner != null && !_spawners.Contains(spawner))
            {
                _spawners.Add(spawner);
            }
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

            SyncLegacyFields();
            RegisterDefaultSpawners();

            GameManager.Instance.ConfigureDoorTiles(
                objectTilemap,
                doorSpawner.EntranceDoorTile,
                doorSpawner.ExitDoorTile,
                doorSpawner.SpecialEntranceDoorTile,
                doorSpawner.SpecialExitDoorTile
            );

            ClearDungeonTiles();
            _tileQuery.ClearOccupancy();

            // Step 1: Layout Planning
            GeneratedRooms = _layoutPlanner.GenerateLayout(
                minRooms,
                maxRooms,
                macroCellSize,
                minNormalRoomSize,
                maxNormalRoomSize,
                fixedBossRoomSize,
                fixedChestRoomSize
            );

            // Step 2: Tilemap Floor / Wall / Roof Rendering
            _tilemapRenderer.RenderRooms(
                GeneratedRooms,
                roofTilemap,
                wallTilemap,
                borderFloorTilemap,
                fillFloorTilemap,
                roofRuleTile,
                wallRuleTile,
                borderFloorRuleTile,
                fillFloorRuleTile
            );

            // Step 3: Content Spawning (Doors -> Enemies -> Props)
            SpawnRoomContents();

            // Step 4: Publish Room Data to GameManager
            PublishDungeonData();

            // Step 5: Position player in starting room and reset stats if playing
            PositionPlayerAtStartRoom();

            // Step 6: Adjust camera bounds to starting room
            CameraBounds cameraBounds = Object.FindAnyObjectByType<CameraBounds>();
            if (cameraBounds != null)
            {
                cameraBounds.AdjustToStartRoom();
            }

            // Debug visualization
            if (debugTilemap != null && debugPathTile != null)
            {
                _tilemapRenderer.DrawDebugConnections(GeneratedRooms, debugTilemap, debugPathTile);
            }

            MarkTilemapsDirtyInEditor();
        }

        private void PositionPlayerAtStartRoom()
        {
            if (GeneratedRooms == null || GeneratedRooms.Count == 0) return;

            Vector3 startPos = GeneratedRooms[0].CenterTilePosition;
            PlayerMovement playerMovement = Object.FindAnyObjectByType<PlayerMovement>();
            if (playerMovement != null)
            {
                if (Application.isPlaying)
                {
                    playerMovement.TeleportTo(startPos);
                    if (playerMovement.TryGetComponent<PlayerStats>(out var stats))
                    {
                        stats.ResetHealth();
                    }
                }
                else
                {
                    Vector3 target = startPos;
                    target.z = playerMovement.transform.position.z;
                    playerMovement.transform.position = target;
#if UNITY_EDITOR
                    EditorUtility.SetDirty(playerMovement.gameObject);
#endif
                }
            }
        }

        private void SpawnRoomContents()
        {
            for (int r = 0; r < GeneratedRooms.Count; r++)
            {
                GameManager.RoomData room = GeneratedRooms[r];
                for (int s = 0; s < _spawners.Count; s++)
                {
                    _spawners[s].SpawnContent(room, _tileQuery, fillFloorTilemap, transform);
                }
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

            for (int s = 0; s < _spawners.Count; s++)
            {
                _spawners[s].ClearSpawnedContent();
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

        private void MarkTilemapsDirtyInEditor()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (roofTilemap != null) EditorUtility.SetDirty(roofTilemap);
                if (wallTilemap != null) EditorUtility.SetDirty(wallTilemap);
                if (borderFloorTilemap != null) EditorUtility.SetDirty(borderFloorTilemap);
                if (fillFloorTilemap != null) EditorUtility.SetDirty(fillFloorTilemap);
                if (objectTilemap != null) EditorUtility.SetDirty(objectTilemap);
                if (debugTilemap != null) EditorUtility.SetDirty(debugTilemap);
            }
#endif
        }
    }
}
