using UnityEngine;

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
