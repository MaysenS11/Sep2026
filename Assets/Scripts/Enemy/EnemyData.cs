using UnityEngine;

public enum EnemyMovementPattern
{
    SingleMove,
    BishopMove,
    RookMove,
    KnightMove,
    QueenMove
}

public enum EnemySmartness
{
    Lazy,
    Mid,
    Smart
}

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Dungeon/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Identity & Visuals")]
    [SerializeField] private string enemyName = "Enemy";
    [SerializeField] private GameObject prefab;

    [Header("Combat Stats")]
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private int attackDamage = 2;

    [Header("Turn & Movement Capability")]
    [SerializeField] private float stepSpeed = 5.0f;
    [SerializeField] private int detectionRange = 6;

    [Header("Chess Patterns")]
    [SerializeField] private EnemyMovementPattern movementPattern = EnemyMovementPattern.SingleMove;
    [SerializeField] private bool usesDiagonalAttack = false;
    [SerializeField] private EnemySmartness smartness = EnemySmartness.Mid;
    [SerializeField] private int maxLineSteps = 3;
    [SerializeField] private int movesInterval = 1;

    [Header("Turn Priority")]
    [SerializeField] private int movePriority = 0;

    [Header("Spawning")]
    [Range(0f, 1f)]
    [SerializeField] private float populationPercentage = 0.2f;

    public string EnemyName => enemyName;
    public GameObject Prefab => prefab;
    public int MaxHealth => maxHealth;
    public int AttackDamage => attackDamage;
    public float StepSpeed => stepSpeed;
    public int DetectionRange => detectionRange;

    public EnemyMovementPattern MovementPattern => movementPattern;
    public bool UsesDiagonalAttack => usesDiagonalAttack;
    public EnemySmartness Smartness => smartness;
    public int MaxLineSteps => maxLineSteps;
    public int MovesInterval => movesInterval;
    public int MovePriority => movePriority;
    public float PopulationPercentage => populationPercentage;
}
