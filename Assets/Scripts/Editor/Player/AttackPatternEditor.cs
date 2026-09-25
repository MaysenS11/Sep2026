#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AttackPatternData))]
public class AttackPatternEditor : Editor
{
    private const int GridSize = 5;
    private const int Center = 2; // (2, 2) corresponds to origin (0, 0)
    private const float CellSize = 35f;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty patternNameProp = serializedObject.FindProperty("patternName");
        EditorGUILayout.PropertyField(patternNameProp);

        AttackPatternData pattern = (AttackPatternData)target;
        HashSet<Vector2Int> offsetSet = new HashSet<Vector2Int>(pattern.TargetOffsets ?? System.Array.Empty<Vector2Int>());

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Attack Pattern Grid (Player at Center facing North ▲)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Click cells to toggle attack area. Blue cell is Player origin (0, 0). Green cells are affected tiles when facing North (0, 1).", MessageType.Info);

        bool changed = false;

        Rect gridRect = GUILayoutUtility.GetRect(GridSize * CellSize, GridSize * CellSize, GUILayout.ExpandWidth(false));
        gridRect.x = (EditorGUIUtility.currentViewWidth - (GridSize * CellSize)) / 2f;

        for (int y = GridSize - 1; y >= 0; y--)
        {
            for (int x = 0; x < GridSize; x++)
            {
                Rect cellRect = new Rect(gridRect.x + x * CellSize, gridRect.y + (GridSize - 1 - y) * CellSize, CellSize - 2f, CellSize - 2f);
                int offsetX = x - Center;
                int offsetY = y - Center;
                Vector2Int offset = new Vector2Int(offsetX, offsetY);

                bool isPlayerCenter = (offsetX == 0 && offsetY == 0);
                bool isSelected = offsetSet.Contains(offset);

                Color originalColor = GUI.backgroundColor;
                if (isPlayerCenter)
                {
                    GUI.backgroundColor = new Color(0.3f, 0.6f, 1f);
                }
                else if (isSelected)
                {
                    GUI.backgroundColor = new Color(0.3f, 0.9f, 0.3f);
                }
                else
                {
                    GUI.backgroundColor = new Color(0.85f, 0.85f, 0.85f);
                }

                string label = isPlayerCenter ? "▲\nPlayer" : (isSelected ? "HIT\n" + $"({offsetX},{offsetY})" : "");

                if (GUI.Button(cellRect, label))
                {
                    if (!isPlayerCenter)
                    {
                        if (isSelected)
                        {
                            offsetSet.Remove(offset);
                        }
                        else
                        {
                            offsetSet.Add(offset);
                        }
                        changed = true;
                    }
                }

                GUI.backgroundColor = originalColor;
            }
        }

        if (changed)
        {
            Undo.RecordObject(pattern, "Modify Attack Pattern Offsets");
            Vector2Int[] newOffsets = new Vector2Int[offsetSet.Count];
            offsetSet.CopyTo(newOffsets);
            pattern.SetOffsets(newOffsets);
            EditorUtility.SetDirty(pattern);
        }

        EditorGUILayout.Space(15);
        SerializedProperty offsetsProp = serializedObject.FindProperty("targetOffsets");
        EditorGUILayout.PropertyField(offsetsProp, true);

        serializedObject.ApplyModifiedProperties();
    }

    [MenuItem("Tools/Combat/Create Default Attack Patterns")]
    public static void CreateDefaultPatterns()
    {
        string dir = "Assets/ScriptableObjects/AttackPatterns";
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
        {
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        }
        if (!AssetDatabase.IsValidFolder(dir))
        {
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "AttackPatterns");
        }

        CreatePatternAsset(dir + "/ForwardStab.asset", "Forward Stab", new Vector2Int[] { new Vector2Int(0, 1) });
        CreatePatternAsset(dir + "/ForwardCleave.asset", "Forward Cleave", new Vector2Int[] { new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1) });
        CreatePatternAsset(dir + "/PiercingThrust.asset", "Piercing Thrust", new Vector2Int[] { new Vector2Int(0, 1), new Vector2Int(0, 2) });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Created default AttackPattern assets in " + dir);

        AssignDefaultPatternsToCharacters();
    }

    [MenuItem("Tools/Combat/Assign Attack Patterns To Characters")]
    public static void AssignDefaultPatternsToCharacters()
    {
        string dir = "Assets/ScriptableObjects/AttackPatterns";
        var stab = AssetDatabase.LoadAssetAtPath<AttackPatternData>(dir + "/ForwardStab.asset");
        var cleave = AssetDatabase.LoadAssetAtPath<AttackPatternData>(dir + "/ForwardCleave.asset");
        var thrust = AssetDatabase.LoadAssetAtPath<AttackPatternData>(dir + "/PiercingThrust.asset");

        string[] guids = AssetDatabase.FindAssets("t:CharacterDefinition");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CharacterDefinition charDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
            if (charDef == null) continue;

            SerializedObject so = new SerializedObject(charDef);
            SerializedProperty prop = so.FindProperty("attackPatterns");
            if (prop != null && prop.isArray)
            {
                prop.arraySize = 3;
                if (prop.GetArrayElementAtIndex(0).objectReferenceValue == null)
                    prop.GetArrayElementAtIndex(0).objectReferenceValue = stab;
                if (prop.GetArrayElementAtIndex(1).objectReferenceValue == null)
                    prop.GetArrayElementAtIndex(1).objectReferenceValue = cleave;
                if (prop.GetArrayElementAtIndex(2).objectReferenceValue == null)
                    prop.GetArrayElementAtIndex(2).objectReferenceValue = thrust;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(charDef);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Assigned default attack patterns to {guids.Length} characters.");
    }

    [MenuItem("Tools/Combat/Setup Scene Player Highlighter")]
    public static void SetupScenePlayerHighlighter()
    {
        var p = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
        if (p == null)
        {
            Debug.LogWarning("No PlayerMovement found in open scene.");
            return;
        }

        var h = p.GetComponent<AttackTileHighlighter>();
        if (h == null)
        {
            h = Undo.AddComponent<AttackTileHighlighter>(p.gameObject);
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/AttackPositionIndicator.prefab");
        var so = new SerializedObject(h);
        var prop = so.FindProperty("attackIndicatorPrefab");
        if (prop != null)
        {
            prop.objectReferenceValue = prefab;
            so.ApplyModifiedProperties();
        }

        EditorUtility.SetDirty(p.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(p.gameObject.scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(p.gameObject.scene);
        Debug.Log("Successfully setup AttackTileHighlighter on " + p.gameObject.name);
    }

    private static void CreatePatternAsset(string path, string name, Vector2Int[] offsets)
    {
        var asset = AssetDatabase.LoadAssetAtPath<AttackPatternData>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<AttackPatternData>();
            asset.name = name;
            asset.SetOffsets(offsets);
            AssetDatabase.CreateAsset(asset, path);
        }
        else
        {
            asset.SetOffsets(offsets);
            EditorUtility.SetDirty(asset);
        }
    }
}
#endif
