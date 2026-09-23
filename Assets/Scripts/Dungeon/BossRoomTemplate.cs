using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dungeon
{
    public class BossRoomTemplate : MonoBehaviour
    {
        [Header("Room Layout")]
        [SerializeField] private Vector2Int roomSize = new Vector2Int(18, 18);

        [Header("Markers")]
        [Tooltip("Transform marker indicating the exact entrance door position.")]
        [SerializeField] private Transform doorMarker;

        [Tooltip("Transform marker indicating where the King boss spawns.")]
        [SerializeField] private Transform kingSpawnPoint;

        [Header("Boss Prefab")]
        [SerializeField] private GameObject kingPrefab;

        [Header("Authored Layer Tilemaps (Optional)")]
        [Tooltip("If assigned/painted in the prefab, these tiles will be copied to the dungeon's main tilemaps at generation time.")]
        [SerializeField] private Tilemap fillFloorTilemap;
        [SerializeField] private Tilemap borderFloorTilemap;
        [SerializeField] private Tilemap wallTilemap;
        [SerializeField] private Tilemap roofTilemap;

        public Vector2Int RoomSize => roomSize;
        public Transform DoorMarker => doorMarker;
        public Transform KingSpawnPoint => kingSpawnPoint;
        public GameObject KingPrefab { get => kingPrefab; set => kingPrefab = value; }
        public Tilemap FillFloorTilemap => fillFloorTilemap;
        public Tilemap BorderFloorTilemap => borderFloorTilemap;
        public Tilemap WallTilemap => wallTilemap;
        public Tilemap RoofTilemap => roofTilemap;

        public bool HasAuthoredTiles()
        {
            return (fillFloorTilemap != null && fillFloorTilemap.GetUsedTilesCount() > 0)
                || (borderFloorTilemap != null && borderFloorTilemap.GetUsedTilesCount() > 0)
                || (wallTilemap != null && wallTilemap.GetUsedTilesCount() > 0)
                || (roofTilemap != null && roofTilemap.GetUsedTilesCount() > 0);
        }

        public void CopyTilesTo(
            Vector2Int worldOriginTile,
            Tilemap targetFillFloor,
            Tilemap targetBorderFloor,
            Tilemap targetWall,
            Tilemap targetRoof)
        {
            CopyLayer(fillFloorTilemap, targetFillFloor, worldOriginTile);
            CopyLayer(borderFloorTilemap, targetBorderFloor, worldOriginTile);
            CopyLayer(wallTilemap, targetWall, worldOriginTile);
            CopyLayer(roofTilemap, targetRoof, worldOriginTile);
        }

        private void CopyLayer(Tilemap source, Tilemap target, Vector2Int worldOriginTile)
        {
            if (source == null || target == null) return;

            BoundsInt bounds = source.cellBounds;
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector3Int srcPos = new Vector3Int(x, y, 0);
                    TileBase tile = source.GetTile(srcPos);
                    if (tile != null)
                    {
                        Vector3Int dstPos = new Vector3Int(worldOriginTile.x + x, worldOriginTile.y + y, 0);
                        target.SetTile(dstPos, tile);
                    }
                }
            }
        }

        public Vector2Int GetLocalDoorTile()
        {
            if (doorMarker != null)
            {
                return new Vector2Int(
                    Mathf.RoundToInt(doorMarker.localPosition.x),
                    Mathf.RoundToInt(doorMarker.localPosition.y)
                );
            }

            return new Vector2Int(roomSize.x / 2, 1);
        }

        public Vector2Int GetWorldDoorTile(Vector2Int worldOriginTile)
        {
            Vector2Int local = GetLocalDoorTile();
            return worldOriginTile + local;
        }

        public GameObject SpawnAndInitializeBoss(GameManager.RoomData room, Transform parentContainer)
        {
            Vector3 spawnPos = kingSpawnPoint != null ? kingSpawnPoint.position : transform.position;

            GameObject bossObj = null;
            if (kingPrefab != null)
            {
                bossObj = Instantiate(kingPrefab, spawnPos, Quaternion.identity, parentContainer);
            }

            var allEnemies = GetComponentsInChildren<EnemyBase>(true);
            for (int i = 0; i < allEnemies.Length; i++)
            {
                allEnemies[i].CurrentRoomIndex = room.RoomIndex;
            }

            if (bossObj != null && bossObj.TryGetComponent<EnemyBase>(out var bossEnemy))
            {
                bossEnemy.CurrentRoomIndex = room.RoomIndex;
            }

            return bossObj;
        }
    }
}
