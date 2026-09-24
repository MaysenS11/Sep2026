using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using Core.Board;

[RequireComponent(typeof(EntityStats))]
public class EnemyBase : MonoBehaviour
{
    [Header("Enemy Configuration")]
    [SerializeField] protected EnemyData enemyData;
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
    protected bool skipNextTurn = false;

    public EnemyData Data => enemyData;
    public EntityStats Stats
    {
        get
        {
            if (stats == null) stats = GetComponent<EntityStats>();
            return stats;
        }
    }
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

    protected virtual void Awake()
    {
        stats = GetComponent<EntityStats>();
        if (enemyData != null && stats != null)
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
        transform.position = BoardCoordinate.GridToWorldCenter(BoardCoordinate.WorldToGrid(transform.position), transform.position.z);
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

    public void SnapToGridCenter()
    {
        transform.position = BoardCoordinate.GridToWorldCenter(BoardCoordinate.WorldToGrid(transform.position), transform.position.z);
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

        if (enemyData is KingData kingData && !kingData.DamageSound.IsNull)
        {
            RuntimeManager.PlayOneShot(kingData.DamageSound);
        }
        else if (!hitSound.IsNull)
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
        if (evt.Entity != gameObject)
        {
            if (!hasDied && enemyData is KingData && currentRoomIndex >= 0)
            {
                if (evt.Entity != null && evt.Entity.TryGetComponent<EnemyBase>(out var deadEnemy) && deadEnemy.CurrentRoomIndex == currentRoomIndex)
                {
                    int dmg = 1;
                    var dm = Dungeon.DungeonManager.Instance != null ? Dungeon.DungeonManager.Instance : FindAnyObjectByType<Dungeon.DungeonManager>();
                    if (dm != null && dm.BossRoomPrefab != null)
                    {
                        dmg = dm.BossRoomPrefab.GetDamageForEnemy(deadEnemy.Data);
                    }
                    if (Stats != null)
                    {
                        Stats.TakeDamage(dmg, evt.Entity);
                    }
                }
            }
            return;
        }

        hasDied = true;

        SetHeartVisibility(false);

        EventBus<EntityDamagedEvent>.Unsubscribe(OnEntityDamaged);
        EventBus<EntityDiedEvent>.Unsubscribe(OnEntityDied);
        EventBus<AttackTargetingChangedEvent>.Unsubscribe(OnAttackTargetingChanged);
        EventBus<EnemyUnregisteredEvent>.Raise(new EnemyUnregisteredEvent(this));

        if (TryGetComponent<Collider2D>(out var col))
        {
            col.enabled = false;
        }

        if (enemyData is KingData kingDataOnDeath && !kingDataOnDeath.DeathSound.IsNull)
        {
            RuntimeManager.PlayOneShot(kingDataOnDeath.DeathSound);
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
        if (stats != null) stats.Initialize(data);
        UpdateHeartVisuals(stats != null ? stats.CurrentHealth : data.MaxHealth);
    }

    public Vector2Int GetGridPosition()
    {
        return BoardCoordinate.WorldToGrid(transform.position);
    }

    public int GetMovePriority()
    {
        return enemyData != null ? enemyData.MovePriority : 99;
    }
}
