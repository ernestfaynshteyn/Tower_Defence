// AI assistance: Codex helped revise slime combat and visuals. Ernest directed the design.
using UnityEngine;

[DisallowMultipleComponent]
public sealed class AcidSlimeBehaviour : MonoBehaviour, IEnemyAttackEffect
{
    [SerializeField, Min(1f)] private float defenceDamageMultiplier = 2f;

    public void DealDamage(PlayerHealth target, float damage, string enemyID)
    {
        if (target != null)
            target.TakeCorrosiveDamage(damage, enemyID, defenceDamageMultiplier);
    }
}
