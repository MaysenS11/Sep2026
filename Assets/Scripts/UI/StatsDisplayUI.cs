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

        foreach (var row in rows)
        {
            if (row == null) continue;
            int statValue = cachedPlayerStats != null ? cachedPlayerStats.GetCurrentStatValue(row.statType) : 1;
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

    private void UpdateRowPips(StatRow row, int currentValue)
    {
        if (row.pips == null) return;

        for (int i = 0; i < row.pips.Length; i++)
        {
            if (row.pips[i] == null) continue;

            bool isActive = i < currentValue;
            row.pips[i].gameObject.SetActive(isActive);
        }
    }
}

