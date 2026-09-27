using System.Collections.Generic;
using UnityEngine;

namespace Dungeon.Spawning
{
    [System.Serializable]
    public class PillarConfig
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private Vector2Int size = new Vector2Int(2, 2);
        [SerializeField] private bool allowInOrNearHallways = false;

        public GameObject Prefab => prefab;
        public Vector2Int Size => (size.x < 1 || size.y < 1) ? Vector2Int.one : size;
        public bool AllowInOrNearHallways => allowInOrNearHallways;

        public PillarConfig() { }

        public PillarConfig(GameObject prefab, Vector2Int size, bool allowInOrNearHallways = false)
        {
            this.prefab = prefab;
            this.size = size;
            this.allowInOrNearHallways = allowInOrNearHallways;
        }

        public void SetPrefab(GameObject go) => prefab = go;
        public void SetSize(Vector2Int newSize) => size = new Vector2Int(Mathf.Max(1, newSize.x), Mathf.Max(1, newSize.y));
        public void SetAllowInOrNearHallways(bool allow) => allowInOrNearHallways = allow;
    }

    [CreateAssetMenu(fileName = "NewPillarSpawnData", menuName = "Dungeon/Pillar Spawn Data")]
    public class PillarSpawnData : ScriptableObject
    {
        [Header("Pillar Configurations")]
        [SerializeField] private List<PillarConfig> pillars = new List<PillarConfig>();

        [Header("Spawn Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float spawnDensity = 0.05f;
        [SerializeField] private int minPerRoom = 0;
        [SerializeField] private int maxPerRoom = 6;

        [Header("Placement Rules")]
        [SerializeField] private bool allowInNormalRooms = true;
        [SerializeField] private bool allowInChestRooms = false;
        [SerializeField] private bool allowInBossRooms = false;
        [SerializeField] private bool avoidDoorTiles = true;
        [SerializeField] private bool avoidRoomCenter = true;

        [Header("Pillar Placement Rules")]
        [SerializeField] private int minDistanceToWalls = 1;
        [SerializeField] private int minDistanceToDoors = 2;
        [SerializeField] private bool allowInOrNearHallways = false;

        public IReadOnlyList<PillarConfig> Pillars => pillars;
        public float SpawnDensity => spawnDensity;
        public int MinPerRoom => minPerRoom;
        public int MaxPerRoom => maxPerRoom;
        public bool AllowInNormalRooms => allowInNormalRooms;
        public bool AllowInChestRooms => allowInChestRooms;
        public bool AllowInBossRooms => allowInBossRooms;
        public bool AvoidDoorTiles => avoidDoorTiles;
        public bool AvoidRoomCenter => avoidRoomCenter;
        public int MinDistanceToWalls => Mathf.Max(0, minDistanceToWalls);
        public int MinDistanceToDoors => Mathf.Max(0, minDistanceToDoors);
        public bool AllowInOrNearHallways => allowInOrNearHallways;

        public void AddPillar(PillarConfig pillar)
        {
            if (pillar != null) pillars.Add(pillar);
        }

        public void ClearPillars()
        {
            pillars.Clear();
        }

        public void SetDensity(float density)
        {
            spawnDensity = Mathf.Clamp01(density);
        }

        public void SetAllowInOrNearHallways(bool allow)
        {
            allowInOrNearHallways = allow;
        }

        public void SetMinDistanceToWalls(int distance)
        {
            minDistanceToWalls = Mathf.Max(0, distance);
        }

        public void SetMinDistanceToDoors(int distance)
        {
            minDistanceToDoors = Mathf.Max(0, distance);
        }

        public IReadOnlyList<PillarConfig> GetConfiguredPillars()
        {
            if (pillars != null && pillars.Count > 0)
            {
                return pillars;
            }
            return System.Array.Empty<PillarConfig>();
        }
    }
}
