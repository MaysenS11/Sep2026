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
