/*using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

[CustomEditor(typeof(SiblingRuleTile))]
public class SiblingRuleTileEditor : RuleTileEditor
{
    private SiblingRuleTile siblingTarget => target as SiblingRuleTile;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (siblingTarget == null || siblingTarget.m_TilingRules == null) return;

        serializedObject.Update();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Strict Multi-Layer Neighbor Targets", EditorStyles.boldLabel);

        for (int i = 0; i < siblingTarget.m_TilingRules.Count; i++)
        {
            var rule = siblingTarget.m_TilingRules[i];

            while (siblingTarget.siblingRules.Count <= i)
            {
                siblingTarget.siblingRules.Add(new SiblingRuleTile.SiblingTilingRule());
            }

            SiblingRuleTile.SiblingTilingRule sRule = siblingTarget.siblingRules[i];

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField($"Rule {i + 1} Assignment", EditorStyles.boldLabel);

            for (int y = 1; y >= -1; y--)
            {
                EditorGUILayout.BeginHorizontal();
                for (int x = -1; x <= 1; x++)
                {
                    if (x == 0 && y == 0)
                    {
                        GUILayout.Box("Self", GUILayout.Width(65), GUILayout.Height(18));
                        continue;
                    }

                    Vector3Int offset = new Vector3Int(x, y, 0);
                    int arrayIdx = (y + 1) * 3 + (x + 1);
                    int constraint = GetNeighborConstraint(rule, offset);

                    if (constraint == SiblingRuleTile.Neighbor.StrictTarget)
                    {
                        sRule.neighborSiblings[arrayIdx] = (TileBase)EditorGUILayout.ObjectField(
                            sRule.neighborSiblings[arrayIdx],
                            typeof(TileBase),
                            false,
                            GUILayout.Width(65)
                        );
                    }
                    else
                    {
                        string label = constraint == 1 ? "This" : (constraint == 2 ? "Not This" : "Any");
                        GUILayout.Label(label, GUILayout.Width(65));
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
            serializedObject.ApplyModifiedProperties();
        }
    }

    private int GetNeighborConstraint(RuleTile.TilingRule rule, Vector3Int offset)
    {
        if (rule.m_NeighborPositions.Contains(offset))
        {
            int index = rule.m_NeighborPositions.IndexOf(offset);
            if (index >= 0 && index < rule.m_Neighbors.Count)
            {
                return rule.m_Neighbors[index];
            }
        }
        return 0;
    }
}*/