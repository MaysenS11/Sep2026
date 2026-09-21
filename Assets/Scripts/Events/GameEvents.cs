using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
    MainMenu,
    GeneratingDungeon,
    Gameplay,
    Paused,
    GameOver
}

public readonly struct GameStateChangedEvent : IEvent
{
    public readonly GameState PreviousState;
    public readonly GameState NewState;

    public GameStateChangedEvent(GameState previousState, GameState newState)
    {
        PreviousState = previousState;
        NewState = newState;
    }
}

public readonly struct GenerateDungeonEvent : IEvent
{
}

public readonly struct DoorTriggeredEvent : IEvent
{
    public readonly DoorType DoorType;
    public readonly Vector2Int DoorTilePosition;

    public DoorTriggeredEvent(DoorType doorType, Vector2Int doorTilePosition)
    {
        DoorType = doorType;
        DoorTilePosition = doorTilePosition;
    }
}

public readonly struct RoomEnteredEvent : IEvent
{
    public readonly GameManager.RoomData Room;

    public RoomEnteredEvent(GameManager.RoomData room)
    {
        Room = room;
    }
}

public readonly struct PlayerActionCompletedEvent : IEvent
{
}

public readonly struct EnemyTurnCompletedEvent : IEvent
{
}

public readonly struct EntityDamagedEvent : IEvent
{
    public readonly GameObject Target;
    public readonly GameObject Source;
    public readonly int Damage;
    public readonly int RemainingHealth;

    public EntityDamagedEvent(GameObject target, GameObject source, int damage, int remainingHealth)
    {
        Target = target;
        Source = source;
        Damage = damage;
        RemainingHealth = remainingHealth;
    }
}

public readonly struct EntityDiedEvent : IEvent
{
    public readonly GameObject Entity;

    public EntityDiedEvent(GameObject entity)
    {
        Entity = entity;
    }
}

public readonly struct PlayerHealthChangedEvent : IEvent
{
    public readonly int CurrentHealth;
    public readonly int MaxHealth;

    public PlayerHealthChangedEvent(int currentHealth, int maxHealth)
    {
        CurrentHealth = currentHealth;
        MaxHealth = maxHealth;
    }
}

public readonly struct PlayerPushedEvent : IEvent
{
    public readonly GameObject Target;
    public readonly Vector3 TargetPosition;
    public readonly Vector2 PushDirection;
    public readonly float PushSpeed;
    public readonly int Damage;
    public readonly GameObject Attacker;

    public PlayerPushedEvent(GameObject target, Vector3 targetPosition, Vector2 pushDirection, float pushSpeed, int damage, GameObject attacker)
    {
        Target = target;
        TargetPosition = targetPosition;
        PushDirection = pushDirection;
        PushSpeed = pushSpeed;
        Damage = damage;
        Attacker = attacker;
    }
}

public readonly struct EnemyTurnPlannedEvent : IEvent
{
    public readonly int PlannedMoveCount;
    public readonly int SkippedCount;

    public EnemyTurnPlannedEvent(int plannedMoveCount, int skippedCount)
    {
        PlannedMoveCount = plannedMoveCount;
        SkippedCount = skippedCount;
    }
}

public readonly struct EnemyMoveBlockedEvent : IEvent
{
    public readonly GameObject Enemy;
    public readonly Vector2Int BlockedTile;

    public EnemyMoveBlockedEvent(GameObject enemy, Vector2Int blockedTile)
    {
        Enemy = enemy;
        BlockedTile = blockedTile;
    }
}

public readonly struct EnemyPathDebugEvent : IEvent
{
    public readonly EnemyBase Enemy;
    public readonly List<Vector2Int> Path;
    public readonly Color Color;

    public EnemyPathDebugEvent(EnemyBase enemy, List<Vector2Int> path, Color color)
    {
        Enemy = enemy;
        Path = path;
        Color = color;
    }
}

public readonly struct EnemyPathDebugClearedEvent : IEvent
{
    public readonly EnemyBase Enemy;

    public EnemyPathDebugClearedEvent(EnemyBase enemy)
    {
        Enemy = enemy;
    }
}

public readonly struct EnemyRegisteredEvent : IEvent
{
    public readonly EnemyBase Enemy;

    public EnemyRegisteredEvent(EnemyBase enemy)
    {
        Enemy = enemy;
    }
}

public readonly struct EnemyUnregisteredEvent : IEvent
{
    public readonly EnemyBase Enemy;

    public EnemyUnregisteredEvent(EnemyBase enemy)
    {
        Enemy = enemy;
    }
}

public readonly struct CharacterUnlockedEvent : IEvent
{
    public readonly int CharacterId;

    public CharacterUnlockedEvent(int characterId)
    {
        CharacterId = characterId;
    }
}

public readonly struct CharacterSelectedEvent : IEvent
{
    public readonly CharacterDefinition Character;

    public CharacterSelectedEvent(CharacterDefinition character)
    {
        Character = character;
    }
}

public readonly struct BossExitDoorTriggeredEvent : IEvent
{
}

public readonly struct PropSpawnedEvent : IEvent
{
    public readonly GameObject Prop;
    public readonly Vector2Int GridTile;
    public readonly GameManager.RoomData Room;

    public PropSpawnedEvent(GameObject prop, Vector2Int gridTile, GameManager.RoomData room)
    {
        Prop = prop;
        GridTile = gridTile;
        Room = room;
    }
}

public readonly struct PropDestroyedEvent : IEvent
{
    public readonly GameObject Prop;

    public PropDestroyedEvent(GameObject prop)
    {
        Prop = prop;
    }
}

public readonly struct ClearDungeonEvent : IEvent
{
}

public enum UISoundType
{
    Click,
    Hover,
    CircleMenu
}

public readonly struct PlayUISoundEvent : IEvent
{
    public readonly UISoundType SoundType;

    public PlayUISoundEvent(UISoundType soundType)
    {
        SoundType = soundType;
    }
}

public readonly struct SharedAudioConfiguredEvent : IEvent
{
    public readonly FMODUnity.EventReference MoveSound;
    public readonly FMODUnity.EventReference HitSound;
    public readonly FMODUnity.EventReference LowLifeSound;
    public readonly FMODUnity.EventReference OpenChestSound;
    public readonly FMODUnity.EventReference DestroyBarrelSound;

    public SharedAudioConfiguredEvent(
        FMODUnity.EventReference moveSound,
        FMODUnity.EventReference hitSound,
        FMODUnity.EventReference lowLifeSound,
        FMODUnity.EventReference openChestSound,
        FMODUnity.EventReference destroyBarrelSound)
    {
        MoveSound = moveSound;
        HitSound = hitSound;
        LowLifeSound = lowLifeSound;
        OpenChestSound = openChestSound;
        DestroyBarrelSound = destroyBarrelSound;
    }
}

public readonly struct RequestSharedAudioEvent : IEvent
{
}

public readonly struct ChestOpenedEvent : IEvent
{
    public readonly Vector3 ChestPosition;
    public readonly bool IsChestRoom;

    public ChestOpenedEvent(Vector3 chestPosition, bool isChestRoom)
    {
        ChestPosition = chestPosition;
        IsChestRoom = isChestRoom;
    }
}

public readonly struct StatUpgradeAppliedEvent : IEvent
{
    public readonly Chest.StatType StatType;
    public readonly int NewTier;

    public StatUpgradeAppliedEvent(Chest.StatType statType, int newTier)
    {
        StatType = statType;
        NewTier = newTier;
    }
}

public readonly struct ChestRewardClosedEvent : IEvent
{
}
