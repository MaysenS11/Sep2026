using UnityEngine;

[CreateAssetMenu(fileName = "GameSpawnSettings", menuName = "Dungeon/Game Spawn Settings")]
public class GameSpawnSettings : ScriptableObject
{
    public const float DEFAULT_ENEMY_DENSITY = 0.08f;
    public const float DEFAULT_BARREL_DENSITY = 0.08f;
    public const float DEFAULT_PAWN_WEIGHT = 0.35f;
    public const float DEFAULT_KNIGHT_WEIGHT = 0.25f;
    public const float DEFAULT_ROOK_WEIGHT = 0.20f;
    public const float DEFAULT_BISHOP_WEIGHT = 0.20f;

    [Header("Densities")]
    [Range(0.01f, 0.30f)]
    [SerializeField] private float enemyDensity = DEFAULT_ENEMY_DENSITY;

    [Range(0.01f, 0.25f)]
    [SerializeField] private float barrelDensity = DEFAULT_BARREL_DENSITY;

    [Header("Distribution Weights")]
    [Range(0f, 1f)]
    [SerializeField] private float pawnWeight = DEFAULT_PAWN_WEIGHT;

    [Range(0f, 1f)]
    [SerializeField] private float knightWeight = DEFAULT_KNIGHT_WEIGHT;

    [Range(0f, 1f)]
    [SerializeField] private float rookWeight = DEFAULT_ROOK_WEIGHT;

    [Range(0f, 1f)]
    [SerializeField] private float bishopWeight = DEFAULT_BISHOP_WEIGHT;

    [System.NonSerialized]
    private bool settingsModifiedSinceLastDungeon = false;

    public event System.Action OnSettingsChanged;

    public bool SettingsModifiedSinceLastDungeon
    {
        get => settingsModifiedSinceLastDungeon;
        set => settingsModifiedSinceLastDungeon = value;
    }

    public float EnemyDensity
    {
        get => enemyDensity;
        set
        {
            enemyDensity = Mathf.Clamp(value, 0.01f, 0.30f);
            settingsModifiedSinceLastDungeon = true;
            OnSettingsChanged?.Invoke();
        }
    }

    public float BarrelDensity
    {
        get => barrelDensity;
        set
        {
            barrelDensity = Mathf.Clamp(value, 0.01f, 0.25f);
            settingsModifiedSinceLastDungeon = true;
            OnSettingsChanged?.Invoke();
        }
    }

    public float PawnWeight
    {
        get => pawnWeight;
        set
        {
            pawnWeight = Mathf.Clamp01(value);
            settingsModifiedSinceLastDungeon = true;
            OnSettingsChanged?.Invoke();
        }
    }

    public float KnightWeight
    {
        get => knightWeight;
        set
        {
            knightWeight = Mathf.Clamp01(value);
            settingsModifiedSinceLastDungeon = true;
            OnSettingsChanged?.Invoke();
        }
    }

    public float RookWeight
    {
        get => rookWeight;
        set
        {
            rookWeight = Mathf.Clamp01(value);
            settingsModifiedSinceLastDungeon = true;
            OnSettingsChanged?.Invoke();
        }
    }

    public float BishopWeight
    {
        get => bishopWeight;
        set
        {
            bishopWeight = Mathf.Clamp01(value);
            settingsModifiedSinceLastDungeon = true;
            OnSettingsChanged?.Invoke();
        }
    }

    public float GetWeightForArchetype(Core.Occupants.EnemyArchetype archetype)
    {
        switch (archetype)
        {
            case Core.Occupants.EnemyArchetype.Pawn:
                return pawnWeight;
            case Core.Occupants.EnemyArchetype.Knight:
                return knightWeight;
            case Core.Occupants.EnemyArchetype.Rook:
                return rookWeight;
            case Core.Occupants.EnemyArchetype.Bishop:
                return bishopWeight;
            default:
                return 1f;
        }
    }

    public float GetWeightForEnemy(EnemyData enemyData)
    {
        if (enemyData == null) return 1f;
        return GetWeightForArchetype(enemyData.Archetype);
    }

    public float GetWeightForEnemy(string enemyName)
    {
        if (string.IsNullOrEmpty(enemyName)) return 1f;
        string lower = enemyName.ToLowerInvariant();
        if (lower.Contains("pawn")) return pawnWeight;
        if (lower.Contains("knight")) return knightWeight;
        if (lower.Contains("rook")) return rookWeight;
        if (lower.Contains("bishop")) return bishopWeight;
        return 1f;
    }

    public void ResetToDefaults()
    {
        enemyDensity = DEFAULT_ENEMY_DENSITY;
        barrelDensity = DEFAULT_BARREL_DENSITY;
        pawnWeight = DEFAULT_PAWN_WEIGHT;
        knightWeight = DEFAULT_KNIGHT_WEIGHT;
        rookWeight = DEFAULT_ROOK_WEIGHT;
        bishopWeight = DEFAULT_BISHOP_WEIGHT;
        settingsModifiedSinceLastDungeon = true;
        OnSettingsChanged?.Invoke();
    }
}
