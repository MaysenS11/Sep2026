using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chest
{
    public static class CardGenerator
    {
        public static List<CardRewardItem> GenerateCards(PlayerStats playerStats, StatCardDatabase database)
        {
            var results = new List<CardRewardItem>();
            if (database == null || playerStats == null)
            {
                return results;
            }

            var availableStats = new List<StatType>();
            var allStats = (StatType[])Enum.GetValues(typeof(StatType));
            for (int i = 0; i < allStats.Length; i++)
            {
                if (playerStats.CanUpgrade(allStats[i]))
                {
                    availableStats.Add(allStats[i]);
                }
            }

            for (int i = 0; i < availableStats.Count; i++)
            {
                int rnd = UnityEngine.Random.Range(i, availableStats.Count);
                var temp = availableStats[i];
                availableStats[i] = availableStats[rnd];
                availableStats[rnd] = temp;
            }

            bool forceHeal = playerStats.CurrentHealth <= (playerStats.MaxHealth / 2);
            int neededStatCards = forceHeal ? 2 : 3;

            if (forceHeal && database.HealCardSprite != null)
            {
                results.Add(new CardRewardItem(database.HealCardSprite));
            }

            int statsToTake = Mathf.Min(neededStatCards, availableStats.Count);
            for (int i = 0; i < statsToTake; i++)
            {
                var stat = availableStats[i];
                Sprite sprite = database.GetSpriteForStat(stat);
                results.Add(new CardRewardItem(stat, sprite));
            }

            while (results.Count < 3 && database.HealCardSprite != null)
            {
                results.Add(new CardRewardItem(database.HealCardSprite));
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
