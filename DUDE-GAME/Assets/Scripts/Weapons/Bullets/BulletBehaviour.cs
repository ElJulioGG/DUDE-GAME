using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BulletBehavior : MonoBehaviour
{
    //[SerializeField] private GameObject particle;

    [Header("Settings")]
    public float speed = 10f;
    public bool enableDamage = true;
    public bool enableBounce = true;
    public bool rotateToDirection = true;
    public float destroyTime = 5f;
    public bool destroyOnInvisible = true; // If true, bullet will be destroyed when it goes off-screen
    public bool spectral = false;

    [Header("Combat")]
    [SerializeField] private int damage = 100;
    [SerializeField] private int bounceLife = 10;
    [SerializeField] private float bulletRadius = 0.1f;

    [Header("Collision Layers")]
    [SerializeField] private LayerMask damageableMask; // Players/enemies
    [SerializeField] private LayerMask bounceableMask; // Walls only

    private Rigidbody2D rb;
    private Vector2 previousPosition;
    private Vector2 direction;
    private bool isQuitting = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic; // Use kinematic for precise control
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void Start()
    {
        direction = transform.right;
        previousPosition = rb.position;
        Physics2D.queriesHitTriggers = false; // Ignore other triggers
        //Instantiate(particle, transform.position, Quaternion.identity);
    }

    void OnApplicationQuit() => isQuitting = true;

    void FixedUpdate()
    {
        if (isQuitting) return;

        Vector2 newPosition = previousPosition + direction * speed * Time.fixedDeltaTime;
        float distance = Vector2.Distance(previousPosition, newPosition);

        // Damage check (players/enemies)
        RaycastHit2D damageHit = Physics2D.CircleCast(
            previousPosition,
            bulletRadius,
            direction,
            distance,
            damageableMask
        );

        if (damageHit.collider != null)
        {
            HandleDamage(damageHit);
            return;
        }

        // Bounce check (walls)
        if (enableBounce)
        {
            RaycastHit2D bounceHit = Physics2D.CircleCast(
                previousPosition,
                bulletRadius,
                direction,
                distance,
                bounceableMask
            );

            if (bounceHit.collider != null)
            {
                HandleBounce(bounceHit);
                return;
            }
        }

        // Movement
        rb.MovePosition(newPosition);
        previousPosition = newPosition;

        // Rotation
        if (rotateToDirection && direction.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        if (!destroyOnInvisible)
        {
            Destroy(gameObject, destroyTime);

        }

        void HandleDamage(RaycastHit2D hit)
        {
            if (hit.collider.CompareTag("Player"))
            {
                hit.collider.GetComponent<PlayerStats>()?.TakeDamage(damage);
            }
            if (!spectral) {
                Destroy(gameObject);
            }
           
        }

        void HandleBounce(RaycastHit2D hit)
        {
            bounceLife--;
            if (bounceLife < 0)
            {
                Destroy(gameObject);
                return;
            }

            // Perfect axis-aligned bounce for rectangles
            Vector2 normal = hit.normal;
            if (hit.collider is BoxCollider2D)
            {
                if (Mathf.Abs(normal.x) > Mathf.Abs(normal.y))
                    normal = new Vector2(Mathf.Sign(normal.x), 0); // Horizontal
                else
                    normal = new Vector2(0, Mathf.Sign(normal.y)); // Vertical
            }

            direction = Vector2.Reflect(direction, normal).normalized;
            rb.position = hit.point + normal * 0.15f; // Extra nudge for rectangles
            previousPosition = rb.position;
        }

        void OnBecameInvisible()
        {
            if (destroyOnInvisible)
            {
                if (!isQuitting) Destroy(gameObject);
            }

        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, bulletRadius);
        }
#endif
    }
}