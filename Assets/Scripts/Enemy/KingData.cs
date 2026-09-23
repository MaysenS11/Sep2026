using UnityEngine;
using FMODUnity;

[CreateAssetMenu(fileName = "NewKingData", menuName = "Enemy/King Data")]
public class KingData : EnemyData
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private EventReference damageSound;
    [SerializeField] private EventReference deathSound;

    [Header("Boss Properties")]
    [SerializeField] private bool isImmobile = true;
    [SerializeField] private bool immuneToDirectAttacks = true;

    public EventReference DamageSound => damageSound;
    public EventReference DeathSound => deathSound;
    public bool IsImmobile => isImmobile;
    public bool ImmuneToDirectAttacks => immuneToDirectAttacks;
}
