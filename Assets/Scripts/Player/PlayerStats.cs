using UnityEngine;

public enum AttackPattern
{
    SingleFacing,
    SurroundingOrthogonal,
    CleaveAndSurround
}

public class PlayerStats : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int currentHealth;

    [Header("Attack Settings")]
    [SerializeField] private int baseAttackDamage = 3;
    [SerializeField] private int bonusAttackDamage = 0;
    [SerializeField] private AttackPattern attackPattern = AttackPattern.SurroundingOrthogonal;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public int TotalAttackDamage => Mathf.Max(0, baseAttackDamage + bonusAttackDamage);
    public AttackPattern CurrentAttackPattern => attackPattern;
    public bool IsDead => currentHealth <= 0;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Start()
    {
        EventBus<PlayerHealthChangedEvent>.Raise(new PlayerHealthChangedEvent(currentHealth, maxHealth));
    }

    public void SetAttackPattern(AttackPattern pattern)
    {
        attackPattern = pattern;
    }

    public void AddBonusDamage(int amount)
    {
        bonusAttackDamage += amount;
    }

    public void IncreaseMaxHealth(int amount, bool healAmount = true)
    {
        maxHealth += amount;
        if (healAmount)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        }
        EventBus<PlayerHealthChangedEvent>.Raise(new PlayerHealthChangedEvent(currentHealth, maxHealth));
    }

    public void Heal(int amount)
    {
        if (IsDead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        EventBus<PlayerHealthChangedEvent>.Raise(new PlayerHealthChangedEvent(currentHealth, maxHealth));
    }

    public void TakeDamage(int amount, GameObject source)
    {
        if (IsDead) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        EventBus<EntityDamagedEvent>.Raise(new EntityDamagedEvent(gameObject, source, amount, currentHealth));
        EventBus<PlayerHealthChangedEvent>.Raise(new PlayerHealthChangedEvent(currentHealth, maxHealth));

        if (IsDead)
        {
            Die();
        }
    }

    private void Die()
    {
        EventBus<EntityDiedEvent>.Raise(new EntityDiedEvent(gameObject));
        Debug.Log("Player has died.");
    }
}
