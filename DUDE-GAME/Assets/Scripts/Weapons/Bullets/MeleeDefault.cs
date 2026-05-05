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
        // 1) Intento generico
        var dmg = other.GetComponentInParent<IDamageable>();
        if (dmg != null)
        {
            dmg.TakeDamage(damage);
            gameObject.SetActive(false);
            return;
        }

        // 2) Logica existente con Player
        if (other.CompareTag("Player"))
        {
            PlayerStats stats = other.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.TakeDamage(damage);
                stats.ApplyKnockbackDirection(aimDirection, knockbackForce);
                gameObject.SetActive(false);
            }
        }
    }
}
