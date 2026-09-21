using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Chest
{
    public class ChestStatDisplayUI : MonoBehaviour
    {
        [System.Serializable]
        public class StatRow
        {
            public string label;
            public StatType statType;
            public TMP_Text labelText;
            public Image[] pips;
        }

        [Header("Sprites")]
        [SerializeField] private Sprite unlockedSprite;
        [SerializeField] private Sprite lockedSprite;
        [SerializeField] private Color unlockedColor = Color.white;
        [SerializeField] private Color lockedColor = new Color(0.35f, 0.35f, 0.35f, 0.8f);

        [Header("Stat Rows")]
        [SerializeField] private StatRow[] rows;

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            PlayerStats stats = UnityEngine.Object.FindAnyObjectByType<PlayerStats>(FindObjectsInactive.Include);
            if (rows == null) return;

            foreach (var row in rows)
            {
                if (row == null) continue;
                if (row.labelText != null && !string.IsNullOrEmpty(row.label))
                {
                    row.labelText.text = row.label;
                }

                int tier = stats != null ? stats.GetUpgradeTier(row.statType) : 0;
                UpdateRowPips(row, tier);
            }
        }

        public void RefreshWithTiers(int rangeTier, int speedTier, int defenceTier, int damageTier)
        {
            if (rows == null) return;
            foreach (var row in rows)
            {
                if (row == null) continue;
                int tier = 0;
                switch (row.statType)
                {
                    case StatType.AttackRange: tier = rangeTier; break;
                    case StatType.Speed: tier = speedTier; break;
                    case StatType.Defence: tier = defenceTier; break;
                    case StatType.AttackDamage: tier = damageTier; break;
                }
                UpdateRowPips(row, tier);
            }
        }

        private void UpdateRowPips(StatRow row, int tier)
        {
            if (row.pips == null) return;
            for (int i = 0; i < row.pips.Length; i++)
            {
                if (row.pips[i] == null) continue;
                bool isUnlocked = i <= tier;
                if (unlockedSprite != null && lockedSprite != null)
                {
                    row.pips[i].sprite = isUnlocked ? unlockedSprite : lockedSprite;
                    row.pips[i].color = Color.white;
                }
                else
                {
                    row.pips[i].color = isUnlocked ? unlockedColor : lockedColor;
                }
            }
        }
    }
}
