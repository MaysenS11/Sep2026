using System;
using System.Collections.Generic;
using Core.Effects;
using UnityEngine;

namespace Core.Occupants
{
    /// Chess piece archetypes governing enemy movement rules and targeting.
    public enum EnemyArchetype
    {
        Pawn,
        Knight,
        Bishop,
        Rook,
        Queen
    }

    /// Pure C# authoritative data model for an enemy unit on the GameBoard.
    /// Holds piece archetype, combat stats, and turn state.
    public class EnemyOccupant : TileOccupant
    {
        public EnemyArchetype Archetype { get; }
        public int AttackDamage { get; set; }
        public int MovePriority { get; set; }
        public int MaxLineSteps { get; set; }
        public bool UsesDiagonalAttack => Archetype == EnemyArchetype.Pawn;
        public EnemySmartness IntelligenceLevel { get; set; }
        public EnemySmartness Smartness
        {
            get => IntelligenceLevel;
            set => IntelligenceLevel = value;
        }
        public bool IsBoss { get; set; }
        public bool IsImmobile
        {
            get => IsBoss;
            set => IsBoss = value;
        }
        public bool ImmuneToDirectAttacks
        {
            get => IsBoss;
            set => IsBoss = value;
        }

        public bool SkipNextTurn { get; set; }

        public EnemyOccupant(
            EnemyArchetype archetype,
            int maxHealth = 4,
            int attackDamage = 1,
            int movePriority = 0,
            int maxLineSteps = -1,
            Vector2Int initialPosition = default,
            int id = 0,
            string name = null,
            EnemySmartness intelligenceLevel = EnemySmartness.Mid,
            bool isBoss = false,
            bool isImmobile = false,
            bool immuneToDirectAttacks = false)
            : base(maxHealth, initialPosition, id, name ?? archetype.ToString())
        {
            Archetype = archetype;
            AttackDamage = attackDamage;
            MovePriority = movePriority;
            MaxLineSteps = maxLineSteps >= 0 ? maxLineSteps : (archetype == EnemyArchetype.Queen ? 7 : (archetype == EnemyArchetype.Pawn ? 1 : 3));
            SkipNextTurn = false;
            IntelligenceLevel = intelligenceLevel;
            IsBoss = isBoss || isImmobile || immuneToDirectAttacks;

            IsPushable = false;
        }

        /// When damaged, enemies are stunned for their next turn.
        public override List<BoardEffect> TakeDamage(int damage, TileOccupant source = null)
        {
            var effects = base.TakeDamage(damage, source);

            if (!IsDead)
            {
                SkipNextTurn = true;
            }

            return effects;
        }

        /// Consumes the skip turn flag if active and returns true if turn was skipped.
        public bool ConsumeTurnSkip()
        {
            if (SkipNextTurn)
            {
                SkipNextTurn = false;
                return true;
            }
            return false;
        }
    }
}
