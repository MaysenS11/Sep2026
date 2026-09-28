using System;
using System.Collections.Generic;
using UnityEngine;
using Core.Effects;

namespace Core.Scheduling
{
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
