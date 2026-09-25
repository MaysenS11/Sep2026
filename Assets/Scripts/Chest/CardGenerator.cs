using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chest
{
    public static class CardGenerator
    {
        public static List<CardRewardItem> GenerateCards(Core.Occupants.PlayerOccupant playerOccupant, StatCardDatabase database)
        {
            var results = new List<CardRewardItem>();
            if (database == null || playerOccupant == null)
            {
                return results;
            }

            var availableStats = new List<StatType>();
            var allStats = (StatType[])Enum.GetValues(typeof(StatType));
            for (int i = 0; i < allStats.Length; i++)
            {
                // Speed is excluded: 4-stat pool (AttackDamage, Defence, Range/Pattern, MaxHealth)
                if (allStats[i] == StatType.Speed) continue;

                if (playerOccupant.CanUpgrade(allStats[i]))
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

            int statsToTake = Mathf.Min(3, availableStats.Count);
            for (int i = 0; i < statsToTake; i++)
            {
                var stat = availableStats[i];
                Sprite sprite = database.GetSpriteForStat(stat);
                results.Add(new CardRewardItem(stat, sprite));
            }

            // If fewer than 3 upgradable stats available (e.g. stats maxed at tier 2), fallback to fill with heal cards
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

        public static List<CardRewardItem> GenerateCards(PlayerStats playerStats, StatCardDatabase database)
        {
            if (playerStats != null && playerStats.Occupant != null)
            {
                return GenerateCards(playerStats.Occupant, database);
            }

            var results = new List<CardRewardItem>();
            if (database == null || playerStats == null)
            {
                return results;
            }

            var availableStats = new List<StatType>();
            var allStats = (StatType[])Enum.GetValues(typeof(StatType));
            for (int i = 0; i < allStats.Length; i++)
            {
                // Speed is excluded: 4-stat pool
                if (allStats[i] == StatType.Speed) continue;

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

            int statsToTake = Mathf.Min(3, availableStats.Count);
            for (int i = 0; i < statsToTake; i++)
            {
                var stat = availableStats[i];
                Sprite sprite = database.GetSpriteForStat(stat);
                results.Add(new CardRewardItem(stat, sprite));
            }

            // If fewer than 3 upgradable stats available (e.g. stats maxed at tier 2), fallback to fill with heal cards
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
