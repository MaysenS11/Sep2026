using UnityEngine;
using FMODUnity;
using Core.Occupants;

public enum EnemySmartness
{
    Dumb = 0,
    Mid = 1,
    Smart = 2
}

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Dungeon/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Prefab")]
    [SerializeField] private GameObject prefab;

    [Header("Archetype")]
    [SerializeField] private EnemyArchetype archetype = EnemyArchetype.Pawn;

    [Header("Combat Stats")]
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private int attackDamage = 2;

    [Header("Chess Patterns")]
    [SerializeField] private EnemySmartness smartness = EnemySmartness.Mid;
    [SerializeField] private int maxLineSteps = 3;

    [Header("Turn Priority")]
    [SerializeField] private int movePriority = 0;

    [Header("Spawning")]
    [Range(0f, 1f)]
    [SerializeField] private float populationPercentage = 0.2f;

    [Header("Audio")]
    [SerializeField] private EventReference attackSound;
    [SerializeField] private EventReference damageSound;
    [SerializeField] private EventReference deathSound;

    [Header("Boss Properties")]
    [SerializeField] private bool isBoss = false;

    public GameObject Prefab => prefab;
    public EnemyArchetype Archetype => archetype;
    public int MaxHealth => maxHealth;
    public int AttackDamage => attackDamage;
    public bool UsesDiagonalAttack => archetype == EnemyArchetype.Pawn;
    public EnemySmartness Smartness => smartness;
    public EnemySmartness IntelligenceLevel => smartness;
    public int MaxLineSteps => maxLineSteps;
    public int MovePriority => movePriority;
    public float PopulationPercentage => populationPercentage;
    public EventReference AttackSound => attackSound;
    public EventReference DamageSound => damageSound;
    public EventReference DeathSound => deathSound;
    public bool IsBoss => isBoss;
    public bool IsImmobile => isBoss;
    public bool ImmuneToDirectAttacks => isBoss;
}
