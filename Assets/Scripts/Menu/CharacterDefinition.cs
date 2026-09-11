using UnityEngine;

[CreateAssetMenu(fileName = "CharacterDefinition", menuName = "Game/Character Definition")]
public class CharacterDefinition : ScriptableObject
{
    [SerializeField] private int characterId;
    [SerializeField] private string characterName;
    [SerializeField] private Sprite maskSprite;
    [SerializeField] private Sprite fullBodySprite;
    [SerializeField] private int startHealth = 3;
    [SerializeField] private bool lockedByDefault = true;
    [SerializeField] private Color lockedTint = new Color(0.25f, 0.25f, 0.25f, 1f);

    public int CharacterId => characterId;
    public string CharacterName => characterName;
    public Sprite MaskSprite => maskSprite;
    public Sprite FullBodySprite => fullBodySprite;
    public int StartHealth => startHealth;
    public bool LockedByDefault => lockedByDefault;
    public Color LockedTint => lockedTint;
}
