using UnityEngine;
using FMODUnity;

[CreateAssetMenu(fileName = "CharacterDefinition", menuName = "Game/Character Definition")]
public class CharacterDefinition : ScriptableObject
{
    [SerializeField] private int characterId;
    [SerializeField] private string characterName;
    [SerializeField] private Sprite maskSprite;
    [SerializeField] private Sprite maskWhiteSprite;
    [SerializeField] private Sprite fullBodySprite;
    [SerializeField] private int startHealth = 3;
    [SerializeField] private bool lockedByDefault = true;
    [SerializeField] private Color lockedTint = new Color(0.25f, 0.25f, 0.25f, 1f);

    [Header("Audio")]
    [SerializeField] private EventReference attackDegenSound;

    [Header("Combat & Attack Patterns")]
    [SerializeField] private AttackPatternData[] attackPatterns = new AttackPatternData[3];

    public int CharacterId => characterId;
    public string CharacterName => characterName;
    public Sprite MaskSprite => maskSprite;
    public Sprite MaskWhiteSprite => maskWhiteSprite;
    public Sprite FullBodySprite => fullBodySprite;
    public int StartHealth => startHealth;
    public bool LockedByDefault => lockedByDefault;
    public Color LockedTint => lockedTint;
    public EventReference AttackDegenSound => attackDegenSound;
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
