using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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

    // Animation Parameter Hashes
    private readonly int Moving = Animator.StringToHash("IsMoving");
    private readonly int MoveX = Animator.StringToHash("MoveX");
    private readonly int MoveY = Animator.StringToHash("MoveY");
    private readonly int attackTrigger = Animator.StringToHash("IsAttacking");
    private readonly int interactTrigger = Animator.StringToHash("Interact");
    private readonly int takeDamageTrigger = Animator.StringToHash("TakeDamage");
    private readonly int dieTrigger = Animator.StringToHash("Die");

    private InputAction moveAction;
    private InputAction attackAction;
    private InputAction interactAction;
    private InputAction menuAction;

    private bool isMoving = false;
    private bool canTakeTurn = true;
    private float nextMoveTime = 0f;
    private bool hasEnteredInitialRoom;
    private float doorTriggerBlockedUntil;
    private Vector2 lastDirection = Vector2.down;
    private PlayerStats playerStats;
    private readonly Collider2D[] hitBuffer = new Collider2D[16];

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
        interactAction = playerMap.FindAction("Interact");
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
        attackAction.performed += OnAttackPerformed;
        interactAction.performed += OnInteractPerformed;
        if (menuAction != null)
        {
            menuAction.performed += OnMenuPerformed;
        }
    }

    private void OnDisable()
    {
        attackAction.performed -= OnAttackPerformed;
        interactAction.performed -= OnInteractPerformed;
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

        inputActions.Disable();
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
        Vector3 destination = TileReservationSystem.SnapToTileCenter(evt.TargetPosition);
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

    private void Start()
    {
        if (playerStats == null) playerStats = GetComponent<PlayerStats>();

        if (GameManager.Instance == null)
        {
            transform.position = TileReservationSystem.SnapToTileCenter(transform.position);
            return;
        }

        if (GameManager.Instance.DungeonDictionary.TryGetValue(GameManager.Instance.CurrentRoomIndex, out GameManager.RoomData room))
        {
            MoveToPosition(TileReservationSystem.SnapToTileCenter(room.CenterTilePosition));
        }
        else
        {
            transform.position = TileReservationSystem.SnapToTileCenter(transform.position);
        }
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

        Vector2Int currentGrid = TileReservationSystem.WorldToGridTile(transform.position);
        Vector2Int targetGrid = currentGrid + new Vector2Int(Mathf.RoundToInt(inputDir.x), Mathf.RoundToInt(inputDir.y));
        Vector3 targetPosition = TileReservationSystem.GetTileCenterWorld(targetGrid, transform.position.z);

        if (IsTileBlocked(targetPosition))
        {
            if (lastDirection != inputDir)
            {
                lastDirection = inputDir;
                if (animator != null)
                {
                    animator.SetFloat(MoveX, lastDirection.x);
                    animator.SetFloat(MoveY, lastDirection.y);
                }
            }
            return;
        }

        lastDirection = inputDir;
        if (animator != null)
        {
            animator.SetFloat(MoveX, lastDirection.x);
            animator.SetFloat(MoveY, lastDirection.y);
        }

        canTakeTurn = false;
        nextMoveTime = Time.time + moveCooldown;
        StartCoroutine(MoveToTile(targetPosition));
    }

    private IEnumerator MoveToTile(Vector3 targetPos)
    {
        isMoving = true;
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

        TryTriggerDoorAtCurrentPosition();

        EventBus<PlayerActionCompletedEvent>.Raise(new PlayerActionCompletedEvent());
    }

    private bool IsTileBlocked(Vector3 targetPos)
    {
        Vector3 centerPos = TileReservationSystem.SnapToTileCenter(targetPos);
        Collider2D hit = Physics2D.OverlapBox(centerPos, new Vector2(tileSize * 0.8f, tileSize * 0.8f), 0f, obstacleLayer);
        return hit != null && hit.gameObject != gameObject;
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        if (!canTakeTurn || isMoving || (ScreenFadeTransition.Instance != null && ScreenFadeTransition.Instance.IsTransitioning)) return;

        canTakeTurn = false;
        nextMoveTime = Time.time + moveCooldown;
        animator.SetTrigger(attackTrigger);

        if (playerStats == null) playerStats = GetComponent<PlayerStats>();
        int damage = playerStats != null ? playerStats.TotalAttackDamage : 3;

        AttackPattern pattern = playerStats != null ? playerStats.CurrentAttackPattern : AttackPattern.SurroundingOrthogonal;
        ExecuteAttack(pattern, damage);

        StartCoroutine(CompleteAttackTurn());
    }

    private void ExecuteAttack(AttackPattern pattern, int damage)
    {
        HashSet<IDamageable> damagedEntities = new HashSet<IDamageable>();

        if (pattern == AttackPattern.SingleFacing)
        {
            Vector3 targetPos = transform.position + new Vector3(lastDirection.x, lastDirection.y, 0) * tileSize;
            DamageAtTile(targetPos, damage, damagedEntities);
        }
        else
        {
            // Surrounding orthogonal tiles (Up, Down, Left, Right)
            Vector3[] checkPositions = new Vector3[]
            {
                transform.position + Vector3.up * tileSize,
                transform.position + Vector3.down * tileSize,
                transform.position + Vector3.left * tileSize,
                transform.position + Vector3.right * tileSize
            };

            foreach (var pos in checkPositions)
            {
                DamageAtTile(pos, damage, damagedEntities);
            }
        }
    }

    private void DamageAtTile(Vector3 targetPos, int damage, HashSet<IDamageable> damagedEntities)
    {
        int hitCount = Physics2D.OverlapBox(targetPos, new Vector2(tileSize * 0.85f, tileSize * 0.85f), 0f, default, hitBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = hitBuffer[i];
            if (hit == null || hit.gameObject == gameObject) continue;

            if (hit.TryGetComponent<IDamageable>(out var damageable) || hit.GetComponentInParent<IDamageable>() is { } parentDamageable && (damageable = parentDamageable) != null)
            {
                if (damagedEntities.Add(damageable))
                {
                    damageable.TakeDamage(damage, gameObject);
                }
            }
        }
    }

    private IEnumerator CompleteAttackTurn()
    {
        yield return new WaitForSeconds(0.25f);
        EventBus<PlayerActionCompletedEvent>.Raise(new PlayerActionCompletedEvent());
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (isMoving || (ScreenFadeTransition.Instance != null && ScreenFadeTransition.Instance.IsTransitioning)) return;
        animator.SetTrigger(interactTrigger);
    }

    private void OnMenuPerformed(InputAction.CallbackContext context)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("StartMenu");
    }

    private void OnDoorTriggered(DoorTriggeredEvent evt)
    {
        if (isMoving || (ScreenFadeTransition.Instance != null && ScreenFadeTransition.Instance.IsTransitioning) || Time.time < doorTriggerBlockedUntil || GameManager.Instance == null) return;

        TransitionThroughDoor(evt.DoorType);
        doorTriggerBlockedUntil = Time.time + 0.2f;
    }

    private void TryTriggerDoorAtCurrentPosition()
    {
        GameManager manager = GameManager.Instance;
        if (manager == null || manager.DoorTilemap == null) return;

        Vector3Int cell = manager.DoorTilemap.WorldToCell(transform.position);
        if (manager.TryResolveDoor(cell, out DoorType doorType))
        {
            GameManager.TriggerDoor(doorType, new Vector2Int(cell.x, cell.y));
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

        if (!targetDoor.HasValue || !manager.DungeonDictionary.TryGetValue(nextRoomIndex, out GameManager.RoomData nextRoom)) return;

        if (ScreenFadeTransition.Instance != null)
        {
            ScreenFadeTransition.Instance.StartCoroutine(ScreenFadeTransition.Instance.PlayTransition(() =>
            {
                MoveToPosition(targetDoor.Value);
                GameManager.NotifyNewRoomEntered(nextRoom);
            }));
        }
        else
        {
            MoveToPosition(targetDoor.Value);
            GameManager.NotifyNewRoomEntered(nextRoom);
        }
    }

    private Vector3? GetEntryDoor(int roomIndex)
    {
        return GameManager.Instance.DungeonDictionary.TryGetValue(roomIndex, out GameManager.RoomData room) ? room.EntryDoorPosition : null;
    }

    private Vector3? GetExitDoor(int roomIndex)
    {
        return GameManager.Instance.DungeonDictionary.TryGetValue(roomIndex, out GameManager.RoomData room) ? room.ExitDoorPosition : null;
    }

    private Vector3? GetSpecialEntryDoor(int roomIndex)
    {
        return GameManager.Instance.DungeonDictionary.TryGetValue(roomIndex, out GameManager.RoomData room) ? room.SpecialEntryDoorPosition : null;
    }

    private Vector3? GetSpecialExitDoor(int roomIndex)
    {
        return GameManager.Instance.DungeonDictionary.TryGetValue(roomIndex, out GameManager.RoomData room) ? room.SpecialExitDoorPosition : null;
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