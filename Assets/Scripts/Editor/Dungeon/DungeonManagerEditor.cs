using UnityEditor;
using UnityEngine;

namespace Dungeon.Editor
{
    [CustomEditor(typeof(DungeonManager), true)]
    public class DungeonManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            DungeonManager manager = (DungeonManager)target;

            EditorGUILayout.Space(10);
            if (GUILayout.Button("Generate Dungeon (In Editor)", GUILayout.Height(30)))
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(manager);
                EventBus<GenerateDungeonEvent>.Raise(new GenerateDungeonEvent());
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            }

            if (GUILayout.Button("Clear All Tiles", GUILayout.Height(25)))
            {
                serializedObject.ApplyModifiedProperties();
                manager.ClearDungeonTiles();
                EventBus<ClearDungeonEvent>.Raise(new ClearDungeonEvent());
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            }
        }
    }
}
