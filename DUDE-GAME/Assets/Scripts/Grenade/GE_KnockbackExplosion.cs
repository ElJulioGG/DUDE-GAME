using UnityEngine;

[CreateAssetMenu(fileName = "GE_KnockbackExplosion", menuName = "Grenades/Effects/Knockback")]
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
        // ApplyEffect runs on the server only in online mode (see Grenade.Explode).
        // Knockback on players is routed through TargetRpc to the owning client,
        // which has physics authority over its character (client-auth NetworkTransform).
        var hits = Physics2D.OverlapCircleAll(center, radius, hitMask);
        foreach (var h in hits)
        {
            if (h == null) continue;

            if (ignoreOwner)
            {
                var psOwnerCheck = h.GetComponentInParent<PlayerStats>();
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

            var ps = h.GetComponentInParent<PlayerStats>();
            if (ps != null)
            {
                if (GameSession.IsOnline)
                {
                    var netCtrl = h.GetComponentInParent<NetworkPlayerController>();
                    if (netCtrl != null) netCtrl.ServerApplyKnockback(center, force);
                }
                else
                {
                    ps.ApplyKnockback(center, force);
                }
                continue;
            }

            var rb = h.attachedRigidbody;
            if (rb != null)
                rb.AddForce(dir * force, ForceMode2D.Impulse);
        }
    }
}
