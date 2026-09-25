using UnityEngine;
using UnityEngine.UI;
using Chest;

public class StatsDisplayUI : MonoBehaviour
{
    [System.Serializable]
    public class StatRow
    {
        public StatType statType;
        public Image[] pips;
    }

    [Header("Stat Rows")]
    [SerializeField] private StatRow[] rows;

    private PlayerStats cachedPlayerStats;

    private void OnEnable()
    {
        EnsurePlayerStats();
        Refresh();
    }

    private void EnsurePlayerStats()
    {
        if (cachedPlayerStats == null)
        {
            cachedPlayerStats = Object.FindAnyObjectByType<PlayerStats>(FindObjectsInactive.Include);
        }

        RestoreReferencesIfMissing();
    }

    private void RestoreReferencesIfMissing()
    {
        if (rows == null || rows.Length == 0)
        {
            var detectedRows = new System.Collections.Generic.List<StatRow>();
            var statDefs = new (string label, StatType type)[]
            {
                ("RANGE", StatType.AttackRange),
                ("SPEED", StatType.Speed),
                ("ATTACK", StatType.AttackDamage),
                ("DEFENCE", StatType.Defence),
                ("HEALTH", StatType.Health)
            };

            for (int i = 0; i < statDefs.Length; i++)
            {
                Transform rowT = transform.Find($"Row_{statDefs[i].label}") ?? transform.Find($"Row_{statDefs[i].type}");
                if (rowT != null)
                {
                    Transform pipsContainer = rowT.Find("Pips");
                    Image[] pipImages = pipsContainer != null ? pipsContainer.GetComponentsInChildren<Image>(true) : null;
                    detectedRows.Add(new StatRow
                    {
                        statType = statDefs[i].type,
                        pips = pipImages
                    });
                }
            }

            if (detectedRows.Count > 0)
            {
                rows = detectedRows.ToArray();
            }
        }
        else
        {
            for (int r = 0; r < rows.Length; r++)
            {
                if (rows[r] == null) continue;
                if (rows[r].pips == null || rows[r].pips.Length == 0)
                {
                    string label = rows[r].statType.ToString();
                    Transform rowT = transform.Find($"Row_{label.ToUpper()}") ?? transform.Find($"Row_{label}");
                    if (rowT != null)
                    {
                        Transform pipsContainer = rowT.Find("Pips");
                        if (pipsContainer != null)
                        {
                            rows[r].pips = pipsContainer.GetComponentsInChildren<Image>(true);
                        }
                    }
                }
            }
        }
    }

    public void SetPlayerStats(PlayerStats playerStats)
    {
        cachedPlayerStats = playerStats;
        Refresh();
    }

    public void Refresh()
    {
        EnsurePlayerStats();
        if (rows == null) return;

        Core.Occupants.PlayerOccupant playerOcc = null;
        if (cachedPlayerStats != null && cachedPlayerStats.Occupant != null)
        {
            playerOcc = cachedPlayerStats.Occupant;
        }
        else if (GameManager.Instance != null && GameManager.Instance.Board != null)
        {
            playerOcc = GameManager.Instance.Board.FindPlayer();
        }

        foreach (var row in rows)
        {
            if (row == null) continue;
            int statValue = 1;
            if (playerOcc != null)
            {
                statValue = playerOcc.GetCurrentStatValue(row.statType);
            }
            else if (cachedPlayerStats != null)
            {
                statValue = cachedPlayerStats.GetCurrentStatValue(row.statType);
            }
            UpdateRowPips(row, statValue);
        }
    }

    public void DisplayCharacter(CharacterDefinition character)
    {
        if (character == null) return;

        EnsurePlayerStats();
        if (cachedPlayerStats != null)
        {
            cachedPlayerStats.SetCharacterDefinition(character);
        }

        Refresh();
    }

    [Header("Preview Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color previewColor = new Color(0.2f, 1f, 0.4f, 0.95f);

    public void PreviewStat(StatType statType)
    {
        EnsurePlayerStats();
        if (rows == null) return;

        Core.Occupants.PlayerOccupant playerOcc = null;
        if (cachedPlayerStats != null && cachedPlayerStats.Occupant != null)
        {
            playerOcc = cachedPlayerStats.Occupant;
        }
        else if (GameManager.Instance != null && GameManager.Instance.Board != null)
        {
            playerOcc = GameManager.Instance.Board.FindPlayer();
        }

        foreach (var row in rows)
        {
            if (row == null) continue;
            int currentValue = (playerOcc != null) 
                ? playerOcc.GetCurrentStatValue(row.statType) 
                : (cachedPlayerStats != null ? cachedPlayerStats.GetCurrentStatValue(row.statType) : 1);

            int previewValue = currentValue;
            if (row.statType == statType)
            {
                int currentTier = (playerOcc != null) 
                    ? playerOcc.GetUpgradeTier(statType) 
                    : (cachedPlayerStats != null ? cachedPlayerStats.GetUpgradeTier(statType) : 0);

                int nextTier = Mathf.Clamp(currentTier + 1, 0, 2);
                int[] values = (playerOcc != null) 
                    ? playerOcc.GetTierValues(statType) 
                    : (cachedPlayerStats != null ? cachedPlayerStats.GetTierValues(statType) : null);

                if (values != null && nextTier < values.Length)
                {
                    previewValue = Mathf.Clamp(values[nextTier], 1, 5);
                }
                else
                {
                    previewValue = Mathf.Min(5, currentValue + 1);
                }
            }

            UpdateRowPipsWithPreview(row, currentValue, previewValue, row.statType == statType);
        }
    }

    public void ClearPreview()
    {
        Refresh();
    }

    private void UpdateRowPipsWithPreview(StatRow row, int currentValue, int previewValue, bool isPreviewRow)
    {
        if (row.pips == null) return;

        for (int i = 0; i < row.pips.Length; i++)
        {
            if (row.pips[i] == null) continue;

            if (i < currentValue)
            {
                row.pips[i].gameObject.SetActive(true);
                row.pips[i].color = normalColor;
            }
            else if (isPreviewRow && i < previewValue)
            {
                row.pips[i].gameObject.SetActive(true);
                row.pips[i].color = previewColor;
            }
            else
            {
                row.pips[i].gameObject.SetActive(false);
            }
        }
    }

    private void UpdateRowPips(StatRow row, int currentValue)
    {
        if (row.pips == null) return;

        for (int i = 0; i < row.pips.Length; i++)
        {
            if (row.pips[i] == null) continue;

            bool isActive = i < currentValue;
            row.pips[i].gameObject.SetActive(isActive);
            if (isActive)
            {
                row.pips[i].color = normalColor;
            }
        }
    }
}

