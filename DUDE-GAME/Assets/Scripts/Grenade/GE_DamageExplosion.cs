using UnityEngine;

[CreateAssetMenu(fileName = "GE_DamageExplosion", menuName = "Grenades/Effects/Damage Explosion")]
public class GE_DamageExplosion : GrenadeEffect
{
    [SerializeField] private float radius = 2.2f;
    [SerializeField] private int damage = 45;
    [SerializeField] private LayerMask hitMask = ~0;

    public override void ApplyEffect(Vector2 center, int ownerPlayerIndex)
    {
        // ApplyEffect runs on the server only in online mode (see Grenade.Explode).
        bool prevQueryHitTriggers = Physics2D.queriesHitTriggers;
        Physics2D.queriesHitTriggers = false;
        var hits = Physics2D.OverlapCircleAll(center, radius, hitMask);
        Physics2D.queriesHitTriggers = prevQueryHitTriggers;

        foreach (var h in hits)
        {
            if (h == null) continue;

            var stats = h.GetComponentInParent<PlayerStats>();
            if (stats != null)
            {
                if (GameSession.IsOnline)
                {
                    var netCtrl = h.GetComponentInParent<NetworkPlayerController>();
                    if (netCtrl != null)
                        netCtrl.ServerTakeDamage(damage);
                    else
                        stats.TakeDamage(damage);
                }
                else
                {
                    stats.TakeDamage(damage);
                }
                continue;
            }

            var damageable = h.GetComponentInParent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(damage);
        }
    }
}
