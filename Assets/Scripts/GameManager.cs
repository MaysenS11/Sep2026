using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum DoorType { EntryDoor, ExitDoor, SpecialExitDoor, SpecialEntryDoor }
public enum RoomType { Start, Normal, Chest, Boss }
public enum RoomShape { Rectangle, LShape, TShape, UShape }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [System.Serializable]
    public class RoomData
    {
        public int RoomIndex;
        public int ParentRoomIndex = -1;
        public RoomType Type;
        public RoomShape Shape;
        public Vector2Int Size;
        public Vector2Int WorldCenterTile;
        public Vector2Int WorldOriginTile;
        public Vector3 WorldCenterPosition;

        // Door positions in world space
        public Vector3? EntryDoorPosition;
        public Vector3? ExitDoorPosition;

        // Chest Room linking
        public bool HasSpecialChestRoom;
        public int SpecialChestRoomIndex = -1;
        public Vector3? SpecialExitDoorPosition;
        public Vector3? SpecialEntryDoorPosition;
    }

    public Dictionary<int, RoomData> DungeonDictionary = new Dictionary<int, RoomData>();
    public int CurrentRoomIndex { get; set; } = 0;

    private Tilemap doorTilemap;
    private TileBase entranceDoorTile;
    private TileBase exitDoorTile;
    private TileBase specialEntranceDoorTile;
    private TileBase specialExitDoorTile;

    // Events
    public static event Action<DoorType, Vector2Int> DoorTriggered;
    public static event Action<RoomData> NewRoomEntered;

    public Tilemap DoorTilemap => doorTilemap;

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

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public static void TriggerDoor(DoorType type, Vector2Int doorTilePos)
    {
        DoorTriggered?.Invoke(type, doorTilePos);
    }

    public static void NotifyNewRoomEntered(RoomData newRoom)
    {
        if (Instance != null)
        {
            Instance.CurrentRoomIndex = newRoom.RoomIndex;
        }
        NewRoomEntered?.Invoke(newRoom);
    }
}