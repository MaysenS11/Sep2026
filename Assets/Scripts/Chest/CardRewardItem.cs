using UnityEngine;

namespace Chest
{
    public struct CardRewardItem
    {
        public readonly bool IsHeal;
        public readonly StatType StatType;
        public readonly Sprite CardSprite;

        public CardRewardItem(Sprite healSprite)
        {
            IsHeal = true;
            StatType = default;
            CardSprite = healSprite;
        }

        public CardRewardItem(StatType statType, Sprite sprite)
        {
            IsHeal = false;
            StatType = statType;
            CardSprite = sprite;
        }
    }
}
