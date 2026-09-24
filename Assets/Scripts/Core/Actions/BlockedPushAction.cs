using System;
using System.Collections.Generic;
using Core.Board;
using Core.Effects;
using Core.Occupants;
using UnityEngine;

namespace Core.Actions
{
    /// Handles an enemy attacking the player when the push destination behind the player is blocked.
    /// 
    /// Resolution rules:
    /// 1. Player does NOT move.
    /// 2. Attacking enemy CANNOT enter player's tile.
    /// 3. Attacking enemy takes 1 recoil damage (emits DamageTakenEffect; if dead emits OccupantDestroyedEffect and removes enemy from board).
    /// 4. Repositioning:
    ///    - Standard enemies (Pawn, Rook, Bishop, Queen): stops 1 tile short of player along approach path (stays at origin if already adjacent).
    ///    - Knight: bounces back to its original starting tile.
    /// 5. Emits BlockedPushRecoilEffect with complete clash/blocker telemetry.
    public class BlockedPushAction : IBoardAction
    {
        public TileOccupant Attacker { get; }
        public PlayerOccupant Player { get; }
        public Vector2Int PushDirection { get; }
        public int RecoilDamage { get; }

        private readonly Vector2Int? _explicitBlockerPos;
        private readonly int _explicitBlockerOccupantId;

        public BlockedPushAction(
            TileOccupant attacker,
            PlayerOccupant player,
            Vector2Int pushDirection = default,
            int recoilDamage = 1,
            Vector2Int? blockerPos = null,
            int blockerOccupantId = 0)
        {
            Attacker = attacker ?? throw new ArgumentNullException(nameof(attacker));
            Player = player ?? throw new ArgumentNullException(nameof(player));
            RecoilDamage = Math.Max(1, recoilDamage);
            _explicitBlockerPos = blockerPos;
            _explicitBlockerOccupantId = blockerOccupantId;

            if (pushDirection == Vector2Int.zero)
            {
                PushDirection = BoardCoordinate.GetDominantCardinalDirection(attacker.GridPosition, player.GridPosition);
            }
            else
            {
                PushDirection = pushDirection;
            }
        }

        public List<BoardEffect> Execute(GameBoard board)
        {
            var effects = new List<BoardEffect>();
            if (board == null || Attacker == null || Player == null) return effects;

            Vector2Int playerTile = Player.GridPosition;
            Vector2Int attackerStart = Attacker.GridPosition;
            Vector2Int pushDir = PushDirection != Vector2Int.zero
                ? PushDirection
                : BoardCoordinate.GetDominantCardinalDirection(attackerStart, playerTile);

            // Resolve blocker tile and occupant
            Vector2Int blockerPos = _explicitBlockerPos ?? (playerTile + pushDir);
            int blockerOccupantId = _explicitBlockerOccupantId;
            if (blockerOccupantId == 0 && board.IsOccupied(blockerPos))
            {
                var blocker = board.GetOccupant(blockerPos);
                if (blocker != null)
                {
                    blockerOccupantId = blocker.Id;
                }
            }

            bool isKnight = (Attacker is EnemyOccupant enemy && enemy.Archetype == EnemyArchetype.Knight) ||
                            BoardCoordinate.IsKnightOffset(playerTile - attackerStart);

            Vector2Int recoilEndTile;
            if (isKnight)
            {
                recoilEndTile = attackerStart;
            }
            else
            {
                Vector2Int delta = playerTile - attackerStart;
                Vector2Int approachStep = BoardCoordinate.GetSignVector(delta);
                recoilEndTile = playerTile - approachStep;

                if (recoilEndTile != attackerStart && !board.CanEnter(recoilEndTile))
                {
                    recoilEndTile = attackerStart;
                }
            }

            effects.Add(new BlockedPushRecoilEffect(
                attackerId: Attacker.Id,
                attackerStartPos: attackerStart,
                playerTile: playerTile,
                recoilEndTile: recoilEndTile,
                recoilDamage: RecoilDamage,
                blockerPos: blockerPos,
                blockerOccupantId: blockerOccupantId
            ));

            if (recoilEndTile != attackerStart)
            {
                board.Move(Attacker, recoilEndTile);
            }

            var damageEffects = Attacker.TakeDamage(RecoilDamage, Player);
            effects.AddRange(damageEffects);

            if (Attacker.IsDead)
            {
                board.Remove(Attacker);
            }

            return effects;
        }

        public override string ToString()
        {
            return $"BlockedPushAction: Attacker {Attacker?.Id} recoiled from Player {Player?.Id} (RecoilEnd: {PushDirection}, Dmg: {RecoilDamage})";
        }
    }
}
