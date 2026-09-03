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
    private float lastMoveTime = -999f; 
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
        
        attackAction.performed += OnAttackPerformed;
        interactAction.performed += OnInteractPerformed;
    }

    private void OnDisable()
    {
        attackAction.performed -= OnAttackPerformed;
        interactAction.performed -= OnInteractPerformed;

        inputActions.Disable();
    }

    private void Update()
    {
        HandleMovement();
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
        return Physics2D.OverlapCircle(targetPos, 0.2f, obstacleLayer) != null;
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        if (isMoving) return;

        animator.SetTrigger(attackTrigger);
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (isMoving) return;
    }
}