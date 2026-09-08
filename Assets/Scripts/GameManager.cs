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
        
        public Vector2Int? EntranceDoorTile;
        public Vector2Int? ExitDoorTile;

        public Vector3? EntryDoorPosition;
        public Vector3? ExitDoorPosition;

        public bool HasSpecialChestRoom;
        public int SpecialChestRoomIndex = -1;
        public Vector3? SpecialExitDoorPosition;
        public Vector3? SpecialEntryDoorPosition;

        public Vector2Int CenterTile;
        public Vector3 CenterPosition;
        public Vector3 CenterTilePosition;
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
        // Find any existing enemies in scene that loaded before GameManager
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
    }

    private void OnDisable()
    {
        EventBus<PlayerActionCompletedEvent>.Unsubscribe(OnPlayerActionCompleted);
        EventBus<EntityDiedEvent>.Unsubscribe(OnEntityDied);
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
        EnemyBase.ClearReservations();
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
        enemyTurnCoroutine = StartCoroutine(ExecuteSimultaneousEnemyTurn());
    }

    private System.Collections.IEnumerator ExecuteSimultaneousEnemyTurn()
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

        EnemyBase.ClearReservations();

        // Launch all enemy turn coroutines simultaneously
        int pendingEnemies = activeEnemies.Count;

        for (int i = 0; i < activeEnemies.Count; i++)
        {
            EnemyBase enemy = activeEnemies[i];
            StartCoroutine(RunSingleEnemyTurn(enemy, playerTransform, () => pendingEnemies--));
        }

        // Wait until all enemies complete their turn
        while (pendingEnemies > 0)
        {
            yield return null;
        }

        EnemyBase.ClearReservations();
        EventBus<EnemyTurnCompletedEvent>.Raise(new EnemyTurnCompletedEvent());
    }

    private System.Collections.IEnumerator RunSingleEnemyTurn(EnemyBase enemy, Transform targetPlayer, System.Action onComplete)
    {
        if (enemy != null)
        {
            yield return StartCoroutine(enemy.ExecuteTurnCoroutine(targetPlayer));
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