using System.Collections;
using UnityEngine;

public class EnemyController : EnemyBase
{
    private static readonly Vector2Int[] OrthogonalDirections = new Vector2Int[]
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    private static readonly Vector2Int[] DiagonalDirections = new Vector2Int[]
    {
        new Vector2Int(1, 1),
        new Vector2Int(-1, 1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, -1)
    };

    private static readonly Vector2Int[] KnightOffsets = new Vector2Int[]
    {
        new Vector2Int(1, 2),
        new Vector2Int(2, 1),
        new Vector2Int(-1, 2),
        new Vector2Int(-2, 1),
        new Vector2Int(1, -2),
        new Vector2Int(2, -1),
        new Vector2Int(-1, -2),
        new Vector2Int(-2, -1)
    };

    private int moveTurnCounter = 0;

    public override IEnumerator ExecuteTurnCoroutine(Transform playerTransform)
    {
        if (stats != null && stats.IsDead) yield break;
        if (playerTransform == null) yield break;

        EnemyMovementPattern movePattern = enemyData != null ? enemyData.MovementPattern : EnemyMovementPattern.OneStepOrthogonal;
        EnemyAttackPattern attackPat = enemyData != null ? enemyData.AttackPattern : EnemyAttackPattern.AdjacentDiagonalTrigger;
        float speed = enemyData != null ? enemyData.StepSpeed : 5.0f;
        int detection = enemyData != null ? enemyData.DetectionRange : 6;
        int maxLineSteps = enemyData != null ? enemyData.MaxLineSteps : 1;
        int interval = enemyData != null ? enemyData.MovesInterval : 1;

        Vector3 currentPos = transform.position;
        Vector3 playerPos = playerTransform.position;

        int distX = Mathf.RoundToInt(Mathf.Abs(currentPos.x - playerPos.x) / tileSize);
        int distY = Mathf.RoundToInt(Mathf.Abs(currentPos.y - playerPos.y) / tileSize);
        int manhattanDist = distX + distY;

        // Check detection range
        if (manhattanDist > detection)
        {
            yield break;
        }

        // Paced movement check (e.g. Knight moves every 2 player moves)
        moveTurnCounter++;
        if (interval > 1 && (moveTurnCounter % interval) != 0)
        {
            yield break;
        }

        // 1. Attack Check: Adjacent Diagonal Trigger (Pawn)
        if (attackPat == EnemyAttackPattern.AdjacentDiagonalTrigger && distX == 1 && distY == 1)
        {
            Vector2Int diagDir = new Vector2Int(
                Mathf.RoundToInt(Mathf.Sign(playerPos.x - currentPos.x)),
                Mathf.RoundToInt(Mathf.Sign(playerPos.y - currentPos.y))
            );
            lastDirection = diagDir;

            // Attack by moving diagonally onto the player's tile and pushing the player 1 tile diagonally in the same direction
            yield return StartCoroutine(AttackAndPushPlayer(playerTransform, playerPos, diagDir, speed));
            yield break;
        }

        // Execute movement and attack patterns based on EnemyData
        switch (movePattern)
        {
            case EnemyMovementPattern.OneStepOrthogonal:
                yield return StartCoroutine(ExecuteOneStepOrthogonal(playerTransform, currentPos, playerPos, manhattanDist, speed));
                break;

            case EnemyMovementPattern.DiagonalLine:
                yield return StartCoroutine(ExecuteLineTurn(playerTransform, currentPos, playerPos, DiagonalDirections, maxLineSteps, speed));
                break;

            case EnemyMovementPattern.OrthogonalLine:
                yield return StartCoroutine(ExecuteLineTurn(playerTransform, currentPos, playerPos, OrthogonalDirections, maxLineSteps, speed));
                break;

            case EnemyMovementPattern.KnightLPattern:
                yield return StartCoroutine(ExecuteKnightTurn(playerTransform, currentPos, playerPos, speed));
                break;
        }
    }

