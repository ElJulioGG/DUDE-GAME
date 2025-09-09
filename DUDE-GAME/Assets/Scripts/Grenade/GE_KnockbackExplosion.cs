using UnityEngine;

[CreateAssetMenu(fileName = "GE_KnockbackExplosion", menuName = "Grenade/Effects/Knockback")]
public class GE_KnockbackExplosion : GrenadeEffect
{
    [Header("Area")]
    public float radius = 3.5f;
    public LayerMask hitMask;          

    [Header("Force")]
    public float maxForce = 12f;      
    public bool useDistanceFalloff = true;

    [Tooltip("Si es true, no empuja al dueño de la granada")]
    public bool ignoreOwner = false;

    public override void ApplyEffect(Vector2 center, int ownerPlayerIndex)
    {
        var hits = Physics2D.OverlapCircleAll(center, radius, hitMask);
        foreach (var h in hits)
        {
            if (ignoreOwner)
            {
                var psOwnerCheck = h.GetComponent<PlayerStats>();
                if (psOwnerCheck != null && psOwnerCheck.GetPlayerIndex() == ownerPlayerIndex)
                    continue;
            }

            Vector2 dir = ((Vector2)h.transform.position - center);
            float dist = dir.magnitude;
            if (dist < 0.0001f) dir = Random.insideUnitCircle.normalized; else dir /= dist;

            float force = maxForce;
            if (useDistanceFalloff)
            {
                float t = 1f - Mathf.Clamp01(dist / radius); 
                force *= t;
            }

            var ps = h.GetComponent<PlayerStats>();
            if (ps != null)
            {
                ps.ApplyKnockback(center, force);
                continue;
            }

            var rb = h.attachedRigidbody;
            if (rb != null)
            {
                rb.AddForce(dir * force, ForceMode2D.Impulse);
            }
        }
    }
}
