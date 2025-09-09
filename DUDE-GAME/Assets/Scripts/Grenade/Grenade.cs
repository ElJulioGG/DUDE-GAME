using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Grenade : MonoBehaviour
{
    public enum State { Safe, Armed, Thrown, Exploded }

    [Header("Config")]
    [SerializeField] private GrenadeDefinition definition;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openSprite;
    [SerializeField] private ParticleSystem armedVfx;   // opcional
    [SerializeField] private ParticleSystem explodeVfx; // opcional

    [Header("Explosion FX (Sprite Animation Prefab)")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float explosionLifetime = 0.6f;
    [SerializeField] private bool matchSortingForExplosion = true;

    [Header("Armed Feedback")]
    [SerializeField] private bool useBlink = true;
    [SerializeField] private Color blinkColor = new Color(1f, 0.5f, 0.5f);
    [SerializeField] private float minBlinkInterval = 0.08f;
    [SerializeField] private float maxBlinkInterval = 0.35f;

    [SerializeField] private bool useBeep = true;
    [SerializeField] private AudioSource beepSource;
    [SerializeField] private AudioClip beepClip;
    [SerializeField] private float minBeepInterval = 0.09f;
    [SerializeField] private float maxBeepInterval = 0.4f;
    [SerializeField] private float minBeepPitch = 1.0f;
    [SerializeField] private float maxBeepPitch = 1.6f;

    private Rigidbody2D rb;
    private Collider2D col;
    private float fuseLeft;
    private bool ticking;
    private State state = State.Safe;
    private int ownerIndex = -1;

    private Color baseColor = Color.white;
    private Coroutine blinkCo;
    private Coroutine beepCo;

    public bool IsSafe => state == State.Safe;
    public bool IsArmed => state == State.Armed;
    public bool IsThrown => state == State.Thrown;

    public void Init(GrenadeDefinition def, int ownerPlayerIndex)
    {
        definition = def;
        ownerIndex = ownerPlayerIndex;

        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sharedMaterial = new PhysicsMaterial2D
        {
            bounciness = definition.bounciness,
            friction = definition.friction
        };

        state = State.Safe;
        fuseLeft = definition.fuseSeconds;
        ticking = false;

        if (spriteRenderer) baseColor = spriteRenderer.color;

        SetClosedVisual();
        SetHeldInHand(true); // empieza “en mano”
    }

    private void SetClosedVisual()
    {
        if (spriteRenderer && closedSprite) spriteRenderer.sprite = closedSprite;
    }

    private void SetOpenVisual()
    {
        if (spriteRenderer && openSprite) spriteRenderer.sprite = openSprite;
        if (armedVfx) armedVfx.Play();
    }

    /// En mano (kinemática, sin colisión) vs mundo (dinámica, con colisión)
    private void SetHeldInHand(bool held)
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!col) col = GetComponent<Collider2D>();

        if (held)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            if (col) col.enabled = false;
        }
        else
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            if (col) col.enabled = true;
        }
    }

    // Primer click: abrir/armar
    public void Arm()
    {
        if (state != State.Safe) return;

        state = State.Armed;
        ticking = true;
        fuseLeft = definition.fuseSeconds;

        SetOpenVisual();
        SetHeldInHand(true); // sigue en mano con fuse activo

        if (useBlink && blinkCo == null) blinkCo = StartCoroutine(BlinkRoutine());
        if (useBeep && beepCo == null) beepCo = StartCoroutine(BeepRoutine());
    }

    // Segundo click: lanzar
    public void Throw(Vector2 dir, float charge01 = 0f)
    {
        if (state == State.Safe) Arm(); // por si acaso

        state = State.Thrown;

        // 🔧 al soltar, forzamos el visual abierto (evita “volver” al cerrado)
        SetOpenVisual();

        float speed = definition.throwSpeed + Mathf.Clamp01(charge01) * definition.maxExtraThrow;
        transform.SetParent(null, true);
        SetHeldInHand(false);
        rb.linearVelocity = dir.normalized * speed;
    }

    /// Soltarla al mundo si estaba armada (por ejemplo, si sueltas el arma)
    public void DropArmed()
    {
        if (state == State.Safe) Arm();   // garantiza que quede armada
        // mantenemos Armed (no hace falta pasar a Thrown)
        SetOpenVisual();                  // 🔧 asegura sprite abierto
        transform.SetParent(null, true);
        SetHeldInHand(false);             // activa física/colisión
    }

    private void Update()
    {
        if (!ticking) return;

        fuseLeft -= Time.deltaTime;
        if (fuseLeft <= 0f)
        {
            StartCoroutine(Explode());
        }
    }

    private IEnumerator Explode()
    {
        if (state == State.Exploded) yield break;

        state = State.Exploded;
        ticking = false;

        if (blinkCo != null) StopCoroutine(blinkCo);
        if (beepCo != null) StopCoroutine(beepCo);
        blinkCo = beepCo = null;

        if (spriteRenderer) spriteRenderer.color = baseColor;

        // Gameplay effects
        Vector2 center = transform.position;
        if (definition != null && definition.effects != null)
        {
            foreach (var eff in definition.effects)
                if (eff != null) eff.ApplyEffect(center, ownerIndex);
        }

        if (armedVfx) armedVfx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (explodeVfx) explodeVfx.Play();

        // VFX animado opcional
        if (explosionPrefab != null)
        {
            var fx = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            if (matchSortingForExplosion)
            {
                var srSelf = spriteRenderer;
                var srFx = fx.GetComponentInChildren<SpriteRenderer>();
                if (srSelf && srFx)
                {
                    srFx.sortingLayerID = srSelf.sortingLayerID;
                    srFx.sortingOrder = srSelf.sortingOrder + 1;
                }
            }
            Destroy(fx, explosionLifetime);
        }

        yield return new WaitForSeconds(0.05f);
        Destroy(gameObject);
    }

    private IEnumerator BlinkRoutine()
    {
        while (state != State.Exploded && ticking)
        {
            float t = Mathf.Clamp01(1f - (fuseLeft / Mathf.Max(0.0001f, definition.fuseSeconds)));
            float interval = Mathf.Lerp(maxBlinkInterval, minBlinkInterval, t);

            if (spriteRenderer) spriteRenderer.color = blinkColor;
            yield return new WaitForSeconds(interval * 0.5f);

            if (spriteRenderer) spriteRenderer.color = baseColor;
            yield return new WaitForSeconds(interval * 0.5f);
        }
    }

    private IEnumerator BeepRoutine()
    {
        while (state != State.Exploded && ticking)
        {
            float t = Mathf.Clamp01(1f - (fuseLeft / Mathf.Max(0.0001f, definition.fuseSeconds)));
            float interval = Mathf.Lerp(maxBeepInterval, minBeepInterval, t);
            float pitch = Mathf.Lerp(minBeepPitch, maxBeepPitch, t);

            if (beepSource && beepClip)
            {
                beepSource.pitch = pitch;
                beepSource.PlayOneShot(beepClip);
            }
            yield return new WaitForSeconds(interval);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
