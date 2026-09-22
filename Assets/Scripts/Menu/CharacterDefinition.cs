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

    public string CharacterName => characterName;
    public Sprite MaskSprite => maskSprite;
    public Sprite MaskWhiteSprite => maskWhiteSprite;
    public Sprite FullBodySprite => fullBodySprite;
    public bool LockedByDefault => lockedByDefault;
    public EventReference AttackSound => attackSound;

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
