using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

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
    }

    private void Start()
    {
        EnemyBase[] existing = Object.FindObjectsByType<EnemyBase>();
        for (int i = 0; i < existing.Length; i++)
        {
            RegisterEnemy(existing[i]);
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
        TileReservationSystem.ClearAll();
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
        if (enemyTurnCoroutine != null)
        {
            StopCoroutine(enemyTurnCoroutine);
        }
        enemyTurnCoroutine = StartCoroutine(ExecuteEnemyTurn());
    }

    private System.Collections.IEnumerator ExecuteEnemyTurn()
    {
        // Clean up null or dead references
        activeEnemies.RemoveAll(e => e == null || (e.Stats != null && e.Stats.IsDead));

        if (activeEnemies.Count == 0)
        {
            EventBus<EnemyTurnCompletedEvent>.Raise(new EnemyTurnCompletedEvent());
            yield break;
        }

        if (playerTransform == null)
        {
            PlayerMovement player = Object.FindAnyObjectByType<PlayerMovement>();
            if (player != null) playerTransform = player.transform;
        }

        TileReservationSystem.ClearAll();

        Vector3 playerPos = playerTransform != null ? playerTransform.position : Vector3.zero;
        activeEnemies.Sort((a, b) =>
        {
            int pa = a.GetMovePriority();
            int pb = b.GetMovePriority();
            if (pa != pb) return pa.CompareTo(pb);

            float da = Vector3.Distance(a.transform.position, playerPos);
            float db = Vector3.Distance(b.transform.position, playerPos);
            return da.CompareTo(db);
        });

        int plannedCount = 0;
        int skippedCount = 0;

        for (int i = 0; i < activeEnemies.Count; i++)
        {
            EnemyBase enemy = activeEnemies[i];
            if (enemy == null) continue;

            MoveIntent intent = enemy.PlanMove(playerTransform);

            if (intent.HasMove)
            {
                var reservationPath = new List<Vector2Int>(intent.Path);
                if (intent.IsAttack && intent.PlayerPushTile.HasValue)
                {
                    reservationPath.Add(intent.PlayerPushTile.Value);
                }

                if (TileReservationSystem.TryReservePath(reservationPath, enemy))
                {
                    enemy.PlannedPath = intent.Path;
                    enemy.CurrentIntent = intent;
                    plannedCount++;
                }
                else
                {
                    EventBus<EnemyMoveBlockedEvent>.Raise(new EnemyMoveBlockedEvent(
                        enemy.gameObject,
                        intent.Path.Count > 0 ? intent.Path[intent.Path.Count - 1] : Vector2Int.zero
                    ));
                    enemy.PlannedPath = null;
                    skippedCount++;
                }
            }
            else
            {
                enemy.PlannedPath = null;
                skippedCount++;
            }
        }

        EventBus<EnemyTurnPlannedEvent>.Raise(new EnemyTurnPlannedEvent(plannedCount, skippedCount));

        for (int i = 0; i < activeEnemies.Count; i++)
        {
            if (activeEnemies[i] != null && activeEnemies[i].PlannedPath != null)
            {
                activeEnemies[i].SetKinematic(true);
            }
        }

        int pendingEnemies = 0;

        for (int i = 0; i < activeEnemies.Count; i++)
        {
            EnemyBase enemy = activeEnemies[i];
            if (enemy != null && enemy.PlannedPath != null)
            {
                pendingEnemies++;
                StartCoroutine(RunSingleEnemyMove(enemy, () => pendingEnemies--));
            }
        }

        while (pendingEnemies > 0)
        {
            yield return null;
        }

        for (int i = 0; i < activeEnemies.Count; i++)
        {
            if (activeEnemies[i] != null)
            {
                activeEnemies[i].SetKinematic(false);
                activeEnemies[i].PlannedPath = null;
            }
        }

        TileReservationSystem.ClearAll();
        EventBus<EnemyTurnCompletedEvent>.Raise(new EnemyTurnCompletedEvent());
    }

    private System.Collections.IEnumerator RunSingleEnemyMove(EnemyBase enemy, System.Action onComplete)
    {
        if (enemy != null)
        {
            yield return StartCoroutine(enemy.ExecuteMove());
        }
        onComplete?.Invoke();
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
        if (doorTilemap == null) return false;

        TileBase tile = doorTilemap.GetTile(cell);
        if (tile == entranceDoorTile) doorType = DoorType.EntryDoor;
        else if (tile == exitDoorTile) doorType = DoorType.ExitDoor;
        else if (tile == specialExitDoorTile) doorType = DoorType.SpecialExitDoor;
        else if (tile == specialEntranceDoorTile) doorType = DoorType.SpecialEntryDoor;
        else return false;

        return true;
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