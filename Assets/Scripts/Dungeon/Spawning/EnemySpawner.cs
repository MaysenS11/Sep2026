using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dungeon.Spawning
{
    [System.Serializable]
    public class EnemySpawner : IDungeonSpawner
    {
        [SerializeField] private GameObject pawnPrefab;

        private readonly List<GameObject> _spawnedEnemies = new List<GameObject>();
        private readonly List<Vector2Int> _validTilesBuffer = new List<Vector2Int>(256);

        public GameObject PawnPrefab
        {
            get => pawnPrefab;
            set => pawnPrefab = value;
        }

        public void SpawnContent(
            GameManager.RoomData room,
            RoomTileQuery tileQuery,
            Tilemap floorTilemap,
            Transform parentContainer)
        {
            if (pawnPrefab == null || floorTilemap == null) return;

            // Room 0 (Start room): Spawn single test pawn away from center
            if (room.RoomIndex == 0 && room.Type == RoomType.Start)
            {
                tileQuery.GetValidInteriorFloorTiles(room, _validTilesBuffer, onlyUnoccupied: true);
                if (_validTilesBuffer.Count == 0) return;

                Vector2Int spawnTile = _validTilesBuffer[0];
                for (int i = 0; i < _validTilesBuffer.Count; i++)
                {
                    Vector2Int candidate = _validTilesBuffer[i];
                    if (candidate != room.CenterTile)
                    {
                        spawnTile = candidate;
                        break;
                    }
                }

                SpawnPawnAt(spawnTile, floorTilemap, parentContainer);
                tileQuery.MarkOccupied(spawnTile);
            }
            // Future enemy spawning logic per room type/difficulty can be naturally expanded here
        }

        private void SpawnPawnAt(Vector2Int tile, Tilemap floorTilemap, Transform parentContainer)
        {
            Vector3 worldPos = floorTilemap.GetCellCenterWorld(new Vector3Int(tile.x, tile.y, 0));
            GameObject pawn = Object.Instantiate(pawnPrefab, worldPos, Quaternion.identity, parentContainer);
            _spawnedEnemies.Add(pawn);

            if (pawn.TryGetComponent<EnemyBase>(out var enemy) && GameManager.Instance != null)
            {
                GameManager.Instance.RegisterEnemy(enemy);
            }
        }

        public void ClearSpawnedContent()
        {
            for (int i = 0; i < _spawnedEnemies.Count; i++)
            {
                if (_spawnedEnemies[i] != null)
                {
                    if (Application.isPlaying)
                    {
                        Object.Destroy(_spawnedEnemies[i]);
                    }
                    else
                    {
                        Object.DestroyImmediate(_spawnedEnemies[i]);
                    }
                }
            }
            _spawnedEnemies.Clear();
        }
    }
}
