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
    [SerializeField] private CharacterDefinition characterDefinition;

    [Header("Configurable Stat Upgrade Values")]
    [Tooltip("Values applied at Base (0), Tier 1, and Tier 2")]
    [SerializeField] private int[] attackDamageValues = new int[3] { 3, 4, 5 };
    [SerializeField] private int[] defenceValues = new int[3] { 0, 1, 2 };
    [SerializeField] private int[] speedValues = new int[3] { 0, 1, 2 };

    [Header("Current Stats")]
    [SerializeField] private int defence = 0;
    [SerializeField] private int speed = 0;

    private readonly System.Collections.Generic.Dictionary<Chest.StatType, int> upgradeTiers = new System.Collections.Generic.Dictionary<Chest.StatType, int>();

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public int TotalAttackDamage => Mathf.Max(0, baseAttackDamage + bonusAttackDamage);
    public AttackPatternData CurrentAttackPattern => currentAttackPattern != null ? currentAttackPattern : defaultAttackPattern;
    public int CurrentPatternIndex => currentPatternIndex;
    public int Defence => defence;
    public int Speed => speed;
    public bool IsDead => currentHealth <= 0;

    public bool CanUpgrade(Chest.StatType statType)
    {
        return GetUpgradeTier(statType) < 2;
    }

    public int GetUpgradeTier(Chest.StatType statType)
    {
        if (upgradeTiers.TryGetValue(statType, out int tier))
        {
            return tier;
        }
        return 0;
    }

    public void UpgradeStat(Chest.StatType statType)
    {
        int currentTier = GetUpgradeTier(statType);
        if (currentTier >= 2) return;

        int newTier = currentTier + 1;
        upgradeTiers[statType] = newTier;

        switch (statType)
        {
            case Chest.StatType.AttackDamage:
                if (newTier < attackDamageValues.Length)
                {
                    baseAttackDamage = attackDamageValues[newTier];
                }
                break;
            case Chest.StatType.Defence:
                if (newTier < defenceValues.Length)
                {
                    defence = defenceValues[newTier];
                }
                break;
            case Chest.StatType.Speed:
                if (newTier < speedValues.Length)
                {
                    speed = speedValues[newTier];
                }
                break;
            case Chest.StatType.AttackRange:
                if (characterDefinition != null)
                {
                    AttackPatternData newPattern = characterDefinition.GetAttackPattern(newTier);
                    if (newPattern != null)
                    {
                        SetAttackPattern(newPattern, newTier);
                    }
                }
                break;
        }

        EventBus<StatUpgradeAppliedEvent>.Raise(new StatUpgradeAppliedEvent(statType, newTier));
    }

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

        int actualDamage = Mathf.Max(1, amount - defence);
        currentHealth = Mathf.Max(0, currentHealth - actualDamage);
        EventBus<EntityDamagedEvent>.Raise(new EntityDamagedEvent(gameObject, source, actualDamage, currentHealth));
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
