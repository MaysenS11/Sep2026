using UnityEngine;

public class EntityStats : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private int currentHealth;
    [SerializeField] private int attackDamage = 2;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public int AttackDamage => attackDamage;
    public bool IsDead => currentHealth <= 0;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void Initialize(EnemyData data)
    {
        if (data != null)
        {
            maxHealth = data.MaxHealth;
            attackDamage = data.AttackDamage;
        }
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount, GameObject source)
    {
        if (IsDead) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        EventBus<EntityDamagedEvent>.Raise(new EntityDamagedEvent(gameObject, source, amount, currentHealth));

        if (IsDead)
        {
            Die();
        }
    }

    private void Die()
    {
        EventBus<EntityDiedEvent>.Raise(new EntityDiedEvent(gameObject));
    }
}
