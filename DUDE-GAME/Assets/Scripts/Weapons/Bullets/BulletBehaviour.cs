using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BulletBehavior : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 10f;
    public bool enableBounce = true;
    public bool destroyOnPlayerHit = true;
    public bool rotateToDirection = true;
    public float destroyTime = 5f;
    public bool destroyOnInvisible = true;

    [Header("Combat")]
    [SerializeField] private int damage = 100;
    [SerializeField] private int bounceLife = 10;
    [SerializeField] private float bulletRadius = 0.1f;

    [Header("Collision Layers")]
    [SerializeField] private LayerMask damageableMask;
    [SerializeField] private LayerMask bounceableMask;

    private Rigidbody2D rb;
    private Vector2 previousPosition;
    private Vector2 direction;
    private bool isQuitting = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void Start()
    {
        direction = transform.right;
        previousPosition = rb.position;
        Physics2D.queriesHitTriggers = false;

        if (!destroyOnInvisible)
        {
            Invoke(nameof(DestroyBullet), destroyTime);
            //Destroy(gameObject, destroyTime);
        }
    }

    void OnApplicationQuit() => isQuitting = true;

    void FixedUpdate()
    {
        if (isQuitting) return;

        Vector2 newPosition = previousPosition + direction * speed * Time.fixedDeltaTime;
        float distance = Vector2.Distance(previousPosition, newPosition);

        RaycastHit2D[] hits = Physics2D.CircleCastAll(
            previousPosition,
            bulletRadius,
            direction,
            distance,
            damageableMask | bounceableMask
        );

        foreach (var hit in hits)
        {
            if (((1 << hit.collider.gameObject.layer) & damageableMask) != 0)
            {
                HandleDamage(hit);
                if (destroyOnPlayerHit) return;
            }

            if (enableBounce && ((1 << hit.collider.gameObject.layer) & bounceableMask) != 0)
            {
                HandleBounce(hit);
                return;
            }
        }

        rb.MovePosition(newPosition);
        previousPosition = newPosition;

        if (rotateToDirection && direction.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    void HandleDamage(RaycastHit2D hit)
    {
        if (hit.collider.CompareTag("Player"))
        {
            hit.collider.GetComponent<PlayerStats>()?.TakeDamage(damage);
        }
        if (destroyOnPlayerHit)
        {
            DestroyBullet();
        }
    }

    void HandleBounce(RaycastHit2D hit)
    {
        bounceLife--;
        if (bounceLife < 0)
        {
            DestroyBullet();
            return;
        }

        Vector2 normal = hit.normal;
        if (hit.collider is BoxCollider2D)
        {
            if (Mathf.Abs(normal.x) > Mathf.Abs(normal.y))
                normal = new Vector2(Mathf.Sign(normal.x), 0);
            else
                normal = new Vector2(0, Mathf.Sign(normal.y));
        }

        direction = Vector2.Reflect(direction, normal).normalized;
        rb.position = hit.point + normal * 0.15f;
        previousPosition = rb.position;
    }

    void OnBecameInvisible()
    {
        if (destroyOnInvisible && !isQuitting)
        {
            DestroyBullet();
        }
    }

    void DestroyBullet()
    {
        // Detach particles before destruction
        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in particles)
        {
            if (ps != null)
            {
                ps.transform.SetParent(null);
                var main = ps.main;
                main.stopAction = ParticleSystemStopAction.Destroy;
                ps.Stop();
            }
        }

        Destroy(gameObject);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, bulletRadius);
    }
#endif
}