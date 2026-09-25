using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core.Actions;
using Core.AI;
using Core.Board;
using Core.Effects;
using Core.Occupants;
using Core.Scheduling;
using Presentation.Board;

namespace Infrastructure
{
    /// Master coordinator orchestrating the strict two-speed turn loop across simulation and presentation layers.
    /// Maintains the authoritative GameBoard, coordinates sequential simulation mutations, schedules path-disjoint
    /// animation batches via TurnBatchScheduler, and drives visual playback via EffectsQueueRunner.
    public class BoardTurnCoordinator
    {
        public GameBoard Board { get; private set; }
        public TurnBatchScheduler BatchScheduler { get; private set; }
        public EffectsQueueRunner EffectsRunner { get; set; }

        public float PlayerMoveDuration { get; set; } = 0.2f;
        public float PlayerAttackDuration { get; set; } = 0.25f;
        public float EnemyTurnTotalDuration { get; set; } = 0.8f;
        public EnemySettings EnemySettings { get; set; }

        public bool IsTurnInProgress { get; private set; }
        public bool IsPlayerInputLocked => IsTurnInProgress;

        // Turn Lifecycle Events
        public event Action OnTurnStarted;
        public event Action OnPlayerActionCompleted;
        public event Action OnEnemyTurnStarted;
        public event Action OnEnemyTurnCompleted;
        public event Action<bool> OnInputLockChanged;

        public BoardTurnCoordinator(GameBoard board = null, TurnBatchScheduler scheduler = null, EffectsQueueRunner effectsRunner = null)
        {
            Board = board ?? new GameBoard(50, 50);
            BatchScheduler = scheduler ?? new TurnBatchScheduler();
            EffectsRunner = effectsRunner;
        }

        public void SetBoard(GameBoard newBoard)
        {
            Board = newBoard ?? throw new ArgumentNullException(nameof(newBoard));
            IsTurnInProgress = false;
        }

        public void ResetTurnState()
        {
            IsTurnInProgress = false;
            OnInputLockChanged?.Invoke(false);
        }

        #region Synchronous Pure Simulation Logic (Headless / Deterministic / Testable)

        /// Attempts to execute player movement on the authoritative GameBoard.
        /// If valid, mutates board state immediately and outputs the resulting MoveEffect.
        public bool SimulatePlayerMove(Vector2Int direction, out MoveEffect moveEffect)
        {
            moveEffect = null;
            if (direction == Vector2Int.zero) return false;

            // Auto-discovery recovery: If board is empty or only has player, discover scene entities
            if (Board.GetAllOccupants().Count <= 1)
            {
                BoardEntityFactory.RegisterSceneEntities(Board, EffectsRunner);
            }

            PlayerOccupant player = Board.FindPlayer();
            if (player == null)
            {
                var pMove = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
                if (pMove != null)
                {
                    Vector2Int pGrid = BoardCoordinate.WorldToGrid(pMove.transform.position);
                    player = BoardEntityFactory.CreatePlayer(pMove.gameObject, pGrid, Board, 6, 2, EffectsRunner);
                }
            }
            if (player == null || player.IsDead)
            {
                return false;
            }

            // Ensure grid position matches actual player transform position if desynced
            var activePMove = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
            if (activePMove != null)
            {
                Vector2Int actualGrid = BoardCoordinate.WorldToGrid(activePMove.transform.position);
                if (player.GridPosition != actualGrid && Board.IsInBounds(actualGrid))
                {
                    Board.Move(player, actualGrid);
                }
            }

            player.SetFacingDirection(direction);

            Vector2Int startPos = player.GridPosition;
            Vector2Int targetPos = startPos + direction;

            if (!Board.CanEnter(targetPos))
            {
                return false;
            }

            bool moved = Board.Move(player, targetPos);
            if (moved)
            {
                moveEffect = new MoveEffect(player.Id, new[] { startPos, targetPos });
                return true;
            }

            return false;
        }

