using System;
using System.Collections.Generic;
using Core.Effects;
using UnityEngine;

namespace Core.Occupants
{
    public abstract class TileOccupant
    {
        private static int _nextId = 1;
        public int Id { get; }
        public Vector2Int GridPosition { get; set; }
        public int CurrentHealth { get; protected set; }
        public int MaxHealth { get; protected set; }
        public bool IsPushable { get; protected set; }
        public virtual bool IsDead => CurrentHealth <= 0;
        public string Name { get; protected set; }
        public event Action<TileOccupant, int, TileOccupant> OnDamaged;
        public event Action<TileOccupant> OnDied;

        protected TileOccupant(int maxHealth, Vector2Int initialPosition = default, int id = 0, string name = null)
        {
            Id = id > 0 ? id : _nextId++;
            MaxHealth = Math.Max(1, maxHealth);
            CurrentHealth = MaxHealth;
            GridPosition = initialPosition;
            IsPushable = false;
            Name = name ?? GetType().Name;
        }

        public virtual List<BoardEffect> TakeDamage(int damage, TileOccupant source = null)
        {
            var effects = new List<BoardEffect>();
            if (IsDead || damage <= 0) return effects;

            int applied = Math.Min(damage, CurrentHealth);
            CurrentHealth -= applied;

            effects.Add(new DamageTakenEffect(Id, applied, CurrentHealth, source != null ? source.Id : 0));

            OnDamaged?.Invoke(this, applied, source);

            if (IsDead)
            {
                effects.Add(new OccupantDestroyedEffect(Id, GridPosition, GetType().Name));
                OnDied?.Invoke(this);
            }

            return effects;
        }

        public virtual void Heal(int amount)
        {
            if (IsDead || amount <= 0) return;
            CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);
        }

        public static void ResetIdCounter(int startingId = 1)
        {
            _nextId = startingId;
        }

        public override string ToString()
        {
            return $"{Name} [ID:{Id}] at {GridPosition} (HP: {CurrentHealth}/{MaxHealth})";
        }
    }
}
