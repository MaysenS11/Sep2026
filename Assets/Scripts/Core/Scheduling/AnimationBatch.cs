using System.Collections.Generic;
using Core.Effects;
using Core.Occupants;
using UnityEngine;

namespace Core.Scheduling
{
    /// Represents a group of independent enemy actions scheduled to animate simultaneously
    /// within an allocated time slice duration.
    public class AnimationBatch
    {
        public int BatchIndex { get; set; }
        public float Duration { get; set; }
        public List<EnactedEnemyAction> Actions { get; } = new List<EnactedEnemyAction>();
        public List<BoardEffect> AllEffects
        {
            get
            {
                var effects = new List<BoardEffect>();
                foreach (var action in Actions)
                {
                    if (action?.EmittedEffects != null)
                    {
                        effects.AddRange(action.EmittedEffects);
                    }
                }
                return effects;
            }
        }

        public AnimationBatch(int batchIndex = 0, float duration = 0f)
        {
            BatchIndex = batchIndex;
            Duration = duration;
        }

        public void AddAction(EnactedEnemyAction action)
        {
            if (action != null)
            {
                Actions.Add(action);
            }
        }

        public bool AffectsTile(Vector2Int tile)
        {
            foreach (var action in Actions)
            {
                if (action == null) continue;
                if (action.StartTile == tile || action.EndTile == tile) return true;
                if (action.TraversedTiles != null && action.TraversedTiles.Contains(tile)) return true;
            }
            return false;
        }

        public HashSet<Vector2Int> GetAllAffectedTiles()
        {
            var tiles = new HashSet<Vector2Int>();
            foreach (var action in Actions)
            {
                if (action == null) continue;
                tiles.Add(action.StartTile);
                tiles.Add(action.EndTile);
                if (action.TraversedTiles != null)
                {
                    foreach (var t in action.TraversedTiles)
                    {
                        tiles.Add(t);
                    }
                }
            }
            return tiles;
        }

        public HashSet<int> GetAllAffectedOccupantIds()
        {
            var ids = new HashSet<int>();
            foreach (var action in Actions)
            {
                if (action == null) continue;
                if (action.Enemy != null) ids.Add(action.Enemy.Id);
                if (action.BlockerOccupantId.HasValue) ids.Add(action.BlockerOccupantId.Value);

                if (action.EmittedEffects != null)
                {
                    foreach (var effect in action.EmittedEffects)
                    {
                        if (effect.OccupantId > 0) ids.Add(effect.OccupantId);
                    }
                }
            }
            return ids;
        }

        public bool ContainsEnemy(int enemyId)
        {
            foreach (var action in Actions)
            {
                if (action?.Enemy != null && action.Enemy.Id == enemyId)
                {
                    return true;
                }
            }
            return false;
        }

        public bool ContainsEnemy(EnemyOccupant enemy)
        {
            if (enemy == null) return false;
            return ContainsEnemy(enemy.Id);
        }

        public bool HasSpatialConflictWith(EnactedEnemyAction candidate)
        {
            if (candidate == null) return false;

            foreach (var existing in Actions)
            {
                if (TurnBatchScheduler.HasSpatialConflict(existing, candidate))
                {
                    return true;
                }
            }
            return false;
        }

        public override string ToString()
        {
            return $"AnimationBatch #{BatchIndex} (Duration: {Duration:F3}s, Actions: {Actions.Count}, Effects: {AllEffects.Count})";
        }
    }
}
