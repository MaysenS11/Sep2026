using UnityEngine;

public interface IDamageable
{
    void TakeDamage(int amount, GameObject source);
    bool IsDead { get; }
}
