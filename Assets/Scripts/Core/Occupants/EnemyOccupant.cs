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
        Queen,
        King
    }

    /// Pure C# authoritative data model for an enemy unit on the GameBoard.
    /// Holds piece archetype, combat stats, and turn state.
    public class EnemyOccupant : TileOccupant
    {
        public EnemyArchetype Archetype { get; }
        public int AttackDamage { get; set; }
        public int MovePriority { get; set; }
        public int DetectionRange { get; set; }
        public int MaxLineSteps { get; set; }
        public bool UsesDiagonalAttack { get; set; }
        public EnemySmartness IntelligenceLevel { get; set; }
        public bool IsImmobile { get; set; }
        public bool ImmuneToDirectAttacks { get; set; }

        /// If true, this enemy skips its next action (e.g. from damage stun or recoil).
        public bool SkipNextTurn { get; set; }

        public EnemyOccupant(
            EnemyArchetype archetype,
            int maxHealth = 4,
            int attackDamage = 1,
            int movePriority = 0,
            int detectionRange = 6,
            int maxLineSteps = 3,
            bool usesDiagonalAttack = false,
            Vector2Int initialPosition = default,
            int id = 0,
            string name = null,
            EnemySmartness? intelligenceLevel = null,
            bool isImmobile = false,
            bool immuneToDirectAttacks = false)
            : base(maxHealth, initialPosition, id, name ?? archetype.ToString())
        {
            Archetype = archetype;
            AttackDamage = attackDamage;
            MovePriority = movePriority;
            DetectionRange = detectionRange;
            MaxLineSteps = maxLineSteps;
            UsesDiagonalAttack = (archetype == EnemyArchetype.Pawn) || usesDiagonalAttack;
            SkipNextTurn = false;

            if (intelligenceLevel.HasValue)
            {
                IntelligenceLevel = intelligenceLevel.Value;
            }
            else
            {
                switch (archetype)
                {
                    case EnemyArchetype.Pawn:
                        IntelligenceLevel = EnemySmartness.Dumb;
                        break;
                    case EnemyArchetype.Knight:
                    case EnemyArchetype.Bishop:
                        IntelligenceLevel = EnemySmartness.Mid;
                        break;
                    case EnemyArchetype.Rook:
                    case EnemyArchetype.Queen:
                        IntelligenceLevel = EnemySmartness.Smart;
                        break;
                    case EnemyArchetype.King:
                        IntelligenceLevel = EnemySmartness.Dumb;
                        break;
                    default:
                        IntelligenceLevel = EnemySmartness.Mid;
                        break;
                }
            }

            IsImmobile = (archetype == EnemyArchetype.King) || isImmobile;
            ImmuneToDirectAttacks = (archetype == EnemyArchetype.King) || immuneToDirectAttacks;

            // Enemies are NEVER pushable
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
