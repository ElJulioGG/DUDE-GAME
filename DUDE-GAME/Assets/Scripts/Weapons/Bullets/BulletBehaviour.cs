using System.Collections;
using FishNet;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Events;
using FMODUnity;
[RequireComponent(typeof(Rigidbody2D))]
public class BulletBehavior : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 10f;
    public bool enableBounce = true;
    public bool pierceWalls = false;
    public bool destroyOnPlayerHit = true;
    public bool rotateToDirection = true;
    public float destroyTime = 5f;
    public bool destroyOnInvisible = true;

    [Header("On Destroy")]
    [SerializeField] private bool onDestroyMethod = false;
    [SerializeField] private UnityEvent onDestroyCallback;

    [Header("On Bounce")]
    [SerializeField] private bool playBounceSound = false;
    //[SerializeField] private string bounceSoundName = "";
    [SerializeField] private EventReference BounceSoundName;
    
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

        if (GameSession.IsOnline)
        {
            // Online: every machine simulates its own local bullets, and visibility-based
            // destruction is unreliable (depends on what each window renders), so all
            // machines clean up with the timer instead.
            Invoke(nameof(DestroyBullet), destroyTime);
        }
        else if (!destroyOnInvisible)
        {
            Invoke(nameof(DestroyBullet), destroyTime);
        }
    }

    void OnApplicationQuit() => isQuitting = true;

    void FixedUpdate()
    {
        // Bullets are local simulations on every machine (the shot event is what gets
        // replicated). Each machine moves, bounces and cleans up its own bullets;
        // only the server's bullets apply damage (see HandleDamage).
        if (GameManager.instance == null || GameManager.instance.destroyProyectiles)
        {
            DestroyBullet();
            return;
        }
        if (isQuitting) return;

        Vector2 newPosition = previousPosition + direction * speed * Time.fixedDeltaTime;
        float distance = Vector2.Distance(previousPosition, newPosition);

        bool prevQueryHitTriggers = Physics2D.queriesHitTriggers;
        Physics2D.queriesHitTriggers = false;
        RaycastHit2D[] hits = Physics2D.CircleCastAll(
            previousPosition,
            bulletRadius,
            direction,
            distance,
            damageableMask | bounceableMask
        );
        Physics2D.queriesHitTriggers = prevQueryHitTriggers;

        foreach (var hit in hits)
        {
            if (((1 << hit.collider.gameObject.layer) & damageableMask) != 0)
            {
                HandleDamage(hit);
                if (destroyOnPlayerHit) return; // Esto se queda igual
            }

            if (((1 << hit.collider.gameObject.layer) & bounceableMask) != 0)
            {
                if (enableBounce)
                {
                    HandleBounce(hit);
                    return;
                }
                if (!pierceWalls)
                {
                    StopAllCoroutines();
                    StartCoroutine(MoveToCollisionAndDestroy(hit.point, Vector2.zero));
                    return;
                }
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
        // Only the authority applies damage; cosmetic bullets on other machines still
        // stop/destroy on impact so they don't visually fly through players.
        bool isAuthority = !GameSession.IsOnline || InstanceFinder.IsServerStarted;

        // Check for player first so damage always routes through the correct online/offline path.
        var playerStats = hit.collider.GetComponentInParent<PlayerStats>();
        if (playerStats != null)
        {
            if (isAuthority)
            {
                if (GameSession.IsOnline)
                {
                    var netCtrl = hit.collider.GetComponentInParent<NetworkPlayerController>();
                    if (netCtrl != null)
                        netCtrl.ServerTakeDamage(damage);
                    else
                        playerStats.TakeDamage(damage);
                }
                else
                {
                    playerStats.TakeDamage(damage);
                }
            }

            if (destroyOnPlayerHit)
            {
                StopAllCoroutines();
                StartCoroutine(MoveToCollisionAndDestroy(hit.point, Vector2.zero));
            }
            return;
        }

        // Non-player IDamageable (NPCs, custom objects)
        var dmgTarget = hit.collider.GetComponentInParent<IDamageable>();
        if (dmgTarget != null)
        {
            if (isAuthority) dmgTarget.TakeDamage(damage);
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

        if (playBounceSound)
        {
            AudioManager.Instance.PlaySound(BounceSoundName, transform.position);
        }
    }


    void OnBecameInvisible()
    {
        // Online: never destroy from visibility — what the server's window renders is
        // not gameplay state (an unfocused editor can fire this instantly and kill
        // every bullet before clients see them). The Start() timer handles cleanup.
        if (GameSession.IsOnline) return;

        if (destroyOnInvisible && !isQuitting)
            DestroyBullet();
    }
    IEnumerator MoveToCollisionAndDestroy(Vector2 targetPoint, Vector2 normal)
    {
        // Mueve la posicion de la bala justo al punto de colision
        rb.position = targetPoint;
        transform.position = targetPoint;

        // Si necesitas ajustar la direccion antes de desaparecer (como en rebotes agotados)
        if (normal != Vector2.zero)
        {
            direction = Vector2.Reflect(direction, normal).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        yield return null; // Espera un frame para que la partocula se actualice

        DestroyBullet();
    }

    void DestroyBullet()
    {
        // Clients must never locally destroy a server-spawned bullet — the server's
        // Despawn removes it everywhere. Locally destroying spawned NetworkObjects
        // desyncs FishNet's object tracking.
        if (GameSession.IsOnline && !InstanceFinder.IsServerStarted)
        {
            var clientNo = GetComponent<NetworkObject>();
            if (clientNo != null && clientNo.IsSpawned) return;
        }

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

        if (onDestroyMethod)
            onDestroyCallback?.Invoke();

        // Bullets are plain local objects now — only despawn through FishNet if this
        // one was actually network-spawned (legacy safety). Calling Despawn on a
        // never-spawned object warns and returns, which used to skip Destroy and
        // leave immortal bullets re-firing their onDestroy callback on every impact.
        if (GameSession.IsOnline && InstanceFinder.IsServerStarted)
        {
            var no = GetComponent<NetworkObject>();
            if (no != null && no.IsSpawned) { InstanceFinder.ServerManager.Despawn(no); return; }
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