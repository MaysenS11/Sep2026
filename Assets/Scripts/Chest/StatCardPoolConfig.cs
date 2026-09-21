using System.Collections.Generic;
using UnityEngine;

namespace Chest
{
    [CreateAssetMenu(fileName = "StatCardPoolConfig", menuName = "Chest/Stat Card Pool Config")]
    public class StatCardPoolConfig : ScriptableObject
    {
        [Header("Stat Upgrade Cards (2 per stat)")]
        [SerializeField] private List<StatCardDefinition> statCards = new List<StatCardDefinition>();

        [Header("Heal Card")]
        [SerializeField] private StatCardDefinition healCardDefinition;

        public IReadOnlyList<StatCardDefinition> StatCards => statCards;
        public StatCardDefinition HealCardDefinition => healCardDefinition;

        public StatCardDefinition GetCard(StatType statType, int tier)
        {
            for (int i = 0; i < statCards.Count; i++)
            {
                var card = statCards[i];
                if (card != null && !card.IsHealCard && card.TargetStat == statType && card.Tier == tier)
                {
                    return card;
                }
            }
            return null;
        }
    }
}
