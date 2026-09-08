using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    // Static reservation set to prevent multiple enemies from picking the same destination tile during simultaneous turns
    private static readonly HashSet<Vector2Int> reservedTiles = new HashSet<Vector2Int>();

    public static void ClearReservations()
    {
        reservedTiles.Clear();
    }

    public static bool ReserveTile(Vector2Int tileCoord)
    {
        return reservedTiles.Add(tileCoord);
    }

    public static void UnreserveTile(Vector2Int tileCoord)
    {
        reservedTiles.Remove(tileCoord);
    }

    public EntityStats Stats => stats;
    public bool IsMoving => isMoving;

    protected virtual void Awake()
    {
        stats = GetComponent<EntityStats>();
        if (enemyData != null)
        {
            stats.Initialize(enemyData);
        }
    }

    [Header("Animation & Death Settings")]
    [SerializeField] protected float destroyDelayAfterDeath = 0.5f;

    protected static readonly int damageHash = Animator.StringToHash("TakeDamage");
    protected static readonly int dieHash = Animator.StringToHash("Die");

    protected virtual void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterEnemy(this);
        }
        EventBus<EntityDamagedEvent>.Subscribe(OnEntityDamaged);
        EventBus<EntityDiedEvent>.Subscribe(OnEntityDied);
    }

    protected virtual void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterEnemy(this);
        }
        EventBus<EntityDamagedEvent>.Unsubscribe(OnEntityDamaged);
        EventBus<EntityDiedEvent>.Unsubscribe(OnEntityDied);
    }

    protected bool hasDied = false;

    protected virtual void OnEntityDamaged(EntityDamagedEvent evt)
    {
        if (hasDied || evt.Target != gameObject || (stats != null && stats.IsDead)) return;

        if (animator != null)
        {
            animator.SetTrigger(damageHash);
        }
    }

    protected virtual void OnEntityDied(EntityDiedEvent evt)
    {
        if (hasDied || evt.Entity != gameObject) return;
        hasDied = true;

        // Unsubscribe immediately so no subsequent damage or death events trigger animations
        EventBus<EntityDamagedEvent>.Unsubscribe(OnEntityDamaged);
        EventBus<EntityDiedEvent>.Unsubscribe(OnEntityDied);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterEnemy(this);
        }

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

    public abstract IEnumerator ExecuteTurnCoroutine(Transform playerTransform);

    public bool IsTileBlocked(Vector3 targetPos)
    {
        Vector2Int gridPos = new Vector2Int(Mathf.RoundToInt(targetPos.x), Mathf.RoundToInt(targetPos.y));
        if (reservedTiles.Contains(gridPos)) return true;

        Collider2D hit = Physics2D.OverlapBox(targetPos, new Vector2(tileSize * 0.8f, tileSize * 0.8f), 0f, blockingLayers);
        if (hit != null && hit.gameObject != gameObject)
        {
            return true;
        }

        return false;
    }

    protected IEnumerator StepToTile(Vector3 targetPos, float speed)
    {
        isMoving = true;
        Vector2Int destGridPos = new Vector2Int(Mathf.RoundToInt(targetPos.x), Mathf.RoundToInt(targetPos.y));
        ReserveTile(destGridPos);

        while (Vector3.Distance(transform.position, targetPos) > 0.001f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPos;
        isMoving = false;
        UnreserveTile(destGridPos);
    }

    protected virtual void PerformAttack(IDamageable target)
    {
        if (target != null && stats != null)
        {
            target.TakeDamage(stats.AttackDamage, gameObject);
        }
    }

    /// <summary>
    /// Executes the shared attack-and-push mechanic: the enemy moves onto the player's tile,
    /// triggering PlayerPushedEvent which pushes the player in the movement direction and deals damage.
    /// </summary>
    protected IEnumerator AttackAndPushPlayer(Transform playerTransform, Vector3 playerTilePos, Vector2 pushDirection, float moveSpeed)
    {
        int damage = stats != null ? stats.AttackDamage : 2;
        Vector3 playerPushedPos = playerTilePos + new Vector3(pushDirection.x, pushDirection.y, 0) * tileSize;

        // Fire PlayerPushedEvent so player smoothly slides in the push direction and takes damage
        EventBus<PlayerPushedEvent>.Raise(new PlayerPushedEvent(
            playerTransform.gameObject,
            playerPushedPos,
            pushDirection,
            moveSpeed * 1.5f,
            damage,
            gameObject
        ));

        // Enemy moves diagonally/orthogonally onto the player's tile
        yield return StartCoroutine(StepToTile(playerTilePos, moveSpeed));
    }
}
