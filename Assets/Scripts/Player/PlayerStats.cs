using UnityEngine;
using FMODUnity;

public class PlayerStats : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int currentHealth;

    [Header("Attack Settings")]
    [SerializeField] private int baseAttackDamage = 3;
    [SerializeField] private int bonusAttackDamage = 0;
    [SerializeField] private AttackPatternData defaultAttackPattern;

    private AttackPatternData currentAttackPattern;
    private int currentPatternIndex = 0;
    private EventReference lowLifeSound;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public int TotalAttackDamage => Mathf.Max(0, baseAttackDamage + bonusAttackDamage);
    public AttackPatternData CurrentAttackPattern => currentAttackPattern != null ? currentAttackPattern : defaultAttackPattern;
    public int CurrentPatternIndex => currentPatternIndex;
    public bool IsDead => currentHealth <= 0;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void OnEnable()
    {
        EventBus<SharedAudioConfiguredEvent>.Subscribe(OnSharedAudioConfigured);
    }

    private void OnDisable()
    {
        EventBus<SharedAudioConfiguredEvent>.Unsubscribe(OnSharedAudioConfigured);
    }

    private void OnSharedAudioConfigured(SharedAudioConfiguredEvent evt)
    {
        lowLifeSound = evt.LowLifeSound;
    }

    private void Start()
    {
        EventBus<PlayerHealthChangedEvent>.Raise(new PlayerHealthChangedEvent(currentHealth, maxHealth));
        if (lowLifeSound.IsNull)
        {
            EventBus<RequestSharedAudioEvent>.Raise(new RequestSharedAudioEvent());
        }
    }

    public void SetAttackPattern(AttackPatternData pattern, int index = 0)
    {
        currentAttackPattern = pattern;
        currentPatternIndex = index;
    }

    public void AddBonusDamage(int amount)
    {
        bonusAttackDamage += amount;
    }

    private bool hasPlayedLowLifeSound = false;

    public void IncreaseMaxHealth(int amount, bool healAmount = true)
    {
        maxHealth += amount;
        if (healAmount)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        }
        if (currentHealth > 4)
        {
            hasPlayedLowLifeSound = false;
        }
        EventBus<PlayerHealthChangedEvent>.Raise(new PlayerHealthChangedEvent(currentHealth, maxHealth));
    }

    public void Heal(int amount)
    {
        if (IsDead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        if (currentHealth > 4)
        {
            hasPlayedLowLifeSound = false;
        }
        EventBus<PlayerHealthChangedEvent>.Raise(new PlayerHealthChangedEvent(currentHealth, maxHealth));
    }

    public void TakeDamage(int amount, GameObject source)
    {
        if (IsDead) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        EventBus<EntityDamagedEvent>.Raise(new EntityDamagedEvent(gameObject, source, amount, currentHealth));
        EventBus<PlayerHealthChangedEvent>.Raise(new PlayerHealthChangedEvent(currentHealth, maxHealth));

        if (!IsDead && currentHealth <= 4)
        {
            if (!hasPlayedLowLifeSound)
            {
                hasPlayedLowLifeSound = true;
                if (!lowLifeSound.IsNull)
                {
                    RuntimeManager.PlayOneShot(lowLifeSound);
                }
                else
                {
                    Debug.LogWarning("[PlayerStats] Low life sound is not assigned.");
                }
            }
        }

        if (IsDead)
        {
            Die();
        }
    }

    private void Die()
    {
        hasPlayedLowLifeSound = false;
        EventBus<EntityDiedEvent>.Raise(new EntityDiedEvent(gameObject));
        Debug.Log("Player has died.");
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        hasPlayedLowLifeSound = false;
        EventBus<PlayerHealthChangedEvent>.Raise(new PlayerHealthChangedEvent(currentHealth, maxHealth));
    }
}
