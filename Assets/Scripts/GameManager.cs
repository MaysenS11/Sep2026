using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Core.Board;
using Core.Occupants;
using Core.Scheduling;
using Infrastructure;
using Presentation.Board;

public enum DoorType { EntryDoor, ExitDoor, SpecialExitDoor, SpecialEntryDoor }
public enum RoomType { Start, Normal, Chest, Boss }
public enum RoomShape { Rectangle, LShape, TShape, UShape }
public enum LRotation { TopRight, TopLeft, BottomRight, BottomLeft }

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.FindAnyObjectByType<GameManager>();
            }
            return _instance;
        }
        private set => _instance = value;
    }

    public BoardTurnCoordinator TurnCoordinator { get; private set; }
    public GameBoard Board => TurnCoordinator?.Board;

    [System.Serializable]
    public class RoomData
    {
        public int RoomIndex;
        public int ParentRoomIndex = -1;
        public Vector2Int MacroPos;
        public RoomType Type;
        public RoomShape Shape;
        public LRotation LRot;
        public Vector2Int Size;
        public Vector2Int WorldOriginTile;

        [SerializeField] private bool hasEntranceDoorTile;
        [SerializeField] private Vector2Int entranceDoorTileValue;
        [SerializeField] private bool hasExitDoorTile;
        [SerializeField] private Vector2Int exitDoorTileValue;

        [SerializeField] private bool hasEntryDoorPosition;
        [SerializeField] private Vector3 entryDoorPositionValue;
        [SerializeField] private bool hasExitDoorPosition;
        [SerializeField] private Vector3 exitDoorPositionValue;

        public bool HasSpecialChestRoom;
        public int SpecialChestRoomIndex = -1;
        [SerializeField] private bool hasSpecialExitDoorPosition;
        [SerializeField] private Vector3 specialExitDoorPositionValue;
        [SerializeField] private bool hasSpecialEntryDoorPosition;
        [SerializeField] private Vector3 specialEntryDoorPositionValue;

        public Vector2Int CenterTile;
        public Vector3 CenterPosition;
        public Vector3 CenterTilePosition;

        public Vector2Int? EntranceDoorTile
        {
            get => hasEntranceDoorTile ? entranceDoorTileValue : (Vector2Int?)null;
            set
            {
                hasEntranceDoorTile = value.HasValue;
                entranceDoorTileValue = value ?? Vector2Int.zero;
            }
        }

        public Vector2Int? ExitDoorTile
        {
            get => hasExitDoorTile ? exitDoorTileValue : (Vector2Int?)null;
            set
            {
                hasExitDoorTile = value.HasValue;
                exitDoorTileValue = value ?? Vector2Int.zero;
            }
        }

        public Vector3? EntryDoorPosition
        {
            get => hasEntryDoorPosition ? entryDoorPositionValue : (Vector3?)null;
            set
            {
                hasEntryDoorPosition = value.HasValue;
                entryDoorPositionValue = value ?? Vector3.zero;
            }
        }

        public Vector3? ExitDoorPosition
        {
            get => hasExitDoorPosition ? exitDoorPositionValue : (Vector3?)null;
            set
            {
                hasExitDoorPosition = value.HasValue;
                exitDoorPositionValue = value ?? Vector3.zero;
            }
        }

        public Vector3? SpecialExitDoorPosition
        {
            get => hasSpecialExitDoorPosition ? specialExitDoorPositionValue : (Vector3?)null;
            set
            {
                hasSpecialExitDoorPosition = value.HasValue;
                specialExitDoorPositionValue = value ?? Vector3.zero;
            }
        }

        public Vector3? SpecialEntryDoorPosition
        {
            get => hasSpecialEntryDoorPosition ? specialEntryDoorPositionValue : (Vector3?)null;
            set
            {
                hasSpecialEntryDoorPosition = value.HasValue;
                specialEntryDoorPositionValue = value ?? Vector3.zero;
            }
        }
    }

    public Dictionary<int, RoomData> DungeonDictionary = new Dictionary<int, RoomData>();
    public int CurrentRoomIndex { get; set; } = 0;

    private Tilemap doorTilemap;
    private TileBase entranceDoorTile;
    private TileBase exitDoorTile;
    private TileBase specialEntranceDoorTile;
    private TileBase specialExitDoorTile;

    public Tilemap DoorTilemap => doorTilemap;

    private readonly List<EnemyBase> activeEnemies = new List<EnemyBase>();
    private Transform playerTransform;
    private Coroutine enemyTurnCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        EffectsQueueRunner runner = Object.FindAnyObjectByType<EffectsQueueRunner>();
        if (runner == null)
        {
            runner = gameObject.AddComponent<EffectsQueueRunner>();
        }

        TurnCoordinator = new BoardTurnCoordinator(new GameBoard(60, 60, new Vector2Int(-30, -30)), new TurnBatchScheduler(), runner);
    }

    public void InitializeBoard(GameBoard newBoard)
    {
        if (TurnCoordinator == null)
        {
            EffectsQueueRunner runner = Object.FindAnyObjectByType<EffectsQueueRunner>();
            if (runner == null) runner = gameObject.AddComponent<EffectsQueueRunner>();
            TurnCoordinator = new BoardTurnCoordinator(newBoard, new TurnBatchScheduler(), runner);
        }
        else
        {
            TurnCoordinator.SetBoard(newBoard);
        }
    }

    private void Start()
    {
        EnemyBase[] existing = Object.FindObjectsByType<EnemyBase>();
        for (int i = 0; i < existing.Length; i++)
        {
            RegisterEnemy(existing[i]);
        }

        if (Board != null)
        {
            BoardEntityFactory.RegisterSceneEntities(Board, TurnCoordinator?.EffectsRunner);
        }
    }

    private void OnEnable()
    {
        EventBus<PlayerActionCompletedEvent>.Subscribe(OnPlayerActionCompleted);
        EventBus<EntityDiedEvent>.Subscribe(OnEntityDied);
        EventBus<EnemyRegisteredEvent>.Subscribe(OnEnemyRegistered);
        EventBus<EnemyUnregisteredEvent>.Subscribe(OnEnemyUnregistered);
    }

    private void OnDisable()
    {
        EventBus<PlayerActionCompletedEvent>.Unsubscribe(OnPlayerActionCompleted);
        EventBus<EntityDiedEvent>.Unsubscribe(OnEntityDied);
        EventBus<EnemyRegisteredEvent>.Unsubscribe(OnEnemyRegistered);
        EventBus<EnemyUnregisteredEvent>.Unsubscribe(OnEnemyUnregistered);
    }

    private void OnEnemyRegistered(EnemyRegisteredEvent evt)
    {
        RegisterEnemy(evt.Enemy);
    }

    private void OnEnemyUnregistered(EnemyUnregisteredEvent evt)
    {
        UnregisterEnemy(evt.Enemy);
    }

    public void RegisterEnemy(EnemyBase enemy)
    {
        if (enemy != null && !activeEnemies.Contains(enemy))
        {
            activeEnemies.Add(enemy);
        }
    }

    public void UnregisterEnemy(EnemyBase enemy)
    {
        if (enemy != null)
        {
            activeEnemies.Remove(enemy);
        }
    }

    public void ClearEnemies()
    {
        activeEnemies.Clear();
        if (Board != null)
        {
            var enemies = new List<EnemyOccupant>(Board.GetOccupantsOfType<EnemyOccupant>());
            for (int i = 0; i < enemies.Count; i++)
            {
                Board.Remove(enemies[i]);
            }
        }
    }

    private void OnEntityDied(EntityDiedEvent evt)
    {
        if (evt.Entity != null && evt.Entity.TryGetComponent<EnemyBase>(out var enemy))
        {
            UnregisterEnemy(enemy);
        }
    }

    private void OnPlayerActionCompleted(PlayerActionCompletedEvent evt)
    {
        if (TurnCoordinator != null && TurnCoordinator.IsTurnInProgress)
        {
            return;
        }

        if (enemyTurnCoroutine != null)
        {
            StopCoroutine(enemyTurnCoroutine);
        }
        enemyTurnCoroutine = StartCoroutine(ExecuteEnemyTurn());
    }

    private System.Collections.IEnumerator ExecuteEnemyTurn()
    {
        if (TurnCoordinator != null)
        {
            yield return StartCoroutine(TurnCoordinator.RunEnemyTurnPhase());
        }
        else
        {
            EventBus<EnemyTurnCompletedEvent>.Raise(new EnemyTurnCompletedEvent());
        }
    }

    public void ConfigureDoorTiles(Tilemap tilemap, TileBase entryTile, TileBase exitTile, TileBase specialEntryTile, TileBase specialExitTile)
    {
        doorTilemap = tilemap;
        entranceDoorTile = entryTile;
        exitDoorTile = exitTile;
        specialEntranceDoorTile = specialEntryTile;
        specialExitDoorTile = specialExitTile;
    }

    public bool TryResolveDoor(Vector3Int cell, out DoorType doorType)
    {
        doorType = default;

        if (doorTilemap != null)
        {
            TileBase tile = doorTilemap.GetTile(cell);
            if (tile != null)
            {
                if (tile == entranceDoorTile)
                {
                    doorType = (DungeonDictionary.TryGetValue(CurrentRoomIndex, out var r) && r.Type == RoomType.Chest)
                        ? DoorType.SpecialEntryDoor
                        : DoorType.EntryDoor;
                    return true;
                }
                if (tile == exitDoorTile)
                {
                    doorType = DoorType.ExitDoor;
                    return true;
                }
                if (tile == specialExitDoorTile)
                {
                    doorType = DoorType.SpecialExitDoor;
                    return true;
                }
                if (tile == specialEntranceDoorTile)
                {
                    doorType = DoorType.SpecialEntryDoor;
                    return true;
                }
            }
        }

        if (DungeonDictionary != null && DungeonDictionary.TryGetValue(CurrentRoomIndex, out var currentRoom))
        {
            Vector2Int grid = new Vector2Int(cell.x, cell.y);
            if (currentRoom.ExitDoorTile.HasValue && currentRoom.ExitDoorTile.Value == grid)
            {
                doorType = (currentRoom.ParentRoomIndex != -1) ? DoorType.SpecialExitDoor : DoorType.ExitDoor;
                return true;
            }
            if (currentRoom.EntranceDoorTile.HasValue && currentRoom.EntranceDoorTile.Value == grid)
            {
                doorType = (currentRoom.Type == RoomType.Chest) ? DoorType.SpecialEntryDoor : DoorType.EntryDoor;
                return true;
            }
        }

        return false;
    }

    public bool TryResolveDoorAtTile(Vector2Int gridPos, out DoorType doorType)
    {
        return TryResolveDoor(new Vector3Int(gridPos.x, gridPos.y, 0), out doorType);
    }

    public static void TriggerDoor(DoorType type, Vector2Int doorTilePos)
    {
        EventBus<DoorTriggeredEvent>.Raise(new DoorTriggeredEvent(type, doorTilePos));
    }

    public static void NotifyNewRoomEntered(RoomData newRoom)
    {
        if (Instance != null)
        {
            Instance.CurrentRoomIndex = newRoom.RoomIndex;
        }
        EventBus<RoomEnteredEvent>.Raise(new RoomEnteredEvent(newRoom));
    }
}