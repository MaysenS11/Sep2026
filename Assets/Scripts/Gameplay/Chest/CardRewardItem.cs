using UnityEngine;

namespace Chest
{
    public struct CardRewardItem
    {
        public readonly bool IsHeal;
        public readonly StatType StatType;
        public readonly int Tier;
        public readonly Sprite CardSprite;
        public readonly StatCardDefinition Definition;

        public CardRewardItem(StatCardDefinition definition, StatType statType, int tier, Sprite sprite, bool isHeal = false)
        {
            Definition = definition;
            StatType = definition != null ? definition.TargetStat : statType;
            Tier = tier;
            CardSprite = sprite;
            IsHeal = isHeal;
        }

        public CardRewardItem(Sprite healSprite)
        {
            Definition = null;
            Tier = 0;
            IsHeal = true;
            StatType = default;
            CardSprite = healSprite;
        }

        public CardRewardItem(StatType statType, Sprite sprite)
        {
            Definition = null;
            Tier = 1;
            IsHeal = false;
            StatType = statType;
            CardSprite = sprite;
        }
    }
}
