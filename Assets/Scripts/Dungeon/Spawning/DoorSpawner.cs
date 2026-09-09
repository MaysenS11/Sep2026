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

        [Header("Placement Settings")]
        [SerializeField] private float minDoorDistance = 5.0f;

        private Tilemap _objectTilemap;
        private TileBase _fillFloorRuleTile;

        private readonly List<Vector2Int> _validFloorTilesBuffer = new List<Vector2Int>(256);
        private readonly List<Vector2Int> _exitCandidatesBuffer = new List<Vector2Int>(256);
        private readonly List<Vector3Int> _placedDoorPositions = new List<Vector3Int>(64);

        public TileBase EntranceDoorTile { get => entranceDoorTile; set => entranceDoorTile = value; }
        public TileBase ExitDoorTile { get => exitDoorTile; set => exitDoorTile = value; }
        public TileBase SpecialEntranceDoorTile { get => specialEntranceDoorTile; set => specialEntranceDoorTile = value; }
        public TileBase SpecialExitDoorTile { get => specialExitDoorTile; set => specialExitDoorTile = value; }
        public float MinDoorDistance { get => minDoorDistance; set => minDoorDistance = value; }

        public void Initialize(Tilemap objectTilemap, TileBase fillFloorRuleTile)
        {
            _objectTilemap = objectTilemap;
            _fillFloorRuleTile = fillFloorRuleTile;
        }

        public void SpawnContent(
            GameManager.RoomData room,
            RoomTileQuery tileQuery,
            Tilemap floorTilemap,
            Transform parentContainer)
        {
            float minDoorDistanceSqr = minDoorDistance * minDoorDistance;
            tileQuery.GetValidInteriorFloorTiles(room, _validFloorTilesBuffer);

            if (_validFloorTilesBuffer.Count < 2) return;

            if (room.Type == RoomType.Start)
            {
                room.EntranceDoorTile = null;
                Vector2Int exit = _validFloorTilesBuffer[Random.Range(0, _validFloorTilesBuffer.Count)];
                room.ExitDoorTile = exit;
                tileQuery.MarkOccupied(exit);
            }
            else if (room.Type == RoomType.Boss)
            {
                Vector2Int entrance = _validFloorTilesBuffer[Random.Range(0, _validFloorTilesBuffer.Count)];
                room.EntranceDoorTile = entrance;
                room.ExitDoorTile = null;
                tileQuery.MarkOccupied(entrance);
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

            if (_objectTilemap == null || floorTilemap == null) return;

            TileBase inTile = room.Type == RoomType.Chest && specialEntranceDoorTile != null ? specialEntranceDoorTile : entranceDoorTile;
            TileBase outTile = room.ParentRoomIndex != -1 && specialExitDoorTile != null ? specialExitDoorTile : exitDoorTile;

            if (room.EntranceDoorTile.HasValue && inTile != null)
            {
                Vector3Int pos = new Vector3Int(room.EntranceDoorTile.Value.x, room.EntranceDoorTile.Value.y, 0);
                if (_fillFloorRuleTile != null) floorTilemap.SetTile(pos, _fillFloorRuleTile);
                _objectTilemap.SetTile(pos, inTile);
                _placedDoorPositions.Add(pos);
            }

            if (room.ExitDoorTile.HasValue && outTile != null)
            {
                Vector3Int pos = new Vector3Int(room.ExitDoorTile.Value.x, room.ExitDoorTile.Value.y, 0);
                if (_fillFloorRuleTile != null) floorTilemap.SetTile(pos, _fillFloorRuleTile);
                _objectTilemap.SetTile(pos, outTile);
                _placedDoorPositions.Add(pos);
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
