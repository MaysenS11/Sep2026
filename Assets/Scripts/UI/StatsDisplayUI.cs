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

    private void OnEnable()
    {
        RestoreReferencesIfMissing();
        Refresh();
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

    public void Refresh()
    {
        RestoreReferencesIfMissing();
        if (rows == null) return;

        Core.Occupants.PlayerOccupant playerOcc = GameManager.Instance?.Board?.FindPlayer();

        foreach (var row in rows)
        {
            if (row == null) continue;
            int statValue = playerOcc != null ? playerOcc.GetCurrentStatValue(row.statType) : 1;
            UpdateRowPips(row, statValue);
        }
    }

    public void DisplayCharacter(CharacterDefinition character)
    {
        if (character == null) return;

        RestoreReferencesIfMissing();
        if (rows == null) return;

        foreach (var row in rows)
        {
            if (row == null) continue;
            int baseValue = GetBaseStatValue(character, row.statType);
            UpdateRowPips(row, baseValue);
        }
    }

    private int GetBaseStatValue(CharacterDefinition character, StatType statType)
    {
        switch (statType)
        {
            case StatType.AttackDamage:
                return (character.AttackTierValues != null && character.AttackTierValues.Length > 0) ? character.AttackTierValues[0] : 1;
            case StatType.Defence:
                return (character.DefenceTierValues != null && character.DefenceTierValues.Length > 0) ? character.DefenceTierValues[0] : 0;
            case StatType.Health:
                return (character.HealthTierValues != null && character.HealthTierValues.Length > 0) ? character.HealthTierValues[0] : 6;
            case StatType.AttackRange:
                return (character.RangeTierValues != null && character.RangeTierValues.Length > 0) ? character.RangeTierValues[0] : 1;
            case StatType.Speed:
                return (character.SpeedTierValues != null && character.SpeedTierValues.Length > 0) ? character.SpeedTierValues[0] : 1;
            default:
                return 1;
        }
    }

    [Header("Preview Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color previewColor = new Color(0.2f, 1f, 0.4f, 0.95f);

    public void PreviewStat(StatType statType)
    {
        RestoreReferencesIfMissing();
        if (rows == null) return;

        Core.Occupants.PlayerOccupant playerOcc = GameManager.Instance?.Board?.FindPlayer();

        foreach (var row in rows)
        {
            if (row == null) continue;
            int currentValue = playerOcc != null ? playerOcc.GetCurrentStatValue(row.statType) : 1;

            int previewValue = currentValue;
            if (row.statType == statType)
            {
                int currentTier = playerOcc != null ? playerOcc.GetUpgradeTier(statType) : 0;
                int nextTier = Mathf.Clamp(currentTier + 1, 0, 2);
                int[] values = playerOcc?.GetTierValues(statType);

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

