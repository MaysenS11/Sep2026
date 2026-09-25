using System.Collections.Generic;
using Core.Board;
using Core.Effects;
using UnityEngine;

namespace Core.Occupants
{
    /// Pure C# authoritative data model for the Player unit on the GameBoard.
    /// Single source of truth for runtime stats (Attack Damage, Defence, Attack Pattern/Range, Current & Max Health).
    /// 
    /// Key Property:
    /// Player is the ONLY TileOccupant in the entire game with IsPushable = true.
    /// All other occupants (enemies, chests, props, obstacles) have IsPushable = false.
    public class PlayerOccupant : TileOccupant
    {
        public Vector2Int FacingDirection { get; set; } = BoardCoordinate.North;
        public int AttackDamage { get; set; }
        public int BonusAttackDamage { get; set; }
        public int TotalAttackDamage => System.Math.Max(0, AttackDamage + BonusAttackDamage);

        public int Defence { get; set; }
        public int Keys { get; set; }

        public AttackPatternData CurrentAttackPattern { get; set; }
        public int CurrentPatternIndex { get; set; }
        public CharacterDefinition CharacterDefinition { get; private set; }
        public WeaponType WeaponType => CharacterDefinition != null ? CharacterDefinition.Weapon : WeaponType.Degen;

        private int[] _attackDamageValues = new int[3] { 3, 4, 5 };
        private int[] _defenceValues = new int[3] { 0, 1, 2 };
        private int[] _rangeValues = new int[3] { 1, 2, 3 };
        private int[] _healthValues = new int[3] { 6, 8, 10 };

        private readonly Dictionary<Chest.StatType, int> _upgradeTiers = new Dictionary<Chest.StatType, int>();

        public PlayerOccupant(
            int maxHealth = 6,
            int attackDamage = 2,
            Vector2Int initialPosition = default,
            int id = 0,
            string name = "Player")
            : base(maxHealth, initialPosition, id, name)
        {
            AttackDamage = attackDamage;
            Defence = 0;
            // CRITICAL: Player is the sole pushable occupant on the board!
            IsPushable = true;
        }

        /// Updates the player's facing direction based on movement or manual turning.
        public void SetFacingDirection(Vector2Int direction)
        {
            if (direction != Vector2Int.zero)
            {
                FacingDirection = BoardCoordinate.GetSignVector(direction);
            }
        }

        public void InitializeFromCharacter(CharacterDefinition definition, int startHealth = 6)
        {
            CharacterDefinition = definition;
            _upgradeTiers.Clear();
            MaxHealth = startHealth;
            CurrentHealth = MaxHealth;

            if (definition != null)
            {
                if (definition.AttackTierValues != null && definition.AttackTierValues.Length > 0)
                {
                    _attackDamageValues = (int[])definition.AttackTierValues.Clone();
                    AttackDamage = _attackDamageValues[0];
                }
                if (definition.DefenceTierValues != null && definition.DefenceTierValues.Length > 0)
                {
                    _defenceValues = (int[])definition.DefenceTierValues.Clone();
                    Defence = _defenceValues[0];
                }
                if (definition.RangeTierValues != null && definition.RangeTierValues.Length > 0)
                {
                    _rangeValues = (int[])definition.RangeTierValues.Clone();
                }
                if (definition.HealthTierValues != null && definition.HealthTierValues.Length > 0)
                {
                    _healthValues = (int[])definition.HealthTierValues.Clone();
                }

                AttackPatternData initialPattern = definition.GetAttackPattern(0);
                if (initialPattern != null)
                {
                    SetAttackPattern(initialPattern, 0);
                }
            }

            EventBus<PlayerHealthChangedEvent>.Raise(new PlayerHealthChangedEvent(CurrentHealth, MaxHealth));
        }

        public void SetAttackPattern(AttackPatternData pattern, int index = 0)
        {
            CurrentAttackPattern = pattern;
            CurrentPatternIndex = index;
        }

