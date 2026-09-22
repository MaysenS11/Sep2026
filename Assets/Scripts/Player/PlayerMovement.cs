using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using FMODUnity;

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
    private InputAction interactAction;
    private InputAction menuAction;
    private InputAction pattern1Action;
    private InputAction pattern2Action;
    private InputAction pattern3Action;

    private bool isMoving = false;
    private bool canTakeTurn = true;
    private float nextMoveTime = 0f;
    private bool hasEnteredInitialRoom;
    private float doorTriggerBlockedUntil;
    private Vector2 lastDirection = Vector2.down;
    private PlayerStats playerStats;
    private readonly Collider2D[] hitBuffer = new Collider2D[16];
    private readonly HashSet<IDamageable> damagedEntitiesBuffer = new HashSet<IDamageable>();
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
        interactAction = playerMap.FindAction("Interact");
        menuAction = playerMap.FindAction("Menu");
        pattern1Action = playerMap.FindAction("1");
        pattern2Action = playerMap.FindAction("2");
        pattern3Action = playerMap.FindAction("3");
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
        interactAction.performed += OnInteractPerformed;
        if (pattern1Action != null) pattern1Action.performed += OnPattern1Performed;
        if (pattern2Action != null) pattern2Action.performed += OnPattern2Performed;
        if (pattern3Action != null) pattern3Action.performed += OnPattern3Performed;
        if (menuAction != null)
        {
            menuAction.performed += OnMenuPerformed;
        }
    }

    private void OnDisable()
    {
        attackAction.performed -= OnAttackPerformed;
        interactAction.performed -= OnInteractPerformed;
        if (pattern1Action != null) pattern1Action.performed -= OnPattern1Performed;
        if (pattern2Action != null) pattern2Action.performed -= OnPattern2Performed;
        if (pattern3Action != null) pattern3Action.performed -= OnPattern3Performed;
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
        bool p1 = (pattern1Action != null && pattern1Action.triggered) || (Keyboard.current != null && (Keyboard.current[Key.Digit1].wasPressedThisFrame || Keyboard.current[Key.Numpad1].wasPressedThisFrame));
        bool p2 = (pattern2Action != null && pattern2Action.triggered) || (Keyboard.current != null && (Keyboard.current[Key.Digit2].wasPressedThisFrame || Keyboard.current[Key.Numpad2].wasPressedThisFrame));
        bool p3 = (pattern3Action != null && pattern3Action.triggered) || (Keyboard.current != null && (Keyboard.current[Key.Digit3].wasPressedThisFrame || Keyboard.current[Key.Numpad3].wasPressedThisFrame));

        if (p1)
        {
            SelectAttackPattern(0);
        }
        else if (p2)
        {
            SelectAttackPattern(1);
        }
        else if (p3)
        {
            SelectAttackPattern(2);
        }

        if (Keyboard.current != null && Keyboard.current[Key.E].wasPressedThisFrame)
        {
            TriggerInteract();
        }

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
            transform.position = TileReservationSystem.SnapToTileCenter(transform.position);
            NotifyTargetingChanged();
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
                NotifyTargetingChanged();
            }
            return;
        }

        bool dirChanged = lastDirection != inputDir;
        lastDirection = inputDir;
        if (animator != null)
        {
            animator.SetFloat(MoveX, lastDirection.x);
            animator.SetFloat(MoveY, lastDirection.y);
        }
        if (dirChanged)
        {
            NotifyTargetingChanged();
        }

        canTakeTurn = false;
        nextMoveTime = Time.time + moveCooldown;
        StartCoroutine(MoveToTile(targetPosition));
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

        AttackPatternData pattern = playerStats != null ? playerStats.CurrentAttackPattern : null;
        ExecuteAttack(pattern, damage);

        StartCoroutine(CompleteAttackTurn());
    }

    private void ExecuteAttack(AttackPatternData pattern, int damage)
    {
        damagedEntitiesBuffer.Clear();
        Vector2Int currentGrid = TileReservationSystem.WorldToGridTile(transform.position);

        if (pattern != null)
        {
            pattern.GetAffectedTiles(currentGrid, lastDirection, attackTilesBuffer);
            for (int i = 0; i < attackTilesBuffer.Count; i++)
            {
                Vector3 targetWorld = TileReservationSystem.GetTileCenterWorld(attackTilesBuffer[i], transform.position.z);
                DamageAtTile(targetWorld, damage, damagedEntitiesBuffer);
            }
        }
        else
        {
            Vector2Int cardinal = AttackPatternData.GetCardinalDirection(lastDirection);
            Vector3 targetWorld = TileReservationSystem.GetTileCenterWorld(currentGrid + cardinal, transform.position.z);
            DamageAtTile(targetWorld, damage, damagedEntitiesBuffer);
        }
    }

    public void NotifyTargetingChanged()
    {
        if (playerStats == null) playerStats = GetComponent<PlayerStats>();
        AttackPatternData pattern = playerStats != null ? playerStats.CurrentAttackPattern : null;
        Vector2Int currentGrid = TileReservationSystem.WorldToGridTile(transform.position);
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
        TriggerInteract();
    }

    private void TriggerInteract()
    {
        Debug.Log("interact pressed");
        if (isMoving || (ScreenFadeTransition.Instance != null && ScreenFadeTransition.Instance.IsTransitioning)) return;
        //animator.SetTrigger(interactTrigger);

        Vector2Int facingDir = new Vector2Int(Mathf.RoundToInt(lastDirection.x), Mathf.RoundToInt(lastDirection.y));
        if (facingDir == Vector2Int.zero) facingDir = Vector2Int.down;

        Vector2Int currentGrid = TileReservationSystem.WorldToGridTile(transform.position);
        Vector2Int targetGrid = currentGrid + facingDir;
        Vector3 targetPos = TileReservationSystem.GetTileCenterWorld(targetGrid, transform.position.z);

        Collider2D[] results = Physics2D.OverlapBoxAll(targetPos, new Vector2(tileSize * 0.8f, tileSize * 0.8f), 0f);
        for (int i = 0; i < results.Length; i++)
        {
            if (results[i] == null) continue;

            if (results[i].TryGetComponent<IInteractable>(out var interactable))
            {
                if (interactable.CanInteract)
                {
                    interactable.Interact(gameObject);
                    break;
                }
            }
            else if (results[i].TryGetComponent<Dungeon.Chest>(out var chest))
            {
                if (chest.TryOpen())
                {
                    Debug.Log("chest opening");
                }
                break;
            }
        }
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