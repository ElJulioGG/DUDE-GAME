using System.Collections;
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
        if (GameManager.instance.destroyProyectiles)
        {
            DestroyBullet();
            return;
        }
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
                if (destroyOnPlayerHit) return; // Esto se queda igual
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

            if (destroyOnPlayerHit)
            {
                StopAllCoroutines();
                StartCoroutine(MoveToCollisionAndDestroy(hit.point, Vector2.zero));
            }
        }
    }




    void HandleBounce(RaycastHit2D hit)
    {
        bounceLife--;
        if (bounceLife < 0)
        {
            StopAllCoroutines();
            StartCoroutine(MoveToCollisionAndDestroy(hit.point, hit.normal));
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
    IEnumerator MoveToCollisionAndDestroy(Vector2 targetPoint, Vector2 normal)
    {
        // Mueve la posición de la bala justo al punto de colisión
        rb.position = targetPoint;
        transform.position = targetPoint;

        // Si necesitas ajustar la dirección antes de desaparecer (como en rebotes agotados)
        if (normal != Vector2.zero)
        {
            direction = Vector2.Reflect(direction, normal).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        yield return null; // Espera un frame para que la partícula se actualice

        DestroyBullet();
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

        // Only draw in Play Mode when we have a valid rb
        if (rb != null)
        {
            Vector2 currentPos = rb.position;
            Vector2 newPos = currentPos + direction.normalized * speed * Time.fixedDeltaTime;
            float distance = Vector2.Distance(currentPos, newPos);

            // Draw direction line
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(currentPos, newPos);

            // Draw circles along the cast path
            int segments = Mathf.CeilToInt(distance / bulletRadius);
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector2 pos = Vector2.Lerp(currentPos, newPos, t);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(pos, bulletRadius);
            }
        }

        // Always draw a wire sphere at the current transform position
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, bulletRadius);
    }
#endif



}