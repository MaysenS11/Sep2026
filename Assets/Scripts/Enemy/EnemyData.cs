using UnityEngine;

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
    [Tooltip("Number of steps taken per turn (1 for Archetype A, 2 for Archetype B, etc.)")]
    [SerializeField] private int stepsPerTurn = 1;
    [SerializeField] private float stepSpeed = 5.0f;
    [SerializeField] private int detectionRange = 6;

    public string EnemyName => enemyName;
    public GameObject Prefab => prefab;
    public int MaxHealth => maxHealth;
    public int AttackDamage => attackDamage;
    public int StepsPerTurn => stepsPerTurn;
    public float StepSpeed => stepSpeed;
    public int DetectionRange => detectionRange;
}
