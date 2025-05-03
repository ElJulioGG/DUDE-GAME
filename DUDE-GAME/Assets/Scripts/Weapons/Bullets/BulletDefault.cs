using UnityEngine;

public class BulletDefault : MonoBehaviour
{
    [SerializeField] private int damage = 100;

    public int Damage => damage;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) // Or check layer
        {
            PlayerStats stats = other.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.TakeDamage(damage);
                //Destroy(gameObject); // Or use pooling
            }
        }
    }
}
