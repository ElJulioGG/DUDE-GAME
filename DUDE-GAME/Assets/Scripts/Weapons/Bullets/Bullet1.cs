using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet1 : MonoBehaviour
{
    public float speed = 10f;
    private Rigidbody2D rb;

    [SerializeField] private int damage = 100;
    public int Damage => damage;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // Use the object's rotation to determine the direction
        Vector2 direction = transform.right;  // The "up" vector of the bullet's rotation (usually pointing in the direction it's facing)

        rb.linearVelocity = direction * speed;  // Set velocity based on that direction
    }

    void OnBecameInvisible()
    {
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStats stats = other.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
    }
}
