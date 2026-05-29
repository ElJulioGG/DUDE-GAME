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
        // Online: only the server applies damage so it's authoritative.
        // The owning client still sees the hitbox visually for feedback.
        if (GameSession.IsOnline && !InstanceFinder.IsServerStarted)
        {
            gameObject.SetActive(false);
            return;
        }

        var stats = other.GetComponentInParent<PlayerStats>();
        if (stats != null)
        {
            if (GameSession.IsOnline)
            {
                var netCtrl = stats.GetComponent<NetworkPlayerController>();
                if (netCtrl != null) netCtrl.ServerTakeDamage(damage);
            }
            else
            {
                stats.TakeDamage(damage);
            }
            stats.ApplyKnockbackDirection(aimDirection, knockbackForce);
            gameObject.SetActive(false);
            return;
        }

        var dmg = other.GetComponentInParent<IDamageable>();
        if (dmg != null)
        {
            dmg.TakeDamage(damage);
            gameObject.SetActive(false);
        }
    }
}
