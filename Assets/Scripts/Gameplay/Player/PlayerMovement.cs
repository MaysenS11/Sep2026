using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using FMODUnity;
using Infrastructure;
using Presentation.Board;
using Presentation.Entities;
using Core.Board;
using Core.Occupants;

/// <summary>
/// Authoritative player input and presentation controller.
/// Stripped of legacy physics raycasts, real-time coroutines, and redundant stat caches.
/// Interacts directly with BoardTurnCoordinator, GameBoard, and PlayerOccupant.
/// Features:
/// 1. Turn-Before-Step (Free turn facing update without passing turn)
/// 2. Auto Bump-to-Attack against attackable occupants (Enemy, Prop, Chest)
/// 3. Bump-denied recoil feedback on walls/obstacles without passing turn
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Character Definition")]
    [SerializeField] private CharacterDefinition characterDefinition;

    // Animation Parameter Hashes
    private static readonly int Moving = Animator.StringToHash("IsMoving");
    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int attackTrigger = Animator.StringToHash("IsAttacking");
    private static readonly int takeDamageTrigger = Animator.StringToHash("TakeDamage");
    private static readonly int dieTrigger = Animator.StringToHash("Die");

    private EventReference moveSound;

    private InputAction moveAction;
    private InputAction attackAction;
    private InputAction menuAction;

    private bool canTakeTurn = true;
    private float nextAllowedInputTime = 0f;
    private Vector2 lastDirection = Vector2.down;
    private Coroutine bumpRecoilCoroutine;

    private readonly List<Vector2Int> attackTilesBuffer = new List<Vector2Int>(16);
    private static readonly HashSet<Vector2Int> currentAttackTiles = new HashSet<Vector2Int>();

    public static IReadOnlyCollection<Vector2Int> CurrentAttackTiles => currentAttackTiles;

    public static bool IsPositionInAttackRange(Vector2Int tile)
    {
        return currentAttackTiles.Contains(tile);
    }

    public Vector2 LastDirection => lastDirection;
    public CharacterDefinition CharacterDefinition => characterDefinition;
    public event System.Action<Vector2Int, Vector2, AttackPatternData> OnAttackTargetingChanged;

    public PlayerOccupant PlayerOccupant
    {
        get
        {
            if (GameManager.Instance != null && GameManager.Instance.Board != null)
            {
                return GameManager.Instance.Board.FindPlayer();
            }
            return null;
        }
    }

    public Vector2Int CurrentGridPosition
    {
        get
        {
            var occupant = PlayerOccupant;
            if (occupant != null) return occupant.GridPosition;
            return BoardCoordinate.WorldToGrid(transform.position);
        }
    }

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (inputActions != null)
        {
            var playerMap = inputActions.FindActionMap("Player");
            if (playerMap != null)
            {
                moveAction = playerMap.FindAction("Move");
                attackAction = playerMap.FindAction("Attack");
                menuAction = playerMap.FindAction("Menu");
            }
        }
    }

    private void OnEnable()
    {
        inputActions?.Enable();
        EventBus<EnemyTurnCompletedEvent>.Subscribe(OnEnemyTurnCompleted);
        EventBus<EntityDamagedEvent>.Subscribe(OnEntityDamaged);
        EventBus<EntityDiedEvent>.Subscribe(OnEntityDied);
        EventBus<SharedAudioConfiguredEvent>.Subscribe(OnSharedAudioConfigured);

        if (attackAction != null) attackAction.performed += OnAttackPerformed;
        if (menuAction != null) menuAction.performed += OnMenuPerformed;
    }

    private void OnDisable()
    {
        if (attackAction != null) attackAction.performed -= OnAttackPerformed;
        if (menuAction != null) menuAction.performed -= OnMenuPerformed;

        EventBus<EnemyTurnCompletedEvent>.Unsubscribe(OnEnemyTurnCompleted);
        EventBus<EntityDamagedEvent>.Unsubscribe(OnEntityDamaged);
        EventBus<EntityDiedEvent>.Unsubscribe(OnEntityDied);
        EventBus<SharedAudioConfiguredEvent>.Unsubscribe(OnSharedAudioConfigured);

        currentAttackTiles.Clear();
        inputActions?.Disable();
    }

    private void Start()
    {
        if (CharacterSelectData.SelectedCharacter != null)
        {
            characterDefinition = CharacterSelectData.SelectedCharacter;
        }

        if (characterDefinition != null)
        {
            UIManager uiManager = Object.FindAnyObjectByType<UIManager>();
            if (uiManager != null && characterDefinition.MaskSprite != null)
            {
                uiManager.SetMaskSprite(characterDefinition.MaskSprite);
            }
        }

        if (moveSound.IsNull)
        {
            EventBus<RequestSharedAudioEvent>.Raise(new RequestSharedAudioEvent());
        }

        transform.position = BoardCoordinate.GridToWorldCenter(BoardCoordinate.WorldToGrid(transform.position), transform.position.z);

        if (GameManager.Instance != null && GameManager.Instance.Board != null)
        {
            Vector2Int gridPos = BoardCoordinate.WorldToGrid(transform.position);
            int maxHp = (characterDefinition != null && characterDefinition.HealthTierValues != null && characterDefinition.HealthTierValues.Length > 0) ? characterDefinition.HealthTierValues[0] : 6;
            int atk = (characterDefinition != null && characterDefinition.AttackTierValues != null && characterDefinition.AttackTierValues.Length > 0) ? characterDefinition.AttackTierValues[0] : 2;
            BoardEntityFactory.CreatePlayer(gameObject, gridPos, GameManager.Instance.Board, maxHp, atk, EffectsQueueRunner.Instance);
        }

        NotifyTargetingChanged();
    }

    private void Update()
    {
        HandleMovementInput();
    }

    #region Input & Turn-Before-Step Controller

    private void HandleMovementInput()
    {
        if (!canTakeTurn || Time.time < nextAllowedInputTime) return;
        if (ScreenFadeTransition.Instance != null && ScreenFadeTransition.Instance.IsTransitioning) return;
        if (GameManager.Instance != null && GameManager.Instance.TurnCoordinator != null && GameManager.Instance.TurnCoordinator.IsTurnInProgress) return;

        if (moveAction == null) return;
        Vector2 rawInput = moveAction.ReadValue<Vector2>();
        if (rawInput == Vector2.zero) return;

        // Resolve dominant orthogonal direction
        Vector2 quantizedDir;
        if (Mathf.Abs(rawInput.x) >= Mathf.Abs(rawInput.y))
        {
            quantizedDir = new Vector2(Mathf.Sign(rawInput.x), 0f);
        }
        else
        {
            quantizedDir = new Vector2(0f, Mathf.Sign(rawInput.y));
        }

        Vector2Int inputDirInt = new Vector2Int(Mathf.RoundToInt(quantizedDir.x), Mathf.RoundToInt(quantizedDir.y));

        // 1. Turn-Before-Step:
        // If input vector != lastDirection: Update facing and targeting preview as a free action without passing turn!
        if (lastDirection != quantizedDir)
        {
            lastDirection = quantizedDir;
            UpdateFacingVisuals(lastDirection);
            PlayerOccupant?.SetFacingDirection(inputDirInt);
            NotifyTargetingChanged();

            // Debounce input to allow single-tap turns without immediate accidental step
            nextAllowedInputTime = Time.time + 0.12f;
            return;
        }

        // 2. Step Forward or Auto Bump Action:
        // Input vector == lastDirection: Attempt to advance or bump-attack into the tile directly ahead
        Vector2Int currentGrid = CurrentGridPosition;
        Vector2Int targetGrid = currentGrid + inputDirInt;
        GameBoard board = GameManager.Instance?.Board;

        if (board != null)
        {
            // Case A: Blocked by wall or out of bounds -> Bump-denied recoil without passing turn
            if (!board.IsInBounds(targetGrid) || board.IsWall(targetGrid))
            {
                PlayBumpDeniedRecoil(targetGrid);
                return;
            }

            // Case B: Tile is occupied
            TileOccupant occupant = board.GetOccupant(targetGrid);
            if (occupant != null)
            {
                if (IsAttackableOccupant(occupant))
                {
                    // Auto Bump-to-Attack: Automatically trigger weapon attack pattern instead of walking
                    ExecuteAttack();
                    return;
                }
                else
                {
                    // Impassable obstacle (e.g. open chest, pillar) -> Bump-denied recoil without passing turn
                    PlayBumpDeniedRecoil(targetGrid);
                    return;
                }
            }

            // Case C: Walkable and free cell -> Execute 1 tile advance via TurnCoordinator
            if (GameManager.Instance.TurnCoordinator != null)
            {
                canTakeTurn = false;
                bool moved = GameManager.Instance.TurnCoordinator.TryExecutePlayerMove(inputDirInt, this, () =>
                {
                    NotifyTargetingChanged();
                    TryTriggerDoorAtCurrentPosition();
                });

                if (moved)
                {
                    if (!moveSound.IsNull) RuntimeManager.PlayOneShot(moveSound);
                    nextAllowedInputTime = Time.time + 0.05f;
                }
                else
                {
                    canTakeTurn = true;
                }
                return;
            }
        }
    }

    private static bool IsAttackableOccupant(TileOccupant occupant)
    {
        if (occupant == null || occupant.IsDead) return false;
        if (occupant is EnemyOccupant) return true;
        if (occupant is DestructiblePropOccupant) return true;
        if (occupant is ChestOccupant chest && !chest.IsOpen) return true;
        return false;
    }

    #endregion

    #region Attack Execution

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        if (!canTakeTurn || (ScreenFadeTransition.Instance != null && ScreenFadeTransition.Instance.IsTransitioning)) return;
        if (GameManager.Instance != null && GameManager.Instance.TurnCoordinator != null && GameManager.Instance.TurnCoordinator.IsTurnInProgress) return;

        ExecuteAttack();
    }

    public void ExecuteAttack()
    {
        if (GameManager.Instance == null || GameManager.Instance.TurnCoordinator == null) return;
        if (GameManager.Instance.TurnCoordinator.IsTurnInProgress) return;

        canTakeTurn = false;
        if (animator != null) animator.SetTrigger(attackTrigger);

        if (characterDefinition != null && !characterDefinition.AttackSound.IsNull)
        {
            RuntimeManager.PlayOneShot(characterDefinition.AttackSound);
        }

        attackTilesBuffer.Clear();
        Vector2Int currentGrid = CurrentGridPosition;
        AttackPatternData pattern = PlayerOccupant?.CurrentAttackPattern ?? (characterDefinition != null ? characterDefinition.GetAttackPattern(0) : null);
        if (pattern != null)
        {
            pattern.GetAffectedTiles(currentGrid, lastDirection, attackTilesBuffer);
        }
        else
        {
            Vector2Int cardinal = AttackPatternData.GetCardinalDirection(lastDirection);
            attackTilesBuffer.Add(currentGrid + cardinal);
        }

        bool started = GameManager.Instance.TurnCoordinator.TryExecutePlayerAttack(attackTilesBuffer, this, () =>
        {
            NotifyTargetingChanged();
        });

        if (!started)
        {
            canTakeTurn = true;
        }
    }

    #endregion

    #region Bump Denied Visual Feedback

    public void PlayBumpDeniedRecoil(Vector2Int targetGrid)
    {
        if (bumpRecoilCoroutine != null)
        {
            StopCoroutine(bumpRecoilCoroutine);
        }
        bumpRecoilCoroutine = StartCoroutine(BumpDeniedRecoilRoutine(targetGrid));
    }

    private IEnumerator BumpDeniedRecoilRoutine(Vector2Int targetGrid)
    {
        canTakeTurn = false;
        Vector3 startPos = BoardCoordinate.GridToWorldCenter(CurrentGridPosition, transform.position.z);
        Vector3 targetPos = BoardCoordinate.GridToWorldCenter(targetGrid, transform.position.z);
        Vector3 bumpPos = Vector3.Lerp(startPos, targetPos, 0.18f);

        float duration = 0.14f;
        float half = duration * 0.5f;

        // Nudge forward toward obstacle
        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Sin(Mathf.Clamp01(elapsed / half) * Mathf.PI * 0.5f);
            transform.position = Vector3.Lerp(startPos, bumpPos, t);
            yield return null;
        }

        // Recoil bounce back to tile center
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Sin(Mathf.Clamp01(elapsed / half) * Mathf.PI * 0.5f);
            transform.position = Vector3.Lerp(bumpPos, startPos, t);
            yield return null;
        }

        transform.position = startPos;
        canTakeTurn = true;
        nextAllowedInputTime = Time.time + 0.08f;
        bumpRecoilCoroutine = null;
    }

    #endregion

    #region Presentation & Targeting Visuals

    private void UpdateFacingVisuals(Vector2 direction)
    {
        if (animator != null)
        {
            animator.SetFloat(MoveX, direction.x);
            animator.SetFloat(MoveY, direction.y);
        }

        if (TryGetComponent<PlayerTileObject>(out var playerTileObj))
        {
            playerTileObj.UpdateFacingDirection(direction);
        }
    }

    public void NotifyTargetingChanged()
    {
        var player = PlayerOccupant;
        AttackPatternData pattern = player?.CurrentAttackPattern ?? (characterDefinition != null ? characterDefinition.GetAttackPattern(0) : null);
        Vector2Int currentGrid = CurrentGridPosition;
        OnAttackTargetingChanged?.Invoke(currentGrid, lastDirection, pattern);

        currentAttackTiles.Clear();
        if (player == null || !player.IsDead)
        {
            if (pattern != null)
            {
                pattern.GetAffectedTiles(currentGrid, lastDirection, attackTilesBuffer);
                for (int i = 0; i < attackTilesBuffer.Count; i++)
                {
                    currentAttackTiles.Add(attackTilesBuffer[i]);
                }
            }
            else
            {
                currentAttackTiles.Add(currentGrid + AttackPatternData.GetCardinalDirection(lastDirection));
            }
        }

        EventBus<AttackTargetingChangedEvent>.Raise(new AttackTargetingChangedEvent(new HashSet<Vector2Int>(currentAttackTiles)));
    }

    public void SelectAttackPattern(int index)
    {
        if (characterDefinition == null) return;
        AttackPatternData pattern = characterDefinition.GetAttackPattern(index);
        if (pattern != null)
        {
            PlayerOccupant?.SetAttackPattern(pattern, index);
            NotifyTargetingChanged();
        }
    }

    #endregion

    #region Turn State & Lifecycle

    private void OnEnemyTurnCompleted(EnemyTurnCompletedEvent evt)
    {
        canTakeTurn = true;
    }

    public void ResetTurnState()
    {
        canTakeTurn = true;
        nextAllowedInputTime = 0f;
        if (animator != null)
        {
            animator.SetBool(Moving, false);
        }
    }

    public void ResetAnimator()
    {
        if (animator == null) return;

        animator.ResetTrigger(dieTrigger);
        animator.ResetTrigger(takeDamageTrigger);
        animator.ResetTrigger(attackTrigger);

        animator.SetBool(Moving, false);
        animator.SetFloat(MoveX, lastDirection.x);
        animator.SetFloat(MoveY, lastDirection.y);

        animator.Rebind();
        animator.Play("IdleTree", 0, 0f);
        animator.Update(0f);
    }

    public void TeleportTo(Vector3 position)
    {
        StopAllCoroutines();
        bumpRecoilCoroutine = null;
        canTakeTurn = true;
        nextAllowedInputTime = 0f;
        ResetAnimator();

        Vector3 target = position;
        target.z = transform.position.z;
        transform.position = target;
    }

    #endregion

    #region Event Handlers & Room Transitions

    private void OnSharedAudioConfigured(SharedAudioConfiguredEvent evt)
    {
        moveSound = evt.MoveSound;
    }

    private void OnEntityDamaged(EntityDamagedEvent evt)
    {
        if (evt.Target != gameObject) return;
        if (animator != null)
        {
            animator.SetTrigger(takeDamageTrigger);
        }
    }

    private void OnEntityDied(EntityDiedEvent evt)
    {
        if (evt.Entity != gameObject) return;

        canTakeTurn = false;
        if (animator != null)
        {
            animator.ResetTrigger(takeDamageTrigger);
            animator.SetTrigger(dieTrigger);
        }

        NotifyTargetingChanged();
    }

    private void OnMenuPerformed(InputAction.CallbackContext context)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("StartMenu");
    }

    private void TryTriggerDoorAtCurrentPosition()
    {
        if (ScreenFadeTransition.Instance != null && ScreenFadeTransition.Instance.IsTransitioning) return;

        GameManager manager = GameManager.Instance;
        if (manager == null) return;

        Vector2Int gridPos = CurrentGridPosition;
        if (manager.TryResolveDoorAtTile(gridPos, out DoorType doorType))
        {
            GameManager.TriggerDoor(doorType, gridPos);
            return;
        }

        if (manager.DoorTilemap != null)
        {
            Vector3Int tilemapCell = manager.DoorTilemap.WorldToCell(transform.position);
            if (manager.TryResolveDoor(tilemapCell, out doorType))
            {
                GameManager.TriggerDoor(doorType, new Vector2Int(tilemapCell.x, tilemapCell.y));
            }
        }
    }

    #endregion
}