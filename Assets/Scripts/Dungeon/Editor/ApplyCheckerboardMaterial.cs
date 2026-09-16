using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dungeon.Editor
{
    public static class ApplyCheckerboardMaterial
    {
        [MenuItem("Tools/Dungeon/Apply Checkerboard Material to Floor Tilemaps")]
        public static void Apply()
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Floor_Checkerboard.mat");
            if (mat == null)
            {
                Debug.LogError("Could not find Assets/Materials/M_Floor_Checkerboard.mat");
                return;
            }

            var dm = Object.FindAnyObjectByType<DungeonManager>();
            if (dm == null)
            {
                Debug.LogError("No DungeonManager found in active scene.");
                return;
            }

            SerializedObject so = new SerializedObject(dm);
            SerializedProperty borderProp = so.FindProperty("borderFloorTilemap");
            SerializedProperty fillProp = so.FindProperty("fillFloorTilemap");

            Tilemap borderTilemap = borderProp?.objectReferenceValue as Tilemap;
            Tilemap fillTilemap = fillProp?.objectReferenceValue as Tilemap;

            int updated = 0;
            if (borderTilemap != null)
            {
                var tr = borderTilemap.GetComponent<TilemapRenderer>();
                if (tr != null)
                {
                    Undo.RecordObject(tr, "Apply Checkerboard Material");
                    tr.sharedMaterial = mat;
                    EditorUtility.SetDirty(tr);
                    updated++;
                }
            }

            if (fillTilemap != null)
            {
                var tr = fillTilemap.GetComponent<TilemapRenderer>();
                if (tr != null)
                {
                    Undo.RecordObject(tr, "Apply Checkerboard Material");
                    tr.sharedMaterial = mat;
                    EditorUtility.SetDirty(tr);
                    updated++;
                }
            }

            Debug.Log($"Applied M_Floor_Checkerboard to {updated} floor tilemaps successfully.");
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(dm.gameObject.scene);
        }
    }
}