        /// Attempts to execute player attack against specified target tiles on the authoritative GameBoard.
        /// Evaluates each tile occupant (Enemy, Chest, Prop) and immediately commits damage, stun, opening, or destruction.
        public bool SimulatePlayerAttack(List<Vector2Int> targetTiles, out List<BoardEffect> emittedEffects)
        {
            emittedEffects = new List<BoardEffect>();

            PlayerOccupant player = Board.FindPlayer();
            if (player == null)
            {
                var pMove = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
                if (pMove != null)
                {
                    Vector2Int pGrid = new Vector2Int(Mathf.FloorToInt(pMove.transform.position.x), Mathf.FloorToInt(pMove.transform.position.y));
                    player = BoardEntityFactory.CreatePlayer(pMove.gameObject, pGrid, Board, 6, 2, EffectsRunner);
                }
            }
            if (player == null || player.IsDead) return false;

            if (targetTiles == null || targetTiles.Count == 0)
            {
                targetTiles = new List<Vector2Int> { player.GridPosition + player.FacingDirection };
            }

            Vector2Int primaryTarget = targetTiles[0];
            Vector2Int attackDir = player.FacingDirection;
            if (primaryTarget != player.GridPosition)
            {
                attackDir = BoardCoordinate.GetSignVector(primaryTarget - player.GridPosition);
                player.SetFacingDirection(attackDir);
            }

            WeaponType weapon = player.WeaponType;

            // 1. Emit player attack lunge visual effect
            emittedEffects.Add(new AttackLungeEffect(player.Id, primaryTarget, attackDir, player.GridPosition));

            // 2. Weapon profile evaluation
            var hitOccupants = new HashSet<TileOccupant>();
            var hitTiles = new List<Vector2Int>();
            var evaluatedTiles = new List<Vector2Int>();

            switch (weapon)
            {
                case WeaponType.Degen:
                {
                    // Degen (Rapier): Single-target strike stopping at first occupant
                    for (int i = 0; i < targetTiles.Count; i++)
                    {
                        Vector2Int cell = targetTiles[i];
                        evaluatedTiles.Add(cell);

                        if (!Board.IsInBounds(cell) || Board.IsWall(cell))
                        {
                            break;
                        }

                        TileOccupant occ = Board.GetOccupant(cell);
                        if (occ != null)
                        {
                            ApplyDamageToOccupant(occ, player, player.AttackDamage, emittedEffects, hitOccupants, hitTiles, cell);
                            // STOPS at first occupant!
                            break;
                        }
                    }
                    break;
                }

                case WeaponType.Halberd:
                {
                    // Halberd: Sweep cleave stopping at first obstacle
                    for (int i = 0; i < targetTiles.Count; i++)
                    {
                        Vector2Int cell = targetTiles[i];
                        evaluatedTiles.Add(cell);

                        if (!Board.IsInBounds(cell) || Board.IsWall(cell))
                        {
                            // Stops extending past first obstacle along sweep
                            break;
                        }

                        TileOccupant occ = Board.GetOccupant(cell);
                        if (occ is ObstacleOccupant || (occ is ChestOccupant chest && chest.IsOpen))
                        {
                            // Impassable obstacle stops sweep
                            break;
                        }

                        if (occ != null)
                        {
                            ApplyDamageToOccupant(occ, player, player.AttackDamage, emittedEffects, hitOccupants, hitTiles, cell);
                        }
                    }
                    break;
                }

                case WeaponType.Pistol:
                {
                    // Pistol: Cardinal piercing projectile passing through aligned targets with damage degradation per hit
                    int hitCount = 0;
                    for (int i = 0; i < targetTiles.Count; i++)
                    {
                        Vector2Int cell = targetTiles[i];
                        evaluatedTiles.Add(cell);

                        if (!Board.IsInBounds(cell) || Board.IsWall(cell))
                        {
                            break; // Wall blocks bullet
                        }

                        TileOccupant occ = Board.GetOccupant(cell);
                        if (occ is ObstacleOccupant)
                        {
                            break; // Solid pillar/obstacle blocks bullet
                        }

                        if (occ != null)
                        {
                            int degradedDamage = Math.Max(1, player.AttackDamage - hitCount);
                            ApplyDamageToOccupant(occ, player, degradedDamage, emittedEffects, hitOccupants, hitTiles, cell);
                            hitCount++;
                        }
                    }
                    break;
                }

                case WeaponType.Flask:
                {
                    // Flask (Raven Mask): Thrown projectile arc originating from player and impacting target tile
                    // Overhead throw: intermediate obstacles/walls do not block the throw
                    for (int i = 0; i < targetTiles.Count; i++)
                    {
                        Vector2Int cell = targetTiles[i];
                        evaluatedTiles.Add(cell);

                        if (!Board.IsInBounds(cell) || Board.IsWall(cell))
                        {
                            continue;
                        }

                        TileOccupant occ = Board.GetOccupant(cell);
                        if (occ != null)
                        {
                            ApplyDamageToOccupant(occ, player, player.AttackDamage, emittedEffects, hitOccupants, hitTiles, cell);
                        }
                    }
                    break;
                }
            }

            // 3. Emit dedicated WeaponAttackEffect for presentation layer VFX
            emittedEffects.Add(new WeaponAttackEffect(
                player.Id,
                weapon,
                player.GridPosition,
                attackDir,
                primaryTarget,
                evaluatedTiles,
                hitTiles
            ));

            return true;
        }

