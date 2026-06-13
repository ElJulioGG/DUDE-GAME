using System.Collections;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Grenade : NetworkBehaviour
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

    [Header("Camera Shake")]
    [SerializeField] private float explosionShakeStrength = 0.4f;
    [SerializeField] private float explosionShakeDuration = 0.3f;

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

    // Syncs grenade state to remote clients in online mode.
    private readonly SyncVar<byte> _netState = new SyncVar<byte>((byte)State.Safe);

    // Which player slot is holding/cooking this grenade — lets non-server machines
    // mirror the cooking visuals (hidden weapon body, pin pop) on their local copy
    // of that player's grenade weapon.
    private readonly SyncVar<int> _netOwnerIndex = new SyncVar<int>(-1);

    // The local weapon visual whose body we hid; restored on throw/despawn.
    private GrenadeWeapon _cosmeticWeapon;

    // While held the grenade FOLLOWS this transform instead of being parented to it.
    // NetworkTransform syncs localPosition — a parented grenade would send hand-local
    // offsets that unparented client copies apply as world coordinates, leaving them
    // invisible at the world origin.
    private Transform _followHand;

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

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        _netState.OnChange += OnNetStateChanged;
        _netOwnerIndex.OnChange += OnNetOwnerChanged;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        _netState.OnChange -= OnNetStateChanged;
        _netOwnerIndex.OnChange -= OnNetOwnerChanged;
    }

    // Restore the hidden weapon body when the grenade leaves (exploded in hand,
    // despawned at round reset...) so client weapons never get stuck invisible.
    public override void OnStopClient()
    {
        base.OnStopClient();
        RestoreCosmeticWeapon();
    }

    // On remote clients, disable physics so NetworkTransform drives position.
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsServerStarted)
        {
            if (!rb) rb = GetComponent<Rigidbody2D>();
            if (!col) col = GetComponent<Collider2D>();
            if (rb) rb.bodyType = RigidbodyType2D.Kinematic;
            if (col) col.enabled = false;

            // Init() never runs on clients — resolve the sprite so the open/blink
            // visuals work, and mirror the server's forceGrenadeOnTop sorting fix,
            // otherwise the replicated grenade renders behind the player/map and
            // looks invisible from arming until it explodes.
            if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            ApplyClientSorting();

            // The server arms the grenade in the same frame it spawns it, so the Armed
            // state often arrives inside the spawn payload without firing OnChange.
            // Apply whatever state we spawned with so the open sprite / blink / beep
            // play on clients. (Safe is ignored by the handler, so this is harmless.)
            OnNetStateChanged((byte)State.Safe, _netState.Value, asServer: false);
        }
    }

    private void ApplyClientSorting()
    {
        var weapon = FindOwnerWeaponVisual();
        if (weapon != null)
        {
            weapon.ApplyGrenadeSorting(this);
            return;
        }

        // Fallback: render just above the owner player's sprite.
        Transform player = GameController.instance != null
            ? GameController.instance.GetPlayerTransform(_netOwnerIndex.Value) : null;
        var playerSprite = player != null ? player.GetComponentInChildren<SpriteRenderer>() : null;
        if (playerSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sortingLayerID = playerSprite.sortingLayerID;
            spriteRenderer.sortingOrder = playerSprite.sortingOrder + 2;
        }
    }

    private void OnNetStateChanged(byte prev, byte next, bool asServer)
    {
        // Non-server machines only — the server (and host) already runs the real
        // logic in Arm()/Throw(), and host-mode invokes this with asServer=false too.
        if (asServer || IsServerStarted || !GameSession.IsOnline) return;
        switch ((State)next)
        {
            case State.Armed:
                state = State.Armed;
                ticking = true;
                if (definition != null) fuseLeft = definition.fuseSeconds;
                SetOpenVisual();
                if (useBlink && blinkCo == null) blinkCo = StartCoroutine(BlinkRoutine());
                if (useBeep && beepCo == null) beepCo = StartCoroutine(BeepRoutine());
                // Mirror the holder's weapon: hide its body sprite and pop the pin,
                // exactly like the server's GrenadeWeapon.Shoot does locally.
                if (_cosmeticWeapon == null)
                {
                    _cosmeticWeapon = FindOwnerWeaponVisual();
                    _cosmeticWeapon?.ShowCookingVisuals();
                }
                BeginLocalHandFollow();
                break;
            case State.Thrown:
                state = State.Thrown;
                heldInHand = false;
                _followHand = null; // NetworkTransform drives the flight from here
                SetOpenVisual();
                RestoreCosmeticWeapon();
                break;
        }
    }

    // The cooked grenade changed hands (hot-potato steal/adopt) — move the hidden
    // weapon body from the old holder's local weapon visual to the new one.
    private void OnNetOwnerChanged(int prev, int next, bool asServer)
    {
        if (asServer || IsServerStarted || !GameSession.IsOnline) return;
        if (_cosmeticWeapon == null) return;
        RestoreCosmeticWeapon();
        _cosmeticWeapon = FindOwnerWeaponVisual();
        _cosmeticWeapon?.ShowCookingVisuals(playPin: false);
        BeginLocalHandFollow(); // re-pin to the new holder's hand
    }

    private GrenadeWeapon FindOwnerWeaponVisual()
    {
        int slot = _netOwnerIndex.Value;
        Transform player = GameController.instance != null
            ? GameController.instance.GetPlayerTransform(slot) : null;
        if (player == null) return null;
        var holder = player.GetComponentInChildren<GunHolder>(true);
        return holder != null ? holder.CurrentGunScript as GrenadeWeapon : null;
    }

    private void RestoreCosmeticWeapon()
    {
        if (_cosmeticWeapon == null) return;
        _cosmeticWeapon.HideCookingVisuals();
        _cosmeticWeapon = null;
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
//#if GRENADE_DEBUG
//            // DEBUG:
//            Debug.Log($"[GRENADE][{_id}] Init SKIPPED state={state} ticking={ticking} fuseLeft={fuseLeft:F2} currentDef={definition?.name ?? "null"} requestedDef={def?.name ?? "null"} owner={ownerIndex}\n{System.Environment.StackTrace}");
//#endif
            return;
        }

        definition = def;
        ownerIndex = ownerPlayerIndex;
        if (GameSession.IsOnline && InstanceFinder.IsServerStarted)
            _netOwnerIndex.Value = ownerPlayerIndex;

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

