using System.Collections.Generic;
using UnityEngine;

namespace Dungeon.Spawning
{
    [System.Serializable]
    public class PillarConfig
    {
        private GameObject prefab;
        private Vector2Int size;
  
        private bool allowInOrNearHallways = false;

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
    public class PillarSpawnData : PropSpawnData
    {
        private bool allowInOrNearHallways;
        [Header("Pillar Configurations")]
        [SerializeField] private List<PillarConfig> pillars = new List<PillarConfig>();

        [Header("Global Pillar Placement Rules")]
        [SerializeField] private int minDistanceToWalls = 1;
        [SerializeField] private int minDistanceToDoors = 2;

        public IReadOnlyList<PillarConfig> Pillars => pillars;
        public bool AllowInOrNearHallways => allowInOrNearHallways;
        public int MinDistanceToWalls => Mathf.Max(0, minDistanceToWalls);
        public int MinDistanceToDoors => Mathf.Max(0, minDistanceToDoors);

        public void AddPillar(PillarConfig pillar)
        {
            if (pillar != null) pillars.Add(pillar);
        }

        public void ClearPillars()
        {
            pillars.Clear();
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
            if (Prefab != null)
            {
                return new List<PillarConfig>
                {
                    new PillarConfig(Prefab, Size, allowInOrNearHallways)
                };
            }
            return System.Array.Empty<PillarConfig>();
        }
    }
}
