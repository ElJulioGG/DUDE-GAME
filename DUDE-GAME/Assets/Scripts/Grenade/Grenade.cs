using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Grenade : MonoBehaviour
{
    // Enable the GRENADE_DEBUG scripting define to surface hot-potato diagnostics.
    public enum State { Safe, Armed, Thrown, Exploded }
    private static int _idSeed;
    [SerializeField, HideInInspector] private int _id;
    private GrenadeWeapon ownerWeapon;
    private bool heldInHand = false;
    public void SetOwner(GrenadeWeapon owner) { ownerWeapon = owner; }

    [Header("Config")]
    [SerializeField] private GrenadeDefinition definition;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openSprite;
    [SerializeField] private ParticleSystem armedVfx;   // opcional
    [SerializeField] private ParticleSystem explodeVfx; // opcional

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
    private bool hasInitialized = false;

    private Color baseColor = Color.white;
    private Coroutine blinkCo;
    private Coroutine beepCo;

    private void Awake()
    {
        if (_id == 0)
        {
            _id = ++_idSeed;
        }
    }

    public bool IsSafe => state == State.Safe;
    public bool IsArmed => state == State.Armed;
    public bool IsThrown => state == State.Thrown;
    public bool IsHeld => heldInHand;
    public bool IsTicking => ticking;
    public float FuseLeft => fuseLeft;
    public State CurrentState => state;
    public int Id => _id;
    public GrenadeDefinition Definition => definition;
    public int OwnerIndex => ownerIndex;
    public void Init(GrenadeDefinition def, int ownerPlayerIndex)
    {
        if (hasInitialized && (ticking || state != State.Safe))
        {
#if GRENADE_DEBUG
            // DEBUG:
            Debug.Log($"[GRENADE][{_id}] Init SKIPPED state={state} ticking={ticking} fuseLeft={fuseLeft:F2} currentDef={definition?.name ?? "null"} requestedDef={def?.name ?? "null"} owner={ownerIndex}\n{System.Environment.StackTrace}");
#endif
            return;
        }

        definition = def;
        ownerIndex = ownerPlayerIndex;

        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sharedMaterial = new PhysicsMaterial2D { bounciness = definition.bounciness, friction = definition.friction };

        state = State.Safe;
        fuseLeft = definition.fuseSeconds;
        ticking = false;

        if (spriteRenderer) baseColor = spriteRenderer.color;

        SetClosedVisual();
        SetHeldInHand(true); // empieza en mano
        hasInitialized = true;

#if GRENADE_DEBUG
        // DEBUG:
        Debug.Log($"[GRENADE][{_id}] Init OK state={state} fuseLeft={fuseLeft:F2} def={definition?.name ?? "null"} owner={ownerIndex}");
#endif
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

    private void SetHeldInHand(bool held)
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!col) col = GetComponent<Collider2D>();

        heldInHand = held; // <- marca estado real

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

#if GRENADE_DEBUG
        // DEBUG:
        Debug.Log($"[GRENADE][{_id}] SetHeldInHand(held={held}) state={state} fuseLeft={fuseLeft:F2} def={definition?.name ?? "null"} ownerIndex={ownerIndex}");
#endif
    }

    public void AttachToHand(GrenadeWeapon newOwner, Transform hand)
    {
        SetOwner(newOwner);
        int newOwnerIndex = ownerIndex;
        if (newOwner != null)
        {
            newOwnerIndex = newOwner.OwnerPlayerIndex;
        }
        AttachToHand(hand, newOwnerIndex);
    }

    public void AttachToHand(Transform newHand, int newOwnerIndex)
    {
        if (state == State.Exploded) return;
        if (newHand == null) return;

        int previousOwnerIndex = ownerIndex;
#if GRENADE_DEBUG
        // DEBUG:
        Debug.Log($"[GRENADE][{_id}] AttachToHand begin owner={previousOwnerIndex} -> {newOwnerIndex} state={state} fuseLeft={fuseLeft:F2} def={definition?.name ?? "null"}");
#endif

        ownerIndex = newOwnerIndex;
        transform.SetParent(newHand, true);
        transform.position = newHand.position;
        SetHeldInHand(true);
        if (state == State.Safe)
            SetClosedVisual();
        else
            SetOpenVisual();

#if GRENADE_DEBUG
        // DEBUG:
        Debug.Log($"[GRENADE][{_id}] AttachToHand end owner={ownerIndex} state={state} fuseLeft={fuseLeft:F2}");
#endif
    }


    // Primer click: abrir/armar
    public void Arm()
    {
        if (state != State.Safe) return;

        State previous = state;
        state = State.Armed;
        ticking = true;
        fuseLeft = definition.fuseSeconds; 
        SetOpenVisual();
        SetHeldInHand(true);

        if (useBlink && blinkCo == null) blinkCo = StartCoroutine(BlinkRoutine());
        if (useBeep && beepCo == null) beepCo = StartCoroutine(BeepRoutine());

#if GRENADE_DEBUG
        // DEBUG:
        Debug.Log($"[GRENADE][{_id}][STATE] {name} {previous} -> {state} fuseLeft={fuseLeft:F2} def={definition?.name ?? "null"} owner={ownerIndex}");
#endif
    }

    // Segundo click: lanzar
    public void Throw(Vector2 dir, float charge01 = 0f)
    {
        if (state == State.Safe) Arm();

        State previous = state;
        state = State.Thrown;
        SetOpenVisual();

        float speed = definition.throwSpeed + Mathf.Clamp01(charge01) * definition.maxExtraThrow;

        transform.SetParent(null, true);
        SetHeldInHand(false);

        if (dir.sqrMagnitude <= 0.0001f)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        else
        {
            rb.linearVelocity = dir.normalized * speed;
        }

#if GRENADE_DEBUG
        // DEBUG:
        Debug.Log($"[GRENADE][{_id}][STATE] {name} {previous} -> {state} fuseLeft={fuseLeft:F2} def={definition?.name ?? "null"} owner={ownerIndex}");
#endif
    }


    /// Soltarla al mundo si estaba armada 
    public void DropArmed()
    {
        if (state == State.Safe) Arm();
        SetOpenVisual();
        transform.SetParent(null, true);
        SetHeldInHand(false);

#if GRENADE_DEBUG
        // DEBUG:
        Debug.Log($"[GRENADE][{_id}] DropArmed state={state} fuseLeft={fuseLeft:F2} def={definition?.name ?? "null"} owner={ownerIndex}");
#endif
    }

    public void DetachFromHand()
    {
        if (state == State.Exploded) return;

#if GRENADE_DEBUG
        // DEBUG:
        Debug.Log($"[GRENADE][{_id}] DetachFromHand owner={ownerIndex} state={state} fuseLeft={fuseLeft:F2}");
#endif

        transform.SetParent(null, true);
        SetHeldInHand(false);
        ownerWeapon = null;
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

        State previous = state;
        state = State.Exploded;
        ticking = false;

        if (col) col.enabled = false;

        if (blinkCo != null) StopCoroutine(blinkCo);
        if (beepCo != null) StopCoroutine(beepCo);
        blinkCo = beepCo = null;

        if (spriteRenderer) spriteRenderer.color = baseColor;

        Vector2 center = transform.position;
        if (definition != null && definition.effects != null)
        {
            foreach (var eff in definition.effects)
                if (eff != null) eff.ApplyEffect(center, ownerIndex);
        }

        if (armedVfx) armedVfx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (explodeVfx) explodeVfx.Play();

        if (definition != null && definition.explosionPrefab != null)
        {
            var fx = Instantiate(definition.explosionPrefab, transform.position, Quaternion.identity);
            SoundFXManager.instance.PlaySoundByName("Explosion", transform, 1f, 1f, false);
            if (spriteRenderer && definition.matchSortingForExplosion)
            {
                var srFx = fx.GetComponentInChildren<SpriteRenderer>();
                if (srFx)
                {
                    srFx.sortingLayerID = spriteRenderer.sortingLayerID;
                    srFx.sortingOrder = spriteRenderer.sortingOrder + 1;
                }
            }
            Destroy(fx, definition.explosionLifetime);
        }

        // Notificar SOLO si explota en mano
        if (heldInHand && ownerWeapon != null)
        {
            ownerWeapon.NotifyHeldGrenadeExploded(this);
            ownerWeapon = null;
        }

#if GRENADE_DEBUG
        // DEBUG:
        Debug.Log($"[GRENADE][{_id}][STATE] {name} {previous} -> {state} fuseLeft={fuseLeft:F2} def={definition?.name ?? "null"} owner={ownerIndex}");
#endif

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
                SoundFXManager.instance.PlaySoundByName("metalPing", transform, 0.7f, 1f, false);
            }
            yield return new WaitForSeconds(interval);
        }
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (_id == 0)
        {
            _id = ++_idSeed;
        }

        int grenadeLayer = LayerMask.NameToLayer("GrenadeLive");
        if (gameObject.layer != grenadeLayer)
        {
            Debug.LogWarning($"[GRENADE][{_id}] Wrong layer: {LayerMask.LayerToName(gameObject.layer)} (expected GrenadeLive)", this);
        }

        if (tag != "Grenade")
        {
            Debug.LogWarning($"[GRENADE][{_id}] Wrong tag: {tag} (expected Grenade)", this);
        }
#endif
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }

}


