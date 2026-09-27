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
        [SerializeField] private Tilemap wallTilemap;
        [SerializeField] private Tilemap floorTilemap;
        [SerializeField] private Tilemap objectTilemap;
        [SerializeField] private Tilemap debugTilemap;

        [Header("Rule Tile Assets")]
        [SerializeField] private TileBase wallRuleTile;
        [SerializeField] private TileBase floorRuleTile;

        [Header("Override Rule Tile Assets (Chest Rooms)")]
        [SerializeField] private TileBase wallOverrideRuleTile;
        [SerializeField] private TileBase floorOverrideRuleTile;

        [Header("Door Assets (Legacy Reference)")]
        [SerializeField] private TileBase entranceDoorTile;
        [SerializeField] private TileBase exitDoorTile;
        [SerializeField] private TileBase specialEntranceDoorTile;
        [SerializeField] private TileBase specialExitDoorTile;
        [SerializeField] private TileBase specialLockedDoorTile;
        [SerializeField] private TileBase specialUnlockedDoorTile;
        [SerializeField] private TileBase specialRoomExitDoorTile;

        public Tilemap DoorTilemap => objectTilemap;
        public TileBase WallOverrideRuleTile { get => wallOverrideRuleTile; set => wallOverrideRuleTile = value; }
        public TileBase FloorOverrideRuleTile { get => floorOverrideRuleTile; set => floorOverrideRuleTile = value; }
        public TileBase EntranceDoorTile => doorSpawner.EntranceDoorTile;
        public TileBase ExitDoorTile => doorSpawner.ExitDoorTile;
        public TileBase SpecialEntranceDoorTile => doorSpawner.SpecialEntranceDoorTile;
        public TileBase SpecialExitDoorTile => doorSpawner.SpecialExitDoorTile;
        public TileBase SpecialLockedDoorTile => doorSpawner.SpecialLockedDoorTile;
        public TileBase SpecialUnlockedDoorTile => doorSpawner.SpecialUnlockedDoorTile;
        public TileBase SpecialRoomExitDoorTile => doorSpawner.SpecialRoomExitDoorTile;

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
        [SerializeField] private Vector2Int fixedBossRoomSize = new Vector2Int(18, 18);
        [SerializeField] private Vector2Int fixedChestRoomSize = new Vector2Int(12, 12);

        [Header("Boss Room")]
        [SerializeField] private BossRoom bossRoomPrefab;
        [SerializeField] private BossRoom activeBossRoomInstance;

        [Header("Room Density & Content Settings")]
        [Tooltip("Master room density: 0.0 = completely empty room, 1.0 = every valid interior floor tile is used.")]
        [Range(0f, 1f)]
        [SerializeField] private float generalRoomDensity = 0.2f;

        [Header("Spawners")]
        [SerializeField] private GameSpawnSettings spawnSettings;
        [SerializeField] private DoorSpawner doorSpawner = new DoorSpawner();
        [SerializeField] private EnemySpawner enemySpawner = new EnemySpawner();
        [SerializeField] private PropSpawner propSpawner = new PropSpawner();
        [SerializeField] private ChestSpawner chestSpawner = new ChestSpawner();

        public GameSpawnSettings SpawnSettings { get => spawnSettings; set => spawnSettings = value; }
        public EnemySpawner EnemySpawner => enemySpawner;
        public DoorSpawner DoorSpawner => doorSpawner;
        public PropSpawner PropSpawner => propSpawner;
        public ChestSpawner ChestSpawner => chestSpawner;
        public BossRoom BossRoomPrefab { get => bossRoomPrefab; set => bossRoomPrefab = value; }
        public float GeneralRoomDensity
        {
            get => generalRoomDensity;
            set => generalRoomDensity = Mathf.Clamp01(value);
        }

        [SerializeField, HideInInspector] private List<GameManager.RoomData> generatedRooms = new List<GameManager.RoomData>();
        public List<GameManager.RoomData> GeneratedRooms => generatedRooms;

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
            EventBus<ClearDungeonEvent>.Subscribe(OnClearDungeonEvent);
            EventBus<RoomEnteredEvent>.Subscribe(OnRoomEntered);
        }

        private void OnDisable()
        {
            EventBus<GenerateDungeonEvent>.Unsubscribe(OnGenerateDungeonEvent);
            EventBus<ClearDungeonEvent>.Unsubscribe(OnClearDungeonEvent);
            EventBus<RoomEnteredEvent>.Unsubscribe(OnRoomEntered);
        }

        private void OnRoomEntered(RoomEnteredEvent evt)
        {
            if (evt.Room != null && evt.Room.Type == RoomType.Boss)
            {
                if (activeBossRoomInstance == null)
                {
                    activeBossRoomInstance = Object.FindAnyObjectByType<BossRoom>();
                }
                if (activeBossRoomInstance != null && GameManager.Instance != null && GameManager.Instance.Board != null)
                {
                    activeBossRoomInstance.ScanAndRegisterBossEntities(
                        evt.Room.WorldOriginTile,
                        GameManager.Instance.Board,
                        GameManager.Instance.TurnCoordinator?.EffectsRunner,
                        evt.Room.RoomIndex
                    );
                }
            }
        }

        private void OnClearDungeonEvent(ClearDungeonEvent evt)
        {
            ClearDungeonTiles();
        }

        private void Start()
        {
            SyncLegacyFields();
            RegisterDefaultSpawners();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ConfigureDoorTiles(
                    objectTilemap,
                    doorSpawner.EntranceDoorTile,
                    doorSpawner.ExitDoorTile,
                    doorSpawner.SpecialEntranceDoorTile,
                    doorSpawner.SpecialExitDoorTile,
                    doorSpawner.SpecialLockedDoorTile,
                    doorSpawner.SpecialUnlockedDoorTile,
                    doorSpawner.SpecialRoomExitDoorTile
                );
            }

            if (spawnSettings != null && spawnSettings.SettingsModifiedSinceLastDungeon)
            {
                spawnSettings.SettingsModifiedSinceLastDungeon = false;
                GenerateAndBuildDungeon();
            }
            else if (generatedRooms != null && generatedRooms.Count > 0)
            {
                if (GameManager.Instance != null)
                {
                    var newBoard = Infrastructure.DungeonBridge.BuildBoardFromDungeon(generatedRooms, 10, floorTilemap);
                    GameManager.Instance.InitializeBoard(newBoard);
                }
                PublishDungeonData();
                PositionPlayerAtStartRoom();
                RegisterSceneEntitiesOnBoard();
                AssignChildChestRoomKeyholders();
                CameraBounds cameraBounds = Object.FindAnyObjectByType<CameraBounds>();
                if (cameraBounds != null)
                {
                    cameraBounds.AdjustToStartRoom();
                }
            }
            else
            {
                GenerateAndBuildDungeon();
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
            if (specialLockedDoorTile != null && doorSpawner.SpecialLockedDoorTile == null) doorSpawner.SpecialLockedDoorTile = specialLockedDoorTile;
            if (specialUnlockedDoorTile != null && doorSpawner.SpecialUnlockedDoorTile == null) doorSpawner.SpecialUnlockedDoorTile = specialUnlockedDoorTile;
            if (specialRoomExitDoorTile != null && doorSpawner.SpecialRoomExitDoorTile == null) doorSpawner.SpecialRoomExitDoorTile = specialRoomExitDoorTile;
            doorSpawner.MinDoorDistance = minDoorDistance;
        }

        private void RegisterDefaultSpawners()
        {
            if (objectTilemap == null)
            {
                var go = GameObject.Find("ObjectTilemap") ?? GameObject.Find("Objects") ?? GameObject.Find("DoorTilemap");
                if (go != null) objectTilemap = go.GetComponent<Tilemap>();
                if (objectTilemap == null)
                {
                    var allTilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
                    foreach (var tm in allTilemaps)
                    {
                        if (tm.name.ToLower().Contains("object") || tm.name.ToLower().Contains("door"))
                        {
                            objectTilemap = tm;
                            break;
                        }
                    }
                }
            }

            _spawners.Clear();
            doorSpawner.Initialize(objectTilemap, floorRuleTile, bossRoomPrefab);
            _spawners.Add(doorSpawner);
            _spawners.Add(propSpawner);
            _spawners.Add(enemySpawner);
            _spawners.Add(chestSpawner);
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
            if (wallTilemap == null || floorTilemap == null)
            {
                Debug.LogWarning("Assign wall and floor layer tilemaps in the Inspector!");
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
                doorSpawner.SpecialExitDoorTile,
                doorSpawner.SpecialLockedDoorTile,
                doorSpawner.SpecialUnlockedDoorTile,
                doorSpawner.SpecialRoomExitDoorTile
            );

            ClearDungeonTiles();
            _tileQuery.ClearOccupancy();

            generatedRooms = _layoutPlanner.GenerateLayout(
                minRooms,
                maxRooms,
                macroCellSize,
                minNormalRoomSize,
                maxNormalRoomSize,
                fixedBossRoomSize,
                fixedChestRoomSize
            );

            _tilemapRenderer.RenderRooms(
                generatedRooms,
                wallTilemap,
                floorTilemap,
                wallRuleTile,
                floorRuleTile,
                bossRoomPrefab,
                wallOverrideRuleTile,
                floorOverrideRuleTile
            );

            GameManager.RoomData bossRoomData = generatedRooms != null ? generatedRooms.Find(r => r.Type == RoomType.Boss) : null;
            if (bossRoomData != null && bossRoomPrefab != null && activeBossRoomInstance == null)
            {
                Vector3 bossPos = floorTilemap != null
                    ? floorTilemap.CellToWorld(new Vector3Int(bossRoomData.WorldOriginTile.x, bossRoomData.WorldOriginTile.y, 0))
                    : new Vector3(bossRoomData.WorldOriginTile.x, bossRoomData.WorldOriginTile.y, 0f);
                activeBossRoomInstance = Object.Instantiate(bossRoomPrefab, bossPos, Quaternion.identity, transform);
                activeBossRoomInstance.name = "BossRoom_Instance";
            }

            if (generatedRooms != null && generatedRooms.Count > 0)
            {
                _tileQuery.MarkOccupied(generatedRooms[0].CenterTile);
            }
            SpawnRoomContents();

            if (GameManager.Instance != null)
            {
                var newBoard = Infrastructure.DungeonBridge.BuildBoardFromDungeon(generatedRooms, 10, floorTilemap);
                GameManager.Instance.InitializeBoard(newBoard);
            }

            PublishDungeonData();

            PositionPlayerAtStartRoom();
            RegisterSceneEntitiesOnBoard();

            CameraBounds cameraBounds = Object.FindAnyObjectByType<CameraBounds>();
            if (cameraBounds != null)
            {
                cameraBounds.AdjustToStartRoom();
            }

            if (debugTilemap != null && debugPathTile != null)
            {
                _tilemapRenderer.DrawDebugConnections(generatedRooms, debugTilemap, debugPathTile);
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
                    GameManager.Instance?.Board?.FindPlayer()?.ResetHealth();
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

                if (GameManager.Instance != null && GameManager.Instance.Board != null)
                {
                    Vector2Int gridPos = GeneratedRooms[0].CenterTile;
                    var charDef = playerMovement.CharacterDefinition;
                    int maxHp = (charDef != null && charDef.HealthTierValues != null && charDef.HealthTierValues.Length > 0) ? charDef.HealthTierValues[0] : 6;
                    int atk = (charDef != null && charDef.AttackTierValues != null && charDef.AttackTierValues.Length > 0) ? charDef.AttackTierValues[0] : 2;
                    Infrastructure.BoardEntityFactory.CreatePlayer(playerMovement.gameObject, gridPos, GameManager.Instance.Board, maxHp, atk, Presentation.Board.EffectsQueueRunner.Instance);
                }
            }
        }

        private void RegisterSceneEntitiesOnBoard()
        {
            if (GameManager.Instance == null || GameManager.Instance.Board == null)
            {
                return;
            }
            Infrastructure.BoardEntityFactory.RegisterSceneEntities(GameManager.Instance.Board, Presentation.Board.EffectsQueueRunner.Instance);
        }

        private void SpawnRoomContents()
        {
            float savedEnemyDensity = enemySpawner.RoomDensity;
            float savedPropDensity = propSpawner.PropDensity;

            // Apply master generalRoomDensity: 0% means 0 items spawn, 100% allows full allocation
            enemySpawner.RoomDensity = savedEnemyDensity * generalRoomDensity;
            propSpawner.PropDensity = savedPropDensity * generalRoomDensity;

            chestSpawner.PrepareDungeonChestPlan(GeneratedRooms);

            for (int r = 0; r < GeneratedRooms.Count; r++)
            {
                GameManager.RoomData room = GeneratedRooms[r];
                for (int s = 0; s < _spawners.Count; s++)
                {
                    _spawners[s].SpawnContent(room, _tileQuery, floorTilemap, transform);
                }
            }

            enemySpawner.RoomDensity = savedEnemyDensity;
            propSpawner.PropDensity = savedPropDensity;

            AssignChildChestRoomKeyholders();
        }

        private void AssignChildChestRoomKeyholders()
        {
            if (GeneratedRooms == null || GeneratedRooms.Count == 0) return;

            var chestRooms = GeneratedRooms.FindAll(r => r.Type == RoomType.Chest);
            if (chestRooms.Count == 0) return;

            var enemySet = new HashSet<EnemyBase>(GetComponentsInChildren<EnemyBase>(true));
            foreach (var eb in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
            {
                if (eb != null) enemySet.Add(eb);
            }
            var allEnemies = new List<EnemyBase>(enemySet);

            // Ensure CurrentRoomIndex is populated from grid position if unassigned (-1)
            for (int eIdx = 0; eIdx < allEnemies.Count; eIdx++)
            {
                EnemyBase e = allEnemies[eIdx];
                if (e != null && e.CurrentRoomIndex < 0)
                {
                    Vector2Int pos = e.GetGridPosition();
                    for (int r = 0; r < GeneratedRooms.Count; r++)
                    {
                        var rm = GeneratedRooms[r];
                        if (pos.x >= rm.WorldOriginTile.x && pos.x < rm.WorldOriginTile.x + rm.Size.x &&
                            pos.y >= rm.WorldOriginTile.y && pos.y < rm.WorldOriginTile.y + rm.Size.y)
                        {
                            e.CurrentRoomIndex = rm.RoomIndex;
                            break;
                        }
                    }
                }
            }

            for (int i = 0; i < chestRooms.Count; i++)
            {
                var chestRoom = chestRooms[i];
                chestRoom.IsLocked = true;

                int parentIdx = chestRoom.ParentRoomIndex;
                var candidates = allEnemies.FindAll(e =>
                    e != null &&
                    !e.name.ToLower().Contains("king") &&
                    e.GetComponent<Keyholder>() == null &&
                    e.GetComponentInParent<Keyholder>() == null &&
                    (parentIdx == -1 || Mathf.Abs(e.CurrentRoomIndex - parentIdx) <= 1));

                if (candidates.Count == 0)
                {
                    candidates = allEnemies.FindAll(e =>
                        e != null &&
                        !e.name.ToLower().Contains("king") &&
                        e.GetComponent<Keyholder>() == null &&
                        e.GetComponentInParent<Keyholder>() == null);
                }

                if (candidates.Count > 0)
                {
                    int pick = Random.Range(0, candidates.Count);
                    EnemyBase selected = candidates[pick];
                    Keyholder kh = selected.GetComponent<Keyholder>();
                    if (kh == null)
                    {
                        kh = selected.gameObject.AddComponent<Keyholder>();
                    }
                    kh.Initialize(chestRoom.RoomIndex);
                }
            }
        }

        [ContextMenu("Clear Dungeon Tiles")]
        public void ClearDungeonTiles()
        {
            if (wallTilemap != null) wallTilemap.ClearAllTiles();
            if (floorTilemap != null) floorTilemap.ClearAllTiles();
            if (debugTilemap != null) debugTilemap.ClearAllTiles();
            if (objectTilemap != null) objectTilemap.ClearAllTiles();

            if (activeBossRoomInstance != null)
            {
                if (Application.isPlaying) Destroy(activeBossRoomInstance.gameObject);
                else DestroyImmediate(activeBossRoomInstance.gameObject);
                activeBossRoomInstance = null;
            }

            if (GameManager.Instance != null && GameManager.Instance.Board != null)
            {
                GameManager.Instance.Board.ClearOccupants();
            }
            if (Presentation.Board.EffectsQueueRunner.Instance != null)
            {
                Presentation.Board.EffectsQueueRunner.Instance.ClearRegistry();
            }

            RegisterDefaultSpawners();
            for (int s = 0; s < _spawners.Count; s++)
            {
                _spawners[s].ClearSpawnedContent();
            }

            var destructibles = GetComponentsInChildren<Presentation.Entities.PropTileObject>(true);
            for (int i = 0; i < destructibles.Length; i++)
            {
                if (destructibles[i] != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(destructibles[i].gameObject);
                    }
                    else
                    {
                        DestroyImmediate(destructibles[i].gameObject);
                    }
                }
            }

            var pillars = GetComponentsInChildren<Presentation.Entities.PillarTileObject>(true);
            for (int i = 0; i < pillars.Length; i++)
            {
                if (pillars[i] != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(pillars[i].gameObject);
                    }
                    else
                    {
                        DestroyImmediate(pillars[i].gameObject);
                    }
                }
            }

            generatedRooms.Clear();
            MarkTilemapsDirtyInEditor();
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
                if (room.SpecialExitDoorTile.HasValue)
                {
                    room.SpecialExitDoorPosition = GetWorldPosition(room.SpecialExitDoorTile);
                }
                if (room.Type == RoomType.Chest && room.EntranceDoorTile.HasValue)
                {
                    room.SpecialEntryDoorPosition = GetWorldPosition(room.EntranceDoorTile);
                }

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

                    parentRoom.HasSpecialChestRoom = true;
                    parentRoom.SpecialChestRoomIndex = room.RoomIndex;

                    if (parentRoom.SpecialExitDoorTile.HasValue)
                    {
                        Vector3? specialDoorWorld = GetWorldPosition(parentRoom.SpecialExitDoorTile);
                        parentRoom.SpecialExitDoorPosition = specialDoorWorld;
                        room.SpecialExitDoorPosition = specialDoorWorld;
                    }
                    else if (parentRoom.SpecialExitDoorPosition.HasValue)
                    {
                        room.SpecialExitDoorPosition = parentRoom.SpecialExitDoorPosition;
                    }

                    GameManager.Instance.DungeonDictionary[parentRoom.RoomIndex] = parentRoom;
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
            return floorTilemap.GetCellCenterWorld(new Vector3Int(tilePosition.x, tilePosition.y, 0));
        }

        private Vector3? GetWorldPosition(Vector2Int? tilePosition)
        {
            if (!tilePosition.HasValue || floorTilemap == null) return null;

            Vector2Int position = tilePosition.Value;
            return floorTilemap.GetCellCenterWorld(new Vector3Int(position.x, position.y, 0));
        }

        private void MarkTilemapsDirtyInEditor()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
                if (wallTilemap != null) EditorUtility.SetDirty(wallTilemap);
                if (floorTilemap != null) EditorUtility.SetDirty(floorTilemap);
                if (objectTilemap != null) EditorUtility.SetDirty(objectTilemap);
                if (debugTilemap != null) EditorUtility.SetDirty(debugTilemap);
            }
#endif
        }

        public void UnlockParentChestDoor(int chestRoomIndex)
        {
            if (generatedRooms == null || objectTilemap == null) return;
            for (int i = 0; i < generatedRooms.Count; i++)
            {
                var room = generatedRooms[i];
                if (room.HasSpecialChestRoom && room.SpecialChestRoomIndex == chestRoomIndex && room.SpecialExitDoorTile.HasValue)
                {
                    TileBase unlockedTile = doorSpawner.SpecialUnlockedDoorTile != null ? doorSpawner.SpecialUnlockedDoorTile : doorSpawner.SpecialExitDoorTile;
                    if (unlockedTile != null)
                    {
                        Vector3Int pos = new Vector3Int(room.SpecialExitDoorTile.Value.x, room.SpecialExitDoorTile.Value.y, 0);
                        objectTilemap.SetTile(pos, unlockedTile);
                    }
                    break;
                }
            }
        }
    }
}
