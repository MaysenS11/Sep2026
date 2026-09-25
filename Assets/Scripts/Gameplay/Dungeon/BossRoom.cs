using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dungeon
{
    public class BossRoom : MonoBehaviour
    {
        [Header("Room Layout")]
        [SerializeField] private Vector2Int roomSize = new Vector2Int(18, 18);

        [Header("Markers")]
        [Tooltip("Transform marker indicating the exact entrance door position.")]
        [SerializeField] private Transform doorMarker;

        [Header("Authored Layer Tilemaps")]
        [Tooltip("If assigned/painted in the prefab, these tiles will be copied to the dungeon's main tilemaps at generation time.")]
        [SerializeField] private Tilemap fillFloorTilemap;
        [SerializeField] private Tilemap borderFloorTilemap;
        [SerializeField] private Tilemap wallTilemap;
        [SerializeField] private Tilemap roofTilemap;

        public Vector2Int RoomSize => roomSize;
        public Transform DoorMarker => doorMarker;

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
                        target.SetTile(new Vector3Int(worldOriginTile.x + x, worldOriginTile.y + y, 0), tile);
                    }
                }
            }
        }

        public Vector2Int GetWorldDoorTile(Vector2Int worldOriginTile)
        {
            Vector2Int local = doorMarker != null
                ? new Vector2Int(Mathf.RoundToInt(doorMarker.localPosition.x), Mathf.RoundToInt(doorMarker.localPosition.y))
                : new Vector2Int(roomSize.x / 2, 1);

            return worldOriginTile + local;
        }

        [Header("Enemy Data Assets")]
        [SerializeField] private EnemyData pawnData;
        [SerializeField] private EnemyData knightData;
        [SerializeField] private EnemyData bishopData;
        [SerializeField] private EnemyData rookData;
        [SerializeField] private EnemyData queenData;

        [Header("Spawn Amounts")]
        [Min(0)] [SerializeField] private int pawnAmount = 0;
        [Min(0)] [SerializeField] private int knightAmount = 0;
        [Min(0)] [SerializeField] private int bishopAmount = 0;
        [Min(0)] [SerializeField] private int rookAmount = 0;
        [Min(0)] [SerializeField] private int queenAmount = 0;

        [System.Serializable]
        public struct PieceSpawnBand
        {
            [Tooltip("Target distance in tiles from the boss (King).")]
            [Min(0f)] public float distanceToBoss;

            [Tooltip("Thickness / spread (+/- tiles) around the target distance.")]
            [Min(0f)] public float bandWidth;

            public PieceSpawnBand(float distance, float width)
            {
                distanceToBoss = distance;
                bandWidth = width;
            }

            public float MinDistance => Mathf.Max(0f, distanceToBoss - (bandWidth * 0.5f));
            public float MaxDistance => distanceToBoss + (bandWidth * 0.5f);
        }

        [Header("Formation Bands (Distance & Band Width from Boss)")]
        [Tooltip("Queen: closest to the King.")]
        [SerializeField] private PieceSpawnBand queenBand = new PieceSpawnBand(2f, 1.5f);

        [Tooltip("Rooks: behind knights and bishops.")]
        [SerializeField] private PieceSpawnBand rookBand = new PieceSpawnBand(4f, 2f);

        [Tooltip("Knights & Bishops: around the same mid-rank height.")]
        [SerializeField] private PieceSpawnBand knightAndBishopBand = new PieceSpawnBand(6f, 2f);

        [Tooltip("Pawns: in front of all others, closest to the player / entrance.")]
        [SerializeField] private PieceSpawnBand pawnBand = new PieceSpawnBand(8.5f, 2.5f);

        [Header("Boss Damage On Enemy Death")]
        [Min(0)] [SerializeField] private int pawnBossDamage = 1;
        [Min(0)] [SerializeField] private int knightBossDamage = 2;
        [Min(0)] [SerializeField] private int bishopBossDamage = 2;
        [Min(0)] [SerializeField] private int rookBossDamage = 3;
        [Min(0)] [SerializeField] private int queenBossDamage = 4;

        public EnemyData PawnData => pawnData;
        public EnemyData KnightData => knightData;
        public EnemyData BishopData => bishopData;
        public EnemyData RookData => rookData;
        public EnemyData QueenData => queenData;

        public int PawnAmount { get => pawnAmount; set => pawnAmount = Mathf.Max(0, value); }
        public int KnightAmount { get => knightAmount; set => knightAmount = Mathf.Max(0, value); }
        public int BishopAmount { get => bishopAmount; set => bishopAmount = Mathf.Max(0, value); }
        public int RookAmount { get => rookAmount; set => rookAmount = Mathf.Max(0, value); }
        public int QueenAmount { get => queenAmount; set => queenAmount = Mathf.Max(0, value); }

        public PieceSpawnBand QueenBand { get => queenBand; set => queenBand = value; }
        public PieceSpawnBand RookBand { get => rookBand; set => rookBand = value; }
        public PieceSpawnBand KnightAndBishopBand { get => knightAndBishopBand; set => knightAndBishopBand = value; }
        public PieceSpawnBand PawnBand { get => pawnBand; set => pawnBand = value; }

        public PieceSpawnBand GetBandForEnemy(EnemyData data)
        {
            if (data == null) return pawnBand;

            if (queenData != null && data == queenData) return queenBand;
            if (rookData != null && data == rookData) return rookBand;
            if (knightData != null && data == knightData) return knightAndBishopBand;
            if (bishopData != null && data == bishopData) return knightAndBishopBand;
            if (pawnData != null && data == pawnData) return pawnBand;

            switch (data.Archetype)
            {
                case Core.Occupants.EnemyArchetype.Queen:
                    return queenBand;
                case Core.Occupants.EnemyArchetype.Rook:
                    return rookBand;
                case Core.Occupants.EnemyArchetype.Knight:
                case Core.Occupants.EnemyArchetype.Bishop:
                    return knightAndBishopBand;
                case Core.Occupants.EnemyArchetype.Pawn:
                default:
                    return pawnBand;
            }
        }

        public int PawnBossDamage { get => pawnBossDamage; set => pawnBossDamage = Mathf.Max(0, value); }
        public int KnightBossDamage { get => knightBossDamage; set => knightBossDamage = Mathf.Max(0, value); }
        public int BishopBossDamage { get => bishopBossDamage; set => bishopBossDamage = Mathf.Max(0, value); }
        public int RookBossDamage { get => rookBossDamage; set => rookBossDamage = Mathf.Max(0, value); }
        public int QueenBossDamage { get => queenBossDamage; set => queenBossDamage = Mathf.Max(0, value); }

        public System.Collections.Generic.List<EnemyData> GetEnemiesToSpawn()
        {
            var list = new System.Collections.Generic.List<EnemyData>();
            AddEnemyCopies(list, pawnData, pawnAmount);
            AddEnemyCopies(list, knightData, knightAmount);
            AddEnemyCopies(list, bishopData, bishopAmount);
            AddEnemyCopies(list, rookData, rookAmount);
            AddEnemyCopies(list, queenData, queenAmount);
            return list;
        }

        private static void AddEnemyCopies(System.Collections.Generic.List<EnemyData> list, EnemyData data, int count)
        {
            if (data == null || count <= 0) return;
            for (int i = 0; i < count; i++)
            {
                list.Add(data);
            }
        }

        public int GetDamageForEnemy(EnemyData data)
        {
            if (data == null) return 1;

            if (pawnData != null && data == pawnData) return pawnBossDamage;
            if (knightData != null && data == knightData) return knightBossDamage;
            if (bishopData != null && data == bishopData) return bishopBossDamage;
            if (rookData != null && data == rookData) return rookBossDamage;
            if (queenData != null && data == queenData) return queenBossDamage;

            switch (data.Archetype)
            {
                case Core.Occupants.EnemyArchetype.Pawn:
                    return pawnBossDamage;
                case Core.Occupants.EnemyArchetype.Knight:
                    return knightBossDamage;
                case Core.Occupants.EnemyArchetype.Bishop:
                    return bishopBossDamage;
                case Core.Occupants.EnemyArchetype.Rook:
                    return rookBossDamage;
                case Core.Occupants.EnemyArchetype.Queen:
                    return queenBossDamage;
                default:
                    return 1;
            }
        }

        private bool _isRegistered = false;

        public void ScanAndRegisterBossEntities(Vector2Int worldOriginTile, Core.Board.GameBoard board, Presentation.Board.EffectsQueueRunner effectsRunner, int roomIndex)
        {
            if (_isRegistered || board == null) return;
            _isRegistered = true;

            var enemies = GetComponentsInChildren<EnemyBase>(true);
            for (int i = 0; i < enemies.Length; i++)
            {
                var enemy = enemies[i];
                if (enemy == null) continue;

                if (enemy.name.ToLower().Contains("king"))
                {
                    continue;
                }

                if (!enemy.gameObject.activeSelf)
                {
                    enemy.gameObject.SetActive(true);
                }

                enemy.CurrentRoomIndex = roomIndex;

                Vector2Int tile = worldOriginTile + new Vector2Int(
                    Mathf.RoundToInt(enemy.transform.localPosition.x),
                    Mathf.RoundToInt(enemy.transform.localPosition.y)
                );

                Core.Occupants.EnemyArchetype archetype = Core.Occupants.EnemyArchetype.Pawn;
                if (enemy.Data != null)
                {
                    archetype = enemy.Data.Archetype;
                }
                else
                {
                    string lower = enemy.name.ToLower();
                    if (lower.Contains("knight")) archetype = Core.Occupants.EnemyArchetype.Knight;
                    else if (lower.Contains("bishop")) archetype = Core.Occupants.EnemyArchetype.Bishop;
                    else if (lower.Contains("rook")) archetype = Core.Occupants.EnemyArchetype.Rook;
                    else if (lower.Contains("queen")) archetype = Core.Occupants.EnemyArchetype.Queen;
                }

                Infrastructure.BoardEntityFactory.CreateEnemy(enemy.gameObject, tile, board, archetype, enemy.Data, effectsRunner);

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.RegisterEnemy(enemy);
                }
                EventBus<EnemyRegisteredEvent>.Raise(new EnemyRegisteredEvent(enemy));
            }
        }
    }
}
