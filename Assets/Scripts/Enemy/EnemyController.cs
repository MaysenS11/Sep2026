using System.Collections;
using System.Collections.Generic;
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

    private static readonly Vector2Int[] AllEightDirections = new Vector2Int[]
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right,
        new Vector2Int(1, 1),
        new Vector2Int(-1, 1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, -1)
    };

    private int moveTurnCounter = 0;

    private readonly List<Vector2Int> _shuffleBuffer = new List<Vector2Int>(8);

    public override MoveIntent PlanMove(Transform playerTransform)
    {
        var noMove = new MoveIntent { HasMove = false };

        if (stats != null && stats.IsDead) return noMove;
        if (ShouldSkipTurn()) return noMove;
        if (playerTransform == null) return noMove;

        EnemyMovementPattern movePattern = enemyData != null ? enemyData.MovementPattern : EnemyMovementPattern.SingleMove;
        bool usesDiagonal = enemyData != null && enemyData.UsesDiagonalAttack;
        EnemySmartness smartness = enemyData != null ? enemyData.Smartness : EnemySmartness.Mid;
        int detection = enemyData != null ? enemyData.DetectionRange : 6;
        int maxLineSteps = enemyData != null ? enemyData.MaxLineSteps : 1;
        int interval = enemyData != null ? enemyData.MovesInterval : 1;

        Vector3 currentPos = TileReservationSystem.SnapToTileCenter(transform.position);
        Vector3 playerPos = TileReservationSystem.SnapToTileCenter(playerTransform.position);

        Vector2Int currentGrid = TileReservationSystem.WorldToGridTile(currentPos);
        Vector2Int playerGrid = TileReservationSystem.WorldToGridTile(playerPos);

        int distX = Mathf.Abs(currentGrid.x - playerGrid.x);
        int distY = Mathf.Abs(currentGrid.y - playerGrid.y);
        int manhattanDist = distX + distY;

        if (manhattanDist > detection) return noMove;

        moveTurnCounter++;
        if (interval > 1 && (moveTurnCounter % interval) != 0) return noMove;

        attackPlayerTransform = playerTransform;

        if (usesDiagonal && distX == 1 && distY == 1)
        {
            Vector2Int diagDir = new Vector2Int(
                Mathf.RoundToInt(Mathf.Sign(playerGrid.x - currentGrid.x)),
                Mathf.RoundToInt(Mathf.Sign(playerGrid.y - currentGrid.y))
            );

            Vector2Int pushTile = playerGrid + diagDir;

            return new MoveIntent
            {
                Path = new List<Vector2Int> { playerGrid },
                FinalDestination = playerPos,
                Direction = diagDir,
                IsAttack = true,
                PlayerPushTile = pushTile,
                HasMove = true
            };
        }

        switch (movePattern)
        {
            case EnemyMovementPattern.SingleMove:
                return PlanOneStepOrthogonal(currentPos, playerPos, manhattanDist, smartness);

            case EnemyMovementPattern.BishopMove:
                return PlanLineTurn(currentPos, playerPos, DiagonalDirections, maxLineSteps, smartness);

            case EnemyMovementPattern.RookMove:
                return PlanLineTurn(currentPos, playerPos, OrthogonalDirections, maxLineSteps, smartness);

            case EnemyMovementPattern.QueenMove:
                return PlanLineTurn(currentPos, playerPos, AllEightDirections, maxLineSteps, smartness);

            case EnemyMovementPattern.KnightMove:
                return PlanKnightTurn(currentPos, playerPos, smartness);

            default:
                return noMove;
        }
    }

    private MoveIntent PlanOneStepOrthogonal(Vector3 currentPos, Vector3 playerPos, int manhattanDist, EnemySmartness smartness)
    {
        var noMove = new MoveIntent { HasMove = false };

        if (manhattanDist <= 1) return noMove;

        Vector2Int currentGrid = TileReservationSystem.WorldToGridTile(currentPos);
        Vector2Int playerGrid = TileReservationSystem.WorldToGridTile(playerPos);
        float currentDistToPlayer = Vector3.Distance(currentPos, playerPos);

        var validCandidates = new List<(Vector2Int dir, Vector2Int grid, Vector3 pos, float dist)>();
        var closerCandidates = new List<(Vector2Int dir, Vector2Int grid, Vector3 pos, float dist)>();

        foreach (var dir in OrthogonalDirections)
        {
            Vector2Int candidateGrid = currentGrid + dir;
            Vector3 candidatePos = TileReservationSystem.GetTileCenterWorld(candidateGrid, currentPos.z);

            if (candidateGrid == playerGrid) continue;

            if (!IsTileBlocked(candidatePos))
            {
                float dist = Vector3.Distance(candidatePos, playerPos);
                var entry = (dir, candidateGrid, candidatePos, dist);
                validCandidates.Add(entry);
                if (dist < currentDistToPlayer)
                {
                    closerCandidates.Add(entry);
                }
            }
        }

        if (validCandidates.Count == 0) return noMove;

        (Vector2Int dir, Vector2Int grid, Vector3 pos, float dist) chosen;

        if (smartness == EnemySmartness.Lazy)
        {
            chosen = validCandidates[Random.Range(0, validCandidates.Count)];
        }
        else if (smartness == EnemySmartness.Mid)
        {
            if (closerCandidates.Count > 0)
            {
                chosen = closerCandidates[Random.Range(0, closerCandidates.Count)];
            }
            else
            {
                chosen = validCandidates[Random.Range(0, validCandidates.Count)];
            }
        }
        else
        {
            validCandidates.Sort((a, b) => a.dist.CompareTo(b.dist));
            chosen = validCandidates[0];
        }

        return new MoveIntent
        {
            Path = new List<Vector2Int> { chosen.grid },
            FinalDestination = chosen.pos,
            Direction = chosen.dir,
            IsAttack = false,
            PlayerPushTile = null,
            HasMove = true
        };
    }

    private MoveIntent PlanLineTurn(Vector3 currentPos, Vector3 playerPos,
        Vector2Int[] directions, int exactSteps, EnemySmartness smartness)
    {
        var noMove = new MoveIntent { HasMove = false };

        Vector2Int currentGrid = TileReservationSystem.WorldToGridTile(currentPos);
        Vector2Int playerGrid = TileReservationSystem.WorldToGridTile(playerPos);

        foreach (var dir in directions)
        {
            var attackPath = new List<Vector2Int>();

            for (int s = 1; s <= exactSteps; s++)
            {
                Vector2Int checkGrid = currentGrid + dir * s;
                Vector3 checkPos = TileReservationSystem.GetTileCenterWorld(checkGrid, currentPos.z);

                if (checkGrid == playerGrid)
                {
                    attackPath.Add(checkGrid);
                    Vector2Int pushTile = checkGrid + dir;

                    return new MoveIntent
                    {
                        Path = attackPath,
                        FinalDestination = checkPos,
                        Direction = dir,
                        IsAttack = true,
                        PlayerPushTile = pushTile,
                        HasMove = true
                    };
                }

                if (IsTileBlocked(checkPos)) break;

                attackPath.Add(checkGrid);
            }
        }

        float currentDistToPlayer = Vector3.Distance(currentPos, playerPos);
        var closerCandidates = new List<(List<Vector2Int> path, Vector3 dest, Vector2Int dir, float dist)>();
        var allCandidates = new List<(List<Vector2Int> path, Vector3 dest, Vector2Int dir, float dist)>();

        foreach (var dir in directions)
        {
            var pathSoFar = new List<Vector2Int>();
            bool blocked = false;

            for (int s = 1; s <= exactSteps; s++)
            {
                Vector2Int stepGrid = currentGrid + dir * s;
                Vector3 stepPos = TileReservationSystem.GetTileCenterWorld(stepGrid, currentPos.z);

                if (IsTileBlocked(stepPos))
                {
                    blocked = true;
                    break;
                }

                pathSoFar.Add(stepGrid);
            }

            if (blocked || pathSoFar.Count < exactSteps) continue;

            Vector2Int finalGrid = currentGrid + dir * exactSteps;
            Vector3 finalPos = TileReservationSystem.GetTileCenterWorld(finalGrid, currentPos.z);
            if (finalGrid == playerGrid) continue;

            float dist = Vector3.Distance(finalPos, playerPos);
            var entry = (pathSoFar, finalPos, dir, dist);
            allCandidates.Add(entry);

            if (dist < currentDistToPlayer)
            {
                closerCandidates.Add(entry);
            }
        }

        if (allCandidates.Count == 0) return noMove;

        (List<Vector2Int> path, Vector3 dest, Vector2Int dir, float dist) selected;

        if (smartness == EnemySmartness.Lazy)
        {
            selected = allCandidates[Random.Range(0, allCandidates.Count)];
        }
        else if (smartness == EnemySmartness.Mid)
        {
            if (closerCandidates.Count > 0)
            {
                selected = closerCandidates[Random.Range(0, closerCandidates.Count)];
            }
            else
            {
                selected = allCandidates[Random.Range(0, allCandidates.Count)];
            }
        }
        else
        {
            allCandidates.Sort((a, b) => a.dist.CompareTo(b.dist));
            selected = allCandidates[0];
        }

        return new MoveIntent
        {
            Path = selected.path,
            FinalDestination = selected.dest,
            Direction = selected.dir,
            IsAttack = false,
            HasMove = true
        };
    }

    private MoveIntent PlanKnightTurn(Vector3 currentPos, Vector3 playerPos, EnemySmartness smartness)
    {
        var noMove = new MoveIntent { HasMove = false };

        Vector2Int currentGrid = TileReservationSystem.WorldToGridTile(currentPos);
        Vector2Int playerGrid = TileReservationSystem.WorldToGridTile(playerPos);
        Vector2Int playerOffset = playerGrid - currentGrid;

        float currentDistToPlayer = Vector3.Distance(currentPos, playerPos);

        foreach (var offset in KnightOffsets)
        {
            if (offset == playerOffset)
            {
                var lPath = TryBuildKnightLPath(currentGrid, offset, isAttack: true);
                if (lPath != null)
                {
                    Vector2 pushDir = new Vector2(offset.x, offset.y).normalized;
                    Vector2Int pushTile = playerGrid + new Vector2Int(
                        Mathf.Clamp(offset.x, -1, 1),
                        Mathf.Clamp(offset.y, -1, 1)
                    );

                    return new MoveIntent
                    {
                        Path = lPath,
                        FinalDestination = playerPos,
                        Direction = pushDir,
                        IsAttack = true,
                        PlayerPushTile = pushTile,
                        HasMove = true
                    };
                }
            }
        }

        var closerCandidates = new List<(List<Vector2Int> path, Vector3 dest, Vector2 dir, float dist)>();
        var allCandidates = new List<(List<Vector2Int> path, Vector3 dest, Vector2 dir, float dist)>();

        foreach (var offset in KnightOffsets)
        {
            Vector2Int destGrid = currentGrid + offset;
            Vector3 destPos = TileReservationSystem.GetTileCenterWorld(destGrid, currentPos.z);

            if (destGrid == playerGrid) continue;

            if (IsTileBlocked(destPos)) continue;

            var lPath = TryBuildKnightLPath(currentGrid, offset, isAttack: false);
            if (lPath == null) continue;

            float dist = Vector3.Distance(destPos, playerPos);
            Vector2 dir = new Vector2(offset.x, offset.y).normalized;

            var entry = (lPath, destPos, dir, dist);
            allCandidates.Add(entry);
            if (dist < currentDistToPlayer)
            {
                closerCandidates.Add(entry);
            }
        }

        if (allCandidates.Count == 0) return noMove;

        (List<Vector2Int> path, Vector3 dest, Vector2 dir, float dist) chosen;

        if (smartness == EnemySmartness.Lazy)
        {
            chosen = allCandidates[Random.Range(0, allCandidates.Count)];
        }
        else if (smartness == EnemySmartness.Mid)
        {
            if (closerCandidates.Count > 0)
            {
                chosen = closerCandidates[Random.Range(0, closerCandidates.Count)];
            }
            else
            {
                chosen = allCandidates[Random.Range(0, allCandidates.Count)];
            }
        }
        else
        {
            allCandidates.Sort((a, b) => a.dist.CompareTo(b.dist));
            chosen = allCandidates[0];
        }

        return new MoveIntent
        {
            Path = chosen.path,
            FinalDestination = chosen.dest,
            Direction = chosen.dir,
            IsAttack = false,
            HasMove = true
        };
    }

    private List<Vector2Int> TryBuildKnightLPath(Vector2Int origin, Vector2Int offset, bool isAttack)
    {
        var routeH = BuildLSteps(origin, offset.x, 0, 0, offset.y, isAttack);
        if (routeH != null) return routeH;

        var routeV = BuildLSteps(origin, 0, offset.y, offset.x, 0, isAttack);
        if (routeV != null) return routeV;

        return null;
    }

    private List<Vector2Int> BuildLSteps(Vector2Int origin, int leg1X, int leg1Y, int leg2X, int leg2Y, bool isAttack)
    {
        var steps = new List<Vector2Int>();
        Vector2Int current = origin;

        int steps1 = Mathf.Max(Mathf.Abs(leg1X), Mathf.Abs(leg1Y));
        Vector2Int dir1 = new Vector2Int(Mathf.Clamp(leg1X, -1, 1), Mathf.Clamp(leg1Y, -1, 1));
        for (int i = 0; i < steps1; i++)
        {
            current += dir1;
            steps.Add(current);
            Vector3 worldPos = TileReservationSystem.GetTileCenterWorld(current, transform.position.z);
            if (IsTileBlocked(worldPos)) return null;
        }

        int steps2 = Mathf.Max(Mathf.Abs(leg2X), Mathf.Abs(leg2Y));
        Vector2Int dir2 = new Vector2Int(Mathf.Clamp(leg2X, -1, 1), Mathf.Clamp(leg2Y, -1, 1));
        for (int i = 0; i < steps2; i++)
        {
            current += dir2;
            steps.Add(current);
            bool isFinalTile = (i == steps2 - 1);
            if (!isFinalTile || !isAttack)
            {
                Vector3 worldPos = TileReservationSystem.GetTileCenterWorld(current, transform.position.z);
                if (IsTileBlocked(worldPos)) return null;
            }
        }

        return steps;
    }
    private static void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
