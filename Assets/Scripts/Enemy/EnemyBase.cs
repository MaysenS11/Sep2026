using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

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

    [Header("Hearts Display")]
    [SerializeField] protected EnemyHeartDisplay heartDisplay;
    [SerializeField] protected GameObject heartsRoot;
    [SerializeField] protected SpriteRenderer[] heartSlots;

    protected EventReference moveSound;
    protected EventReference hitSound;

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
        if (heartDisplay == null)
        {
            heartDisplay = GetComponentInChildren<EnemyHeartDisplay>(true);
        }
        if (heartDisplay != null && enemyData != null)
        {
            heartDisplay.SetSprites(enemyData.HeartFullSprite, enemyData.HeartEmptySprite);
        }
        SetHeartVisibility(false);
        UpdateHeartVisuals(stats != null ? stats.CurrentHealth : (enemyData != null ? enemyData.MaxHealth : 0));
    }

    protected virtual void Start()
    {
        transform.position = TileReservationSystem.SnapToTileCenter(transform.position);
        if (moveSound.IsNull || hitSound.IsNull)
        {
            EventBus<RequestSharedAudioEvent>.Raise(new RequestSharedAudioEvent());
        }
        SetHeartVisibility(PlayerMovement.IsPositionInAttackRange(GetGridPosition()));
    }

    [Header("Animation & Death Settings")]
    [SerializeField] protected float destroyDelayAfterDeath = 0.5f;

    protected static readonly int damageHash = Animator.StringToHash("TakeDamage");
    protected static readonly int dieHash = Animator.StringToHash("Die");

    protected int currentRoomIndex = -1;
    protected bool isAggroed = false;

    public int CurrentRoomIndex
    {
        get => currentRoomIndex;
        set => currentRoomIndex = value;
    }

    public bool IsAggroed
    {
        get => isAggroed;
        set => isAggroed = value;
    }

    protected virtual void OnEnable()
    {
        EventBus<EnemyRegisteredEvent>.Raise(new EnemyRegisteredEvent(this));
        EventBus<EntityDamagedEvent>.Subscribe(OnEntityDamaged);
        EventBus<EntityDiedEvent>.Subscribe(OnEntityDied);
        EventBus<SharedAudioConfiguredEvent>.Subscribe(OnSharedAudioConfigured);
        EventBus<RoomEnteredEvent>.Subscribe(OnRoomEntered);
        EventBus<AttackTargetingChangedEvent>.Subscribe(OnAttackTargetingChanged);
    }

    protected virtual void OnDisable()
    {
        EventBus<EnemyUnregisteredEvent>.Raise(new EnemyUnregisteredEvent(this));
        EventBus<EntityDamagedEvent>.Unsubscribe(OnEntityDamaged);
        EventBus<EntityDiedEvent>.Unsubscribe(OnEntityDied);
        EventBus<SharedAudioConfiguredEvent>.Unsubscribe(OnSharedAudioConfigured);
        EventBus<RoomEnteredEvent>.Unsubscribe(OnRoomEntered);
        EventBus<AttackTargetingChangedEvent>.Unsubscribe(OnAttackTargetingChanged);
        EventBus<EnemyPathDebugClearedEvent>.Raise(new EnemyPathDebugClearedEvent(this));
    }

    protected virtual void OnRoomEntered(RoomEnteredEvent evt)
    {
        if (currentRoomIndex < 0)
        {
            Vector2Int myTile = GetGridPosition();
            if (evt.Room != null &&
                myTile.x >= evt.Room.WorldOriginTile.x && myTile.x < evt.Room.WorldOriginTile.x + evt.Room.Size.x &&
                myTile.y >= evt.Room.WorldOriginTile.y && myTile.y < evt.Room.WorldOriginTile.y + evt.Room.Size.y)
            {
                currentRoomIndex = evt.Room.RoomIndex;
            }
        }

        if (evt.Room != null && currentRoomIndex == evt.Room.RoomIndex)
        {
            isAggroed = true;
        }
        else
        {
            isAggroed = false;
        }
    }

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        SnapToGridCenter();
    }

    public void SnapToGridCenter()
    {
        transform.position = TileReservationSystem.SnapToTileCenter(transform.position);
    }

    private void OnSharedAudioConfigured(SharedAudioConfiguredEvent evt)
    {
        moveSound = evt.MoveSound;
        hitSound = evt.HitSound;
    }

    protected bool hasDied = false;

    protected virtual void OnEntityDamaged(EntityDamagedEvent evt)
    {
        if (evt.Target != gameObject) return;

        if (!hitSound.IsNull)
        {
            RuntimeManager.PlayOneShot(hitSound);
        }

        if (evt.Source != null && evt.Source.GetComponent<PlayerMovement>() != null)
        {
            skipNextTurn = true;
        }

        if (animator != null)
        {
            animator.SetTrigger(damageHash);
        }

        UpdateHeartVisuals(evt.RemainingHealth);
    }

    public void SetHeartVisibility(bool visible)
    {
        if (hasDied) visible = false;

        if (heartDisplay != null)
        {
            heartDisplay.SetVisible(visible);
        }
        else if (heartsRoot != null && heartsRoot.activeSelf != visible)
        {
            heartsRoot.SetActive(visible);
        }
    }

    protected virtual void OnAttackTargetingChanged(AttackTargetingChangedEvent evt)
    {
        if (hasDied)
        {
            SetHeartVisibility(false);
            return;
        }

        bool inRange = evt.AffectedTiles != null && evt.AffectedTiles.Contains(GetGridPosition());
        SetHeartVisibility(inRange);
    }

    public virtual void UpdateHeartVisuals(int remainingHealth)
    {
        int maxHealth = stats != null ? stats.MaxHealth : (enemyData != null ? enemyData.MaxHealth : remainingHealth);
        if (heartDisplay != null)
        {
            if (enemyData != null)
            {
                heartDisplay.SetSprites(enemyData.HeartFullSprite, enemyData.HeartEmptySprite);
            }
            heartDisplay.UpdateHearts(remainingHealth, maxHealth);
            return;
        }

        if (heartSlots == null || enemyData == null) return;

        for (int i = 0; i < heartSlots.Length; i++)
        {
            if (heartSlots[i] == null) continue;
            heartSlots[i].sprite = (i < remainingHealth) ? enemyData.HeartFullSprite : enemyData.HeartEmptySprite;
        }
    }

    protected virtual void OnEntityDied(EntityDiedEvent evt)
    {
        if (hasDied || evt.Entity != gameObject) return;
        hasDied = true;

        SetHeartVisibility(false);

        EventBus<EntityDamagedEvent>.Unsubscribe(OnEntityDamaged);
        EventBus<EntityDiedEvent>.Unsubscribe(OnEntityDied);
        EventBus<AttackTargetingChangedEvent>.Unsubscribe(OnAttackTargetingChanged);
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
        UpdateHeartVisuals(stats != null ? stats.CurrentHealth : data.MaxHealth);
    }

    public abstract MoveIntent PlanMove(Transform playerTransform);
    public virtual IEnumerator ExecuteMove()
    {
        if (!currentIntent.HasMove || plannedPath == null || plannedPath.Count == 0)
            yield break;

        float speed = enemyData != null ? enemyData.StepSpeed : 5f;

        if (currentIntent.IsAttack && attackPlayerTransform != null)
        {
            if (enemyData != null && !enemyData.AttackSound.IsNull)
            {
                RuntimeManager.PlayOneShot(enemyData.AttackSound);
            }

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

        SetHeartVisibility(PlayerMovement.IsPositionInAttackRange(GetGridPosition()));

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
        if (!moveSound.IsNull)
        {
            RuntimeManager.PlayOneShot(moveSound);
        }

        while (Vector3.Distance(transform.position, targetPos) > 0.001f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
            yield return null;
        }

        transform.position = TileReservationSystem.SnapToTileCenter(targetPos);
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
