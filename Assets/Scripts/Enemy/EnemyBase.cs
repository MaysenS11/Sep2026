using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct MoveIntent
{
    public List<Vector2Int> Path;
    public Vector3 FinalDestination;
    public Vector2 Direction;
    public bool IsAttack;
    public Vector2Int? PlayerPushTile;
    public bool HasMove;
}

[RequireComponent(typeof(EntityStats))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Enemy Configuration")]
    [SerializeField] protected EnemyData enemyData;
    [SerializeField] protected float tileSize = 1.0f;
    [SerializeField] protected LayerMask blockingLayers;
    [SerializeField] protected Animator animator;

    protected EntityStats stats;
    protected bool isMoving;
    protected Vector2 lastDirection = Vector2.down;
    protected Rigidbody2D rb;

    protected MoveIntent currentIntent;
    protected List<Vector2Int> plannedPath;
    protected Transform attackPlayerTransform;
    protected bool skipNextTurn = false;

    public EnemyData Data => enemyData;
    public EntityStats Stats => stats;
    public bool IsMoving => isMoving;
    public bool SkipNextTurn
    {
        get => skipNextTurn;
        set => skipNextTurn = value;
    }

    public virtual bool ShouldSkipTurn()
    {
        if (skipNextTurn)
        {
            skipNextTurn = false;
            return true;
        }
        return false;
    }

    public List<Vector2Int> PlannedPath
    {
        get => plannedPath;
        set => plannedPath = value;
    }

    public MoveIntent CurrentIntent
    {
        get => currentIntent;
        set => currentIntent = value;
    }

    protected virtual void Awake()
    {
        stats = GetComponent<EntityStats>();
        rb = GetComponent<Rigidbody2D>();
        if (enemyData != null)
        {
            stats.Initialize(enemyData);
        }
    }

    protected virtual void Start()
    {
        transform.position = TileReservationSystem.SnapToTileCenter(transform.position);
    }

    [Header("Animation & Death Settings")]
    [SerializeField] protected float destroyDelayAfterDeath = 0.5f;

    protected static readonly int damageHash = Animator.StringToHash("TakeDamage");
    protected static readonly int dieHash = Animator.StringToHash("Die");

    protected virtual void OnEnable()
    {
        EventBus<EnemyRegisteredEvent>.Raise(new EnemyRegisteredEvent(this));
        EventBus<EntityDamagedEvent>.Subscribe(OnEntityDamaged);
        EventBus<EntityDiedEvent>.Subscribe(OnEntityDied);
    }

    protected virtual void OnDisable()
    {
        EventBus<EnemyUnregisteredEvent>.Raise(new EnemyUnregisteredEvent(this));
        EventBus<EntityDamagedEvent>.Unsubscribe(OnEntityDamaged);
        EventBus<EntityDiedEvent>.Unsubscribe(OnEntityDied);
        EventBus<EnemyPathDebugClearedEvent>.Raise(new EnemyPathDebugClearedEvent(this));
    }

    protected bool hasDied = false;

    protected virtual void OnEntityDamaged(EntityDamagedEvent evt)
    {
        if (evt.Target != gameObject) return;

        if (evt.Source != null && evt.Source.GetComponent<PlayerMovement>() != null)
        {
            skipNextTurn = true;
        }

        if (animator != null)
        {
            animator.SetTrigger(damageHash);
        }
    }

    protected virtual void OnEntityDied(EntityDiedEvent evt)
    {
        if (hasDied || evt.Entity != gameObject) return;
        hasDied = true;

        EventBus<EntityDamagedEvent>.Unsubscribe(OnEntityDamaged);
        EventBus<EntityDiedEvent>.Unsubscribe(OnEntityDied);
        EventBus<EnemyPathDebugClearedEvent>.Raise(new EnemyPathDebugClearedEvent(this));
        EventBus<EnemyUnregisteredEvent>.Raise(new EnemyUnregisteredEvent(this));

        if (TryGetComponent<Collider2D>(out var col))
        {
            col.enabled = false;
        }

        if (animator != null)
        {
            animator.ResetTrigger(damageHash);
            animator.SetTrigger(dieHash);
        }

        Destroy(gameObject, destroyDelayAfterDeath);
    }

    public virtual void SetData(EnemyData data)
    {
        enemyData = data;
        if (stats == null) stats = GetComponent<EntityStats>();
        stats.Initialize(data);
    }

    public abstract MoveIntent PlanMove(Transform playerTransform);
    public virtual IEnumerator ExecuteMove()
    {
        if (!currentIntent.HasMove || plannedPath == null || plannedPath.Count == 0)
            yield break;

        float speed = enemyData != null ? enemyData.StepSpeed : 5f;

        if (currentIntent.IsAttack && attackPlayerTransform != null)
        {
            int damage = stats != null ? stats.AttackDamage : 2;
            Vector3 pushDir3 = new Vector3(currentIntent.Direction.x, currentIntent.Direction.y, 0);
            Vector3 playerPushedPos = currentIntent.FinalDestination + pushDir3 * tileSize;

            EventBus<PlayerPushedEvent>.Raise(new PlayerPushedEvent(
                attackPlayerTransform.gameObject,
                playerPushedPos,
                currentIntent.Direction,
                speed * 1.5f,
                damage,
                gameObject
            ));
        }

        lastDirection = currentIntent.Direction;

        EventBus<EnemyPathDebugEvent>.Raise(new EnemyPathDebugEvent(this, plannedPath, GetGizmoColor()));

        for (int i = 0; i < plannedPath.Count; i++)
        {
            Vector3 stepTarget = TileReservationSystem.GetTileCenterWorld(plannedPath[i], transform.position.z);
            yield return StartCoroutine(StepToTile(stepTarget, speed));
        }

        transform.position = TileReservationSystem.SnapToTileCenter(transform.position);

        plannedPath = null;
        EventBus<EnemyPathDebugClearedEvent>.Raise(new EnemyPathDebugClearedEvent(this));
    }

    public Vector2Int GetGridPosition()
    {
        return TileReservationSystem.WorldToGridTile(transform.position);
    }
    public int GetMovePriority()
    {
        return enemyData != null ? enemyData.MovePriority : 99;
    }

    public bool IsTileBlocked(Vector3 targetPos)
    {
        return TileReservationSystem.IsTileBlocked(targetPos, this, tileSize, blockingLayers);
    }

    public void SetKinematic(bool kinematic)
    {
        if (rb != null)
        {
            rb.bodyType = kinematic ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
        }
    }

    protected IEnumerator StepToTile(Vector3 targetPos, float speed)
    {
        isMoving = true;

        while (Vector3.Distance(transform.position, targetPos) > 0.001f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPos;
        isMoving = false;
    }

    protected virtual void PerformAttack(IDamageable target)
    {
        if (target != null && stats != null)
        {
            target.TakeDamage(stats.AttackDamage, gameObject);
        }
    }

    public Color GetGizmoColor()
    {
        if (enemyData == null) return Color.white;

        switch (enemyData.MovementPattern)
        {
            case EnemyMovementPattern.KnightMove:
                return Color.magenta;
            case EnemyMovementPattern.SingleMove:
                return Color.green;
            case EnemyMovementPattern.BishopMove:
                return Color.cyan;
            case EnemyMovementPattern.RookMove:
                return Color.yellow;
            case EnemyMovementPattern.QueenMove:
                return Color.red;
            default:
                return Color.white;
        }
    }

    private void OnDrawGizmos()
    {
        DrawPathGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        DrawPathGizmos();
    }

    private void DrawPathGizmos()
    {
        if (plannedPath == null || plannedPath.Count == 0) return;

        Gizmos.color = GetGizmoColor();
        Vector3 boxSize = new Vector3(tileSize * 0.95f, tileSize * 0.95f, 0.1f);

        for (int i = 0; i < plannedPath.Count; i++)
        {
            Vector3 tileWorldPos = TileReservationSystem.GetTileCenterWorld(plannedPath[i], transform.position.z);
            Gizmos.DrawWireCube(tileWorldPos, boxSize);
        }
    }
}
