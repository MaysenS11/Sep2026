using UnityEngine;
using FMODUnity;

public class PlayerStats : MonoBehaviour
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
    [SerializeField] private int[] rangeValues = new int[3] { 1, 2, 3 };
    [SerializeField] private int[] healthValues = new int[3] { 1, 2, 3 };

    [Header("Current Stats")]
    [SerializeField] private int defence = 0;
    [SerializeField] private int speed = 0;

    private readonly System.Collections.Generic.Dictionary<Chest.StatType, int> upgradeTiers = new System.Collections.Generic.Dictionary<Chest.StatType, int>();

    private Core.Occupants.PlayerOccupant _cachedOccupant;
    public Core.Occupants.PlayerOccupant Occupant
    {
        get
        {
            if (_cachedOccupant == null && GameManager.Instance != null && GameManager.Instance.Board != null)
            {
                _cachedOccupant = GameManager.Instance.Board.FindPlayer();
            }
            return _cachedOccupant;
        }
        set => _cachedOccupant = value;
    }

    public CharacterDefinition CharacterDefinition => characterDefinition;
    public int MaxHealth => Occupant != null ? Occupant.MaxHealth : maxHealth;
    public int CurrentHealth => Occupant != null ? Occupant.CurrentHealth : currentHealth;
    public int TotalAttackDamage => Occupant != null ? Occupant.TotalAttackDamage : Mathf.Max(0, baseAttackDamage + bonusAttackDamage);
    public AttackPatternData CurrentAttackPattern => (Occupant != null && Occupant.CurrentAttackPattern != null) ? Occupant.CurrentAttackPattern : (currentAttackPattern != null ? currentAttackPattern : defaultAttackPattern);
    public int CurrentPatternIndex => Occupant != null ? Occupant.CurrentPatternIndex : currentPatternIndex;
    public int Defence => Occupant != null ? Occupant.Defence : defence;
    public int Speed => speed;
    public bool IsDead => Occupant != null ? Occupant.IsDead : currentHealth <= 0;

    public void SyncFromOccupant()
    {
        if (Occupant != null)
        {
            currentHealth = Occupant.CurrentHealth;
            maxHealth = Occupant.MaxHealth;
            baseAttackDamage = Occupant.AttackDamage;
            defence = Occupant.Defence;
        }
    }

    public bool CanUpgrade(Chest.StatType statType)
    {
        if (Occupant != null) return Occupant.CanUpgrade(statType);
        if (statType == Chest.StatType.Speed) return false;
        return GetUpgradeTier(statType) < 2;
    }

    public int GetUpgradeTier(Chest.StatType statType)
    {
        if (Occupant != null) return Occupant.GetUpgradeTier(statType);
        if (upgradeTiers.TryGetValue(statType, out int tier))
        {
            return tier;
        }
        return 0;
    }

    public int[] GetTierValues(Chest.StatType statType)
    {
        if (Occupant != null)
        {
            var tierVals = Occupant.GetTierValues(statType);
            if (tierVals != null) return tierVals;
        }
        switch (statType)
        {
            case Chest.StatType.AttackDamage:
                return attackDamageValues;
            case Chest.StatType.Defence:
                return defenceValues;
            case Chest.StatType.Speed:
                return speedValues;
            case Chest.StatType.AttackRange:
                return rangeValues;
            case Chest.StatType.Health:
                return healthValues;
            default:
                return null;
        }
    }

    public int GetCurrentStatValue(Chest.StatType statType)
    {
        if (Occupant != null) return Occupant.GetCurrentStatValue(statType);
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
        if (Occupant != null)
        {
            Occupant.UpgradeStat(statType);
            SyncFromOccupant();
            return;
        }

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
            case Chest.StatType.Health:
                if (newTier < healthValues.Length)
                {
                    IncreaseMaxHealth(healthValues[newTier] - healthValues[currentTier]);
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

    [Header("Common Player Defaults")]
    [SerializeField] private int defaultStartHealth = 6;
    [SerializeField] private Color lockedTint = new Color(0.25f, 0.25f, 0.25f, 1f);

    public Color LockedTint => lockedTint;

    public void SetCharacterDefinition(CharacterDefinition definition)
    {
        characterDefinition = definition;
        upgradeTiers.Clear();
        maxHealth = defaultStartHealth;
        currentHealth = maxHealth;

        if (definition != null)
        {
            if (definition.AttackTierValues != null && definition.AttackTierValues.Length > 0)
            {
                attackDamageValues = (int[])definition.AttackTierValues.Clone();
                baseAttackDamage = attackDamageValues[0];
            }
            if (definition.DefenceTierValues != null && definition.DefenceTierValues.Length > 0)
            {
                defenceValues = (int[])definition.DefenceTierValues.Clone();
                defence = defenceValues[0];
            }
            if (definition.SpeedTierValues != null && definition.SpeedTierValues.Length > 0)
            {
                speedValues = (int[])definition.SpeedTierValues.Clone();
                speed = speedValues[0];
            }
            if (definition.RangeTierValues != null && definition.RangeTierValues.Length > 0)
            {
                rangeValues = (int[])definition.RangeTierValues.Clone();
            }
            if (definition.HealthTierValues != null && definition.HealthTierValues.Length > 0)
            {
                healthValues = (int[])definition.HealthTierValues.Clone();
            }

            AttackPatternData initialPattern = definition.GetAttackPattern(0);
            if (initialPattern != null)
            {
                SetAttackPattern(initialPattern, 0);
            }
        }

        if (Occupant != null)
        {
            Occupant.InitializeFromCharacter(definition, defaultStartHealth);
            SyncFromOccupant();
        }

        EventBus<PlayerHealthChangedEvent>.Raise(new PlayerHealthChangedEvent(currentHealth, maxHealth));
    }

    public void SetAttackPattern(AttackPatternData pattern, int index = 0)
    {
        currentAttackPattern = pattern;
        currentPatternIndex = index;
        if (Occupant != null)
        {
            Occupant.SetAttackPattern(pattern, index);
        }
    }

    public void AddBonusDamage(int amount)
    {
        bonusAttackDamage += amount;
        if (Occupant != null)
        {
            Occupant.BonusAttackDamage += amount;
        }
    }

    private bool hasPlayedLowLifeSound = false;

    public void IncreaseMaxHealth(int amount, bool healAmount = true)
    {
        if (Occupant != null)
        {
            Occupant.IncreaseMaxHealth(amount, healAmount);
            SyncFromOccupant();
            return;
        }

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
        if (Occupant != null)
        {
            Occupant.Heal(amount);
            SyncFromOccupant();
            return;
        }

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

        if (Occupant != null)
        {
            Occupant.TakeDamage(amount);
            SyncFromOccupant();
            EventBus<EntityDamagedEvent>.Raise(new EntityDamagedEvent(gameObject, source, Mathf.Max(1, amount - defence), currentHealth));
        }
        else
        {
            int actualDamage = Mathf.Max(1, amount - defence);
            currentHealth = Mathf.Max(0, currentHealth - actualDamage);
            EventBus<EntityDamagedEvent>.Raise(new EntityDamagedEvent(gameObject, source, actualDamage, currentHealth));
            EventBus<PlayerHealthChangedEvent>.Raise(new PlayerHealthChangedEvent(currentHealth, maxHealth));
        }

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
    }

    public void ResetHealth()
    {
        if (Occupant != null)
        {
            Occupant.ResetHealth();
            SyncFromOccupant();
            hasPlayedLowLifeSound = false;
            return;
        }

        currentHealth = maxHealth;
        hasPlayedLowLifeSound = false;
        EventBus<PlayerHealthChangedEvent>.Raise(new PlayerHealthChangedEvent(currentHealth, maxHealth));
    }
}
