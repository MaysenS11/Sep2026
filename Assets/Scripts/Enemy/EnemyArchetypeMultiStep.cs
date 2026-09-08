using System.Collections;
using UnityEngine;

public class EnemyArchetypeMultiStep : EnemyBase
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

        int stepsToTake = enemyData != null ? enemyData.StepsPerTurn : 2;
        int detection = enemyData != null ? enemyData.DetectionRange : 8;
        float speed = enemyData != null ? enemyData.StepSpeed : 6.0f;

        for (int step = 0; step < stepsToTake; step++)
        {
            if (stats != null && stats.IsDead) yield break;

            Vector3 currentPos = transform.position;
            Vector3 playerPos = playerTransform.position;

            int distX = Mathf.RoundToInt(Mathf.Abs(currentPos.x - playerPos.x) / tileSize);
            int distY = Mathf.RoundToInt(Mathf.Abs(currentPos.y - playerPos.y) / tileSize);
            int manhattanDist = distX + distY;

            // If adjacent to player, attack and stop stepping
            if (manhattanDist == 1)
            {
                if (playerTransform.TryGetComponent<IDamageable>(out var target))
                {
                    PerformAttack(target);
                }
                yield return new WaitForSeconds(0.2f);
                yield break;
            }

            // If player is outside detection range, stop chasing
            if (manhattanDist > detection)
            {
                yield break;
            }

            // Pick best orthogonal step
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
            else
            {
                // Unreachable or cornered
                break;
            }
        }
    }
}
