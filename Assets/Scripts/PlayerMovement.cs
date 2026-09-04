using System.Collections;
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

    private InputAction moveAction;
    private InputAction attackAction;
    private InputAction interactAction;

    private bool isMoving = false;
    private bool hasEnteredInitialRoom;
    private float lastMoveTime = -999f; 
    private float doorTriggerBlockedUntil;
    private Vector2 lastDirection = Vector2.down;

    private void Awake()
    {
        if (animator == null) 
        {
            Debug.LogError("Animator component is missing!");
            return;
        }

        var playerMap = inputActions.FindActionMap("Player");
        moveAction = playerMap.FindAction("Move");
        attackAction = playerMap.FindAction("Attack");
        interactAction = playerMap.FindAction("Interact");
    }

    private void OnEnable()
    {
        inputActions.Enable();
        GameManager.DoorTriggered += OnDoorTriggered;
        GameManager.NewRoomEntered += OnNewRoomEntered;
        attackAction.performed += OnAttackPerformed;
        interactAction.performed += OnInteractPerformed;
    }

    private void OnDisable()
    {
        attackAction.performed -= OnAttackPerformed;
        interactAction.performed -= OnInteractPerformed;
        GameManager.DoorTriggered -= OnDoorTriggered;
        GameManager.NewRoomEntered -= OnNewRoomEntered;

        inputActions.Disable();
    }

    private void Update()
    {
        HandleMovement();
    }

    private void Start()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.DungeonDictionary.TryGetValue(GameManager.Instance.CurrentRoomIndex, out GameManager.RoomData room))
        {
            MoveToPosition(room.WorldCenterPosition);
        }
    }

    private void HandleMovement()
    {
        if (isMoving || Time.time < lastMoveTime + moveCooldown)  return;

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

        lastDirection = inputDir;
        Vector3 targetPosition = transform.position + new Vector3(inputDir.x, inputDir.y, 0) * tileSize;

        if (!IsTileBlocked(targetPosition))
        {
            lastMoveTime = Time.time; 
            StartCoroutine(MoveToTile(targetPosition));
        }
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
    }

    private bool IsTileBlocked(Vector3 targetPos)
    {
        return Physics2D.OverlapBox(targetPos, new Vector2(tileSize * 0.8f, tileSize * 0.8f), 0f, obstacleLayer) != null;
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        if (isMoving) return;

        animator.SetTrigger(attackTrigger);
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (isMoving) return;
        animator.SetTrigger(interactTrigger);
    }

    private void OnDoorTriggered(DoorType doorType, Vector2Int doorTilePosition)
    {
        if (isMoving || Time.time < doorTriggerBlockedUntil || GameManager.Instance == null) return;

        TransitionThroughDoor(doorType);
        doorTriggerBlockedUntil = Time.time + 0.2f;
    }

    private void OnNewRoomEntered(GameManager.RoomData room)
    {
        if (hasEnteredInitialRoom || room.RoomIndex != 0) return;
        hasEnteredInitialRoom = true;
        MoveToPosition(room.WorldCenterPosition);
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

        MoveToPosition(targetDoor.Value);
        GameManager.NotifyNewRoomEntered(nextRoom);
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

    private void OnDrawGizmos()
{
    Gizmos.color = Color.red;
    // Calculates where the next tile check will happen based on last direction
    Vector3 testPos = transform.position + new Vector3(lastDirection.x, lastDirection.y, 0) * tileSize;
    
    // Draw both the circle check and a square box check
    Gizmos.DrawWireSphere(testPos, 0.2f);
    Gizmos.DrawWireCube(testPos, new Vector3(tileSize * 0.8f, tileSize * 0.8f, 0f));
}
}