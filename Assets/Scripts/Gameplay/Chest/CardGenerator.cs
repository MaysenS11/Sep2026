using System.Collections.Generic;
using UnityEngine;

namespace Chest
{
    public static class CardGenerator
    {
        private static readonly StatType[] StatPool = new StatType[]
        {
            StatType.AttackDamage,
            StatType.AttackRange,
            StatType.Defence,
            StatType.Health
        };

        public static List<CardRewardItem> GenerateCards(Core.Occupants.PlayerOccupant playerOccupant, StatCardDatabase database)
        {
            var results = new List<CardRewardItem>();
            if (database == null || playerOccupant == null)
            {
                return results;
            }

            var eligibleCards = new List<CardRewardItem>();

            for (int i = 0; i < StatPool.Length; i++)
            {
                var stat = StatPool[i];
                var cardDef = database.GetCardForStat(stat);
                int currentTier = playerOccupant.GetUpgradeTier(stat);

                if (currentTier < 2)
                {
                    int nextTier = currentTier + 1;
                    Sprite sprite = cardDef != null ? cardDef.GetSpriteForTier(nextTier) : database.GetSpriteForStat(stat, nextTier);
                    eligibleCards.Add(new CardRewardItem(cardDef, stat, nextTier, sprite, false));
                }
                else if (stat == StatType.Health)
                {
                    if (playerOccupant.CurrentHealth <= playerOccupant.MaxHealth / 2f)
                    {
                        var healDef = cardDef != null && cardDef.IsHealCard ? cardDef : database.GetHealCard();
                        Sprite healSprite = healDef != null ? healDef.Tier1Sprite : database.HealCardSprite;
                        eligibleCards.Add(new CardRewardItem(healDef, StatType.Health, 0, healSprite, true));
                    }
                }
            }

            for (int i = 0; i < eligibleCards.Count; i++)
            {
                int rnd = UnityEngine.Random.Range(i, eligibleCards.Count);
                var temp = eligibleCards[i];
                eligibleCards[i] = eligibleCards[rnd];
                eligibleCards[rnd] = temp;
            }

            int countToTake = Mathf.Min(3, eligibleCards.Count);
            for (int i = 0; i < countToTake; i++)
            {
                results.Add(eligibleCards[i]);
            }

            var healCardDef = database.GetHealCard() ?? database.GetCardForStat(StatType.Health);
            Sprite fallbackHealSprite = healCardDef != null ? healCardDef.Tier1Sprite : database.HealCardSprite;

            while (results.Count < 3 && fallbackHealSprite != null)
            {
                results.Add(new CardRewardItem(healCardDef, StatType.Health, 0, fallbackHealSprite, true));
            }

            for (int i = 0; i < results.Count; i++)
            {
                int rnd = UnityEngine.Random.Range(i, results.Count);
                var temp = results[i];
                results[i] = results[rnd];
                results[rnd] = temp;
            }

            return results;
        }
    }
}
