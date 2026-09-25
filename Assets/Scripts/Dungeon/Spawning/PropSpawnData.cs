using UnityEngine;

namespace Dungeon.Spawning
{
    [CreateAssetMenu(fileName = "NewPropSpawnData", menuName = "Dungeon/Prop Spawn Data")]
    public class PropSpawnData : ScriptableObject
    {
        [Header("Prefab & Identity")]
        [SerializeField] private string propName = "Prop";
        [SerializeField] private GameObject prefab;

        [Header("Footprint Dimensions")]
        [Tooltip("Multi-tile footprint dimensions (e.g. 1x1, 1x2, 2x2)")]
        [SerializeField] private Vector2Int size = Vector2Int.one;

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

        public string PropName => propName;
        public GameObject Prefab => prefab;
        public Vector2Int Size => (size.x < 1 || size.y < 1) ? Vector2Int.one : size;
        public float SpawnDensity => spawnDensity;
        public int MinPerRoom => minPerRoom;
        public int MaxPerRoom => maxPerRoom;
        public bool AllowInNormalRooms => allowInNormalRooms;
        public bool AllowInChestRooms => allowInChestRooms;
        public bool AllowInBossRooms => allowInBossRooms;
        public bool AvoidDoorTiles => avoidDoorTiles;
        public bool AvoidRoomCenter => avoidRoomCenter;

        public void SetDensity(float density)
        {
            spawnDensity = Mathf.Clamp01(density);
        }

        public void SetSize(Vector2Int newSize)
        {
            size = new Vector2Int(Mathf.Max(1, newSize.x), Mathf.Max(1, newSize.y));
        }
    }
}
