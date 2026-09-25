using System.Collections.Generic;
using UnityEngine;

namespace Chest
{
    [CreateAssetMenu(fileName = "StatCardDatabase", menuName = "Chest/Stat Card Database")]
    public class StatCardDatabase : ScriptableObject
    {
        [SerializeField] private List<StatCardDefinition> cards = new List<StatCardDefinition>();

        public IReadOnlyList<StatCardDefinition> Cards => cards;

        public StatCardDefinition GetCardForStat(StatType statType)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null && (cards[i].TargetStat == statType || (statType == StatType.Health && cards[i].IsHealCard)))
                {
                    return cards[i];
                }
            }
            return null;
        }

        public StatCardDefinition GetHealCard()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null && (cards[i].IsHealCard || cards[i].TargetStat == StatType.Health))
                {
                    return cards[i];
                }
            }
            return null;
        }

        public Sprite GetSpriteForStat(StatType statType, int tier = 1)
        {
            var card = GetCardForStat(statType);
            return card != null ? card.GetSpriteForTier(tier) : null;
        }

        public Sprite HealCardSprite
        {
            get
            {
                var healCard = GetHealCard();
                return healCard != null ? healCard.Tier1Sprite : null;
            }
        }
    }
}
