using UnityEngine;

namespace Dungeon.Spawning
{
    [CreateAssetMenu(fileName = "NewPropSpawnData", menuName = "Dungeon/Prop Spawn Data")]
    public class PropSpawnData : ScriptableObject
    {
        [Header("Prefab & Identity")]
        [SerializeField] private string propName = "Prop";
        [SerializeField] private GameObject prefab;

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
    }
}