//#if GRENADE_DEBUG
//        // DEBUG:
//        Debug.Log($"[GRENADE][{_id}] Init OK state={state} fuseLeft={fuseLeft:F2} def={definition?.name ?? "null"} owner={ownerIndex}");
//#endif
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

        heldInHand = held;

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

//#if GRENADE_DEBUG
//        // DEBUG:
//        Debug.Log($"[GRENADE][{_id}] SetHeldInHand(held={held}) state={state} fuseLeft={fuseLeft:F2} def={definition?.name ?? "null"} ownerIndex={ownerIndex}");
//#endif
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
//#if GRENADE_DEBUG
//        // DEBUG:
//        Debug.Log($"[GRENADE][{_id}] AttachToHand begin owner={previousOwnerIndex} -> {newOwnerIndex} state={state} fuseLeft={fuseLeft:F2} def={definition?.name ?? "null"}");
//#endif

        ownerIndex = newOwnerIndex;
        if (GameSession.IsOnline && InstanceFinder.IsServerStarted)
            _netOwnerIndex.Value = newOwnerIndex;
        _followHand = newHand;
        transform.position = newHand.position;
        SetHeldInHand(true);
        if (state == State.Safe)
            SetClosedVisual();
        else
            SetOpenVisual();

//#if GRENADE_DEBUG
//        // DEBUG:
//        Debug.Log($"[GRENADE][{_id}] AttachToHand end owner={ownerIndex} state={state} fuseLeft={fuseLeft:F2}");
//#endif
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

        if (GameSession.IsOnline && IsServerStarted)
            _netState.Value = (byte)State.Armed;

