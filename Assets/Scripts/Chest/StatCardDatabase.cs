using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chest
{
    [Serializable]
    public class StatCardConfig
    {
        public StatType statType;
        public Sprite cardSprite;
    }

    [CreateAssetMenu(fileName = "StatCardDatabase", menuName = "Chest/Stat Card Database")]
    public class StatCardDatabase : ScriptableObject
    {
        [SerializeField] private List<StatCardConfig> statCards = new List<StatCardConfig>();
        [SerializeField] private Sprite healCardSprite;

        public Sprite HealCardSprite => healCardSprite;

        public Sprite GetSpriteForStat(StatType statType)
        {
            for (int i = 0; i < statCards.Count; i++)
            {
                if (statCards[i] != null && statCards[i].statType == statType)
                {
                    return statCards[i].cardSprite;
                }
            }
            return null;
        }
    }
}
