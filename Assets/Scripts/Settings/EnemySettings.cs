using UnityEngine;

[CreateAssetMenu(fileName = "EnemySettings", menuName = "Dungeon/Enemy Settings")]
public class EnemySettings : ScriptableObject
{
    [Header("Heart Visuals")]
    [SerializeField] private Sprite heartFullSprite;
    [SerializeField] private Sprite heartEmptySprite;

    [Header("Turn & Movement Timing")]
    [SerializeField] private float enemyMoveDuration = 0.25f;

    public Sprite HeartFullSprite => heartFullSprite;
    public Sprite HeartEmptySprite => heartEmptySprite;
    public float EnemyMoveDuration => enemyMoveDuration;
}