        private void ApplyDamageToOccupant(
            TileOccupant occupant,
            PlayerOccupant player,
            int damage,
            List<BoardEffect> emittedEffects,
            HashSet<TileOccupant> hitOccupants,
            List<Vector2Int> hitTiles,
            Vector2Int cell)
        {
            if (occupant == null || !hitOccupants.Add(occupant)) return;
            hitTiles.Add(cell);

            if (occupant is EnemyOccupant enemy)
            {
                if (enemy.ImmuneToDirectAttacks)
                {
                    return;
                }

                var dmgEffects = enemy.TakeDamage(damage, player);
                enemy.SkipNextTurn = true;
                emittedEffects.AddRange(dmgEffects);

                if (enemy.IsDead)
                {
                    Board.Remove(enemy);
                }
            }
            else if (occupant is ChestOccupant chest)
            {
                var chestEffects = chest.TakeDamage(1, player);
                emittedEffects.AddRange(chestEffects);
            }
            else if (occupant is DestructiblePropOccupant prop)
            {
                var propEffects = prop.TakeDamage(damage, player);
                emittedEffects.AddRange(propEffects);

                if (prop.IsDead)
                {
                    Board.Remove(prop);
                }
            }
            else
            {
                var otherEffects = occupant.TakeDamage(damage, player);
                emittedEffects.AddRange(otherEffects);

                if (occupant.IsDead)
                {
                    Board.Remove(occupant);
                }
            }
        }

        /// Executes pure simulation for the entire enemy phase:
        /// Sequentially evaluates each active EnemyOccupant via EnemyAIFactory, mutates GameBoard,
        /// accumulates EnactedEnemyActions, and groups them into path-disjoint AnimationBatches.
        public List<EnactedEnemyAction> SimulateEnemyTurn(out List<AnimationBatch> batches)
        {
            var enactedActions = new List<EnactedEnemyAction>();
            batches = new List<AnimationBatch>();

            PlayerOccupant player = Board.FindPlayer();
            Vector2Int playerPos = player != null ? player.GridPosition : Vector2Int.zero;

            // Collect active alive enemies
            var activeEnemies = new List<EnemyOccupant>(Board.GetOccupantsOfType<EnemyOccupant>());
            if (activeEnemies.Count == 0)
            {
                BoardEntityFactory.RegisterSceneEntities(Board, EffectsRunner);
                activeEnemies = new List<EnemyOccupant>(Board.GetOccupantsOfType<EnemyOccupant>());
            }

            activeEnemies.RemoveAll(e => e.IsDead);

            // Sort enemies by MovePriority ascending, then by Manhattan distance to player ascending
            activeEnemies.Sort((a, b) =>
            {
                int p = a.MovePriority.CompareTo(b.MovePriority);
                if (p != 0) return p;
                int da = BoardCoordinate.ManhattanDistance(a.GridPosition, playerPos);
                int db = BoardCoordinate.ManhattanDistance(b.GridPosition, playerPos);
                return da.CompareTo(db);
            });

            // Sequential AI evaluation and board mutation
            for (int i = 0; i < activeEnemies.Count; i++)
            {
                EnemyOccupant enemy = activeEnemies[i];
                if (enemy == null || enemy.IsDead || !Board.IsInBounds(enemy.GridPosition)) continue;

                Vector2Int startTile = enemy.GridPosition;
                EnemyIntent intent = EnemyAIFactory.EvaluateEnemy(Board, enemy);

                if (intent == null || intent.IntentType == IntentType.Wait || intent.GeneratedAction == null)
                {
                    continue;
                }

                // Execute action immediately against authoritative GameBoard
                List<BoardEffect> effects = intent.GeneratedAction.Execute(Board);
                Vector2Int endTile = enemy.GridPosition;
                Vector2Int? blockedTile = null;
                int? blockerId = null;

                if (intent.GeneratedAction is BlockedPushAction)
                {
                    foreach (var eff in effects)
                    {
                        if (eff is BlockedPushRecoilEffect recoil)
                        {
                            blockedTile = recoil.BlockerPos;
                            blockerId = recoil.BlockerOccupantId;
                            endTile = recoil.RecoilEndTile;
                            break;
                        }
                    }
                }

                var enacted = new EnactedEnemyAction(
                    enemy: enemy,
                    intent: intent,
                    emittedEffects: effects,
                    startTile: startTile,
                    endTile: endTile,
                    traversedTiles: null,
                    blockedTile: blockedTile,
                    blockerOccupantId: blockerId
                );

                enactedActions.Add(enacted);
            }

            float enemyDuration = (EnemySettings != null && EnemySettings.EnemyMoveDuration > 0f)
                ? EnemySettings.EnemyMoveDuration
                : EnemyTurnTotalDuration;
            batches = BatchScheduler.BuildBatches(enactedActions, enemyDuration);
            return enactedActions;
        }

        #endregion

        #region Coroutine-Driven Playback (Two-Speed Turn Loop)

