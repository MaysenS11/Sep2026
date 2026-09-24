using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using FMODUnity;
using Infrastructure;
using Presentation.Board;
using Presentation.Entities;
using Core.Board;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float moveSpeed = 5.0f;      
    [SerializeField] private float moveCooldown = 2.0f;   
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Character Definition")]
    [SerializeField] private CharacterDefinition characterDefinition;

    // Animation Parameter Hashes
    private readonly int Moving = Animator.StringToHash("IsMoving");
    private readonly int MoveX = Animator.StringToHash("MoveX");
    private readonly int MoveY = Animator.StringToHash("MoveY");
    private readonly int attackTrigger = Animator.StringToHash("IsAttacking");
    private readonly int interactTrigger = Animator.StringToHash("Interact");
    private readonly int takeDamageTrigger = Animator.StringToHash("TakeDamage");
    private readonly int dieTrigger = Animator.StringToHash("Die");

    private EventReference moveSound;

    private InputAction moveAction;
    private InputAction attackAction;
    private InputAction menuAction;

    private bool isMoving = false;
    private bool canTakeTurn = true;
    private float nextMoveTime = 0f;
    private bool hasEnteredInitialRoom;
    private float doorTriggerBlockedUntil;
    private Vector2 lastDirection = Vector2.down;
    private PlayerStats playerStats;
    private readonly List<Vector2Int> attackTilesBuffer = new List<Vector2Int>(16);

    private static readonly HashSet<Vector2Int> currentAttackTiles = new HashSet<Vector2Int>();
    public static IReadOnlyCollection<Vector2Int> CurrentAttackTiles => currentAttackTiles;

    public static bool IsPositionInAttackRange(Vector2Int tile)
    {
        return currentAttackTiles.Contains(tile);
    }

    public Vector2 LastDirection => lastDirection;
    public event System.Action<Vector2Int, Vector2, AttackPatternData> OnAttackTargetingChanged;

    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        if (playerStats == null)
        {
            playerStats = gameObject.AddComponent<PlayerStats>();
        }

        if (animator == null) 
        {
            Debug.LogError("Animator component is missing!");
            return;
        }

        var playerMap = inputActions.FindActionMap("Player");
        moveAction = playerMap.FindAction("Move");
        attackAction = playerMap.FindAction("Attack");
        menuAction = playerMap.FindAction("Menu");
    }

    private void OnEnable()
    {
        inputActions.Enable();
        EventBus<DoorTriggeredEvent>.Subscribe(OnDoorTriggered);
        EventBus<RoomEnteredEvent>.Subscribe(OnRoomEntered);
        EventBus<EnemyTurnCompletedEvent>.Subscribe(OnEnemyTurnCompleted);
        EventBus<PlayerPushedEvent>.Subscribe(OnPlayerPushed);
        EventBus<EntityDamagedEvent>.Subscribe(OnEntityDamaged);
        EventBus<EntityDiedEvent>.Subscribe(OnEntityDied);
        EventBus<SharedAudioConfiguredEvent>.Subscribe(OnSharedAudioConfigured);
        attackAction.performed += OnAttackPerformed;
        if (menuAction != null)
        {
            menuAction.performed += OnMenuPerformed;
        }
    }

    private void OnDisable()
    {
        attackAction.performed -= OnAttackPerformed;
        if (menuAction != null)
        {
            menuAction.performed -= OnMenuPerformed;
        }
        EventBus<DoorTriggeredEvent>.Unsubscribe(OnDoorTriggered);
        EventBus<RoomEnteredEvent>.Unsubscribe(OnRoomEntered);
        EventBus<EnemyTurnCompletedEvent>.Unsubscribe(OnEnemyTurnCompleted);
        EventBus<PlayerPushedEvent>.Unsubscribe(OnPlayerPushed);
        EventBus<EntityDamagedEvent>.Unsubscribe(OnEntityDamaged);
        EventBus<EntityDiedEvent>.Unsubscribe(OnEntityDied);
        EventBus<SharedAudioConfiguredEvent>.Unsubscribe(OnSharedAudioConfigured);

        currentAttackTiles.Clear();
        inputActions.Disable();
    }

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
        StartCoroutine(HandlePlayerDeathSequence());
    }

    private IEnumerator HandlePlayerDeathSequence()
    {
        yield return new WaitForSeconds(0.95f);

        EventBus<GenerateDungeonEvent>.Raise(new GenerateDungeonEvent());
    }

    private void OnPlayerPushed(PlayerPushedEvent evt)
    {
        if (evt.Target != gameObject) return;
        StartCoroutine(ExecutePushCoroutine(evt));
    }

    private IEnumerator ExecutePushCoroutine(PlayerPushedEvent evt)
    {
        Vector3 destination = BoardCoordinate.GridToWorldCenter(BoardCoordinate.WorldToGrid(evt.TargetPosition), transform.position.z);
        destination.z = transform.position.z;
        if (IsTileBlocked(destination))
        {
            destination = transform.position;
        }

        if (destination != transform.position)
        {
            isMoving = true;
            float pushSpeed = evt.PushSpeed > 0f ? evt.PushSpeed : moveSpeed * 1.5f;
            while (Vector3.Distance(transform.position, destination) > 0.001f)
            {
                transform.position = Vector3.MoveTowards(transform.position, destination, pushSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = destination;
            isMoving = false;
            TryTriggerDoorAtCurrentPosition();
        }

        if (playerStats != null && evt.Damage > 0)
        {
            playerStats.TakeDamage(evt.Damage, evt.Attacker);
        }
    }

    private void Update()
    {
        HandleMovement();
    }

    private void OnPattern1Performed(InputAction.CallbackContext context)
    {
        SelectAttackPattern(0);
    }

    private void OnPattern2Performed(InputAction.CallbackContext context)
    {
        SelectAttackPattern(1);
    }

    private void OnPattern3Performed(InputAction.CallbackContext context)
    {
        SelectAttackPattern(2);
    }

    private void Start()
    {
        if (CharacterSelectData.SelectedCharacter != null)
        {
            characterDefinition = CharacterSelectData.SelectedCharacter;
        }

        if (playerStats == null) playerStats = GetComponent<PlayerStats>();

        if (characterDefinition != null && playerStats != null)
        {
            playerStats.SetCharacterDefinition(characterDefinition);
            AttackPatternData initialPattern = characterDefinition.GetAttackPattern(0);
            if (initialPattern != null)
            {
                playerStats.SetAttackPattern(initialPattern, 0);
            }

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

        if (GameManager.Instance == null)
        {
            transform.position = BoardCoordinate.GridToWorldCenter(BoardCoordinate.WorldToGrid(transform.position), transform.position.z);
            NotifyTargetingChanged();
            return;
        }

        if (GameManager.Instance.DungeonDictionary.TryGetValue(GameManager.Instance.CurrentRoomIndex, out GameManager.RoomData room))
        {
            MoveToPosition(BoardCoordinate.GridToWorldCenter(BoardCoordinate.WorldToGrid(room.CenterTilePosition), transform.position.z));
        }
        else
        {
            transform.position = BoardCoordinate.GridToWorldCenter(BoardCoordinate.WorldToGrid(transform.position), transform.position.z);
        }

        if (GameManager.Instance != null && GameManager.Instance.Board != null)
        {
            Vector2Int gridPos = BoardCoordinate.WorldToGrid(transform.position);
            int maxHp = playerStats != null ? playerStats.MaxHealth : 6;
            int atk = playerStats != null ? playerStats.TotalAttackDamage : 2;
            BoardEntityFactory.CreatePlayer(gameObject, gridPos, GameManager.Instance.Board, maxHp, atk, EffectsQueueRunner.Instance);
        }

        NotifyTargetingChanged();
    }

    private void OnEnemyTurnCompleted(EnemyTurnCompletedEvent evt)
    {
        canTakeTurn = true;
    }

    private void HandleMovement()
    {
        if (!canTakeTurn || isMoving || Time.time < nextMoveTime || (ScreenFadeTransition.Instance != null && ScreenFadeTransition.Instance.IsTransitioning)) return;

        Vector2 inputDir = moveAction.ReadValue<Vector2>();

        if (inputDir == Vector2.zero) return;

        if (Mathf.Abs(inputDir.x) > 0.5f)
        {
            inputDir = new Vector2(Mathf.Sign(inputDir.x), 0);
        }
        else if (Mathf.Abs(inputDir.y) > 0.5f)
        {
            inputDir = new Vector2(0, Mathf.Sign(inputDir.y));
        }
        else
        {
            return;
        }

        Vector2Int inputDirInt = new Vector2Int(Mathf.RoundToInt(inputDir.x), Mathf.RoundToInt(inputDir.y));
        if (GameManager.Instance != null && GameManager.Instance.TurnCoordinator != null)
        {
            bool dirChanged = lastDirection != inputDir;
            lastDirection = inputDir;
            UpdateFacingVisuals(lastDirection);
            if (dirChanged)
            {
                NotifyTargetingChanged();
            }

            bool moved = GameManager.Instance.TurnCoordinator.TryExecutePlayerMove(inputDirInt, this, () =>
            {
                NotifyTargetingChanged();
                TryTriggerDoorAtCurrentPosition();
            });

            if (moved)
            {
                if (!moveSound.IsNull) RuntimeManager.PlayOneShot(moveSound);
                canTakeTurn = false;
                nextMoveTime = Time.time + moveCooldown;
            }
            return;
        }

        Vector2Int currentGrid = BoardCoordinate.WorldToGrid(transform.position);
        Vector2Int targetGrid = currentGrid + inputDirInt;
        Vector3 targetPosition = BoardCoordinate.GridToWorldCenter(targetGrid, transform.position.z);

        if (IsTileBlocked(targetPosition))
        {
            if (lastDirection != inputDir)
            {
                lastDirection = inputDir;
                UpdateFacingVisuals(lastDirection);
                NotifyTargetingChanged();
            }
            return;
        }

        bool directionChanged = lastDirection != inputDir;
        lastDirection = inputDir;
        UpdateFacingVisuals(lastDirection);
        if (directionChanged)
        {
            NotifyTargetingChanged();
        }

        canTakeTurn = false;
        nextMoveTime = Time.time + moveCooldown;
        StartCoroutine(MoveToTile(targetPosition));
    }

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

    private IEnumerator MoveToTile(Vector3 targetPos)
    {
        isMoving = true;
        if (!moveSound.IsNull)
        {
            RuntimeManager.PlayOneShot(moveSound);
        }

        animator.SetBool(Moving, true);
        animator.SetFloat(MoveX, lastDirection.x);
        animator.SetFloat(MoveY, lastDirection.y);

        while (Vector3.Distance(transform.position, targetPos) > 0.001f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPos;
        isMoving = false;
        animator.SetBool(Moving, false);

        NotifyTargetingChanged();

        TryTriggerDoorAtCurrentPosition();

        EventBus<PlayerActionCompletedEvent>.Raise(new PlayerActionCompletedEvent());
    }

    private bool IsTileBlocked(Vector3 targetPos)
    {
        Vector3 centerPos = BoardCoordinate.GridToWorldCenter(BoardCoordinate.WorldToGrid(targetPos), targetPos.z);
        Collider2D hit = Physics2D.OverlapBox(centerPos, new Vector2(tileSize * 0.8f, tileSize * 0.8f), 0f, obstacleLayer);
        return hit != null && hit.gameObject != gameObject;
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        if (!canTakeTurn || isMoving || (ScreenFadeTransition.Instance != null && ScreenFadeTransition.Instance.IsTransitioning)) return;

        if (GameManager.Instance != null && GameManager.Instance.TurnCoordinator != null)
        {
            canTakeTurn = false;
            nextMoveTime = Time.time + moveCooldown;
            if (animator != null) animator.SetTrigger(attackTrigger);

            if (characterDefinition != null && !characterDefinition.AttackSound.IsNull)
            {
                RuntimeManager.PlayOneShot(characterDefinition.AttackSound);
            }

            attackTilesBuffer.Clear();
            Vector2Int currentGrid = BoardCoordinate.WorldToGrid(transform.position);
            if (playerStats == null) playerStats = GetComponent<PlayerStats>();
            AttackPatternData pattern = playerStats != null ? playerStats.CurrentAttackPattern : null;
            if (pattern != null)
            {
                pattern.GetAffectedTiles(currentGrid, lastDirection, attackTilesBuffer);
            }
            else
            {
                Vector2Int cardinal = AttackPatternData.GetCardinalDirection(lastDirection);
                attackTilesBuffer.Add(currentGrid + cardinal);
            }

            GameManager.Instance.TurnCoordinator.TryExecutePlayerAttack(attackTilesBuffer, this, () =>
            {
                NotifyTargetingChanged();
            });
            return;
        }

        canTakeTurn = false;
        nextMoveTime = Time.time + moveCooldown;
        animator.SetTrigger(attackTrigger);

        if (characterDefinition != null)
        {
            if (!characterDefinition.AttackSound.IsNull)
            {
                RuntimeManager.PlayOneShot(characterDefinition.AttackSound);
            }
            else
            {
                Debug.LogWarning($"[PlayerMovement] Attack sound not assigned for character: {characterDefinition.name}");
            }
        }
        else
        {
            Debug.LogWarning("[PlayerMovement] characterDefinition is not assigned.");
        }

        if (playerStats == null) playerStats = GetComponent<PlayerStats>();
        int damage = playerStats != null ? playerStats.TotalAttackDamage : 3;

        AttackPatternData legacyPattern = playerStats != null ? playerStats.CurrentAttackPattern : null;

        StartCoroutine(CompleteAttackTurn());
    }

    public void NotifyTargetingChanged()
    {
        if (playerStats == null) playerStats = GetComponent<PlayerStats>();
        AttackPatternData pattern = playerStats != null ? playerStats.CurrentAttackPattern : null;
        Vector2Int currentGrid = BoardCoordinate.WorldToGrid(transform.position);
        OnAttackTargetingChanged?.Invoke(currentGrid, lastDirection, pattern);

        currentAttackTiles.Clear();
        if (playerStats == null || !playerStats.IsDead)
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
        if (characterDefinition == null || playerStats == null) return;
        AttackPatternData pattern = characterDefinition.GetAttackPattern(index);
        if (pattern != null)
        {
            playerStats.SetAttackPattern(pattern, index);
            NotifyTargetingChanged();
        }
    }

    private IEnumerator CompleteAttackTurn()
    {
        yield return new WaitForSeconds(0.25f);
        EventBus<PlayerActionCompletedEvent>.Raise(new PlayerActionCompletedEvent());
    }

    private void OnMenuPerformed(InputAction.CallbackContext context)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("StartMenu");
    }

    private void OnDoorTriggered(DoorTriggeredEvent evt)
    {
        if (isMoving || (ScreenFadeTransition.Instance != null && ScreenFadeTransition.Instance.IsTransitioning) || Time.time < doorTriggerBlockedUntil || GameManager.Instance == null) return;

        doorTriggerBlockedUntil = Time.time + 2.0f;
        TransitionThroughDoor(evt.DoorType);
    }

    private void TryTriggerDoorAtCurrentPosition()
    {
        if (Time.time < doorTriggerBlockedUntil || (ScreenFadeTransition.Instance != null && ScreenFadeTransition.Instance.IsTransitioning)) return;

        GameManager manager = GameManager.Instance;
        if (manager == null) return;

        Vector2Int gridPos;
        if (manager.Board != null && manager.Board.FindPlayer() is { } player)
        {
            gridPos = player.GridPosition;
        }
        else
        {
            gridPos = Core.Board.BoardCoordinate.WorldToGrid(transform.position);
        }

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

    private void OnRoomEntered(RoomEnteredEvent evt)
    {
        if (hasEnteredInitialRoom || evt.Room.RoomIndex != 0) return;
        hasEnteredInitialRoom = true;
    }

    private void TransitionThroughDoor(DoorType doorType)
    {
        GameManager manager = GameManager.Instance;
        if (manager == null || !manager.DungeonDictionary.TryGetValue(manager.CurrentRoomIndex, out GameManager.RoomData currentRoom)) return;

        int nextRoomIndex;
        Vector3? targetDoor;
        if (doorType == DoorType.ExitDoor)
        {
            nextRoomIndex = currentRoom.RoomIndex + 1;
            targetDoor = GetEntryDoor(nextRoomIndex);
        }
        else if (doorType == DoorType.EntryDoor)
        {
            nextRoomIndex = currentRoom.RoomIndex - 1;
            targetDoor = GetExitDoor(nextRoomIndex);
        }
        else if (doorType == DoorType.SpecialExitDoor)
        {
            nextRoomIndex = currentRoom.SpecialChestRoomIndex;
            targetDoor = GetSpecialEntryDoor(nextRoomIndex);
        }
        else
        {
            nextRoomIndex = currentRoom.ParentRoomIndex;
            targetDoor = GetSpecialExitDoor(nextRoomIndex);
        }

        if (!targetDoor.HasValue || !manager.DungeonDictionary.TryGetValue(nextRoomIndex, out GameManager.RoomData nextRoom))
        {
            doorTriggerBlockedUntil = 0f;
            return;
        }

        // Abort previous room turn phase and input locks
        manager.TurnCoordinator?.ResetTurnState();

        void OnTransitionComplete()
        {
            MoveToPosition(targetDoor.Value);
            Vector2Int newGrid = Core.Board.BoardCoordinate.WorldToGrid(targetDoor.Value);

            if (manager.Board != null)
            {
                var playerOcc = manager.Board.FindPlayer();
                if (playerOcc != null)
                {
                    manager.Board.Move(playerOcc, newGrid);
                }
            }

            if (TryGetComponent<PlayerTileObject>(out var playerTileObj))
            {
                playerTileObj.SnapToGrid(newGrid);
            }

            GameManager.NotifyNewRoomEntered(nextRoom);

            if (manager.Board != null)
            {
                BoardEntityFactory.RegisterSceneEntities(manager.Board, manager.TurnCoordinator?.EffectsRunner);
            }

            canTakeTurn = true;
            nextMoveTime = 0f;
            doorTriggerBlockedUntil = Time.time + 1.0f;
            manager.TurnCoordinator?.ResetTurnState();
            NotifyTargetingChanged();
        }

        if (ScreenFadeTransition.Instance != null)
        {
            ScreenFadeTransition.Instance.StartCoroutine(ScreenFadeTransition.Instance.PlayTransition(OnTransitionComplete));
        }
        else
        {
            OnTransitionComplete();
        }
    }

    private Vector3? GetEntryDoor(int roomIndex)
    {
        if (GameManager.Instance != null && GameManager.Instance.DungeonDictionary.TryGetValue(roomIndex, out GameManager.RoomData room))
        {
            if (room.EntryDoorPosition.HasValue) return room.EntryDoorPosition.Value;
            if (room.EntranceDoorTile.HasValue) return Core.Board.BoardCoordinate.GridToWorldCenter(room.EntranceDoorTile.Value);
            return Core.Board.BoardCoordinate.GridToWorldCenter(room.CenterTile);
        }
        return null;
    }

    private Vector3? GetExitDoor(int roomIndex)
    {
        if (GameManager.Instance != null && GameManager.Instance.DungeonDictionary.TryGetValue(roomIndex, out GameManager.RoomData room))
        {
            if (room.ExitDoorPosition.HasValue) return room.ExitDoorPosition.Value;
            if (room.ExitDoorTile.HasValue) return Core.Board.BoardCoordinate.GridToWorldCenter(room.ExitDoorTile.Value);
            return Core.Board.BoardCoordinate.GridToWorldCenter(room.CenterTile);
        }
        return null;
    }

    private Vector3? GetSpecialEntryDoor(int roomIndex)
    {
        if (GameManager.Instance != null && GameManager.Instance.DungeonDictionary.TryGetValue(roomIndex, out GameManager.RoomData room))
        {
            if (room.SpecialEntryDoorPosition.HasValue) return room.SpecialEntryDoorPosition.Value;
            if (room.EntranceDoorTile.HasValue) return Core.Board.BoardCoordinate.GridToWorldCenter(room.EntranceDoorTile.Value);
            return Core.Board.BoardCoordinate.GridToWorldCenter(room.CenterTile);
        }
        return null;
    }

    private Vector3? GetSpecialExitDoor(int roomIndex)
    {
        if (GameManager.Instance != null && GameManager.Instance.DungeonDictionary.TryGetValue(roomIndex, out GameManager.RoomData room))
        {
            if (room.SpecialExitDoorPosition.HasValue) return room.SpecialExitDoorPosition.Value;
            if (room.ExitDoorTile.HasValue) return Core.Board.BoardCoordinate.GridToWorldCenter(room.ExitDoorTile.Value);
            return Core.Board.BoardCoordinate.GridToWorldCenter(room.CenterTile);
        }
        return null;
    }

    private void MoveToPosition(Vector3 position)
    {
        Vector3 target = position;
        target.z = transform.position.z;
        transform.position = target;
    }

    public void TeleportTo(Vector3 position)
    {
        StopAllCoroutines();
        isMoving = false;
        canTakeTurn = true;
        nextMoveTime = 0f;
        doorTriggerBlockedUntil = 0f;
        ResetAnimator();
        MoveToPosition(position);
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

    public void ResetTurnState()
    {
        isMoving = false;
        canTakeTurn = true;
        nextMoveTime = 0f;
        doorTriggerBlockedUntil = 0f;
        if (animator != null)
        {
            animator.SetBool(Moving, false);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 testPos = transform.position + new Vector3(lastDirection.x, lastDirection.y, 0) * tileSize;
        
        Gizmos.DrawWireSphere(testPos, 0.2f);
        Gizmos.DrawWireCube(testPos, new Vector3(tileSize * 0.8f, tileSize * 0.8f, 0f));
    }
}