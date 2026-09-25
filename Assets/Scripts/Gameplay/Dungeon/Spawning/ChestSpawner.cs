using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dungeon.Spawning
{
    [System.Serializable]
    public class ChestSpawner : IDungeonSpawner
    {
        [Header("Chest Prefabs")]
        [SerializeField] private GameObject chestFrontPrefab;
        [SerializeField] private GameObject chestLeftPrefab;
        [SerializeField] private GameObject chestRightPrefab;

        [Header("Spawn Settings")]
        [SerializeField] private int totalDungeonChests = 3;
        [SerializeField] private int minDistanceToDoors = 3;
        [SerializeField] private int minDistanceToProps = 2;

        private readonly List<GameObject> _spawnedChests = new List<GameObject>();
        private readonly HashSet<int> _roomsWithChest = new HashSet<int>();
        private readonly List<Vector2Int> _borderTilesBuffer = new List<Vector2Int>(128);

        public GameObject ChestFrontPrefab
        {
            get => chestFrontPrefab;
            set => chestFrontPrefab = value;
        }

        public GameObject ChestLeftPrefab
        {
            get => chestLeftPrefab;
            set => chestLeftPrefab = value;
        }

        public GameObject ChestRightPrefab
        {
            get => chestRightPrefab;
            set => chestRightPrefab = value;
        }

        public int TotalDungeonChests
        {
            get => totalDungeonChests;
            set => totalDungeonChests = Mathf.Max(0, value);
        }

        public int MinDistanceToDoors
        {
            get => minDistanceToDoors;
            set => minDistanceToDoors = Mathf.Max(1, value);
        }

        public int MinDistanceToProps
        {
            get => minDistanceToProps;
            set => minDistanceToProps = Mathf.Max(1, value);
        }

        public void PrepareDungeonChestPlan(List<GameManager.RoomData> rooms)
        {
            _roomsWithChest.Clear();
            if (rooms == null || rooms.Count == 0 || totalDungeonChests <= 0) return;

            List<int> eligibleRoomIndices = new List<int>();
            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i].Type == RoomType.Normal || rooms[i].Type == RoomType.Start)
                {
                    eligibleRoomIndices.Add(rooms[i].RoomIndex);
                }
            }

            for (int i = 0; i < eligibleRoomIndices.Count; i++)
            {
                int r = Random.Range(i, eligibleRoomIndices.Count);
                int temp = eligibleRoomIndices[i];
                eligibleRoomIndices[i] = eligibleRoomIndices[r];
                eligibleRoomIndices[r] = temp;
            }

            int countToSelect = Mathf.Min(totalDungeonChests, eligibleRoomIndices.Count);
            for (int i = 0; i < countToSelect; i++)
            {
                _roomsWithChest.Add(eligibleRoomIndices[i]);
            }
        }

        public void SpawnContent(
            GameManager.RoomData room,
            RoomTileQuery tileQuery,
            Tilemap floorTilemap,
            Transform parentContainer)
        {
            if (floorTilemap == null) return;

            if (room.Type == RoomType.Chest && chestFrontPrefab != null)
            {
                SpawnChestAt(chestFrontPrefab, room.CenterTile, room, tileQuery, floorTilemap, parentContainer, "Chest Room Center");
                return;
            }

            if ((room.Type != RoomType.Normal && room.Type != RoomType.Start) || !_roomsWithChest.Contains(room.RoomIndex)) return;

            List<(RoomEdge edge, GameObject prefab)> candidates = new List<(RoomEdge, GameObject)>()
            {
                (RoomEdge.Top, chestFrontPrefab),
                (RoomEdge.Left, chestLeftPrefab),
                (RoomEdge.Right, chestRightPrefab)
            };

            candidates.RemoveAll(c => c.prefab == null);
            if (candidates.Count == 0) return;

            for (int i = 0; i < candidates.Count; i++)
            {
                int r = Random.Range(i, candidates.Count);
                var temp = candidates[i];
                candidates[i] = candidates[r];
                candidates[r] = temp;
            }

            for (int c = 0; c < candidates.Count; c++)
            {
                var candidate = candidates[c];
                tileQuery.GetBorderFloorTiles(room, candidate.edge, _borderTilesBuffer, onlyUnoccupied: true);

                FilterCandidateTiles(_borderTilesBuffer, room, tileQuery);

                if (_borderTilesBuffer.Count > 0)
                {
                    int pickIndex = Random.Range(0, _borderTilesBuffer.Count);
                    Vector2Int spawnTile = _borderTilesBuffer[pickIndex];

                    SpawnChestAt(candidate.prefab, spawnTile, room, tileQuery, floorTilemap, parentContainer, $"Chest {candidate.edge}");
                    break;
                }
            }
        }

        private void FilterCandidateTiles(List<Vector2Int> tiles, GameManager.RoomData room, RoomTileQuery tileQuery)
        {
            if (room.EntranceDoorTile.HasValue)
            {
                Vector2Int door = room.EntranceDoorTile.Value;
                Vector2Int landing = new Vector2Int(door.x, door.y - 1);
                tiles.RemoveAll(t =>
                    ChebyshevDistance(t, door) < minDistanceToDoors ||
                    ChebyshevDistance(t, landing) < minDistanceToDoors);
            }

            if (room.ExitDoorTile.HasValue)
            {
                Vector2Int door = room.ExitDoorTile.Value;
                Vector2Int landing = new Vector2Int(door.x, door.y - 1);
                tiles.RemoveAll(t =>
                    ChebyshevDistance(t, door) < minDistanceToDoors ||
                    ChebyshevDistance(t, landing) < minDistanceToDoors);
            }

            if (minDistanceToProps > 1)
            {
                tiles.RemoveAll(t => IsNearOccupiedTile(t, tileQuery, minDistanceToProps - 1));
            }
        }

        private bool IsNearOccupiedTile(Vector2Int tile, RoomTileQuery tileQuery, int radius)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    if (tileQuery.IsOccupied(new Vector2Int(tile.x + dx, tile.y + dy)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static int ChebyshevDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        private void SpawnChestAt(
            GameObject prefab,
            Vector2Int tile,
            GameManager.RoomData room,
            RoomTileQuery tileQuery,
            Tilemap floorTilemap,
            Transform parentContainer,
            string label)
        {
            Vector3 worldPos = floorTilemap.GetCellCenterWorld(new Vector3Int(tile.x, tile.y, 0));
            GameObject chest = Object.Instantiate(prefab, worldPos, Quaternion.identity, parentContainer);
            _spawnedChests.Add(chest);
            tileQuery.MarkOccupied(tile);

            if (GameManager.Instance != null && GameManager.Instance.Board != null)
            {
                bool isChestRoom = room != null && room.Type == RoomType.Chest;
                Infrastructure.BoardEntityFactory.CreateChest(chest, tile, GameManager.Instance.Board, isChestRoom, Presentation.Board.EffectsQueueRunner.Instance);
            }

            EventBus<PropSpawnedEvent>.Raise(new PropSpawnedEvent(chest, tile, room));
            RegisterUndoInEditor(chest, label);
        }

        public void ClearSpawnedContent()
        {
            for (int i = 0; i < _spawnedChests.Count; i++)
            {
                if (_spawnedChests[i] != null)
                {
                    if (Application.isPlaying)
                    {
                        Object.Destroy(_spawnedChests[i]);
                    }
                    else
                    {
                        Object.DestroyImmediate(_spawnedChests[i]);
                    }
                }
            }
            _spawnedChests.Clear();
            _roomsWithChest.Clear();

            var dm = DungeonManager.Instance != null ? DungeonManager.Instance : Object.FindAnyObjectByType<DungeonManager>();
            if (dm != null)
            {
                var transforms = dm.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                {
                    if (transforms[i] != null && transforms[i] != dm.transform && transforms[i].name.StartsWith("Chest"))
                    {
                        if (Application.isPlaying)
                        {
                            Object.Destroy(transforms[i].gameObject);
                        }
                        else
                        {
                            Object.DestroyImmediate(transforms[i].gameObject);
                        }
                    }
                }
            }
        }

        private static void RegisterUndoInEditor(GameObject obj, string name)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.RegisterCreatedObjectUndo(obj, $"Spawn {name}");
                EditorUtility.SetDirty(obj);
            }
#endif
        }
    }
}
