using UnityEngine;

/// <summary>
/// Entry point for the frag explosion prefab.
/// Its particle-system appearance and timing are authored in the prefab; this
/// class only starts the effect when the grenade detonates.
/// </summary>
public static class FragExplosionEffect
{
    public static void Play(GameObject effectInstance)
    {
        GrenadeDamage.ConfigureFragExplosionEffect(effectInstance);
    }
}
