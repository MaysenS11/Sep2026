using UnityEngine;

namespace Chest
{
    [CreateAssetMenu(fileName = "NewStatCard", menuName = "Chest/Stat Card Definition")]
    public class StatCardDefinition : ScriptableObject
    {
        [SerializeField] private StatType statType;
        [SerializeField] private bool isHealCard;
        [SerializeField] private Sprite tier1Sprite;
        [SerializeField] private Sprite tier2Sprite;

        public StatType TargetStat => statType;
        public bool IsHealCard => isHealCard;
        public Sprite Tier1Sprite => tier1Sprite;
        public Sprite Tier2Sprite => tier2Sprite;

        public Sprite GetSpriteForTier(int tier)
        {
            if (tier == 2 && tier2Sprite != null)
            {
                return tier2Sprite;
            }
            return tier1Sprite;
        }
    }
}
