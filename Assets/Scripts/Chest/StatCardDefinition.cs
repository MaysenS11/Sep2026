using UnityEngine;

namespace Chest
{
    [CreateAssetMenu(fileName = "NewStatCard", menuName = "Chest/Stat Card Definition")]
    public class StatCardDefinition : ScriptableObject
    {
        [SerializeField] private string cardId;
        [SerializeField] private string cardTitle;
        [TextArea(2, 4)]
        [SerializeField] private string description;
        [SerializeField] private Sprite cardSprite;
        [SerializeField] private bool isHealCard;
        [SerializeField] private StatType statType;
        [Range(1, 2)]
        [SerializeField] private int tier = 1;
        [SerializeField] private int upgradeValue = 1;

        public string CardId => cardId;
        public string CardTitle => cardTitle;
        public string Description => description;
        public Sprite CardSprite => cardSprite;
        public bool IsHealCard => isHealCard;
        public StatType TargetStat => statType;
        public int Tier => tier;
        public int UpgradeValue => upgradeValue;
    }
}
