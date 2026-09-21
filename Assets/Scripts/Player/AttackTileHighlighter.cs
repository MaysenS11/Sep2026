using System.Collections.Generic;
using UnityEngine;

public class AttackTileHighlighter : MonoBehaviour
{
    [Header("Indicator Prefab & Settings")]
    [SerializeField] private GameObject attackIndicatorPrefab;
    [SerializeField] private int initialPoolSize = 8;

    private readonly List<GameObject> indicatorPool = new List<GameObject>();
    private readonly List<Vector2Int> targetTilesBuffer = new List<Vector2Int>(16);
    private PlayerMovement playerMovement;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        InitializePool();
    }

    private void OnEnable()
    {
        if (playerMovement != null)
        {
            playerMovement.OnAttackTargetingChanged += UpdateHighlight;
        }

        EventBus<RoomEnteredEvent>.Subscribe(OnRoomEntered);
        EventBus<EntityDiedEvent>.Subscribe(OnEntityDied);
    }

    private void OnDisable()
    {
        if (playerMovement != null)
        {
            playerMovement.OnAttackTargetingChanged -= UpdateHighlight;
        }

        EventBus<RoomEnteredEvent>.Unsubscribe(OnRoomEntered);
        EventBus<EntityDiedEvent>.Unsubscribe(OnEntityDied);

        HideAllIndicators();
    }

    private void Start()
    {
        if (indicatorPool.Count == 0)
        {
            InitializePool();
        }

        if (playerMovement != null)
        {
            playerMovement.NotifyTargetingChanged();
        }

        StartCoroutine(WaitForFadeAndRefresh());
    }

    private System.Collections.IEnumerator WaitForFadeAndRefresh()
    {
        while (ScreenFadeTransition.Instance != null && ScreenFadeTransition.Instance.IsTransitioning)
        {
            yield return null;
        }

        if (playerMovement != null)
        {
            playerMovement.NotifyTargetingChanged();
        }
    }

    private void InitializePool()
    {
        if (attackIndicatorPrefab == null)
        {
            attackIndicatorPrefab = Resources.Load<GameObject>("AttackPositionIndicator");
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreatePooledIndicator();
        }
    }

    private GameObject CreatePooledIndicator()
    {
        if (attackIndicatorPrefab == null) return null;

        GameObject indicator = Instantiate(attackIndicatorPrefab, transform);
        indicator.SetActive(false);
        indicatorPool.Add(indicator);
        return indicator;
    }

    private void UpdateHighlight(Vector2Int origin, Vector2 facingDir, AttackPatternData pattern)
    {
        if (ScreenFadeTransition.Instance != null && ScreenFadeTransition.Instance.IsTransitioning)
        {
            HideAllIndicators();
            return;
        }

        if (pattern == null)
        {
            Vector2Int cardinal = AttackPatternData.GetCardinalDirection(facingDir);
            PositionSingleIndicator(origin + cardinal);
            return;
        }

        pattern.GetAffectedTiles(origin, facingDir, targetTilesBuffer);
        int neededCount = targetTilesBuffer.Count;

        while (indicatorPool.Count < neededCount)
        {
            CreatePooledIndicator();
        }

        for (int i = 0; i < indicatorPool.Count; i++)
        {
            if (i < neededCount)
            {
                Vector3 worldPos = TileReservationSystem.GetTileCenterWorld(targetTilesBuffer[i], transform.position.z);
                indicatorPool[i].transform.position = worldPos;
                indicatorPool[i].SetActive(true);
            }
            else
            {
                indicatorPool[i].SetActive(false);
            }
        }
    }

    private void PositionSingleIndicator(Vector2Int cell)
    {
        if (indicatorPool.Count == 0)
        {
            CreatePooledIndicator();
        }

        if (indicatorPool.Count > 0 && indicatorPool[0] != null)
        {
            Vector3 worldPos = TileReservationSystem.GetTileCenterWorld(cell, transform.position.z);
            indicatorPool[0].transform.position = worldPos;
            indicatorPool[0].SetActive(true);
        }

        for (int i = 1; i < indicatorPool.Count; i++)
        {
            indicatorPool[i].SetActive(false);
        }
    }

    private void OnRoomEntered(RoomEnteredEvent evt)
    {
        if (playerMovement != null)
        {
            playerMovement.NotifyTargetingChanged();
        }
    }

    private void OnEntityDied(EntityDiedEvent evt)
    {
        if (evt.Entity == gameObject)
        {
            HideAllIndicators();
        }
    }

    public void HideAllIndicators()
    {
        for (int i = 0; i < indicatorPool.Count; i++)
        {
            if (indicatorPool[i] != null)
            {
                indicatorPool[i].SetActive(false);
            }
        }
    }
}
