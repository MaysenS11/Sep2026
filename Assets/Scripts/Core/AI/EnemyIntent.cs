using System.Collections.Generic;
using Core.Actions;
using Core.Occupants;
using UnityEngine;

namespace Core.AI
{
    /// Type of turn action intended by an enemy unit.
    public enum IntentType
    {
        Wait,
        Move,
        Jump,
        AttackPush,
        BlockedPush
    }

    /// Authoritative decision data model output by enemy AI during turn calculation.
    /// Contains the acting enemy, intent type, target coordinates, path waypoints,
    /// attack push direction, and the concrete executable IBoardAction.
    public class EnemyIntent
    {
        /// The acting enemy occupant.
        public EnemyOccupant Enemy { get; }

        /// The category of action planned.
        public IntentType IntentType { get; }

        /// The primary destination tile of the action.
        public Vector2Int TargetPosition { get; }

        /// Full sequence of tiles traversed by the occupant during this action.
        /// For straight-line pieces, contains start, intermediate, and destination tiles.
        /// For Knights, contains only start and destination tiles.
        public List<Vector2Int> Path { get; }

        /// The direction the player will be pushed if this intent is an attack.
        public Vector2Int PushDirection { get; }

        /// The concrete simulation action ready to be executed on the GameBoard.
        public IBoardAction GeneratedAction { get; }

        public EnemyIntent(
            EnemyOccupant enemy,
            IntentType intentType,
            Vector2Int targetPosition,
            List<Vector2Int> path = null,
            Vector2Int pushDirection = default,
            IBoardAction generatedAction = null)
        {
            Enemy = enemy;
            IntentType = intentType;
            TargetPosition = targetPosition;
            Path = path ?? new List<Vector2Int>();
            PushDirection = pushDirection;
            GeneratedAction = generatedAction;
        }

        /// Creates a Wait intent where the enemy remains stationary and performs no action.
        public static EnemyIntent CreateWait(EnemyOccupant enemy)
        {
            Vector2Int pos = enemy != null ? enemy.GridPosition : Vector2Int.zero;
            return new EnemyIntent(
                enemy: enemy,
                intentType: IntentType.Wait,
                targetPosition: pos,
                path: new List<Vector2Int> { pos },
                pushDirection: Vector2Int.zero,
                generatedAction: null
            );
        }

        public override string ToString()
        {
            return $"EnemyIntent [{IntentType}] Enemy={Enemy?.Id} ({Enemy?.Archetype}) Target={TargetPosition} PushDir={PushDirection} PathSteps={Path?.Count ?? 0}";
        }
    }
}
