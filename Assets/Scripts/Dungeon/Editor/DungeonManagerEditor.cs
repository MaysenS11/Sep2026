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
                EventBus<GenerateDungeonEvent>.Raise(new GenerateDungeonEvent());
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            }

            if (GUILayout.Button("Clear All Tiles", GUILayout.Height(25)))
            {
                manager.ClearDungeonTiles();
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            }
        }
    }
}
