using UnityEngine;
using FMODUnity;

[CreateAssetMenu(fileName = "CharacterDefinition", menuName = "Game/Character Definition")]
public class CharacterDefinition : ScriptableObject
{
    [SerializeField] private string characterName;
    [SerializeField] private Sprite maskSprite;
    [SerializeField] private Sprite maskWhiteSprite;
    [SerializeField] private Sprite fullBodySprite;
    [SerializeField] private bool lockedByDefault = true;

    [Header("Audio")]
    [SerializeField] private EventReference attackSound;

    [Header("Stats Configuration")]
    [SerializeField] private int[] attackTierValues;

    [SerializeField] private int[] defenceTierValues;

    [SerializeField] private int[] healthTierValues;

    [SerializeField] private int[] rangeTierValues;

    [SerializeField] private int[] speedTierValues;

    
    [Header("Attack Patterns")]
    [SerializeField] private AttackPatternData[] attackPatterns = new AttackPatternData[3];

    [Header("Weapon Configuration")]
    [SerializeField] private Core.Effects.WeaponType weaponType = Core.Effects.WeaponType.Degen;

    public string CharacterName => characterName;
    public Sprite MaskSprite => maskSprite;
    public Sprite MaskWhiteSprite => maskWhiteSprite;
    public Sprite FullBodySprite => fullBodySprite;
    public bool LockedByDefault => lockedByDefault;
    public EventReference AttackSound => attackSound;
    public Core.Effects.WeaponType Weapon
    {
        get
        {
            if (weaponType != Core.Effects.WeaponType.Degen) return weaponType;
            if (string.IsNullOrEmpty(characterName)) return Core.Effects.WeaponType.Degen;
            string lower = characterName.ToLowerInvariant();
            if (lower.Contains("flask") || lower.Contains("raven") || lower.Contains("alchemist")) return Core.Effects.WeaponType.Flask;
            if (lower.Contains("pistol") || lower.Contains("gun") || lower.Contains("ranger") || lower.Contains("revolver")) return Core.Effects.WeaponType.Pistol;
            if (lower.Contains("halberd") || lower.Contains("spear") || lower.Contains("cleave") || lower.Contains("axe")) return Core.Effects.WeaponType.Halberd;
            return Core.Effects.WeaponType.Degen;
        }
    }
    public void SetWeaponType(Core.Effects.WeaponType type) => weaponType = type;

    public int[] AttackTierValues => attackTierValues;

    public int[] DefenceTierValues => defenceTierValues;

    public int[] HealthTierValues => healthTierValues;

    public int[] RangeTierValues => rangeTierValues;

    public int[] SpeedTierValues => speedTierValues;

    

    public System.Collections.Generic.IReadOnlyList<AttackPatternData> AttackPatterns => attackPatterns;

    public AttackPatternData GetAttackPattern(int index)
    {
        if (attackPatterns != null && index >= 0 && index < attackPatterns.Length)
        {
            return attackPatterns[index];
        }
        return null;
    }
}