//#if GRENADE_DEBUG
//        // DEBUG:
//        Debug.Log($"[GRENADE][{_id}][STATE] {name} {previous} -> {state} fuseLeft={fuseLeft:F2} def={definition?.name ?? "null"} owner={ownerIndex}");
//#endif
    }

    // Segundo click: lanzar
    public void Throw(Vector2 dir, float charge01 = 0f)
    {
        if (state == State.Safe) Arm();

        State previous = state;
        state = State.Thrown;
        SetOpenVisual();

        float speed = definition.throwSpeed + Mathf.Clamp01(charge01) * definition.maxExtraThrow;

        _followHand = null;
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

        if (GameSession.IsOnline && IsServerStarted)
            _netState.Value = (byte)State.Thrown;

//#if GRENADE_DEBUG
//        // DEBUG:
//        Debug.Log($"[GRENADE][{_id}][STATE] {name} {previous} -> {state} fuseLeft={fuseLeft:F2} def={definition?.name ?? "null"} owner={ownerIndex}");
//#endif
    }


    /// Soltarla al mundo si estaba armada
    public void DropArmed()
    {
        if (state == State.Safe) Arm();
        SetOpenVisual();
        _followHand = null;
        SetHeldInHand(false);

//#if GRENADE_DEBUG
//        // DEBUG:
//        Debug.Log($"[GRENADE][{_id}] DropArmed state={state} fuseLeft={fuseLeft:F2} def={definition?.name ?? "null"} owner={ownerIndex}");
//#endif
    }

    public void DetachFromHand()
    {
        if (state == State.Exploded) return;

//#if GRENADE_DEBUG
//        // DEBUG:
//        Debug.Log($"[GRENADE][{_id}] DetachFromHand owner={ownerIndex} state={state} fuseLeft={fuseLeft:F2}");
//#endif

        _followHand = null;
        SetHeldInHand(false);
        ownerWeapon = null;
    }

    // Client machines: pin a held primed grenade to the holder's LOCAL hand, exactly
    // like the server/offline does with its own hand. Without this the grenade is
    // dragged by NetworkTransform updates that arrive ticks late, so it visibly
    // trailed behind the moving holder instead of sitting in their hand.
    private void BeginLocalHandFollow()
    {
        var weapon = _cosmeticWeapon != null ? _cosmeticWeapon : FindOwnerWeaponVisual();
        if (weapon == null) return;
        _followHand = weapon.HandPoint;
        heldInHand = true;
        if (_followHand != null)
            transform.position = _followHand.position;
    }

    // Track the holder's hand while held. Done by position copy instead of parenting
    // so NetworkTransform keeps syncing world-space coordinates.
    private void LateUpdate()
    {
        // The holder's weapon visual may not exist yet when Armed arrives (spawn
        // races) — keep retrying until the hand is resolved.
        if (GameSession.IsOnline && !IsServerStarted && state == State.Armed && _followHand == null)
            BeginLocalHandFollow();

        if (heldInHand && _followHand != null)
            transform.position = _followHand.position;
    }

    private void Update()
    {
        if (!ticking) return;

        fuseLeft -= Time.deltaTime;

        // Remote clients tick locally for visual accuracy but never trigger explosion.
        if (GameSession.IsOnline && !IsServerStarted) return;

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

        // Damage runs on server only (or always in local mode).
        if (!GameSession.IsOnline || IsServerStarted)
        {
            if (definition != null && definition.effects != null)
            {
                foreach (var eff in definition.effects)
                    if (eff != null) eff.ApplyEffect(center, ownerIndex);
            }
        }

        // Broadcast explosion visuals to all remote clients before destroying.
        if (GameSession.IsOnline && IsServerStarted)
            ObserversExplodeVisuals(center);

        // Play visuals locally (server in online, everyone in local).
        if (!GameSession.IsOnline || IsServerStarted)
            PlayExplosionVfx(center);

        // Notificar SOLO si explota en mano
        if (heldInHand && ownerWeapon != null)
        {
            ownerWeapon.NotifyHeldGrenadeExploded(this);
            ownerWeapon = null;
        }

//#if GRENADE_DEBUG
//        // DEBUG:
//        Debug.Log($"[GRENADE][{_id}][STATE] {name} {previous} -> {state} fuseLeft={fuseLeft:F2} def={definition?.name ?? "null"} owner={ownerIndex}");
//#endif

        yield return new WaitForSeconds(0.05f);
        if (GameSession.IsOnline && IsServerStarted)
            ServerManager.Despawn(NetworkObject);
        else
            Destroy(gameObject);
    }

    [ObserversRpc(ExcludeServer = true)]
    private void ObserversExplodeVisuals(Vector2 center) => PlayExplosionVfx(center);

    private void PlayExplosionVfx(Vector2 center)
    {
        if (armedVfx) armedVfx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (explodeVfx) explodeVfx.Play();

        if (definition != null && definition.explosionPrefab != null)
        {
            var fx = Instantiate(definition.explosionPrefab, center, Quaternion.identity);
            //SoundFXManager.instance.PlaySoundByName("Explosion", transform, 1f, 1f, false);
            AudioManager.Instance.PlaySound(FMODEvents.Instance.Explosion, center);
            if (CameraShakeManager.Instance != null) CameraShakeManager.Instance.Shake(explosionShakeStrength, explosionShakeDuration);
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
            AudioManager.Instance.PlaySound(FMODEvents.Instance.GranadeBeep, transform.position);
            if (beepSource && beepClip)
            {
                beepSource.pitch = pitch;
                beepSource.PlayOneShot(beepClip);
            }
            yield return new WaitForSeconds(interval);
        }
    }

//    private void OnValidate()
//    {
//#if UNITY_EDITOR
//        if (_id == 0)
//        {
//            _id = ++_idSeed;
//        }

//        int grenadeLayer = LayerMask.NameToLayer("GrenadeLive");
//        if (gameObject.layer != grenadeLayer)
//        {
//            Debug.LogWarning($"[GRENADE][{_id}] Wrong layer: {LayerMask.LayerToName(gameObject.layer)} (expected GrenadeLive)", this);
//        }

//        if (tag != "Grenade")
//        {
//            Debug.LogWarning($"[GRENADE][{_id}] Wrong tag: {tag} (expected Grenade)", this);
//        }
//#endif
//    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }

}
