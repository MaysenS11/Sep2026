using System.Collections.Generic;
using Core.Actions;
using Core.Occupants;
using UnityEngine;

namespace Core.AI
{
    public enum IntentType
    {
        Wait,
        Move,
        Jump,
        AttackPush,
        BlockedPush
    }

    public class EnemyIntent
    {
        public EnemyOccupant Enemy { get; }

        public IntentType IntentType { get; }

        public Vector2Int TargetPosition { get; }

        public List<Vector2Int> Path { get; }

        public Vector2Int PushDirection { get; }

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
