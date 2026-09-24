using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dungeon.Spawning
{
    [System.Serializable]
    public class EnemySpawner : IDungeonSpawner
    {
        [Header("Spawn Settings")]
        [Tooltip("Fraction of the room spawn budget allocated to enemies (0 = no enemies, 1 = 100% enemies)")]
        [Range(0f, 1f)]
        [SerializeField] private float roomDensity = 0.5f;

        [Header("Per-Enemy Type Distribution Weights (0.0 to 1.0)")]
        [Range(0f, 1f)] [SerializeField] private float pawnWeight = 1.0f;
        [Range(0f, 1f)] [SerializeField] private float knightWeight = 1.0f;
        [Range(0f, 1f)] [SerializeField] private float rookWeight = 1.0f;
        [Range(0f, 1f)] [SerializeField] private float bishopWeight = 1.0f;

        [Header("Enemy Prefabs")]
        [SerializeField] private GameObject pawnPrefab;
        [SerializeField] private GameObject knightPrefab;
        [SerializeField] private GameObject rookPrefab;
        [SerializeField] private GameObject bishopPrefab;

        private readonly List<GameObject> _spawnedEnemies = new List<GameObject>();
        private readonly List<Vector2Int> _validTilesBuffer = new List<Vector2Int>(256);

        public float RoomDensity
        {
            get => roomDensity;
            set => roomDensity = Mathf.Clamp01(value);
        }

        public float PawnWeight { get => pawnWeight; set => pawnWeight = Mathf.Clamp01(value); }
        public float KnightWeight { get => knightWeight; set => knightWeight = Mathf.Clamp01(value); }
        public float RookWeight { get => rookWeight; set => rookWeight = Mathf.Clamp01(value); }
        public float BishopWeight { get => bishopWeight; set => bishopWeight = Mathf.Clamp01(value); }

        public GameObject PawnPrefab
        {
            get => pawnPrefab;
            set => pawnPrefab = value;
        }

        public GameObject KnightPrefab
        {
            get => knightPrefab;
            set => knightPrefab = value;
        }

        public GameObject RookPrefab
        {
            get => rookPrefab;
            set => rookPrefab = value;
        }

        public GameObject BishopPrefab
        {
            get => bishopPrefab;
            set => bishopPrefab = value;
        }

        public void SpawnContent(
            GameManager.RoomData room,
            RoomTileQuery tileQuery,
            Tilemap floorTilemap,
            Transform parentContainer)
        {
            if (floorTilemap == null) return;
            if (room.Type == RoomType.Chest) return;

            if (room.Type == RoomType.Boss)
            {
                SpawnBossRoomContent(room, tileQuery, floorTilemap, parentContainer);
                return;
            }

            tileQuery.GetValidInteriorFloorTiles(room, _validTilesBuffer, onlyUnoccupied: true);

            if (room.RoomIndex == 0)
            {
                _validTilesBuffer.RemoveAll(t =>
                    Mathf.Abs(t.x - room.CenterTile.x) <= 1 &&
                    Mathf.Abs(t.y - room.CenterTile.y) <= 1);
            }

            if (_validTilesBuffer.Count == 0) return;

            // 0% room density means explicitly 0 enemies spawned
            if (roomDensity <= 0f) return;

            int targetCount = Mathf.RoundToInt(roomDensity * _validTilesBuffer.Count);
            if (targetCount <= 0 && roomDensity > 0f) targetCount = 1;

            List<(GameObject prefab, float weight)> candidates = BuildWeightedCandidates(room);
            if (candidates.Count == 0) return;

            float totalWeight = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                totalWeight += candidates[i].weight;
            }

            if (totalWeight <= 0f) return;

            int toSpawn = Mathf.Min(targetCount, _validTilesBuffer.Count);
            for (int i = 0; i < toSpawn; i++)
            {
                if (_validTilesBuffer.Count == 0) break;

                GameObject chosenPrefab = PickPrefab(candidates, totalWeight);
                if (chosenPrefab == null) continue;

                int pickIndex = Random.Range(0, _validTilesBuffer.Count);
                Vector2Int spawnTile = _validTilesBuffer[pickIndex];
                _validTilesBuffer.RemoveAt(pickIndex);

                SpawnEnemyAt(chosenPrefab, spawnTile, floorTilemap, parentContainer, room.RoomIndex);
                tileQuery.MarkOccupied(spawnTile);
            }
        }

        private float GetConfiguredWeight(EnemyData enemyData)
        {
            if (enemyData == null) return 1f;
            switch (enemyData.MovementPattern)
            {
                case EnemyMovementPattern.SingleMove:
                    return pawnWeight;
                case EnemyMovementPattern.KnightMove:
                    return knightWeight;
                case EnemyMovementPattern.RookMove:
                    return rookWeight;
                case EnemyMovementPattern.BishopMove:
                    return bishopWeight;
                default:
                    return 1f;
            }
        }

        private float GetConfiguredWeight(string enemyName)
        {
            if (string.IsNullOrEmpty(enemyName)) return 1f;
            string lower = enemyName.ToLowerInvariant();
            if (lower.Contains("pawn")) return pawnWeight;
            if (lower.Contains("knight")) return knightWeight;
            if (lower.Contains("rook")) return rookWeight;
            if (lower.Contains("bishop")) return bishopWeight;
            return 1f;
        }

        private List<(GameObject prefab, float weight)> BuildWeightedCandidates(GameManager.RoomData room)
        {
            List<(GameObject prefab, float weight)> candidates = new List<(GameObject prefab, float weight)>();
            GameObject[] availablePrefabs = { pawnPrefab, knightPrefab, rookPrefab, bishopPrefab };

            int totalRooms = 1;
            if (GameManager.Instance != null && GameManager.Instance.DungeonDictionary != null && GameManager.Instance.DungeonDictionary.Count > 0)
            {
                totalRooms = GameManager.Instance.DungeonDictionary.Count;
            }

            float roomProgress = Mathf.Clamp01((float)room.RoomIndex / Mathf.Max(1, totalRooms - 1));

            for (int i = 0; i < availablePrefabs.Length; i++)
            {
                GameObject prefab = availablePrefabs[i];
                if (prefab == null) continue;

                if (prefab.TryGetComponent<EnemyBase>(out var enemy) && enemy.Data != null)
                {
                    float configuredWeight = GetConfiguredWeight(enemy.Data);
                    float popPct = enemy.Data.PopulationPercentage * configuredWeight;
                    if (popPct <= 0f) continue;

                    float priorityNorm = Mathf.Clamp01(enemy.Data.MovePriority / 5f);
                    float difficultyFactor = Mathf.Max(0.01f, 1f - Mathf.Abs(roomProgress - priorityNorm));
                    float weight = popPct * difficultyFactor;

                    candidates.Add((prefab, weight));
                }
                else
                {
                    float configuredWeight = GetConfiguredWeight(prefab.name);
                    if (configuredWeight > 0f)
                    {
                        candidates.Add((prefab, configuredWeight));
                    }
                }
            }

            return candidates;
        }

        private GameObject PickPrefab(List<(GameObject prefab, float weight)> candidates, float totalWeight)
        {
            float roll = Random.Range(0f, totalWeight);
            float accum = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                accum += candidates[i].weight;
                if (roll <= accum)
                {
                    return candidates[i].prefab;
                }
            }
            return candidates[candidates.Count - 1].prefab;
        }

        private void SpawnBossRoomContent(
            GameManager.RoomData room,
            RoomTileQuery tileQuery,
            Tilemap floorTilemap,
            Transform parentContainer)
        {
            var dm = DungeonManager.Instance != null ? DungeonManager.Instance : Object.FindAnyObjectByType<DungeonManager>();
            if (dm == null || dm.BossRoomPrefab == null) return;

            BossRoom bossRoom = dm.BossRoomPrefab;

            GameObject bossObj = bossRoom.SpawnBossAtRoom(room.WorldOriginTile, floorTilemap, parentContainer, room.RoomIndex);
            if (bossObj != null)
            {
                _spawnedEnemies.Add(bossObj);
                RegisterUndoInEditor(bossObj, "King Boss");

                if (GameManager.Instance != null && GameManager.Instance.Board != null)
                {
                    Vector2Int bTile = bossRoom.GetWorldBossTile(room.WorldOriginTile);
                    EnemyData bossData = bossObj.TryGetComponent<EnemyBase>(out var eb) ? eb.Data : null;
                    Infrastructure.BoardEntityFactory.CreateEnemy(bossObj, bTile, GameManager.Instance.Board, Core.Occupants.EnemyArchetype.Queen, bossData, Presentation.Board.EffectsQueueRunner.Instance);
                }
            }

            Vector2Int bossTile = bossRoom.GetWorldBossTile(room.WorldOriginTile);
            tileQuery.MarkOccupied(bossTile);

            List<EnemyData> enemiesToSpawn = bossRoom.GetEnemiesToSpawn();
            if (enemiesToSpawn == null || enemiesToSpawn.Count == 0) return;

            enemiesToSpawn.Sort((a, b) =>
            {
                int pa = a != null ? a.MovePriority : 0;
                int pb = b != null ? b.MovePriority : 0;
                return pb.CompareTo(pa);
            });

            tileQuery.GetValidInteriorFloorTiles(room, _validTilesBuffer, onlyUnoccupied: true);

            _validTilesBuffer.Remove(bossTile);
            if (room.EntranceDoorTile.HasValue)
            {
                Vector2Int door = room.EntranceDoorTile.Value;
                _validTilesBuffer.Remove(door);
                _validTilesBuffer.Remove(new Vector2Int(door.x, door.y - 1));
                _validTilesBuffer.Remove(new Vector2Int(door.x, door.y + 1));
            }

            Vector2Int doorTile = room.EntranceDoorTile ?? bossRoom.GetWorldDoorTile(room.WorldOriginTile);
            Vector2 forwardDir = ((Vector2)(bossTile - doorTile)).normalized;
            bool hasForwardAxis = forwardDir.sqrMagnitude > 0.001f;

            var availableTiles = new List<Vector2Int>(_validTilesBuffer);

            var enemyGroups = new List<List<EnemyData>>();
            var handled = new HashSet<EnemyData>();
            for (int i = 0; i < enemiesToSpawn.Count; i++)
            {
                EnemyData data = enemiesToSpawn[i];
                if (data == null || handled.Contains(data)) continue;

                var group = new List<EnemyData>();
                for (int j = 0; j < enemiesToSpawn.Count; j++)
                {
                    if (enemiesToSpawn[j] == data)
                    {
                        group.Add(data);
                    }
                }
                handled.Add(data);
                enemyGroups.Add(group);
            }

            enemyGroups.Sort((g1, g2) =>
            {
                BossRoom.PieceSpawnBand b1 = bossRoom.GetBandForEnemy(g1[0]);
                BossRoom.PieceSpawnBand b2 = bossRoom.GetBandForEnemy(g2[0]);
                return b1.distanceToBoss.CompareTo(b2.distanceToBoss);
            });

            for (int g = 0; g < enemyGroups.Count; g++)
            {
                List<EnemyData> group = enemyGroups[g];
                EnemyData groupData = group[0];
                BossRoom.PieceSpawnBand band = bossRoom.GetBandForEnemy(groupData);

                float minDist = band.MinDistance;
                float maxDist = band.MaxDistance;
                float minDistSqr = minDist * minDist;
                float maxDistSqr = maxDist * maxDist;

                List<Vector2Int> bandCandidates = new List<Vector2Int>();
                for (int t = 0; t < availableTiles.Count; t++)
                {
                    Vector2Int tile = availableTiles[t];
                    float distSqr = (tile - bossTile).sqrMagnitude;
                    if (distSqr >= minDistSqr && distSqr <= maxDistSqr)
                    {
                        if (hasForwardAxis)
                        {
                            Vector2 toTile = (Vector2)(tile - doorTile);
                            if (Vector2.Dot(toTile, forwardDir) < -0.5f)
                            {
                                continue;
                            }
                        }
                        bandCandidates.Add(tile);
                    }
                }

                if (bandCandidates.Count < group.Count)
                {
                    var fallbackCandidates = new List<Vector2Int>(availableTiles);
                    fallbackCandidates.Sort((a, b) =>
                    {
                        float diffA = Mathf.Abs(Vector2Int.Distance(a, bossTile) - band.distanceToBoss);
                        float diffB = Mathf.Abs(Vector2Int.Distance(b, bossTile) - band.distanceToBoss);
                        return diffA.CompareTo(diffB);
                    });

                    for (int f = 0; f < fallbackCandidates.Count; f++)
                    {
                        if (!bandCandidates.Contains(fallbackCandidates[f]))
                        {
                            bandCandidates.Add(fallbackCandidates[f]);
                            if (bandCandidates.Count >= group.Count) break;
                        }
                    }
                }

                for (int i = 0; i < bandCandidates.Count; i++)
                {
                    int r = Random.Range(i, bandCandidates.Count);
                    Vector2Int temp = bandCandidates[i];
                    bandCandidates[i] = bandCandidates[r];
                    bandCandidates[r] = temp;
                }

                int toSpawn = Mathf.Min(group.Count, bandCandidates.Count);
                for (int i = 0; i < toSpawn; i++)
                {
                    Vector2Int spawnTile = bandCandidates[i];
                    SpawnConfiguredEnemyAt(groupData, spawnTile, floorTilemap, parentContainer, room.RoomIndex);
                    tileQuery.MarkOccupied(spawnTile);
                    availableTiles.Remove(spawnTile);
                }
            }
        }

        private void SpawnConfiguredEnemyAt(EnemyData data, Vector2Int tile, Tilemap floorTilemap, Transform parentContainer, int roomIndex)
        {
            Vector3 worldPos = floorTilemap.GetCellCenterWorld(new Vector3Int(tile.x, tile.y, 0));
            GameObject enemyObj = Object.Instantiate(data.Prefab, worldPos, Quaternion.identity, parentContainer);
            _spawnedEnemies.Add(enemyObj);

            if (enemyObj.TryGetComponent<EnemyBase>(out var enemyBase))
            {
                enemyBase.SetData(data);
                enemyBase.CurrentRoomIndex = roomIndex;
            }

            if (GameManager.Instance != null && GameManager.Instance.Board != null)
            {
                Core.Occupants.EnemyArchetype archetype = Core.Occupants.EnemyArchetype.Pawn;
                switch (data.MovementPattern)
                {
                    case EnemyMovementPattern.SingleMove: archetype = Core.Occupants.EnemyArchetype.Pawn; break;
                    case EnemyMovementPattern.KnightMove: archetype = Core.Occupants.EnemyArchetype.Knight; break;
                    case EnemyMovementPattern.BishopMove: archetype = Core.Occupants.EnemyArchetype.Bishop; break;
                    case EnemyMovementPattern.RookMove: archetype = Core.Occupants.EnemyArchetype.Rook; break;
                    case EnemyMovementPattern.QueenMove: archetype = Core.Occupants.EnemyArchetype.Queen; break;
                }
                Infrastructure.BoardEntityFactory.CreateEnemy(enemyObj, tile, GameManager.Instance.Board, archetype, data, Presentation.Board.EffectsQueueRunner.Instance);
            }

            RegisterUndoInEditor(enemyObj, data.EnemyName);
        }

        private void SpawnEnemyAt(GameObject prefab, Vector2Int tile, Tilemap floorTilemap, Transform parentContainer, int roomIndex)
        {
            Vector3 worldPos = floorTilemap.GetCellCenterWorld(new Vector3Int(tile.x, tile.y, 0));
            GameObject enemyObj = Object.Instantiate(prefab, worldPos, Quaternion.identity, parentContainer);
            _spawnedEnemies.Add(enemyObj);

            EnemyData data = null;
            if (enemyObj.TryGetComponent<EnemyBase>(out var enemyBase))
            {
                enemyBase.CurrentRoomIndex = roomIndex;
                data = enemyBase.Data;
            }

            if (GameManager.Instance != null && GameManager.Instance.Board != null)
            {
                Core.Occupants.EnemyArchetype archetype = Core.Occupants.EnemyArchetype.Pawn;
                if (data != null)
                {
                    switch (data.MovementPattern)
                    {
                        case EnemyMovementPattern.SingleMove: archetype = Core.Occupants.EnemyArchetype.Pawn; break;
                        case EnemyMovementPattern.KnightMove: archetype = Core.Occupants.EnemyArchetype.Knight; break;
                        case EnemyMovementPattern.BishopMove: archetype = Core.Occupants.EnemyArchetype.Bishop; break;
                        case EnemyMovementPattern.RookMove: archetype = Core.Occupants.EnemyArchetype.Rook; break;
                        case EnemyMovementPattern.QueenMove: archetype = Core.Occupants.EnemyArchetype.Queen; break;
                    }
                }
                else if (prefab != null)
                {
                    string lower = prefab.name.ToLowerInvariant();
                    if (lower.Contains("knight")) archetype = Core.Occupants.EnemyArchetype.Knight;
                    else if (lower.Contains("bishop")) archetype = Core.Occupants.EnemyArchetype.Bishop;
                    else if (lower.Contains("rook")) archetype = Core.Occupants.EnemyArchetype.Rook;
                    else if (lower.Contains("queen") || lower.Contains("king")) archetype = Core.Occupants.EnemyArchetype.Queen;
                }
                Infrastructure.BoardEntityFactory.CreateEnemy(enemyObj, tile, GameManager.Instance.Board, archetype, data, Presentation.Board.EffectsQueueRunner.Instance);
            }

            RegisterUndoInEditor(enemyObj, prefab.name);
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

            var dm = DungeonManager.Instance != null ? DungeonManager.Instance : Object.FindAnyObjectByType<DungeonManager>();
            if (dm != null)
            {
                var enemies = dm.GetComponentsInChildren<EnemyBase>(true);
                for (int i = 0; i < enemies.Length; i++)
                {
                    if (enemies[i] != null)
                    {
                        if (Application.isPlaying)
                        {
                            Object.Destroy(enemies[i].gameObject);
                        }
                        else
                        {
                            Object.DestroyImmediate(enemies[i].gameObject);
                        }
                    }
                }
            }
        }

        private static void RegisterUndoInEditor(GameObject enemyObj, string name)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.RegisterCreatedObjectUndo(enemyObj, $"Spawn Enemy {name}");
                EditorUtility.SetDirty(enemyObj);
            }
#endif
        }
    }
}
