using UnityEngine;

[CreateAssetMenu(fileName = "EnemySettings", menuName = "Dungeon/Enemy Settings")]
public class EnemySettings : ScriptableObject
{
    [Header("Heart Visuals")]
    [SerializeField] private Sprite heartFullSprite;
    [SerializeField] private Sprite heartEmptySprite;

    [Header("Turn & Movement Timing")]
    [SerializeField] private float enemyMoveDuration = 0.3f;

    [Header("Stun Visual")]
    [SerializeField] private Sprite stunSprite;

    public Sprite HeartFullSprite => heartFullSprite;
    public Sprite HeartEmptySprite => heartEmptySprite;
    public Sprite StunSprite => stunSprite;
    public float EnemyMoveDuration => enemyMoveDuration;
}
