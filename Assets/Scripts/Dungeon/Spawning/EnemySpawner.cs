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
            if (room.Type == RoomType.Boss || room.Type == RoomType.Chest) return;

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
                    float configuredWeight = GetConfiguredWeight(enemy.Data.EnemyName);
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

        private void SpawnEnemyAt(GameObject prefab, Vector2Int tile, Tilemap floorTilemap, Transform parentContainer, int roomIndex)
        {
            Vector3 worldPos = floorTilemap.GetCellCenterWorld(new Vector3Int(tile.x, tile.y, 0));
            GameObject enemyObj = Object.Instantiate(prefab, worldPos, Quaternion.identity, parentContainer);
            _spawnedEnemies.Add(enemyObj);

            if (enemyObj.TryGetComponent<EnemyBase>(out var enemyBase))
            {
                enemyBase.CurrentRoomIndex = roomIndex;
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