        public bool CanUpgrade(Chest.StatType statType)
        {
            if (statType == Chest.StatType.Speed) return false;
            return GetUpgradeTier(statType) < 2;
        }

        public int GetUpgradeTier(Chest.StatType statType)
        {
            if (_upgradeTiers.TryGetValue(statType, out int tier))
            {
                return tier;
            }
            return 0;
        }

        public int[] GetTierValues(Chest.StatType statType)
        {
            switch (statType)
            {
                case Chest.StatType.AttackDamage:
                    return _attackDamageValues;
                case Chest.StatType.Defence:
                    return _defenceValues;
                case Chest.StatType.AttackRange:
                    return _rangeValues;
                case Chest.StatType.Health:
                    return _healthValues;
                default:
                    return null;
            }
        }

        public int GetCurrentStatValue(Chest.StatType statType)
        {
            int tier = Mathf.Clamp(GetUpgradeTier(statType), 0, 2);
            int[] values = GetTierValues(statType);
            if (values != null && tier < values.Length)
            {
                return Mathf.Clamp(values[tier], 1, 5);
            }
            return 1;
        }

        public void UpgradeStat(Chest.StatType statType)
        {
            if (!CanUpgrade(statType)) return;

            int currentTier = GetUpgradeTier(statType);
            int newTier = currentTier + 1;
            _upgradeTiers[statType] = newTier;

            switch (statType)
            {
                case Chest.StatType.AttackDamage:
                    if (newTier < _attackDamageValues.Length)
                    {
                        AttackDamage = _attackDamageValues[newTier];
                    }
                    break;
                case Chest.StatType.Defence:
                    if (newTier < _defenceValues.Length)
                    {
                        Defence = _defenceValues[newTier];
                    }
                    break;
                case Chest.StatType.AttackRange:
                    if (CharacterDefinition != null)
                    {
                        AttackPatternData newPattern = CharacterDefinition.GetAttackPattern(newTier);
                        if (newPattern != null)
                        {
                            SetAttackPattern(newPattern, newTier);
                        }
                    }
                    break;
                case Chest.StatType.Health:
                    if (newTier < _healthValues.Length)
                    {
                        int diff = _healthValues[newTier] - _healthValues[currentTier];
                        IncreaseMaxHealth(Mathf.Max(1, diff));
                    }
                    break;
            }

            EventBus<StatUpgradeAppliedEvent>.Raise(new StatUpgradeAppliedEvent(statType, newTier));
        }

        public void IncreaseMaxHealth(int amount, bool healAmount = true)
        {
            if (amount <= 0) return;
            MaxHealth += amount;
            if (healAmount)
            {
                CurrentHealth = System.Math.Min(MaxHealth, CurrentHealth + amount);
            }
            EventBus<PlayerHealthChangedEvent>.Raise(new PlayerHealthChangedEvent(CurrentHealth, MaxHealth));
        }

        public override void Heal(int amount)
        {
            if (IsDead || amount <= 0) return;
            base.Heal(amount);
            EventBus<PlayerHealthChangedEvent>.Raise(new PlayerHealthChangedEvent(CurrentHealth, MaxHealth));
        }

        public void ResetHealth()
        {
            CurrentHealth = MaxHealth;
            EventBus<PlayerHealthChangedEvent>.Raise(new PlayerHealthChangedEvent(CurrentHealth, MaxHealth));
        }

        public override List<BoardEffect> TakeDamage(int damage, TileOccupant source = null)
        {
            var effects = new List<BoardEffect>();
            if (IsDead || damage <= 0) return effects;

            int actualDamage = System.Math.Max(1, damage - Defence);
            effects = base.TakeDamage(actualDamage, source);

            EventBus<PlayerHealthChangedEvent>.Raise(new PlayerHealthChangedEvent(CurrentHealth, MaxHealth));

            if (IsDead)
            {
                EventBus<EntityDiedEvent>.Raise(new EntityDiedEvent(null));
            }

            return effects;
        }
    }
}
