using UnityEngine;
using Core.Board;

public enum HeartDepletionOrder
{
    FirstToLast,
    LastToFirst
}

public class EnemyHeartDisplay : MonoBehaviour
{
    [SerializeField] private GameObject heartsRoot;
    [SerializeField] private SpriteRenderer[] heartSlots;
    [SerializeField] private HeartDepletionOrder depletionOrder = HeartDepletionOrder.FirstToLast;
    [SerializeField] private Sprite fullHeartSprite;
    [SerializeField] private Sprite emptyHeartSprite;

    private GameObject rootEntity;
    private EnemyBase rootEnemy;
    private bool isDead;

    public GameObject HeartsRoot => heartsRoot;
    public SpriteRenderer[] HeartSlots => heartSlots;
    public HeartDepletionOrder DepletionOrder
    {
        get => depletionOrder;
        set => depletionOrder = value;
    }

    private void Awake()
    {
        CacheRootReferences();
        if (heartsRoot != null)
        {
            heartsRoot.SetActive(false);
        }
    }

    private void CacheRootReferences()
    {
        if (rootEntity == null)
        {
            rootEnemy = GetComponentInParent<EnemyBase>();
            rootEntity = rootEnemy != null ? rootEnemy.gameObject : transform.root.gameObject;
        }
    }

    private void OnEnable()
    {
        CacheRootReferences();
        EventBus<EntityDamagedEvent>.Subscribe(OnEntityDamaged);
        EventBus<EntityDiedEvent>.Subscribe(OnEntityDied);
        EventBus<AttackTargetingChangedEvent>.Subscribe(OnAttackTargetingChanged);
    }

    private void OnDisable()
    {
        EventBus<EntityDamagedEvent>.Unsubscribe(OnEntityDamaged);
        EventBus<EntityDiedEvent>.Unsubscribe(OnEntityDied);
        EventBus<AttackTargetingChangedEvent>.Unsubscribe(OnAttackTargetingChanged);
    }

    public void SetVisible(bool visible)
    {
        if (isDead) visible = false;

        if (heartsRoot != null && heartsRoot.activeSelf != visible)
        {
            heartsRoot.SetActive(visible);
        }
    }

    public void SetSprites(Sprite fullSprite, Sprite emptySprite)
    {
        if (fullSprite != null) fullHeartSprite = fullSprite;
        if (emptySprite != null) emptyHeartSprite = emptySprite;
        int maxHealth = (rootEnemy != null && rootEnemy.Data != null) ? rootEnemy.Data.MaxHealth : 1;
        UpdateHearts(maxHealth, maxHealth);
    }

    public void UpdateHearts(int currentHealth, int maxHealth)
    {
        if (heartSlots == null || heartSlots.Length == 0) return;

        int totalHearts = heartSlots.Length;
        int fullCount = 0;
        if (currentHealth > 0 && maxHealth > 0)
        {
            fullCount = Mathf.Clamp(Mathf.CeilToInt((float)currentHealth / maxHealth * totalHearts), 1, totalHearts);
        }

        int emptyCount = totalHearts - fullCount;

        for (int i = 0; i < totalHearts; i++)
        {
            if (heartSlots[i] == null) continue;

            bool isFull = (depletionOrder == HeartDepletionOrder.FirstToLast)
                ? (i >= emptyCount)
                : (i < fullCount);

            heartSlots[i].sprite = isFull ? fullHeartSprite : emptyHeartSprite;
        }
    }

    private void OnEntityDamaged(EntityDamagedEvent evt)
    {
        if (evt.Target != rootEntity && evt.Target != gameObject) return;

        int maxHealth = (rootEnemy != null && rootEnemy.Data != null) ? rootEnemy.Data.MaxHealth : evt.RemainingHealth;
        UpdateHearts(evt.RemainingHealth, maxHealth);
    }

    private void OnAttackTargetingChanged(AttackTargetingChangedEvent evt)
    {
        if (isDead)
        {
            SetVisible(false);
            return;
        }

        Vector2Int gridPos = BoardCoordinate.WorldToGrid(rootEntity != null ? rootEntity.transform.position : transform.position);
        bool inRange = evt.AffectedTiles != null && evt.AffectedTiles.Contains(gridPos);
        SetVisible(inRange);
    }

    private void OnEntityDied(EntityDiedEvent evt)
    {
        if (evt.Entity != rootEntity && evt.Entity != gameObject) return;

        isDead = true;
        SetVisible(false);
    }
}