using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dungeon.Spawning
{
    [System.Serializable]
    public class PropSpawner : IDungeonSpawner
    {
        [Header("Prop Spawn Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float propDensity = 0.5f;

        [Header("Prop Spawn Configurations")]
        [SerializeField] private PropSpawnData barrelSpawnData;
        [SerializeField] private PropSpawnData pillarSpawnData;
        [SerializeField] private List<PropSpawnData> additionalPropData = new List<PropSpawnData>();

        private readonly List<GameObject> _spawnedProps = new List<GameObject>();
        private readonly List<Vector2Int> _validTilesBuffer = new List<Vector2Int>(256);

        public float PropDensity
        {
            get => propDensity;
            set => propDensity = Mathf.Clamp01(value);
        }

        public PropSpawnData BarrelSpawnData
        {
            get => barrelSpawnData;
            set => barrelSpawnData = value;
        }

        public PropSpawnData PillarSpawnData
        {
            get => pillarSpawnData;
            set => pillarSpawnData = value;
        }

        public List<PropSpawnData> AdditionalPropData => additionalPropData;

        public void SpawnContent(
            GameManager.RoomData room,
            RoomTileQuery tileQuery,
            Tilemap floorTilemap,
            Transform parentContainer)
        {
            if (floorTilemap == null) return;

            if (barrelSpawnData != null)
            {
                SpawnConfiguredProp(barrelSpawnData, room, tileQuery, floorTilemap, parentContainer);
            }

            if (pillarSpawnData != null)
            {
                SpawnConfiguredProp(pillarSpawnData, room, tileQuery, floorTilemap, parentContainer);
            }

            if (additionalPropData != null)
            {
                for (int i = 0; i < additionalPropData.Count; i++)
                {
                    PropSpawnData data = additionalPropData[i];
                    if (data != null)
                    {
                        SpawnConfiguredProp(data, room, tileQuery, floorTilemap, parentContainer);
                    }
                }
            }
        }

        private void SpawnConfiguredProp(
            PropSpawnData data,
            GameManager.RoomData room,
            RoomTileQuery tileQuery,
            Tilemap floorTilemap,
            Transform parentContainer)
        {
            if (data == null || data.Prefab == null) return;

            if (room.Type == RoomType.Normal && !data.AllowInNormalRooms) return;
            if (room.Type == RoomType.Chest && !data.AllowInChestRooms) return;
            if (room.Type == RoomType.Boss && !data.AllowInBossRooms) return;

            tileQuery.GetValidInteriorFloorTiles(room, _validTilesBuffer, onlyUnoccupied: true);

            if (data.AvoidRoomCenter)
            {
                _validTilesBuffer.RemoveAll(t =>
                    Mathf.Abs(t.x - room.CenterTile.x) <= 1 &&
                    Mathf.Abs(t.y - room.CenterTile.y) <= 1);
            }

            if (data.AvoidDoorTiles)
            {
                RemoveTilesNear(room.EntranceDoorTile, 1);
                RemoveTilesNear(room.ExitDoorTile, 1);
            }

            // Always prevent spawning on stair doors or directly on the interaction step in front (stair.y - 1)
            RemoveStairAndLandingZone(room.EntranceDoorTile);
            RemoveStairAndLandingZone(room.ExitDoorTile);

            if (_validTilesBuffer.Count == 0) return;

            // 0% prop density means explicitly 0 props spawned
            if (propDensity <= 0f || data.SpawnDensity <= 0f) return;

            float combinedDensity = propDensity * data.SpawnDensity;
            int targetCount = Mathf.RoundToInt(combinedDensity * _validTilesBuffer.Count);
            if (targetCount <= 0 && combinedDensity > 0f && data.MinPerRoom > 0)
            {
                targetCount = data.MinPerRoom;
            }
            if (data.MaxPerRoom > 0 && targetCount > data.MaxPerRoom)
            {
                targetCount = data.MaxPerRoom;
            }

            int toSpawn = Mathf.Min(targetCount, _validTilesBuffer.Count);
            for (int i = 0; i < toSpawn; i++)
            {
                if (_validTilesBuffer.Count == 0) break;

                int pickIndex = Random.Range(0, _validTilesBuffer.Count);
                Vector2Int spawnTile = _validTilesBuffer[pickIndex];
                _validTilesBuffer.RemoveAt(pickIndex);

                Vector3 worldPos = floorTilemap.GetCellCenterWorld(new Vector3Int(spawnTile.x, spawnTile.y, 0));
                GameObject propObj = Object.Instantiate(data.Prefab, worldPos, Quaternion.identity, parentContainer);
                _spawnedProps.Add(propObj);
                tileQuery.MarkOccupied(spawnTile);

                if (GameManager.Instance != null && GameManager.Instance.Board != null)
                {
                    bool isPillar = (data != null && data.PropName != null && data.PropName.ToLowerInvariant().Contains("pillar")) ||
                                    (data != null && data.Prefab != null && data.Prefab.name.ToLowerInvariant().Contains("pillar"));
                    if (isPillar)
                    {
                        Infrastructure.BoardEntityFactory.CreateObstacle(propObj, spawnTile, GameManager.Instance.Board, Core.Occupants.ObstacleType.Pillar, Presentation.Board.EffectsQueueRunner.Instance);
                    }
                    else
                    {
                        Core.Occupants.DestructiblePropType propType = Core.Occupants.DestructiblePropType.Barrel;
                        if (data != null && data.PropName != null && data.PropName.ToLowerInvariant().Contains("crate"))
                        {
                            propType = Core.Occupants.DestructiblePropType.Crate;
                        }
                        int hp = 1;
                        Infrastructure.BoardEntityFactory.CreateProp(propObj, spawnTile, GameManager.Instance.Board, propType, hp, Presentation.Board.EffectsQueueRunner.Instance);
                    }
                }

                EventBus<PropSpawnedEvent>.Raise(new PropSpawnedEvent(propObj, spawnTile, room));
                RegisterUndoInEditor(propObj, data.PropName);
            }
        }

        private void RemoveTilesNear(Vector2Int? doorTile, int radius)
        {
            if (!doorTile.HasValue) return;
            Vector2Int pos = doorTile.Value;
            _validTilesBuffer.RemoveAll(t =>
                Mathf.Abs(t.x - pos.x) <= radius &&
                Mathf.Abs(t.y - pos.y) <= radius);
        }

        private void RemoveStairAndLandingZone(Vector2Int? doorTile)
        {
            if (!doorTile.HasValue) return;
            Vector2Int pos = doorTile.Value;
            Vector2Int landing = new Vector2Int(pos.x, pos.y - 1);
            _validTilesBuffer.RemoveAll(t => t == pos || t == landing);
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

        private static void RegisterUndoInEditor(GameObject obj, string name)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.RegisterCreatedObjectUndo(obj, $"Spawn Prop {name}");
                EditorUtility.SetDirty(obj);
            }
#endif
        }
    }
}

