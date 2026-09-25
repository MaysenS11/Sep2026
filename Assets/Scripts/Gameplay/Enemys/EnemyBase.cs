using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using Core.Board;
using Core.Occupants;

public class EnemyBase : MonoBehaviour
{
    [Header("Enemy Configuration")]
    [SerializeField] protected EnemyData enemyData;
    [SerializeField] protected EnemySettings enemySettings;
    [SerializeField] protected Animator animator;

    [Header("Hearts Display")]
    [SerializeField] protected EnemyHeartDisplay heartDisplay;
    [SerializeField] protected GameObject heartsRoot;
    [SerializeField] protected SpriteRenderer[] heartSlots;

    protected EventReference moveSound;
    protected EventReference hitSound;
    protected EventReference deathSound;

    protected bool isMoving;
    protected Vector2 lastDirection = Vector2.down;
    protected bool skipNextTurn = false;

    public EnemyData Data => enemyData;
    public EnemySettings Settings => enemySettings;
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
        if (heartDisplay == null)
        {
            heartDisplay = GetComponentInChildren<EnemyHeartDisplay>(true);
        }
        if (heartDisplay != null && enemySettings != null)
        {
            heartDisplay.SetSprites(enemySettings.HeartFullSprite, enemySettings.HeartEmptySprite);
        }
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
        deathSound = evt.DeathSound;
    }

    protected bool hasDied = false;

    protected virtual void OnEntityDamaged(EntityDamagedEvent evt)
    {
        if (evt.Target != gameObject) return;

        bool isKing = (enemyData != null && enemyData.IsBoss) || gameObject.name.Contains("King");
        if (isKing && enemyData != null && !enemyData.DamageSound.IsNull)
        {
            RuntimeManager.PlayOneShot(enemyData.DamageSound);
        }
        else if (enemyData != null && !enemyData.DamageSound.IsNull)
        {
            RuntimeManager.PlayOneShot(enemyData.DamageSound);
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
        int maxHealth = enemyData != null ? enemyData.MaxHealth : remainingHealth;
        if (heartDisplay != null)
        {
            if (enemySettings != null)
            {
                heartDisplay.SetSprites(enemySettings.HeartFullSprite, enemySettings.HeartEmptySprite);
            }
            heartDisplay.UpdateHearts(remainingHealth, maxHealth);
            return;
        }

        if (heartSlots == null || enemySettings == null) return;

        for (int i = 0; i < heartSlots.Length; i++)
        {
            if (heartSlots[i] == null) continue;
            heartSlots[i].sprite = (i < remainingHealth) ? enemySettings.HeartFullSprite : enemySettings.HeartEmptySprite;
        }
    }

    protected static readonly int decapitateHash = Animator.StringToHash("Decapitate");

    protected virtual void OnEntityDied(EntityDiedEvent evt)
    {
        if (evt.Entity != gameObject)
        {
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

        bool isKing = (enemyData != null && enemyData.IsBoss) || gameObject.name.Contains("King");
        if (isKing)
        {
            if (animator != null)
            {
                animator.ResetTrigger(damageHash);
                animator.SetTrigger(dieHash);
                animator.SetTrigger(decapitateHash);
            }

            if (enemyData != null && !enemyData.DeathSound.IsNull)
            {
                RuntimeManager.PlayOneShot(enemyData.DeathSound);
            }
            else if (!deathSound.IsNull)
            {
                RuntimeManager.PlayOneShot(deathSound);
            }

            StartCoroutine(KingDefeatSequence());
            return;
        }

        if (animator != null)
        {
            animator.ResetTrigger(damageHash);
            animator.SetTrigger(dieHash);
        }

        if (enemyData != null && !enemyData.DeathSound.IsNull)
        {
            RuntimeManager.PlayOneShot(enemyData.DeathSound);
        }
        else if (!deathSound.IsNull)
        {
            RuntimeManager.PlayOneShot(deathSound);
        }

        Destroy(gameObject, destroyDelayAfterDeath);
    }

    private System.Collections.IEnumerator KingDefeatSequence()
    {
        yield return new WaitForSeconds(destroyDelayAfterDeath > 0 ? destroyDelayAfterDeath : 1.0f);

        float runTime = UIManager.Instance != null ? UIManager.Instance.ElapsedTime : 0f;
        string maskId = "Default";
        if (CharacterSelectData.SelectedCharacter != null)
        {
            maskId = !string.IsNullOrEmpty(CharacterSelectData.SelectedCharacter.CharacterName)
                ? CharacterSelectData.SelectedCharacter.CharacterName
                : CharacterSelectData.SelectedCharacter.name;
        }

        string defeatKey = $"first_king_defeat_{maskId}";
        bool isFirstDefeat = PlayerPrefs.GetInt(defeatKey, 0) == 0;
        if (isFirstDefeat)
        {
            PlayerPrefs.SetInt(defeatKey, 1);
            int currentKeys = PlayerPrefs.GetInt("MaskKeys", 0) + 1;
            PlayerPrefs.SetInt("MaskKeys", currentKeys);
            PlayerPrefs.Save();
        }

        EventBus<GameWonEvent>.Raise(new GameWonEvent(runTime, maskId, isFirstDefeat));
        EventBus<GameStateChangedEvent>.Raise(new GameStateChangedEvent(GameState.Gameplay, GameState.GameOver));

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowWinScreen();
        }

        Destroy(gameObject, 0.5f);
    }

    public virtual void SetData(EnemyData data)
    {
        enemyData = data;
        if (heartDisplay != null && enemySettings != null)
        {
            heartDisplay.SetSprites(enemySettings.HeartFullSprite, enemySettings.HeartEmptySprite);
        }
        UpdateHeartVisuals(data != null ? data.MaxHealth : 1);
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
