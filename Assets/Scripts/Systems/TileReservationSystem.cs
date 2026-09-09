using System.Collections.Generic;
using UnityEngine;

public static class TileReservationSystem
{
    private static readonly HashSet<Vector2Int> _reservedTiles = new HashSet<Vector2Int>();
    private static readonly Dictionary<EnemyBase, List<Vector2Int>> _enemyPaths = new Dictionary<EnemyBase, List<Vector2Int>>();

    public static Vector2Int WorldToGridTile(Vector3 worldPos)
    {
        return new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));
    }

    public static Vector3 GetTileCenterWorld(Vector2Int tile, float z = 0f)
    {
        return new Vector3(tile.x + 0.5f, tile.y + 0.5f, z);
    }

    public static Vector3 SnapToTileCenter(Vector3 worldPos)
    {
        Vector2Int grid = WorldToGridTile(worldPos);
        return GetTileCenterWorld(grid, worldPos.z);
    }

    public static bool IsTileReserved(Vector2Int tile, EnemyBase requester)
    {
        if (!_reservedTiles.Contains(tile)) return false;

        if (requester != null && _enemyPaths.TryGetValue(requester, out var path))
        {
            for (int i = 0; i < path.Count; i++)
            {
                if (path[i] == tile) return false;
            }
        }

        return true;
    }

    public static bool IsTileBlocked(Vector3 worldPos, EnemyBase requester, float tileSize, LayerMask blockingLayers)
    {
        Vector2Int gridPos = WorldToGridTile(worldPos);

        if (IsTileReserved(gridPos, requester)) return true;

        Vector3 centerPos = GetTileCenterWorld(gridPos, worldPos.z);
        Collider2D hit = Physics2D.OverlapBox(centerPos, new Vector2(tileSize * 0.8f, tileSize * 0.8f), 0f, blockingLayers);
        if (hit != null && (requester == null || hit.gameObject != requester.gameObject))
        {
            return true;
        }

        return false;
    }

    public static bool TryReservePath(List<Vector2Int> path, EnemyBase requester)
    {
        if (path == null || path.Count == 0) return false;

        for (int i = 0; i < path.Count; i++)
        {
            if (IsTileReserved(path[i], requester)) return false;
        }

        if (!_enemyPaths.TryGetValue(requester, out var existingPath))
        {
            existingPath = new List<Vector2Int>();
            _enemyPaths[requester] = existingPath;
        }

        for (int i = 0; i < path.Count; i++)
        {
            _reservedTiles.Add(path[i]);
            existingPath.Add(path[i]);
        }

        return true;
    }

    public static void ReleaseEnemy(EnemyBase enemy)
    {
        if (_enemyPaths.TryGetValue(enemy, out var path))
        {
            for (int i = 0; i < path.Count; i++)
            {
                _reservedTiles.Remove(path[i]);
            }
            _enemyPaths.Remove(enemy);
        }
    }

    public static void ClearAll()
    {
        _reservedTiles.Clear();
        _enemyPaths.Clear();
    }
}