        /// Attempts to execute player movement and launches the two-speed turn loop on the given runner.
        /// Returns true if the move was valid and turn loop started.
        public bool TryExecutePlayerMove(Vector2Int direction, MonoBehaviour coroutineRunner, Action onMovementDone = null)
        {
            if (IsTurnInProgress || coroutineRunner == null) return false;

            if (!SimulatePlayerMove(direction, out var moveEffect))
            {
                // Can't move (blocked or invalid), facing direction was updated
                return false;
            }

            IsTurnInProgress = true;
            OnInputLockChanged?.Invoke(true);
            OnTurnStarted?.Invoke();

            coroutineRunner.StartCoroutine(RunPlayerMoveTurn(moveEffect, onMovementDone));
            return true;
        }

        /// Attempts to execute player attack and launches the two-speed turn loop on the given runner.
        /// Returns true if the attack was initiated and turn loop started.
        public bool TryExecutePlayerAttack(List<Vector2Int> targetTiles, MonoBehaviour coroutineRunner, Action onAttackDone = null)
        {
            if (IsTurnInProgress || coroutineRunner == null) return false;

            if (!SimulatePlayerAttack(targetTiles, out var attackEffects))
            {
                return false;
            }

            IsTurnInProgress = true;
            OnInputLockChanged?.Invoke(true);
            OnTurnStarted?.Invoke();

            coroutineRunner.StartCoroutine(RunPlayerAttackTurn(attackEffects, onAttackDone));
            return true;
        }

        private IEnumerator RunPlayerMoveTurn(MoveEffect moveEffect, Action onMovementDone)
        {
            // Player Animation Phase: plays at player animation speed
            if (EffectsRunner != null && moveEffect != null)
            {
                yield return EffectsRunner.StartCoroutine(EffectsRunner.PlaySingleEffect(moveEffect, PlayerMoveDuration));
            }
            else if (PlayerMoveDuration > 0f)
            {
                yield return new WaitForSeconds(PlayerMoveDuration);
            }

            onMovementDone?.Invoke();
            if (!IsTurnInProgress)
            {
                yield break;
            }
            OnPlayerActionCompleted?.Invoke();
            EventBus<PlayerActionCompletedEvent>.Raise(new PlayerActionCompletedEvent());

            // Transition to Enemy Turn Phase
            yield return RunEnemyTurnPhase();
        }

        private IEnumerator RunPlayerAttackTurn(List<BoardEffect> attackEffects, Action onAttackDone)
        {
            // Player Animation Phase: plays at player attack speed
            if (EffectsRunner != null && attackEffects != null && attackEffects.Count > 0)
            {
                int pending = 0;
                for (int i = 0; i < attackEffects.Count; i++)
                {
                    var effect = attackEffects[i];
                    if (effect == null) continue;
                    pending++;
                    EffectsRunner.StartCoroutine(RunSingleEffectWithCounter(effect, PlayerAttackDuration, () => pending--));
                }

                while (pending > 0)
                {
                    yield return null;
                }
            }
            else if (PlayerAttackDuration > 0f)
            {
                yield return new WaitForSeconds(PlayerAttackDuration);
            }

            onAttackDone?.Invoke();
            if (!IsTurnInProgress)
            {
                yield break;
            }
            OnPlayerActionCompleted?.Invoke();
            EventBus<PlayerActionCompletedEvent>.Raise(new PlayerActionCompletedEvent());

            // Transition to Enemy Turn Phase
            yield return RunEnemyTurnPhase();
        }

        private IEnumerator RunSingleEffectWithCounter(BoardEffect effect, float duration, Action onComplete)
        {
            try
            {
                if (EffectsRunner != null)
                {
                    yield return EffectsRunner.StartCoroutine(EffectsRunner.PlaySingleEffect(effect, duration));
                }
            }
            finally
            {
                onComplete?.Invoke();
            }
        }

        /// Executes the Enemy Turn Phase:
        /// 1. Simulates enemy actions and builds batches.
        /// 2. Dispatches batches to EffectsQueueRunner to animate concurrently within batches.
        /// 3. Unlocks player input when playback completes.
        public IEnumerator RunEnemyTurnPhase()
        {
            OnEnemyTurnStarted?.Invoke();

            // Run simulation mutation and scheduling
            SimulateEnemyTurn(out var batches);

            // Playback animation batches across evenly sliced durations
            if (EffectsRunner != null && batches != null && batches.Count > 0)
            {
                yield return EffectsRunner.StartCoroutine(EffectsRunner.PlayTurnBatches(batches));
            }

            // Complete enemy phase and unlock player input
            IsTurnInProgress = false;
            OnInputLockChanged?.Invoke(false);
            OnEnemyTurnCompleted?.Invoke();
            EventBus<EnemyTurnCompletedEvent>.Raise(new EnemyTurnCompletedEvent());
        }

        #endregion
    }
}
