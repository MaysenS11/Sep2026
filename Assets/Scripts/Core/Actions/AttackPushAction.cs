using System;
using System.Collections.Generic;
using Core.Board;
using Core.Effects;
using Core.Occupants;
using UnityEngine;

namespace Core.Actions
{
    /// Handles an enemy attacking the player when the push destination behind the player is clear.
    /// Pushes the player to the clear destination, inflicts attack damage, and moves the attacking
    /// enemy into the player's former tile.
    public class AttackPushAction : IBoardAction
    {
        public TileOccupant Attacker { get; }
        public PlayerOccupant Player { get; }
        public Vector2Int PushDirection { get; }
        public int Damage { get; }

        public AttackPushAction(TileOccupant attacker, PlayerOccupant player, Vector2Int pushDirection = default, int damage = -1)
        {
            Attacker = attacker ?? throw new ArgumentNullException(nameof(attacker));
            Player = player ?? throw new ArgumentNullException(nameof(player));

            if (pushDirection == Vector2Int.zero)
            {
                PushDirection = BoardCoordinate.GetDominantCardinalDirection(attacker.GridPosition, player.GridPosition);
            }
            else
            {
                PushDirection = pushDirection;
            }

            if (damage > 0)
            {
                Damage = damage;
            }
            else if (attacker is EnemyOccupant enemy)
            {
                Damage = enemy.AttackDamage;
            }
            else
            {
                Damage = 1;
            }
        }

        public List<BoardEffect> Execute(GameBoard board)
        {
            var effects = new List<BoardEffect>();
            if (board == null || Attacker == null || Player == null) return effects;

            Vector2Int playerTile = Player.GridPosition;
            Vector2Int attackerStart = Attacker.GridPosition;
            Vector2Int pushDir = PushDirection != Vector2Int.zero ? PushDirection : BoardCoordinate.GetDominantCardinalDirection(attackerStart, playerTile);
            Vector2Int pushDest = playerTile + pushDir;

            if (!board.CanEnter(pushDest))
            {
                return effects;
            }

            board.Move(Player, pushDest);

            var damageEffects = Player.TakeDamage(Damage, Attacker);
            effects.AddRange(damageEffects);

            if (Player.IsDead)
            {
                board.Remove(Player);
            }

            board.Move(Attacker, playerTile);

            effects.Add(new PushDisplacementEffect(Player.Id, playerTile, pushDest, pushDir));
            effects.Add(new AttackLungeEffect(Attacker.Id, playerTile, pushDir, attackerStart));
            effects.Add(new MoveEffect(Attacker.Id, new[] { attackerStart, playerTile }));

            return effects;
        }

        public override string ToString()
        {
            return $"AttackPushAction: Attacker {Attacker?.Id} attacking Player {Player?.Id} (PushDir: {PushDirection}, Dmg: {Damage})";
        }
    }
}
