using System;
using System.Collections.Generic;
using UnityEngine;
using Core.Effects;

namespace Core.Scheduling
{
    /// Pure C# Turn Batching & Dependency Scheduler.
    /// Analyzes sequential enacted enemy actions, determines spatial path conflicts,
    /// vacated tile ordering, blocker arrival and departure dependencies, and groups actions into
    /// path-disjoint concurrent animation batches with evenly divided time slices.
    public class TurnBatchScheduler
    {
        /// Schedules enacted enemy actions into concurrent animation batches.
        public List<AnimationBatch> BuildBatches(List<EnactedEnemyAction> actions, float totalTurnDuration = 1.0f)
        {
            var batches = new List<AnimationBatch>();
            if (actions == null || actions.Count == 0)
            {
                return batches;
            }

            var actionBatchMap = new Dictionary<EnactedEnemyAction, int>();

            foreach (var action in actions)
            {
                if (action == null) continue;

                int minBatchIndex = 0;
                foreach (var kvp in actionBatchMap)
                {
                    var prevAction = kvp.Key;
                    int prevBatch = kvp.Value;

                    if (MustPrecede(prevAction, action))
                    {
                        minBatchIndex = Math.Max(minBatchIndex, prevBatch + 1);
                    }
                }

                int targetBatchIndex = minBatchIndex;
                while (targetBatchIndex < batches.Count)
                {
                    if (!batches[targetBatchIndex].HasSpatialConflictWith(action))
                    {
                        break;
                    }
                    targetBatchIndex++;
                }

                while (batches.Count <= targetBatchIndex)
                {
                    batches.Add(new AnimationBatch(batches.Count));
                }

                batches[targetBatchIndex].AddAction(action);
                actionBatchMap[action] = targetBatchIndex;
            }

            float batchDuration = batches.Count > 0 ? totalTurnDuration / batches.Count : 0f;
            for (int i = 0; i < batches.Count; i++)
            {
                batches[i].BatchIndex = i;
                batches[i].Duration = batchDuration;
            }

            return batches;
        }

        /// Checks whether two actions conflict spatially and CANNOT run concurrently in the same batch.
        /// Conflict occurs if:
        /// 1. Their traversed tile paths overlap.
        /// 2. Action B enters/affects a tile Action A vacates or starts on.
        /// 3. Action A enters/affects a tile Action B vacates or starts on.
        /// 4. Both actions share destination or starting tiles.
        public static bool HasSpatialConflict(EnactedEnemyAction a, EnactedEnemyAction b)
        {
            if (a == null || b == null) return false;
            if (ReferenceEquals(a, b)) return false;

            if (a.TraversedTiles != null && b.TraversedTiles != null)
            {
                if (a.TraversedTiles.Overlaps(b.TraversedTiles))
                {
                    return true;
                }
            }

            if (a.TraversedTiles != null)
            {
                if (a.TraversedTiles.Contains(b.StartTile) || a.TraversedTiles.Contains(b.EndTile))
                {
                    return true;
                }
            }

            if (b.TraversedTiles != null)
            {
                if (b.TraversedTiles.Contains(a.StartTile) || b.TraversedTiles.Contains(a.EndTile))
                {
                    return true;
                }
            }

            if (a.StartTile == b.EndTile || b.StartTile == a.EndTile)
            {
                return true;
            }
            if (a.StartTile == b.StartTile || a.EndTile == b.EndTile)
            {
                return true;
            }

            return false;
        }

        /// Determines whether action A MUST be scheduled in a strictly earlier batch than action B.
        /// Precedence is required when:
        /// 1. Blocker Arrival: Action B was blocked by an obstacle/unit, and Action A was the unit that moved
        ///    into that blocking tile (b.BlockedTile) during this turn.
        /// 2. Blocker Departure: Action B pushes into a tile where the blocker (Action A) was residing and moving away.
        ///    Action A must move away before Action B's push into that tile occurs.
        /// 3. Vacated Tile Chaining: Action A moved out of a tile (a.StartTile != a.EndTile) and Action B
        ///    enters or traverses that tile.
        public static bool MustPrecede(EnactedEnemyAction a, EnactedEnemyAction b)
        {
            if (a == null || b == null) return false;
            if (ReferenceEquals(a, b)) return false;

            if (b.BlockedTile.HasValue)
            {
                Vector2Int blockedTile = b.BlockedTile.Value;
                if (a.EndTile == blockedTile)
                {
                    return true;
                }

                if (b.BlockerOccupantId.HasValue && a.Enemy != null && a.Enemy.Id == b.BlockerOccupantId.Value)
                {
                    return true;
                }
            }

            if (a.StartTile != a.EndTile)
            {
                if (b.Intent != null && b.Intent.PushDirection != Vector2Int.zero)
                {
                    Vector2Int pushDest = b.Intent.TargetPosition + b.Intent.PushDirection;
                    if (a.StartTile == pushDest)
                    {
                        return true;
                    }
                }

                if (b.EmittedEffects != null)
                {
                    foreach (var effect in b.EmittedEffects)
                    {
                        if (effect is PushDisplacementEffect push && push.EndPos == a.StartTile)
                        {
                            return true;
                        }
                    }
                }

                if (b.EndTile == a.StartTile)
                {
                    return true;
                }

                if (b.TraversedTiles != null && b.TraversedTiles.Contains(a.StartTile))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
