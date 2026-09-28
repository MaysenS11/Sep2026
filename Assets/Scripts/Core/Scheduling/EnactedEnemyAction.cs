using System;
using System.Collections.Generic;
using Core.AI;
using Core.Board;
using Core.Effects;
using Core.Occupants;
using UnityEngine;

namespace Core.Scheduling
{
    public class EnactedEnemyAction
    {
        public EnemyOccupant Enemy { get; }
        public EnemyIntent Intent { get; }
        public List<BoardEffect> EmittedEffects { get; }
        public HashSet<Vector2Int> TraversedTiles { get; }
        public Vector2Int StartTile { get; }
        public Vector2Int EndTile { get; }
        public Vector2Int? BlockedTile { get; }
        public int? BlockerOccupantId { get; }

        public EnactedEnemyAction(
            EnemyOccupant enemy,
            EnemyIntent intent,
            List<BoardEffect> emittedEffects = null,
            Vector2Int? startTile = null,
            Vector2Int? endTile = null,
            HashSet<Vector2Int> traversedTiles = null,
            Vector2Int? blockedTile = null,
            int? blockerOccupantId = null)
        {
            Enemy = enemy;
            Intent = intent;
            EmittedEffects = emittedEffects ?? new List<BoardEffect>();

            if (startTile.HasValue)
            {
                StartTile = startTile.Value;
            }
            else if (intent?.Path != null && intent.Path.Count > 0)
            {
                StartTile = intent.Path[0];
            }
            else if (enemy != null)
            {
                StartTile = enemy.GridPosition;
            }
            else
            {
                StartTile = Vector2Int.zero;
            }

            if (endTile.HasValue)
            {
                EndTile = endTile.Value;
            }
            else if (intent != null)
            {
                EndTile = intent.TargetPosition;
            }
            else
            {
                EndTile = StartTile;
            }

            BlockedTile = blockedTile;
            BlockerOccupantId = blockerOccupantId;

            if (EmittedEffects != null)
            {
                foreach (var effect in EmittedEffects)
                {
                    if (effect is BlockedPushRecoilEffect recoil)
                    {
                        if (!BlockedTile.HasValue && recoil.BlockerPos != default)
                        {
                            BlockedTile = recoil.BlockerPos;
                        }
                        if (!BlockerOccupantId.HasValue && recoil.BlockerOccupantId > 0)
                        {
                            BlockerOccupantId = recoil.BlockerOccupantId;
                        }
                        if (!endTile.HasValue)
                        {
                            EndTile = recoil.RecoilEndTile;
                        }
                        if (!startTile.HasValue)
                        {
                            StartTile = recoil.AttackerStartPos;
                        }
                    }
                }
            }

            if (traversedTiles != null)
            {
                TraversedTiles = new HashSet<Vector2Int>(traversedTiles);
            }
            else
            {
                TraversedTiles = new HashSet<Vector2Int>();
                ComputeTraversedTiles();
            }
        }

        private void ComputeTraversedTiles()
        {
            TraversedTiles.Add(StartTile);
            TraversedTiles.Add(EndTile);

            bool isKnight = (Enemy != null && Enemy.Archetype == EnemyArchetype.Knight) ||
                            (Intent != null && Intent.IntentType == IntentType.Jump) ||
                            BoardCoordinate.IsKnightOffset(EndTile - StartTile);

            if (isKnight)
            {
                if (Intent != null)
                {
                    if (Intent.IntentType == IntentType.AttackPush || Intent.IntentType == IntentType.BlockedPush)
                    {
                        TraversedTiles.Add(Intent.TargetPosition); // Player's tile
                        if (BlockedTile.HasValue)
                        {
                            TraversedTiles.Add(BlockedTile.Value);
                        }
                        if (Intent.PushDirection != Vector2Int.zero)
                        {
                            TraversedTiles.Add(Intent.TargetPosition + Intent.PushDirection);
                        }
                    }
                }
            }
            else
            {
                if (Intent?.Path != null && Intent.Path.Count > 0)
                {
                    foreach (var pt in Intent.Path)
                    {
                        TraversedTiles.Add(pt);
                    }
                }
                else
                {
                    Vector2Int delta = EndTile - StartTile;
                    int steps = Math.Max(Math.Abs(delta.x), Math.Abs(delta.y));
                    if (steps > 0 && (delta.x == 0 || delta.y == 0 || Math.Abs(delta.x) == Math.Abs(delta.y)))
                    {
                        Vector2Int stepDir = BoardCoordinate.GetSignVector(delta);
                        Vector2Int current = StartTile;
                        for (int i = 0; i <= steps; i++)
                        {
                            TraversedTiles.Add(current);
                            current += stepDir;
                        }
                    }
                }

                if (Intent != null)
                {
                    if (Intent.IntentType == IntentType.AttackPush || Intent.IntentType == IntentType.BlockedPush)
                    {
                        TraversedTiles.Add(Intent.TargetPosition); // Player tile
                        if (BlockedTile.HasValue)
                        {
                            TraversedTiles.Add(BlockedTile.Value);
                        }
                        if (Intent.PushDirection != Vector2Int.zero)
                        {
                            TraversedTiles.Add(Intent.TargetPosition + Intent.PushDirection);
                        }
                    }
                }
            }

            if (EmittedEffects != null)
            {
                foreach (var effect in EmittedEffects)
                {
                    if (effect is MoveEffect move && move.Path != null)
                    {
                        foreach (var p in move.Path) TraversedTiles.Add(p);
                    }
                    else if (effect is PushDisplacementEffect push)
                    {
                        TraversedTiles.Add(push.StartPos);
                        TraversedTiles.Add(push.EndPos);
                    }
                    else if (effect is AttackLungeEffect lunge)
                    {
                        TraversedTiles.Add(lunge.StartPos);
                        TraversedTiles.Add(lunge.TargetPos);
                    }
                    else if (effect is BlockedPushRecoilEffect recoil)
                    {
                        TraversedTiles.Add(recoil.AttackerStartPos);
                        TraversedTiles.Add(recoil.PlayerTile);
                        TraversedTiles.Add(recoil.RecoilEndTile);
                        if (recoil.BlockerPos != default) TraversedTiles.Add(recoil.BlockerPos);
                    }
                }
            }
        }

        public override string ToString()
        {
            string blockerStr = BlockedTile.HasValue ? $" Blocker={BlockedTile} (ID={BlockerOccupantId})" : "";
            return $"EnactedEnemyAction: Enemy={Enemy?.Id} ({Enemy?.Archetype}) Start={StartTile} End={EndTile}{blockerStr} TraversedCount={TraversedTiles.Count} Effects={EmittedEffects.Count}";
        }
    }
}
