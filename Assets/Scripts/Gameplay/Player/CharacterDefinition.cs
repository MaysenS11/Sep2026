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
            if (attackPatterns != null)
            {
                for (int i = 0; i < attackPatterns.Length; i++)
                {
                    var pattern = attackPatterns[i];
                    if (pattern == null) continue;
                    string name = !string.IsNullOrEmpty(pattern.name) ? pattern.name : pattern.PatternName;
                    if (string.IsNullOrEmpty(name)) continue;
                    string lower = name.ToLowerInvariant();
                    if (lower.Contains("hellebarde") || lower.Contains("halberd")) return Core.Effects.WeaponType.Halberd;
                    if (lower.Contains("pistol")) return Core.Effects.WeaponType.Pistol;
                    if (lower.Contains("potion")) return Core.Effects.WeaponType.Flask;
                    if (lower.Contains("degen")) return Core.Effects.WeaponType.Degen;
                }
            }
            return weaponType;
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
