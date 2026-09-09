using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dungeon.Spawning
{
    [System.Serializable]
    public class PropSpawner : IDungeonSpawner
    {
        [Header("Chest Prefabs")]
        [SerializeField] private GameObject normalChestPrefab;

        [Header("Obstacle & Destructible Prefabs")]
        [SerializeField] private GameObject obstaclePrefab;
        [SerializeField] private GameObject cratePrefab;

        private readonly List<GameObject> _spawnedProps = new List<GameObject>();
        private readonly List<Vector2Int> _validTilesBuffer = new List<Vector2Int>(256);

        public void SpawnContent(
            GameManager.RoomData room,
            RoomTileQuery tileQuery,
            Tilemap floorTilemap,
            Transform parentContainer)
        {
            if (floorTilemap == null) return;

            if (room.Type == RoomType.Chest && normalChestPrefab != null)
            {
                Vector2Int chestTile = room.CenterTile;
                Vector3 worldPos = floorTilemap.GetCellCenterWorld(new Vector3Int(chestTile.x, chestTile.y, 0));
                GameObject chest = Object.Instantiate(normalChestPrefab, worldPos, Quaternion.identity, parentContainer);
                _spawnedProps.Add(chest);
                tileQuery.MarkOccupied(chestTile);
            }

            if (room.Type == RoomType.Normal)
            {
                // Can spawn crates and obstacles on unoccupied tiles
            }
        }

        public void ClearSpawnedContent()
        {
            for (int i = 0; i < _spawnedProps.Count; i++)
            {
                if (_spawnedProps[i] != null)
                {
                    if (Application.isPlaying)
                    {
                        Object.Destroy(_spawnedProps[i]);
                    }
                    else
                    {
                        Object.DestroyImmediate(_spawnedProps[i]);
                    }
                }
            }
            _spawnedProps.Clear();
        }
    }
}
