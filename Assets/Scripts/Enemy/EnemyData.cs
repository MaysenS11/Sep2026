using UnityEngine;

public enum EnemyMovementPattern
{
    OneStepOrthogonal, // Pawn: 1 step orthogonally per player move
    DiagonalLine,      // Bishop: 1 to 3 steps diagonally per player move
    OrthogonalLine,    // Rook: 1 to 3 steps orthogonally per player move
    KnightLPattern     // Knight: L-shape movement every 2 player moves
}

public enum EnemyAttackPattern
{
    AdjacentDiagonalTrigger, // Pawn: attacks if player is on/steps onto adjacent diagonal tile
    StepOntoPlayerTile       // Bishop, Rook, Knight: attacks if moving onto the player's tile
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
    [SerializeField] private EnemyMovementPattern movementPattern = EnemyMovementPattern.OneStepOrthogonal;
    [SerializeField] private EnemyAttackPattern attackPattern = EnemyAttackPattern.AdjacentDiagonalTrigger;
    [Tooltip("For line movers (Bishop, Rook), max steps allowed per turn")]
    [SerializeField] private int maxLineSteps = 3;
    [Tooltip("For Knight or paced movers, how many player moves between turns (e.g. 2 for Knight)")]
    [SerializeField] private int movesInterval = 1;

    public string EnemyName => enemyName;
    public GameObject Prefab => prefab;
    public int MaxHealth => maxHealth;
    public int AttackDamage => attackDamage;
    public float StepSpeed => stepSpeed;
    public int DetectionRange => detectionRange;

    public EnemyMovementPattern MovementPattern => movementPattern;
    public EnemyAttackPattern AttackPattern => attackPattern;
    public int MaxLineSteps => maxLineSteps;
    public int MovesInterval => movesInterval;
}
