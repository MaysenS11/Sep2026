using UnityEditor;
using UnityEngine;
using Dungeon;
using Dungeon.Spawning;

namespace Setup
{
    public static class BarrelSetup
    {
        [MenuItem("Tools/Setup Barrel Prefab and Config")]
        public static void Configure()
        {
            string prefabPath = "Assets/Prefab/Barrel.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError("Barrel prefab not found at " + prefabPath);
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

            EnemyController enemyCtrl = instance.GetComponent<EnemyController>();
            if (enemyCtrl != null) Object.DestroyImmediate(enemyCtrl, true);

            EntityStats entityStats = instance.GetComponent<EntityStats>();
            if (entityStats != null) Object.DestroyImmediate(entityStats, true);

            DestructibleProp destructible = instance.GetComponent<DestructibleProp>();
            if (destructible == null) destructible = instance.AddComponent<DestructibleProp>();

            instance.layer = 6;

            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);

            if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder("Assets/Data/Props")) AssetDatabase.CreateFolder("Assets/Data", "Props");

            string dataPath = "Assets/Data/Props/BarrelSpawnData.asset";
            PropSpawnData spawnData = AssetDatabase.LoadAssetAtPath<PropSpawnData>(dataPath);
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

