using FishNet;
using UnityEngine;

public class MeleeDefault : MonoBehaviour
{
    [SerializeField] private int damage = 20;
    [SerializeField] private float knockbackForce = 10f;
    public int Damage => damage;

    private Vector2 aimDirection = Vector2.right;

    public void SetAimDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude > 0.01f)
            aimDirection = dir.normalized;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // The swing plays on every machine (melee broadcast). Damage stays
        // server-authoritative, but knockback is applied directly on the machine
        // that OWNS the victim's physics — that removes the server → TargetRpc leg,
        // so getting punched (and seeing it) is one network hop faster. The server
        // still sends its knockback RPC as a fallback; the owner ignores it when a
        // local push already happened (LastKnockbackTime dedupe).
        var stats = other.GetComponentInParent<PlayerStats>();
        if (stats != null)
        {
            if (GameSession.IsOnline)
            {
                var netCtrl = stats.GetComponent<NetworkPlayerController>();
                if (netCtrl != null)
                {
                    if (InstanceFinder.IsServerStarted)
                    {
                        netCtrl.ServerTakeDamage(damage);
                        netCtrl.ServerApplyKnockbackDirection(aimDirection, knockbackForce);
                    }
                    else if (netCtrl.IsOwner)
                    {
                        // Victim-side prediction: this machine owns the victim's body.
                        stats.ApplyKnockbackDirection(aimDirection, knockbackForce);
                    }
                }
            }
            else
            {
                stats.TakeDamage(damage);
                stats.ApplyKnockbackDirection(aimDirection, knockbackForce);
            }
            gameObject.SetActive(false);
            return;
        }

        var dmg = other.GetComponentInParent<IDamageable>();
        if (dmg != null)
        {
            // Non-player damage (chicken, eggs...) is authority-only; cosmetic swings
            // on other machines still consume the hitbox for visual parity.
            if (!GameSession.IsOnline || InstanceFinder.IsServerStarted)
                dmg.TakeDamage(damage);
            gameObject.SetActive(false);
        }
    }
}
