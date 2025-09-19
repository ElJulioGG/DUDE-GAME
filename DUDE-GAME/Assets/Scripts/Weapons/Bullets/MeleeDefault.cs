using UnityEngine;

public class MeleeDefault : MonoBehaviour
{
    [SerializeField] private int damage = 20;
    [SerializeField] private float knockbackForce = 10f;
    public int Damage => damage;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1) Intento genérico
        var dmg = other.GetComponentInParent<IDamageable>();
        if (dmg != null)
        {
            dmg.TakeDamage(damage);
            gameObject.SetActive(false); // como ya haces
            return;
        }

        // 2) Tu lógica existente con Player
        if (other.CompareTag("Player"))
        {
            PlayerStats stats = other.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.TakeDamage(damage);
                stats.ApplyKnockback(transform.position, knockbackForce);
                gameObject.SetActive(false);
            }
        }
    }


}
