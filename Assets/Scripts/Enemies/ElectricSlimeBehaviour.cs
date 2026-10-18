using UnityEngine;

[DisallowMultipleComponent]
public sealed class ElectricSlimeBehaviour : MonoBehaviour, IEnemyAttackEffect
{
    [SerializeField, Range(0f, 1f)] private float stunChance = 0.25f;
    [SerializeField, Min(0f)] private float stunDuration = 1.25f;

    public void DealDamage(PlayerHealth target, float damage, string enemyID)
    {
        if (target == null)
            return;

        target.TakeDamage(damage, enemyID);
        if (target != null && target.currentHealth > 0f && Random.value < stunChance)
            target.ApplyStun(stunDuration);
    }
}
