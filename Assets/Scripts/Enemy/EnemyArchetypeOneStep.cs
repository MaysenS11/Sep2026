using System.Collections;
using UnityEngine;

public class EnemyArchetypeOneStep : EnemyBase
{
    private static readonly Vector2Int[] OrthogonalDirections = new Vector2Int[]
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    public override IEnumerator ExecuteTurnCoroutine(Transform playerTransform)
    {
        if (stats != null && stats.IsDead) yield break;
        if (playerTransform == null) yield break;

        Vector3 currentPos = transform.position;
        Vector3 playerPos = playerTransform.position;

        int distX = Mathf.RoundToInt(Mathf.Abs(currentPos.x - playerPos.x) / tileSize);
        int distY = Mathf.RoundToInt(Mathf.Abs(currentPos.y - playerPos.y) / tileSize);
        int manhattanDist = distX + distY;

        int detection = enemyData != null ? enemyData.DetectionRange : 6;
        float speed = enemyData != null ? enemyData.StepSpeed : 5.0f;

        // 1. If adjacent to player, attack!
        if (manhattanDist == 1)
        {
            if (playerTransform.TryGetComponent<IDamageable>(out var target))
            {
                PerformAttack(target);
            }
            yield return new WaitForSeconds(0.2f);
            yield break;
        }

        // 2. If out of detection range, skip or idle
        if (manhattanDist > detection)
        {
            yield break;
        }

        // 3. Step 1 tile orthogonally towards player
        Vector2Int bestDir = Vector2Int.zero;
        int bestDist = int.MaxValue;

        foreach (var dir in OrthogonalDirections)
        {
            Vector3 candidatePos = currentPos + new Vector3(dir.x, dir.y, 0) * tileSize;
            if (!IsTileBlocked(candidatePos))
            {
                int newDistX = Mathf.RoundToInt(Mathf.Abs(candidatePos.x - playerPos.x) / tileSize);
                int newDistY = Mathf.RoundToInt(Mathf.Abs(candidatePos.y - playerPos.y) / tileSize);
                int newDist = newDistX + newDistY;

                // Don't step directly onto the player's tile (attacks are from distance 1)
                if (newDist > 0 && newDist < bestDist)
                {
                    bestDist = newDist;
                    bestDir = dir;
                }
            }
        }

        if (bestDir != Vector2Int.zero)
        {
            lastDirection = bestDir;
            Vector3 targetPos = currentPos + new Vector3(bestDir.x, bestDir.y, 0) * tileSize;
            yield return StartCoroutine(StepToTile(targetPos, speed));
        }
    }
}
