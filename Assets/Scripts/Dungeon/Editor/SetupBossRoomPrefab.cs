using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dungeon.Editor
{
    public static class SetupBossRoomPrefab
    {
        [MenuItem("Dungeon/Setup Boss Room Prefab")]
        public static void Setup()
        {
            var kingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Enemys/King.prefab");
            var root = new GameObject("BossRoom_18x18");
            var template = root.AddComponent<BossRoomTemplate>();
            template.KingPrefab = kingPrefab;

            var doorMarker = new GameObject("DoorMarker");
            doorMarker.transform.SetParent(root.transform);
            doorMarker.transform.localPosition = new Vector3(9f, 1f, 0f);

            var kingMarker = new GameObject("KingSpawnMarker");
            kingMarker.transform.SetParent(root.transform);
            kingMarker.transform.localPosition = new Vector3(9f, 9f, 0f);

            var gridObj = new GameObject("Grid");
            gridObj.transform.SetParent(root.transform);
            gridObj.transform.localPosition = Vector3.zero;
            var grid = gridObj.AddComponent<Grid>();

            var fillFloorObj = new GameObject("FillFloor");
            fillFloorObj.transform.SetParent(gridObj.transform);
            fillFloorObj.transform.localPosition = Vector3.zero;
            var fillFloorTilemap = fillFloorObj.AddComponent<UnityEngine.Tilemaps.Tilemap>();
            var fillFloorRenderer = fillFloorObj.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();
            fillFloorRenderer.sortingOrder = 0;

            var borderFloorObj = new GameObject("BorderFloor");
            borderFloorObj.transform.SetParent(gridObj.transform);
            borderFloorObj.transform.localPosition = Vector3.zero;
            var borderFloorTilemap = borderFloorObj.AddComponent<UnityEngine.Tilemaps.Tilemap>();
            var borderFloorRenderer = borderFloorObj.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();
            borderFloorRenderer.sortingOrder = 1;

            var wallObj = new GameObject("Wall");
            wallObj.transform.SetParent(gridObj.transform);
            wallObj.transform.localPosition = Vector3.zero;
            var wallTilemap = wallObj.AddComponent<UnityEngine.Tilemaps.Tilemap>();
            var wallRenderer = wallObj.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();
            wallRenderer.sortingOrder = 2;

            var roofObj = new GameObject("Roof");
            roofObj.transform.SetParent(gridObj.transform);
            roofObj.transform.localPosition = Vector3.zero;
            var roofTilemap = roofObj.AddComponent<UnityEngine.Tilemaps.Tilemap>();
            var roofRenderer = roofObj.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();
            roofRenderer.sortingOrder = 3;

            var so = new SerializedObject(template);
            so.FindProperty("doorMarker").objectReferenceValue = doorMarker.transform;
            so.FindProperty("kingSpawnPoint").objectReferenceValue = kingMarker.transform;
            so.FindProperty("fillFloorTilemap").objectReferenceValue = fillFloorTilemap;
            so.FindProperty("borderFloorTilemap").objectReferenceValue = borderFloorTilemap;
            so.FindProperty("wallTilemap").objectReferenceValue = wallTilemap;
            so.FindProperty("roofTilemap").objectReferenceValue = roofTilemap;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (!AssetDatabase.IsValidFolder("Assets/Prefab/Dungeon"))
            {
                AssetDatabase.CreateFolder("Assets/Prefab", "Dungeon");
            }

            string prefabPath = "Assets/Prefab/Dungeon/BossRoom_18x18.prefab";
            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            var dm = Object.FindAnyObjectByType<DungeonManager>();
            if (dm != null && savedPrefab != null)
            {
                var dmSO = new SerializedObject(dm);
                dmSO.FindProperty("bossRoomPrefab").objectReferenceValue = savedPrefab.GetComponent<BossRoomTemplate>();
                var bossSizeProp = dmSO.FindProperty("fixedBossRoomSize");
                if (bossSizeProp != null)
                {
                    bossSizeProp.vector2IntValue = new Vector2Int(18, 18);
                }
                dmSO.ApplyModifiedProperties();
                EditorUtility.SetDirty(dm);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(dm.gameObject.scene);
            }

            Debug.Log("BossRoom Prefab successfully created at: " + prefabPath);
        }

        [MenuItem("Dungeon/Inspect Boss Room Prefab Bounds")]
        public static void InspectPrefabBounds()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Dungeon/BossRoom_18x18.prefab");
            if (prefab == null)
            {
                Debug.LogError("Could not load BossRoom_18x18.prefab");
                return;
            }

            var template = prefab.GetComponent<BossRoomTemplate>();
            if (template != null)
            {
                Debug.Log($"DoorMarker localPos: {template.DoorMarker?.localPosition}, KingMarker localPos: {template.KingSpawnPoint?.localPosition}");
                LogTilemapBounds("FillFloor", template.FillFloorTilemap);
                LogTilemapBounds("BorderFloor", template.BorderFloorTilemap);
                LogTilemapBounds("Wall", template.WallTilemap);
                LogTilemapBounds("Roof", template.RoofTilemap);
            }
        }

        private static void LogTilemapBounds(string name, UnityEngine.Tilemaps.Tilemap tm)
        {
            if (tm == null)
            {
                Debug.Log($"Tilemap {name}: null");
                return;
            }
            tm.CompressBounds();
            Debug.Log($"Tilemap {name}: used={tm.GetUsedTilesCount()}, bounds={tm.cellBounds}");
        }

        [MenuItem("Dungeon/Shift Prefab Tiles To Origin")]
        public static void ShiftPrefabTilesToOrigin()
        {
            string prefabPath = "Assets/Prefab/Dungeon/BossRoom_18x18.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError("Could not load " + prefabPath);
                return;
            }

            var root = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            var template = root.GetComponent<BossRoomTemplate>();
            if (template == null)
            {
                Object.DestroyImmediate(root);
                return;
            }

            Tilemap refTm = template.BorderFloorTilemap != null && template.BorderFloorTilemap.GetUsedTilesCount() > 0
                ? template.BorderFloorTilemap
                : template.FillFloorTilemap;

            if (refTm == null || refTm.GetUsedTilesCount() == 0)
            {
                Debug.LogWarning("No tiles found to shift.");
                Object.DestroyImmediate(root);
                return;
            }

            refTm.CompressBounds();
            Vector3Int shift = -refTm.cellBounds.min;
            Debug.Log($"Shifting painted tiles by offset: {shift}");

            ShiftTilemap(template.FillFloorTilemap, shift);
            ShiftTilemap(template.BorderFloorTilemap, shift);
            ShiftTilemap(template.WallTilemap, shift);
            ShiftTilemap(template.RoofTilemap, shift);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();

            Debug.Log("Successfully shifted prefab tiles to origin (0, 0)!");
        }

        private static void ShiftTilemap(Tilemap tm, Vector3Int shift)
        {
            if (tm == null || tm.GetUsedTilesCount() == 0) return;

            tm.CompressBounds();
            BoundsInt bounds = tm.cellBounds;
            var list = new System.Collections.Generic.List<(Vector3Int pos, TileBase tile)>();

            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector3Int p = new Vector3Int(x, y, 0);
                    TileBase t = tm.GetTile(p);
                    if (t != null) list.Add((p, t));
                }
            }

            tm.ClearAllTiles();
            for (int i = 0; i < list.Count; i++)
            {
                tm.SetTile(list[i].pos + shift, list[i].tile);
            }
            tm.CompressBounds();
        }

        [MenuItem("Dungeon/Test Generate Dungeon")]
        public static void TestGenerate()
        {
            var dm = Object.FindAnyObjectByType<DungeonManager>();
            if (dm != null)
            {
                dm.GenerateAndBuildDungeon();
                var bossRoom = dm.GeneratedRooms.Find(r => r.Type == RoomType.Boss);
                if (bossRoom != null)
                {
                    Debug.Log($"[BossRoom Test] Boss room found! Size: {bossRoom.Size}, Index: {bossRoom.RoomIndex}, Entrance: {bossRoom.EntranceDoorTile}");
                    var allEnemies = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
                    EnemyBase king = null;
                    for (int i = 0; i < allEnemies.Length; i++)
                    {
                        if (allEnemies[i].name.Contains("King"))
                        {
                            king = allEnemies[i];
                            break;
                        }
                    }
                    Debug.Log($"[BossRoom Test] King spawned: {(king != null)}, King RoomIndex: {(king != null ? king.CurrentRoomIndex : -1)}");
                }
            }
        }
    }
}
