using UnityEngine;

[CreateAssetMenu(fileName = "GE_DamageExplosion", menuName = "Grenades/Effects/Damage Explosion")]
public class GE_DamageExplosion : GrenadeEffect
{
    [SerializeField] private float radius = 2.2f;
    [SerializeField] private int damage = 45;
    [SerializeField] private LayerMask hitMask = ~0;

    public override void ApplyEffect(Vector2 center, int ownerPlayerIndex)
    {
        var hits = Physics2D.OverlapCircleAll(center, radius, hitMask);
        foreach (var h in hits)
        {
            var stats = h.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.TakeDamage(damage);
                continue;
            }

            var damageable = h.GetComponentInParent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(damage);
        }
    }
}
