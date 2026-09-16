using UnityEngine;

[CreateAssetMenu(fileName = "EnemyTicketSettings", menuName = "Dungeon/Enemy Ticket Settings")]
public class EnemyTicketSettings : ScriptableObject
{
    [Tooltip("Maximum number of enemies allowed to actively advance/attack towards the player in a single turn.")]
    [SerializeField] private int maxActiveFollowers = 3;

    public int MaxActiveFollowers
    {
        get => maxActiveFollowers;
        set => maxActiveFollowers = Mathf.Max(1, value);
    }
}