    /// <summary>
    /// Pawn movement: strictly moves 1 tile orthogonally towards the player.
    /// If already adjacent (distance == 1), it stands its ground and never moves away.
    /// Regular diagonal movement is strictly forbidden.
    /// </summary>
    private IEnumerator ExecuteOneStepOrthogonal(Transform playerTransform, Vector3 currentPos, Vector3 playerPos, int manhattanDist, float speed)
    {
        // If already orthogonally adjacent to player, do not move away
        if (manhattanDist <= 1)
        {
            yield break;
        }

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

                // Only move closer to the player, never move directly onto player during normal movement
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

    /// <summary>
    /// Line movement for Rook (Orthogonal) and Bishop (Diagonal) up to maxLineSteps.
    /// Attacks if moving onto the player tile, pushing the player in that direction.
    /// </summary>
    private IEnumerator ExecuteLineTurn(Transform playerTransform, Vector3 currentPos, Vector3 playerPos, Vector2Int[] directions, int maxSteps, float speed)
    {
        // 1. Check direct line of sight attack to player within maxSteps
        foreach (var dir in directions)
        {
            for (int s = 1; s <= maxSteps; s++)
            {
                Vector3 checkPos = currentPos + new Vector3(dir.x * s, dir.y * s, 0) * tileSize;

                if (Vector3.Distance(checkPos, playerPos) < 0.1f)
                {
                    lastDirection = dir;
                    yield return StartCoroutine(AttackAndPushPlayer(playerTransform, playerPos, dir, speed));
                    yield break;
                }

                if (IsTileBlocked(checkPos))
                {
                    break;
                }
            }
        }

        // 2. Normal movement along the line that brings enemy closest to player
        Vector3 bestStepPos = currentPos;
        float bestDistToPlayer = Vector3.Distance(currentPos, playerPos);
        Vector2 bestDir = Vector2.zero;

        foreach (var dir in directions)
        {
            for (int s = 1; s <= maxSteps; s++)
            {
                Vector3 candidatePos = currentPos + new Vector3(dir.x * s, dir.y * s, 0) * tileSize;
                if (IsTileBlocked(candidatePos))
                {
                    break;
                }

                float dist = Vector3.Distance(candidatePos, playerPos);
                if (dist > 0.1f && dist < bestDistToPlayer)
                {
                    bestDistToPlayer = dist;
                    bestStepPos = candidatePos;
                    bestDir = dir;
                }
            }
        }

        if (bestStepPos != currentPos)
        {
            lastDirection = bestDir;
            yield return StartCoroutine(StepToTile(bestStepPos, speed));
        }
    }

    /// <summary>
    /// Knight L-pattern movement: leaps directly onto player tile if in range (attack & push),
    /// otherwise leaps to the unblocked L-tile closest to player.
    /// </summary>
    private IEnumerator ExecuteKnightTurn(Transform playerTransform, Vector3 currentPos, Vector3 playerPos, float speed)
    {
        Vector2Int playerOffset = new Vector2Int(
            Mathf.RoundToInt((playerPos.x - currentPos.x) / tileSize),
            Mathf.RoundToInt((playerPos.y - currentPos.y) / tileSize)
        );

        // 1. Check direct attack leap
        foreach (var offset in KnightOffsets)
        {
            if (offset == playerOffset)
            {
                Vector2 pushDir = new Vector2(offset.x, offset.y).normalized;
                lastDirection = pushDir;
                yield return StartCoroutine(AttackAndPushPlayer(playerTransform, playerPos, pushDir, speed));
                yield break;
            }
        }

        // 2. Leap to closest unblocked tile
        Vector2Int bestOffset = Vector2Int.zero;
        float bestDistance = float.MaxValue;

        foreach (var offset in KnightOffsets)
        {
            Vector3 candidatePos = currentPos + new Vector3(offset.x, offset.y, 0) * tileSize;
            if (!IsTileBlocked(candidatePos))
            {
                float dist = Vector3.Distance(candidatePos, playerPos);
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestOffset = offset;
                }
            }
        }

        if (bestOffset != Vector2Int.zero)
        {
            lastDirection = new Vector2(bestOffset.x, bestOffset.y).normalized;
            Vector3 targetPos = currentPos + new Vector3(bestOffset.x, bestOffset.y, 0) * tileSize;
            yield return StartCoroutine(StepToTile(targetPos, speed));
        }
    }
}
