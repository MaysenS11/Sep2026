using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewAttackPattern", menuName = "Game/Attack Pattern")]
public class AttackPatternData : ScriptableObject
{
    [SerializeField] private string patternName;
    [SerializeField] private Vector2Int[] targetOffsets = new Vector2Int[] { new Vector2Int(0, 1) };

    public string PatternName => patternName;
    public IReadOnlyList<Vector2Int> TargetOffsets => targetOffsets;

    public void SetOffsets(Vector2Int[] offsets)
    {
        targetOffsets = offsets;
    }

    public List<Vector2Int> GetAffectedTiles(Vector2Int origin, Vector2 facingDir)
    {
        List<Vector2Int> tiles = new List<Vector2Int>();
        if (targetOffsets == null || targetOffsets.Length == 0) return tiles;

        Vector2Int cardinal = GetCardinalDirection(facingDir);

        for (int i = 0; i < targetOffsets.Length; i++)
        {
            Vector2Int rotated = RotateOffset(targetOffsets[i], cardinal);
            tiles.Add(origin + rotated);
        }

        return tiles;
    }

    public static Vector2Int GetCardinalDirection(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
        {
            if (dir.x > 0.01f) return Vector2Int.right;
            if (dir.x < -0.01f) return Vector2Int.left;
        }
        else
        {
            if (dir.y > 0.01f) return Vector2Int.up;
            if (dir.y < -0.01f) return Vector2Int.down;
        }

        return Vector2Int.up;
    }

    public static Vector2Int RotateOffset(Vector2Int offset, Vector2Int cardinal)
    {
        if (cardinal == Vector2Int.up)
        {
            return offset;
        }
        if (cardinal == Vector2Int.down)
        {
            return new Vector2Int(-offset.x, -offset.y);
        }
        if (cardinal == Vector2Int.right)
        {
            return new Vector2Int(offset.y, -offset.x);
        }
        if (cardinal == Vector2Int.left)
        {
            return new Vector2Int(-offset.y, offset.x);
        }

        return offset;
    }
}
