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
