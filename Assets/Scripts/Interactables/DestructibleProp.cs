using UnityEngine;

public class DestructibleProp : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 1;
    [SerializeField] private int currentHealth;
    [SerializeField] private GameObject breakEffectPrefab;
    [SerializeField] private float destroyDelay = 0.05f;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0;

    private bool _isDestroyed = false;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount, GameObject source)
    {
        if (_isDestroyed) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        EventBus<EntityDamagedEvent>.Raise(new EntityDamagedEvent(gameObject, source, amount, currentHealth));

        if (IsDead)
        {
            Break();
        }
    }

    private void Break()
    {
        if (_isDestroyed) return;
        _isDestroyed = true;

        if (TryGetComponent<Collider2D>(out var col))
        {
            col.enabled = false;
        }

        EventBus<EntityDiedEvent>.Raise(new EntityDiedEvent(gameObject));
        EventBus<PropDestroyedEvent>.Raise(new PropDestroyedEvent(gameObject));

        if (breakEffectPrefab != null)
        {
            Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject, destroyDelay);
    }
}
