using UnityEngine;

public static class GameSpawnSettings
{
    private const string KEY_ENEMY_DENSITY = "Setting_EnemyDensity";
    private const string KEY_BARREL_DENSITY = "Setting_BarrelDensity";
    private const string KEY_PAWN_WEIGHT = "Setting_PawnWeight";
    private const string KEY_KNIGHT_WEIGHT = "Setting_KnightWeight";
    private const string KEY_ROOK_WEIGHT = "Setting_RookWeight";
    private const string KEY_BISHOP_WEIGHT = "Setting_BishopWeight";

    public const float DEFAULT_ENEMY_DENSITY = 0.08f;
    public const float DEFAULT_BARREL_DENSITY = 0.08f;
    public const float DEFAULT_PAWN_WEIGHT = 0.35f;
    public const float DEFAULT_KNIGHT_WEIGHT = 0.25f;
    public const float DEFAULT_ROOK_WEIGHT = 0.20f;
    public const float DEFAULT_BISHOP_WEIGHT = 0.20f;

    private static bool _initialized = false;
    private static float _enemyDensity;
    private static float _barrelDensity;
    private static float _pawnWeight;
    private static float _knightWeight;
    private static float _rookWeight;
    private static float _bishopWeight;

    public static bool SettingsModifiedSinceLastDungeon { get; set; } = false;

    public static System.Action OnSettingsChanged;

    private static void EnsureInitialized()
    {
        if (_initialized) return;

        _enemyDensity = PlayerPrefs.GetFloat(KEY_ENEMY_DENSITY, DEFAULT_ENEMY_DENSITY);
        _barrelDensity = PlayerPrefs.GetFloat(KEY_BARREL_DENSITY, DEFAULT_BARREL_DENSITY);
        _pawnWeight = PlayerPrefs.GetFloat(KEY_PAWN_WEIGHT, DEFAULT_PAWN_WEIGHT);
        _knightWeight = PlayerPrefs.GetFloat(KEY_KNIGHT_WEIGHT, DEFAULT_KNIGHT_WEIGHT);
        _rookWeight = PlayerPrefs.GetFloat(KEY_ROOK_WEIGHT, DEFAULT_ROOK_WEIGHT);
        _bishopWeight = PlayerPrefs.GetFloat(KEY_BISHOP_WEIGHT, DEFAULT_BISHOP_WEIGHT);

        _initialized = true;
    }

    public static float EnemyDensity
    {
        get
        {
            EnsureInitialized();
            return _enemyDensity;
        }
        set
        {
            EnsureInitialized();
            _enemyDensity = Mathf.Clamp(value, 0.01f, 0.30f);
            SettingsModifiedSinceLastDungeon = true;
            PlayerPrefs.SetFloat(KEY_ENEMY_DENSITY, _enemyDensity);
            OnSettingsChanged?.Invoke();
        }
    }

    public static float BarrelDensity
    {
        get
        {
            EnsureInitialized();
            return _barrelDensity;
        }
        set
        {
            EnsureInitialized();
            _barrelDensity = Mathf.Clamp(value, 0.01f, 0.25f);
            SettingsModifiedSinceLastDungeon = true;
            PlayerPrefs.SetFloat(KEY_BARREL_DENSITY, _barrelDensity);
            OnSettingsChanged?.Invoke();
        }
    }

    public static float PawnWeight
    {
        get
        {
            EnsureInitialized();
            return _pawnWeight;
        }
        set
        {
            EnsureInitialized();
            _pawnWeight = Mathf.Clamp01(value);
            SettingsModifiedSinceLastDungeon = true;
            PlayerPrefs.SetFloat(KEY_PAWN_WEIGHT, _pawnWeight);
            OnSettingsChanged?.Invoke();
        }
    }

    public static float KnightWeight
    {
        get
        {
            EnsureInitialized();
            return _knightWeight;
        }
        set
        {
            EnsureInitialized();
            _knightWeight = Mathf.Clamp01(value);
            SettingsModifiedSinceLastDungeon = true;
            PlayerPrefs.SetFloat(KEY_KNIGHT_WEIGHT, _knightWeight);
            OnSettingsChanged?.Invoke();
        }
    }

    public static float RookWeight
    {
        get
        {
            EnsureInitialized();
            return _rookWeight;
        }
        set
        {
            EnsureInitialized();
            _rookWeight = Mathf.Clamp01(value);
            SettingsModifiedSinceLastDungeon = true;
            PlayerPrefs.SetFloat(KEY_ROOK_WEIGHT, _rookWeight);
            OnSettingsChanged?.Invoke();
        }
    }

    public static float BishopWeight
    {
        get
        {
            EnsureInitialized();
            return _bishopWeight;
        }
        set
        {
            EnsureInitialized();
            _bishopWeight = Mathf.Clamp01(value);
            SettingsModifiedSinceLastDungeon = true;
            PlayerPrefs.SetFloat(KEY_BISHOP_WEIGHT, _bishopWeight);
            OnSettingsChanged?.Invoke();
        }
    }

    public static float GetWeightForEnemy(string enemyName)
    {
        EnsureInitialized();
        if (string.IsNullOrEmpty(enemyName)) return 1f;

        string lower = enemyName.ToLowerInvariant();
        if (lower.Contains("pawn")) return _pawnWeight;
        if (lower.Contains("knight")) return _knightWeight;
        if (lower.Contains("rook")) return _rookWeight;
        if (lower.Contains("bishop")) return _bishopWeight;

        return 1f;
    }

    public static void ResetToDefaults()
    {
        SettingsModifiedSinceLastDungeon = true;
        EnemyDensity = DEFAULT_ENEMY_DENSITY;
        BarrelDensity = DEFAULT_BARREL_DENSITY;
        PawnWeight = DEFAULT_PAWN_WEIGHT;
        KnightWeight = DEFAULT_KNIGHT_WEIGHT;
        RookWeight = DEFAULT_ROOK_WEIGHT;
        BishopWeight = DEFAULT_BISHOP_WEIGHT;
        PlayerPrefs.Save();
        OnSettingsChanged?.Invoke();
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }
}
