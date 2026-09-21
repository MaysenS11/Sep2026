using UnityEditor;
using UnityEngine;
using Dungeon;
using Dungeon.Spawning;

namespace Setup
{
    public static class BarrelSetup
    {
        private static T FindAsset<T>(string filter, string defaultPath) where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets(filter);
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null) return asset;
            }
            return AssetDatabase.LoadAssetAtPath<T>(defaultPath);
        }

        [MenuItem("Tools/Setup Barrel Prefab and Config")]
        public static void Configure()
        {
            GameObject prefab = FindAsset<GameObject>("Barrel t:Prefab", "Assets/Prefab/Barrel.prefab");
            if (prefab == null)
            {
                Debug.LogError("Barrel prefab not found");
                return;
            }
            string prefabPath = AssetDatabase.GetAssetPath(prefab);

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

            EnemyController enemyCtrl = instance.GetComponent<EnemyController>();
            if (enemyCtrl != null) Object.DestroyImmediate(enemyCtrl, true);

            EntityStats entityStats = instance.GetComponent<EntityStats>();
            if (entityStats != null) Object.DestroyImmediate(entityStats, true);

            DestructibleProp destructible = instance.GetComponent<DestructibleProp>();
            if (destructible == null) destructible = instance.AddComponent<DestructibleProp>();

            PropSpawnData spawnData = FindAsset<PropSpawnData>("BarrelSpawnData t:PropSpawnData", "Assets/ScriptableObjects/BarrelSpawnData.asset");
            if (spawnData != null)
            {
                SerializedObject destSo = new SerializedObject(destructible);
                destSo.FindProperty("propData").objectReferenceValue = spawnData;
                destSo.ApplyModifiedProperties();
            }

            instance.layer = 6;

            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);

            if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder("Assets/Data/Props")) AssetDatabase.CreateFolder("Assets/Data", "Props");

            string dataPath = "Assets/ScriptableObjects/BarrelSpawnData.asset";
            spawnData = AssetDatabase.LoadAssetAtPath<PropSpawnData>(dataPath);
            if (spawnData == null)
            {
                spawnData = ScriptableObject.CreateInstance<PropSpawnData>();
                AssetDatabase.CreateAsset(spawnData, dataPath);
            }

            SerializedObject so = new SerializedObject(spawnData);
            so.FindProperty("propName").stringValue = "Barrel";
            so.FindProperty("prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            so.FindProperty("spawnDensity").floatValue = 0.08f;
            so.FindProperty("minPerRoom").intValue = 1;
            so.FindProperty("maxPerRoom").intValue = 5;
            so.FindProperty("allowInNormalRooms").boolValue = true;
            so.FindProperty("avoidDoorTiles").boolValue = true;
            so.FindProperty("avoidRoomCenter").boolValue = true;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(spawnData);

            DungeonManager dm = Object.FindAnyObjectByType<DungeonManager>();
            if (dm != null)
            {
                SerializedObject dmSo = new SerializedObject(dm);
                SerializedProperty propSpawnerProp = dmSo.FindProperty("propSpawner");
                SerializedProperty barrelDataProp = propSpawnerProp.FindPropertyRelative("barrelSpawnData");
                barrelDataProp.objectReferenceValue = spawnData;
                dmSo.ApplyModifiedProperties();
                EditorUtility.SetDirty(dm);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(dm.gameObject.scene);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Successfully setup Barrel prefab and PropSpawnData asset.");
        }

        [MenuItem("Tools/Setup Chest Prefabs on DungeonManager")]
        public static void SetupChestPrefabs()
        {
            DungeonManager dm = Object.FindAnyObjectByType<DungeonManager>();
            if (dm == null)
            {
                Debug.LogError("No DungeonManager found in current scene!");
                return;
            }

            GameObject front = FindAsset<GameObject>("ChestFront t:Prefab", "Assets/Prefab/ChestFront.prefab");
            GameObject left = FindAsset<GameObject>("ChestLeft t:Prefab", "Assets/Prefab/ChestLeft.prefab");
            GameObject right = FindAsset<GameObject>("ChestRight t:Prefab", "Assets/Prefab/ChestRight.prefab");

            SerializedObject so = new SerializedObject(dm);
            SerializedProperty chestSpawnerProp = so.FindProperty("chestSpawner");
            if (chestSpawnerProp != null)
            {
                chestSpawnerProp.FindPropertyRelative("chestFrontPrefab").objectReferenceValue = front;
                chestSpawnerProp.FindPropertyRelative("chestLeftPrefab").objectReferenceValue = left;
                chestSpawnerProp.FindPropertyRelative("chestRightPrefab").objectReferenceValue = right;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(dm);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(dm.gameObject.scene);
                Debug.Log("Successfully assigned ChestFront, ChestLeft, and ChestRight prefabs to DungeonManager.");
            }
        }

        [MenuItem("Tools/Generate Dungeon Test")]
        public static void GenerateDungeonTest()
        {
            DungeonManager dm = Object.FindAnyObjectByType<DungeonManager>();
            if (dm != null)
            {
                dm.GenerateAndBuildDungeon();
                DestructibleProp[] barrels = Object.FindObjectsByType<DestructibleProp>(FindObjectsSortMode.None);
                Debug.Log($"[Dungeon Test] Dungeon generated successfully. Spawned barrels count: {barrels.Length}");
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(dm.gameObject.scene);
            }
            else
            {
                Debug.LogError("No DungeonManager found in current scene!");
            }
        }
    }
}

