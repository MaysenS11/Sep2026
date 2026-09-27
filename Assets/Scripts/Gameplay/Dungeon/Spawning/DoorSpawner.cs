using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dungeon.Spawning
{
    [System.Serializable]
    public class DoorSpawner : IDungeonSpawner
    {
        [Header("Door Tiles")]
        [SerializeField] private TileBase entranceDoorTile;
        [SerializeField] private TileBase exitDoorTile;
        [SerializeField] private TileBase specialEntranceDoorTile;
        [SerializeField] private TileBase specialExitDoorTile;
        [SerializeField] private TileBase specialLockedDoorTile;
        [SerializeField] private TileBase specialUnlockedDoorTile;
        [SerializeField] private TileBase specialRoomExitDoorTile;

        [Header("Placement Settings")]
        [SerializeField] private float minDoorDistance = 5.0f;

        private Tilemap _objectTilemap;
        private TileBase _floorRuleTile;
        private BossRoom _bossRoom;

        private readonly List<Vector2Int> _validFloorTilesBuffer = new List<Vector2Int>(256);
        private readonly List<Vector2Int> _exitCandidatesBuffer = new List<Vector2Int>(256);
        private readonly List<Vector3Int> _placedDoorPositions = new List<Vector3Int>(64);

        public TileBase EntranceDoorTile { get => entranceDoorTile; set => entranceDoorTile = value; }
        public TileBase ExitDoorTile { get => exitDoorTile; set => exitDoorTile = value; }
        public TileBase SpecialEntranceDoorTile { get => specialEntranceDoorTile; set => specialEntranceDoorTile = value; }
        public TileBase SpecialExitDoorTile { get => specialExitDoorTile; set => specialExitDoorTile = value; }
        public TileBase SpecialLockedDoorTile { get => specialLockedDoorTile; set => specialLockedDoorTile = value; }
        public TileBase SpecialUnlockedDoorTile { get => specialUnlockedDoorTile; set => specialUnlockedDoorTile = value; }
        public TileBase SpecialRoomExitDoorTile { get => specialRoomExitDoorTile; set => specialRoomExitDoorTile = value; }
        public float MinDoorDistance { get => minDoorDistance; set => minDoorDistance = value; }
        public BossRoom BossRoom { get => _bossRoom; set => _bossRoom = value; }

        public void Initialize(Tilemap objectTilemap, TileBase floorRuleTile, BossRoom bossRoom = null)
        {
            _objectTilemap = objectTilemap;
            _floorRuleTile = floorRuleTile;
            _bossRoom = bossRoom;
        }

        public void SpawnContent(
            GameManager.RoomData room,
            RoomTileQuery tileQuery,
            Tilemap floorTilemap,
            Transform parentContainer)
        {
            float minDoorDistanceSqr = minDoorDistance * minDoorDistance;
            tileQuery.GetValidInteriorFloorTiles(room, _validFloorTilesBuffer);

            if (room.Type == RoomType.Boss)
            {
                Vector2Int entrance;
                if (_bossRoom != null)
                {
                    entrance = _bossRoom.GetWorldDoorTile(room.WorldOriginTile);
                }
                else if (_validFloorTilesBuffer.Count > 0)
                {
                    entrance = _validFloorTilesBuffer[Random.Range(0, _validFloorTilesBuffer.Count)];
                }
                else
                {
                    entrance = room.CenterTile;
                }

                room.EntranceDoorTile = entrance;
                room.ExitDoorTile = null;
                tileQuery.MarkOccupied(entrance);
            }
            else if (room.Type == RoomType.Chest)
            {
                Vector2Int entrance = new Vector2Int(room.CenterTile.x, room.CenterTile.y - 4);
                room.EntranceDoorTile = entrance;
                room.ExitDoorTile = null;
                tileQuery.MarkOccupied(entrance);
            }
            else if (room.Type == RoomType.Start)
            {
                // Start room has NO entry door behind the player
                room.EntranceDoorTile = null;

                if (_validFloorTilesBuffer.Count > 0)
                {
                    int maxSqrDist = -1;
                    Vector2Int farthestTile = _validFloorTilesBuffer[0];
                    for (int t = 0; t < _validFloorTilesBuffer.Count; t++)
                    {
                        Vector2Int tile = _validFloorTilesBuffer[t];
                        int sqrDist = (tile - room.CenterTile).sqrMagnitude;
                        if (sqrDist > maxSqrDist)
                        {
                            maxSqrDist = sqrDist;
                            farthestTile = tile;
                        }
                    }
                    room.ExitDoorTile = farthestTile;
                    tileQuery.MarkOccupied(farthestTile);
                }
            }
            else if (_validFloorTilesBuffer.Count < 2)
            {
                if (_validFloorTilesBuffer.Count == 1)
                {
                    Vector2Int entrance = _validFloorTilesBuffer[0];
                    room.EntranceDoorTile = entrance;
                    room.ExitDoorTile = entrance;
                    tileQuery.MarkOccupied(entrance);
                }
            }
            else
            {
                Vector2Int entrance = _validFloorTilesBuffer[Random.Range(0, _validFloorTilesBuffer.Count)];
                room.EntranceDoorTile = entrance;
                tileQuery.MarkOccupied(entrance);

                _exitCandidatesBuffer.Clear();
                for (int t = 0; t < _validFloorTilesBuffer.Count; t++)
                {
                    Vector2Int tile = _validFloorTilesBuffer[t];
                    if ((tile - entrance).sqrMagnitude >= minDoorDistanceSqr)
                    {
                        _exitCandidatesBuffer.Add(tile);
                    }
                }

                Vector2Int chosenExit;
                if (_exitCandidatesBuffer.Count > 0)
                {
                    chosenExit = _exitCandidatesBuffer[Random.Range(0, _exitCandidatesBuffer.Count)];
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
                    chosenExit = farthestTile;
                }

                room.ExitDoorTile = chosenExit;
                tileQuery.MarkOccupied(chosenExit);
            }

            // Spawn SpecialExitDoorTile in parent room leading to child chest room
            if (room.HasSpecialChestRoom && room.SpecialChestRoomIndex >= 0 && room.Type != RoomType.Chest)
            {
                var specialDoorCandidates = new List<Vector2Int>();
                for (int t = 0; t < _validFloorTilesBuffer.Count; t++)
                {
                    Vector2Int tile = _validFloorTilesBuffer[t];
                    if (tileQuery.IsOccupied(tile)) continue;
                    if (room.EntranceDoorTile.HasValue && (tile - room.EntranceDoorTile.Value).sqrMagnitude < 4) continue;
                    if (room.ExitDoorTile.HasValue && (tile - room.ExitDoorTile.Value).sqrMagnitude < 4) continue;
                    specialDoorCandidates.Add(tile);
                }

                Vector2Int chosenSpecialDoor = Vector2Int.zero;
                bool foundSpecial = false;
                if (specialDoorCandidates.Count > 0)
                {
                    chosenSpecialDoor = specialDoorCandidates[Random.Range(0, specialDoorCandidates.Count)];
                    foundSpecial = true;
                }
                else
                {
                    for (int t = 0; t < _validFloorTilesBuffer.Count; t++)
                    {
                        Vector2Int tile = _validFloorTilesBuffer[t];
                        if (!tileQuery.IsOccupied(tile))
                        {
                            chosenSpecialDoor = tile;
                            foundSpecial = true;
                            break;
                        }
                    }
                }

                if (foundSpecial)
                {
                    room.SpecialExitDoorTile = chosenSpecialDoor;
                    tileQuery.MarkOccupied(chosenSpecialDoor);
                }
            }

            if (_objectTilemap == null || floorTilemap == null) return;

            TileBase specialTile = specialExitDoorTile != null ? specialExitDoorTile : (specialEntranceDoorTile != null ? specialEntranceDoorTile : exitDoorTile);

            if (room.Type != RoomType.Start && room.EntranceDoorTile.HasValue)
            {
                TileBase inTile;
                if (room.Type == RoomType.Chest)
                {
                    inTile = specialRoomExitDoorTile != null ? specialRoomExitDoorTile : specialTile;
                }
                else
                {
                    inTile = entranceDoorTile;
                }

                if (inTile != null)
                {
                    Vector3Int pos = new Vector3Int(room.EntranceDoorTile.Value.x, room.EntranceDoorTile.Value.y, 0);
                    _objectTilemap.SetTile(pos, inTile);
                    _placedDoorPositions.Add(pos);
                }
            }

            if (room.ExitDoorTile.HasValue && exitDoorTile != null)
            {
                Vector3Int pos = new Vector3Int(room.ExitDoorTile.Value.x, room.ExitDoorTile.Value.y, 0);
                _objectTilemap.SetTile(pos, exitDoorTile);
                _placedDoorPositions.Add(pos);
            }

            if (room.SpecialExitDoorTile.HasValue)
            {
                TileBase parentSpecialTile = specialTile;
                bool isTargetLocked = true;
                if (room.HasSpecialChestRoom && room.SpecialChestRoomIndex >= 0)
                {
                    if (GameManager.Instance != null && GameManager.Instance.DungeonDictionary != null &&
                        GameManager.Instance.DungeonDictionary.TryGetValue(room.SpecialChestRoomIndex, out var childRoom))
                    {
                        isTargetLocked = childRoom.IsLocked;
                    }
                }

                if (isTargetLocked && specialLockedDoorTile != null)
                {
                    parentSpecialTile = specialLockedDoorTile;
                }
                else if (!isTargetLocked && specialUnlockedDoorTile != null)
                {
                    parentSpecialTile = specialUnlockedDoorTile;
                }

                if (parentSpecialTile != null)
                {
                    Vector3Int pos = new Vector3Int(room.SpecialExitDoorTile.Value.x, room.SpecialExitDoorTile.Value.y, 0);
                    _objectTilemap.SetTile(pos, parentSpecialTile);
                    _placedDoorPositions.Add(pos);
                }
            }
        }

        public void ClearSpawnedContent()
        {
            if (_objectTilemap != null)
            {
                for (int i = 0; i < _placedDoorPositions.Count; i++)
                {
                    _objectTilemap.SetTile(_placedDoorPositions[i], null);
                }
            }
            _placedDoorPositions.Clear();
        }
    }
}
