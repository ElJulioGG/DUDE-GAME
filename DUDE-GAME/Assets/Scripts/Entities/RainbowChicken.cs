using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class RainbowChicken : MonoBehaviour, IDamageable
{
    // ================== VIDA / DROP ==================
    [Header("Vida")]
    [SerializeField, Min(1)] private int maxHP = 5;

    [Header("Ventana anti-dobles impactos")]
    [SerializeField, Range(0f, 0.3f)] private float hitWindowSeconds = 0.1f;

    [Header("DROP (Modo A: Prefabs)")]
    [SerializeField] private List<GameObject> possiblePickupPrefabs = new();

    [Header("Opciones de Drop")]
    [SerializeField] private float dropSpawnRadius = 0.25f;
    [SerializeField] private bool dropOnDeath = true;
    [SerializeField, Min(0)] private int extraDeathDrops = 0;

    // ================== HEADLESS RUN (Feature 2) ==================
    [Header("Headless Run")]
    [SerializeField] private float headlessDuration = 1.5f;
    [SerializeField] private float headlessSpeedMult = 2.5f;
    [SerializeField] private float headlessDirInterval = 0.3f;
    [SerializeField] private float headlessDropInterval = 0.5f;
    [SerializeField] private float headlessFlashInterval = 0.08f;
    [SerializeField] private Color headlessFlashColor = Color.red;

    // ================== CUCCO REVENGE (Feature 3) ==================
    [Header("Cucco Revenge")]
    [SerializeField] private GameObject revengeChickenPrefab;
    [SerializeField] private int revengeCount = 4;

    // ================== GOLDEN CHICKEN (Feature 4) ==================
    [Header("Golden Chicken")]
    [SerializeField] private float goldenChance = 0.15f;
    [SerializeField] private Color goldenTint = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private int goldenMaxHP = 8;
    [SerializeField] private float goldenSpeedMult = 1.3f;
    [SerializeField] private int goldenExtraDrops = 2;
    [SerializeField] private GameObject goldenAuraParticlePrefab;

    // ================== REVENGE EGG (Feature 5) ==================
    [Header("Revenge Egg")]
    [SerializeField] private GameObject revengeEggPrefab;

    // ================== SPEED TRAIL (Feature 6) ==================
    [Header("Speed Trail")]
    [SerializeField] private GameObject speedBoostPrefab;
    [SerializeField] private float speedTrailInterval = 0.3f;

    // ================== SFX ==================
    [Header("SFX")]
    [SerializeField] private string spawnSfxName = "ChickenSpawn";
    [SerializeField] private string hitSfxName = "ChickenHit";

    [Header("Death Random Noises")]
    [SerializeField] private List<string> randomDeathSfxNames = new();
    [SerializeField, Range(1, 5)] private int deathNoisesCount = 3;
    [SerializeField, Range(0f, 1f)] private float deathNoiseVolume = 0.8f;
    [SerializeField, Range(0.5f, 2f)] private float deathNoisePitchMin = 0.9f, deathNoisePitchMax = 1.1f;

    [SerializeField, Range(0f, 1f)] private float spawnVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] private float hitVolume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float deathVolume = 0.9f;
    [SerializeField] private bool randomizePitch = true;
    [SerializeField, Range(0.5f, 2f)] private float spawnPitchMin = 0.95f, spawnPitchMax = 1.05f;
    [SerializeField, Range(0.5f, 2f)] private float hitPitchMin = 0.95f, hitPitchMax = 1.05f;
    [SerializeField, Range(0.5f, 2f)] private float deathPitchMin = 0.95f, deathPitchMax = 1.05f;
    [SerializeField] private bool shakeOnHit = true;

    [Header("Eventos")]
    public UnityEvent onDamaged;
    public UnityEvent onDeath;

    // ================== MOVIMIENTO ==================
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float directionChangeInterval = 2.0f;

    [Header("Evitación de obstáculos")]
    [SerializeField] private float bodyRadius = 0.25f;
    [SerializeField] private float lookAheadDistance = 0.75f;
    [SerializeField, Range(6, 32)] private int sampleDirections = 16;
    [SerializeField] private float maxProbeDistance = 4f;

    [Header("Capas de colisión")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private LayerMask hazardMask;

    [Header("Player Awareness")]
    [SerializeField] private float playerDetectRadius = 6f;
    [SerializeField] private float playerFleeRadius = 3.5f;
    [SerializeField] private float playerScorePenalty = 2.0f;
    [SerializeField] private LayerMask playerMask;

    [Header("Damage Reaction")]
    [SerializeField] private float panicDuration = 2.0f;
    [SerializeField] private float panicSpeedMultiplier = 1.8f;
    [SerializeField] private float knockbackDistance = 0.4f;
    [SerializeField] private float knockbackDuration = 0.12f;

    [Header("Anti-atasco")]
    [SerializeField] private float stuckCheckPeriod = 0.5f;
    [SerializeField] private float minAdvanceDistance = 0.06f;

    [Header("Suavizado y estabilidad")]
    [SerializeField] private float turnSmoothTime = 0.15f;
    [SerializeField] private float avoidCooldown = 0.08f;
    [SerializeField, Range(0.7f, 1.2f)] private float castRadiusShrink = 0.9f;

    // ================== ANIMACIÓN (1 sprite derecha) ==================
    [Header("Animación (1 sprite derecha)")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private string animParamMoving = "Moving";
    [SerializeField] private string animParamSpeed = "Speed";
    [SerializeField] private float idleSpeedThreshold = 0.02f;
    [SerializeField] private float speedToAnimMultiplier = 1f;
    [Tooltip("Tilts the visual ±15° to hint Up/Down with the right-facing sprite.")]
    [SerializeField] private bool rotateForUpDown = true;


    // ================== Spawn ==================

    [SerializeField] private GameObject spawnFeatherParticles;
    [SerializeField] private GameObject spawnFeatherParticles2;

    // ================== Arena bounds ==================
    private Camera _arenaCamera;
    private Vector2 _arenaMin;
    private Vector2 _arenaMax;
    private bool _hasBounds;

    // ================== Privados ==================
    private int _hp;
    private float _nextHitAllowedTime = -999f;

    private Rigidbody2D _rb;
    private Collider2D _col;
    private Shaker _shaker;

    private Vector2 _dir = Vector2.right;
    private Vector2 _desiredDir = Vector2.right;
    private Vector2 _dirVel;

    private float _nextDirTime;
    private float _lastStuckCheck;
    private Vector2 _lastStuckPos;
    private float _avoidUntil = 0f;

    private Vector2 _prevPos;
    private Vector2 _lastNonZeroDir = Vector2.right;

    // --- Panic / Knockback state ---
    private bool _inPanic;
    private float _panicEndTime;
    private Vector2 _lastDamageSourcePos;

    private bool _inKnockback;
    private float _knockbackStartTime;
    private Vector2 _knockbackFrom;
    private Vector2 _knockbackTo;

    // --- Player detection cache (zero-alloc) ---
    private static readonly Collider2D[] _playerHits = new Collider2D[8];

    // --- Detected players this frame ---
    private int _nearbyPlayerCount;
    private Vector2 _threatCenter;

    // --- Headless state (Feature 2) ---
    private bool _isHeadless;
    private float _headlessEndTime;
    private float _nextHeadlessDrop;
    private float _nextHeadlessDirChange;
    private float _nextFlashToggle;
    private bool _flashIsRed;
    private Color _originalSpriteColor;

    // --- Golden state (Feature 4) ---
    private bool _isGolden;
    private float _baseMoveSpeed;

    // --- Speed trail state (Feature 6) ---
    private float _nextSpeedTrailDrop;

    private LayerMask CombinedAvoidMask => obstacleMask | hazardMask;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();
        _shaker = GetComponent<Shaker>();

        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.gravityScale = 0f;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        _col.isTrigger = false;

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
        if (visualRoot == null && sprite != null) visualRoot = sprite.transform;

        _baseMoveSpeed = moveSpeed;
        _originalSpriteColor = sprite != null ? sprite.color : Color.white;

        Instantiate(spawnFeatherParticles, transform.position, Quaternion.identity);
        Instantiate(spawnFeatherParticles2, transform.position, Quaternion.identity);
        SoundFXManager.instance.PlaySoundByName("chicken", transform, 1f, 1f, false);
        SoundFXManager.instance.PlaySoundByName("pillowHit", transform, 1f, 1f, false);
        SoundFXManager.instance.PlaySoundByName("puffSmoke", transform, 1f, 1f, false);
    }



    private void OnEnable()
    {
        _hp = Mathf.Max(1, maxHP);
        _nextHitAllowedTime = -999f;

        _dir = _desiredDir = Random.insideUnitCircle.normalized;
        _nextDirTime = Time.time + RandomizeInterval(directionChangeInterval);

        _lastStuckPos = _rb.position;
        _lastStuckCheck = Time.time + stuckCheckPeriod;
        _avoidUntil = 0f;

        _prevPos = _rb.position;
        _lastNonZeroDir = Vector2.right;

        _inPanic = false;
        _inKnockback = false;
        _isHeadless = false;
        _nearbyPlayerCount = 0;
        _nextSpeedTrailDrop = 0f;

        // --- Golden Chicken roll (Feature 4) ---
        moveSpeed = _baseMoveSpeed;
        _isGolden = Random.value < goldenChance;
        if (_isGolden)
        {
            _hp = goldenMaxHP;
            moveSpeed *= goldenSpeedMult;
            if (sprite != null) sprite.color = goldenTint;
            if (goldenAuraParticlePrefab != null) Instantiate(goldenAuraParticlePrefab, transform);
        }
        else
        {
            if (sprite != null) sprite.color = _originalSpriteColor;
        }

        PlaySfx(spawnSfxName, spawnVolume, RandomPitch(spawnPitchMin, spawnPitchMax));

        // Cache arena bounds from camera
        CacheArenaBounds();
    }

    private void CacheArenaBounds()
    {
        _arenaCamera = Camera.main;
        if (_arenaCamera == null)
        {
            _hasBounds = false;
            return;
        }
        float z = _arenaCamera.nearClipPlane;
        Vector3 bottomLeft = _arenaCamera.ViewportToWorldPoint(new Vector3(0.02f, 0.02f, z));
        Vector3 topRight   = _arenaCamera.ViewportToWorldPoint(new Vector3(0.98f, 0.98f, z));
        _arenaMin = new Vector2(bottomLeft.x, bottomLeft.y);
        _arenaMax = new Vector2(topRight.x, topRight.y);
        _hasBounds = true;
    }

    private Vector2 ClampToBounds(Vector2 pos)
    {
        if (!_hasBounds) return pos;
        pos.x = Mathf.Clamp(pos.x, _arenaMin.x, _arenaMax.x);
        pos.y = Mathf.Clamp(pos.y, _arenaMin.y, _arenaMax.y);
        return pos;
    }

    private void BounceDirectionIfClamped(Vector2 original, Vector2 clamped)
    {
        if (!_hasBounds) return;
        if (Mathf.Abs(clamped.x - original.x) > 0.001f) _desiredDir.x = -_desiredDir.x;
        if (Mathf.Abs(clamped.y - original.y) > 0.001f) _desiredDir.y = -_desiredDir.y;
        _dir = _desiredDir;
    }

    private void FixedUpdate()
    {
        if (!_hasBounds) CacheArenaBounds();

        // === HEADLESS RUN (Feature 2) — takes over all movement ===
        if (_isHeadless)
        {
            HandleHeadlessRun();
            return;
        }

        // === Speed Trail during panic (Feature 6) ===
        if (_inPanic && speedBoostPrefab != null && Time.time >= _nextSpeedTrailDrop)
        {
            Instantiate(speedBoostPrefab, _rb.position, Quaternion.identity);
            _nextSpeedTrailDrop = Time.time + speedTrailInterval;
        }

        // 0) panic timeout
        if (_inPanic && Time.time >= _panicEndTime)
            _inPanic = false;

        // 1) knockback — lerp position, skip normal movement
        if (_inKnockback)
        {
            float t = (Time.time - _knockbackStartTime) / knockbackDuration;
            if (t >= 1f)
            {
                _rb.MovePosition(ClampToBounds(_knockbackTo));
                _inKnockback = false;
            }
            else
            {
                _rb.MovePosition(ClampToBounds(Vector2.Lerp(_knockbackFrom, _knockbackTo, t)));
                _prevPos = _rb.position;
                return; // skip all movement logic during knockback
            }
        }

        // 2) detect nearby players (zero-alloc)
        DetectNearbyPlayers();

        // 3) direction change by timer (faster in panic)
        float effectiveInterval = _inPanic
            ? directionChangeInterval * 0.6f
            : directionChangeInterval;

        if (Time.time >= _nextDirTime)
        {
            PickBestDirection();
            _nextDirTime = Time.time + RandomizeInterval(effectiveInterval);
        }

        // 4) avoidance
        if (Time.time >= _avoidUntil && AheadBlocked(_desiredDir, lookAheadDistance))
        {
            PickBestDirection();
            _avoidUntil = Time.time + avoidCooldown;
        }

        // 5) anti-stuck
        if (Time.time >= _lastStuckCheck)
        {
            float moved = Vector2.Distance(_rb.position, _lastStuckPos);
            if (moved < minAdvanceDistance)
            {
                PickBestDirection();
                _avoidUntil = Time.time + avoidCooldown;
            }
            _lastStuckPos = _rb.position;
            _lastStuckCheck = Time.time + stuckCheckPeriod;
        }

        // 6) smooth turn + movement
        _dir = Vector2.SmoothDamp(_dir, _desiredDir, ref _dirVel, turnSmoothTime, Mathf.Infinity, Time.fixedDeltaTime);
        if (_dir.sqrMagnitude < 0.0001f) _dir = _desiredDir;

        float effectiveSpeed = _inPanic ? moveSpeed * panicSpeedMultiplier : moveSpeed;
        Vector2 next = _rb.position + _dir.normalized * effectiveSpeed * Time.fixedDeltaTime;
        Vector2 clamped = ClampToBounds(next);
        BounceDirectionIfClamped(next, clamped);
        _rb.MovePosition(clamped);

        // 7) animation + visual orientation
        UpdateAnimatorAndFacing(clamped);
    }

    // ================== HEADLESS RUN (Feature 2) ==================

    private void EnterHeadlessState()
    {
        _isHeadless = true;
        _headlessEndTime = Time.time + headlessDuration;
        _nextHeadlessDrop = Time.time + headlessDropInterval;
        _nextHeadlessDirChange = Time.time + headlessDirInterval;
        _nextFlashToggle = Time.time + headlessFlashInterval;
        _flashIsRed = false;

        // Random initial direction
        _desiredDir = Random.insideUnitCircle.normalized;
        _dir = _desiredDir;
    }

    private void HandleHeadlessRun()
    {
        // Flash sprite color
        if (sprite != null && Time.time >= _nextFlashToggle)
        {
            _flashIsRed = !_flashIsRed;
            sprite.color = _flashIsRed ? headlessFlashColor : (_isGolden ? goldenTint : _originalSpriteColor);
            _nextFlashToggle = Time.time + headlessFlashInterval;
        }

        // Random erratic direction change
        if (Time.time >= _nextHeadlessDirChange)
        {
            _desiredDir = Random.insideUnitCircle.normalized;
            _dir = _desiredDir;
            _nextHeadlessDirChange = Time.time + headlessDirInterval;
        }

        // Drop pickup on timer
        if (Time.time >= _nextHeadlessDrop)
        {
            DropOnePickup();
            _nextHeadlessDrop = Time.time + headlessDropInterval;
        }

        // Time expired → real death
        if (Time.time >= _headlessEndTime)
        {
            _isHeadless = false;
            if (sprite != null) sprite.color = _isGolden ? goldenTint : _originalSpriteColor;
            Die();
            return;
        }

        // Avoidance during headless
        if (AheadBlocked(_desiredDir, lookAheadDistance))
            _desiredDir = Random.insideUnitCircle.normalized;

        // Move at headless speed
        float headlessSpeed = moveSpeed * headlessSpeedMult;
        Vector2 next = _rb.position + _desiredDir.normalized * headlessSpeed * Time.fixedDeltaTime;
        Vector2 clamped = ClampToBounds(next);
        BounceDirectionIfClamped(next, clamped);
        _rb.MovePosition(clamped);

        UpdateAnimatorAndFacing(clamped);
    }

    // ================== PLAYER DETECTION ==================

    private void DetectNearbyPlayers()
    {
        _nearbyPlayerCount = Physics2D.OverlapCircleNonAlloc(
            _rb.position, playerDetectRadius, _playerHits, playerMask);

        if (_nearbyPlayerCount <= 0)
        {
            _threatCenter = _rb.position; // no threat
            return;
        }

        // Weighted average: closer players contribute more
        Vector2 weightedSum = Vector2.zero;
        float totalWeight = 0f;

        for (int i = 0; i < _nearbyPlayerCount; i++)
        {
            Vector2 pPos = _playerHits[i].transform.position;
            float dist = Vector2.Distance(_rb.position, pPos);
            if (dist < 0.01f) dist = 0.01f;
            float w = 1f / dist;
            weightedSum += pPos * w;
            totalWeight += w;
        }

        _threatCenter = weightedSum / totalWeight;

        // If in panic, blend threat center toward last damage source
        if (_inPanic)
            _threatCenter = Vector2.Lerp(_threatCenter, _lastDamageSourcePos, 0.5f);
    }

    private void UpdateAnimatorAndFacing(Vector2 newPos)
    {
        if (animator == null || sprite == null || visualRoot == null)
        {
            _prevPos = newPos;
            return;
        }

        Vector2 vel = (newPos - _prevPos) / Time.fixedDeltaTime;
        _prevPos = newPos;

        float speed = vel.magnitude;
        bool moving = speed > idleSpeedThreshold;
        Vector2 d = moving ? vel.normalized : _lastNonZeroDir;
        if (d.sqrMagnitude > 0.0001f) _lastNonZeroDir = d;

        // --- Animator params ---
        animator.SetBool(animParamMoving, moving);
        animator.SetFloat(animParamSpeed, speed * speedToAnimMultiplier);

        // --- Visual orientation with 1 right-facing sprite ---
        if (Mathf.Abs(d.x) >= Mathf.Abs(d.y))
        {
            // Horizontal
            visualRoot.localRotation = Quaternion.identity;
            sprite.flipX = d.x < 0f;
        }
        else
        {
            // Vertical — subtle tilt instead of 90° rotation
            if (rotateForUpDown)
            {
                float tiltZ = (d.y >= 0f) ? 15f : -15f;
                visualRoot.localRotation = Quaternion.Euler(0f, 0f, tiltZ);
                sprite.flipX = _lastNonZeroDir.x < 0f; // keep last horizontal facing
            }
            else
            {
                visualRoot.localRotation = Quaternion.identity;
                sprite.flipX = false;
            }
        }
    }

    private void PickBestDirection()
    {
        float bestScore = -1f;
        Vector2 best = _desiredDir;

        // Weights depend on panic state
        float alignWeight = _inPanic ? 0.05f : 0.3f;
        float playerWeight = _nearbyPlayerCount > 0
            ? (_inPanic ? playerScorePenalty * 1.5f : playerScorePenalty)
            : 0f;

        // Direction away from threat
        Vector2 awayFromThreat = Vector2.zero;
        if (_nearbyPlayerCount > 0)
        {
            awayFromThreat = (_rb.position - _threatCenter);
            if (awayFromThreat.sqrMagnitude > 0.0001f)
                awayFromThreat = awayFromThreat.normalized;
        }

        for (int i = 0; i < sampleDirections; i++)
        {
            float ang = (i / (float)sampleDirections) * Mathf.PI * 2f;
            Vector2 candidate = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            float clearance = MeasureClearance(candidate, maxProbeDistance);
            float alignment = Mathf.Clamp01(Vector2.Dot(candidate, _dir) * 0.5f + 0.5f);

            float playerScore = 0f;
            if (_nearbyPlayerCount > 0)
                playerScore = Vector2.Dot(candidate, awayFromThreat) * 0.5f + 0.5f; // 0..1

            float score = clearance + alignment * alignWeight + playerScore * playerWeight;

            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        if (bestScore <= 0f) best = Random.insideUnitCircle.normalized;
        _desiredDir = best.normalized;
    }

    private bool AheadBlocked(Vector2 dir, float dist)
    {
        Vector2 origin = _rb.position + dir * 0.05f;
        float r = bodyRadius * castRadiusShrink;
        RaycastHit2D hit = Physics2D.CircleCast(origin, r, dir, dist, CombinedAvoidMask);
        return hit.collider != null;
    }

    private float MeasureClearance(Vector2 dir, float maxDist)
    {
        float r = bodyRadius * castRadiusShrink;
        RaycastHit2D hit = Physics2D.CircleCast(_rb.position, r, dir, maxDist, CombinedAvoidMask);
        return hit.collider ? hit.distance : maxDist;
    }

    private static float RandomizeInterval(float mean) => Random.Range(mean * 0.75f, mean * 1.25f);

    // ================== DAÑO / DROP ==================
    public void TakeDamage(int amount = 1)
    {
        if (_isHeadless) return; // invulnerable during headless run
        if (Time.time < _nextHitAllowedTime) return;
        _nextHitAllowedTime = Time.time + hitWindowSeconds;
        if (_hp <= 0) return;

        _hp -= 1;

        // --- Infer damage source (nearest player) ---
        Vector2 damageSourcePos = InferDamageSource();
        _lastDamageSourcePos = damageSourcePos;

        // --- Enter panic ---
        _inPanic = true;
        _panicEndTime = Time.time + panicDuration;

        // --- Knockback away from source ---
        Vector2 knockDir = (_rb.position - damageSourcePos);
        if (knockDir.sqrMagnitude < 0.0001f)
            knockDir = Random.insideUnitCircle;
        knockDir = knockDir.normalized;

        Vector2 knockTarget = _rb.position + knockDir * knockbackDistance;
        // Only knockback if no wall behind
        if (!AheadBlocked(knockDir, knockbackDistance + bodyRadius))
        {
            _inKnockback = true;
            _knockbackStartTime = Time.time;
            _knockbackFrom = _rb.position;
            _knockbackTo = knockTarget;
        }

        // --- Immediately pick flee direction ---
        DetectNearbyPlayers();
        PickBestDirection();
        _nextDirTime = Time.time + RandomizeInterval(directionChangeInterval * 0.6f);

        PlaySfx(hitSfxName, hitVolume, RandomPitch(hitPitchMin, hitPitchMax));

        // BUGFIX: Random.Range(0, 1) always returns 0 (int overload, exclusive upper bound)
        int randomIndexSound = Random.Range(0, 2);
        if (randomIndexSound == 0)
        {
            Instantiate(spawnFeatherParticles, transform.position, Quaternion.identity);
        }
        else
        {
            Instantiate(spawnFeatherParticles2, transform.position, Quaternion.identity);
        }

        if (shakeOnHit) _shaker?.Shake();
        onDamaged?.Invoke();

        DropOnePickup();

        // --- Headless run instead of instant death (Feature 2) ---
        if (_hp <= 0)
            EnterHeadlessState();
    }

    private Vector2 InferDamageSource()
    {
        // Use cached player hits if available
        if (_nearbyPlayerCount > 0)
            return FindClosestPlayerInCache();

        // If no players in normal detect radius, try double radius
        int count = Physics2D.OverlapCircleNonAlloc(
            _rb.position, playerDetectRadius * 2f, _playerHits, playerMask);

        if (count > 0)
        {
            float closest = float.MaxValue;
            Vector2 closestPos = _rb.position;
            for (int i = 0; i < count; i++)
            {
                Vector2 p = _playerHits[i].transform.position;
                float d = Vector2.SqrMagnitude(_rb.position - p);
                if (d < closest) { closest = d; closestPos = p; }
            }
            return closestPos;
        }

        // Fallback: damage came from the direction the chicken was facing (behind it)
        return _rb.position - _dir.normalized * 2f;
    }

    private Vector2 FindClosestPlayerInCache()
    {
        float closest = float.MaxValue;
        Vector2 closestPos = _rb.position;
        for (int i = 0; i < _nearbyPlayerCount; i++)
        {
            Vector2 p = _playerHits[i].transform.position;
            float d = Vector2.SqrMagnitude(_rb.position - p);
            if (d < closest) { closest = d; closestPos = p; }
        }
        return closestPos;
    }

    /// <summary>
    /// Finds the closest PlayerStats to award kill points.
    /// Uses cached player hits, expands radius x3 if nobody found.
    /// </summary>
    private PlayerStats FindClosestPlayerStats()
    {
        // Try cached hits first
        int count = _nearbyPlayerCount;
        if (count <= 0)
        {
            count = Physics2D.OverlapCircleNonAlloc(
                _rb.position, playerDetectRadius * 3f, _playerHits, playerMask);
        }

        if (count <= 0) return null;

        float closest = float.MaxValue;
        PlayerStats closestStats = null;

        for (int i = 0; i < count; i++)
        {
            if (_playerHits[i] == null) continue;
            float d = Vector2.SqrMagnitude(_rb.position - (Vector2)_playerHits[i].transform.position);
            if (d < closest)
            {
                var stats = _playerHits[i].GetComponentInParent<PlayerStats>();
                if (stats != null && stats.playerAlive)
                {
                    closest = d;
                    closestStats = stats;
                }
            }
        }

        return closestStats;
    }

    private void Die()
    {
        PlayRandomDeathNoises();

        Instantiate(spawnFeatherParticles, transform.position, Quaternion.identity);
        Instantiate(spawnFeatherParticles2, transform.position, Quaternion.identity);

        // --- Find killer for revenge ---
        PlayerStats killer = FindClosestPlayerStats();

        // --- Death drops ---
        if (dropOnDeath)
        {
            int totalExtraDrops = extraDeathDrops + (_isGolden ? goldenExtraDrops : 0);
            DropOnePickup();
            for (int i = 0; i < totalExtraDrops; i++) DropOnePickup();
        }

        // --- Cucco Revenge swarm (Feature 3) ---
        if (killer != null && revengeChickenPrefab != null)
        {
            int count = _isGolden ? revengeCount + 2 : revengeCount;
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Random.insideUnitCircle * 0.5f;
                var mini = Instantiate(revengeChickenPrefab, (Vector2)transform.position + offset, Quaternion.identity);
                var revenge = mini.GetComponent<MiniChickenRevenge>();
                if (revenge != null) revenge.SetTarget(killer.transform);
            }
        }

        // --- Revenge Egg (Feature 5) ---
        if (revengeEggPrefab != null)
            Instantiate(revengeEggPrefab, transform.position, Quaternion.identity);

        onDeath?.Invoke();
        Destroy(gameObject);
    }

    private void PlayRandomDeathNoises()
    {
        if (randomDeathSfxNames == null || randomDeathSfxNames.Count == 0 || SoundFXManager.instance == null)
            return;

        string clip = randomDeathSfxNames[Random.Range(0, randomDeathSfxNames.Count)];
        float pitch = RandomPitch(deathNoisePitchMin, deathNoisePitchMax);
        float vol = deathNoiseVolume * Random.Range(0.8f, 1.2f);

        SoundFXManager.instance.PlaySoundByName(clip, transform, vol, pitch, false);
    }


    private void DropOnePickup()
    {
        Vector2 spawnPos = _rb.position + Random.insideUnitCircle * dropSpawnRadius;

        if (possiblePickupPrefabs != null && possiblePickupPrefabs.Count > 0)
        {
            var prefab = possiblePickupPrefabs[Random.Range(0, possiblePickupPrefabs.Count)];
            if (prefab != null) Instantiate(prefab, spawnPos, Quaternion.identity);
            return;
        }
    }

    // ============== SFX utils ==============
    private void PlaySfx(string clipName, float volume, float pitch = 1f, bool loop = false)
    {
        if (SoundFXManager.instance == null) return;
        if (string.IsNullOrEmpty(clipName)) return;
        SoundFXManager.instance.PlaySoundByName(clipName, transform, volume, pitch, loop);
    }

    private float RandomPitch(float min, float max)
    {
        if (!randomizePitch) return 1f;
        if (min > max) (min, max) = (max, min);
        return Random.Range(min, max);
    }

    // ============== reorientación por colisiones ==============
    private bool _pushingPlayerIntoWall = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsInMask(other.gameObject.layer, CombinedAvoidMask))
            PickBestDirection();
    }
    private void OnCollisionEnter2D(Collision2D other)
    {
        if (IsInMask(other.gameObject.layer, CombinedAvoidMask))
            PickBestDirection();
    }

    private void OnCollisionStay2D(Collision2D other)
    {
        if (!other.gameObject.CompareTag("Player")) return;

        Vector2 playerPos = other.transform.position;
        float checkDist = 0.8f;
        RaycastHit2D hit = Physics2D.Raycast(playerPos, _dir, checkDist, obstacleMask);

        if (hit.collider != null && !_pushingPlayerIntoWall)
        {
            _pushingPlayerIntoWall = true;
            PickBestDirection();
            _avoidUntil = Time.time + avoidCooldown;
        }
        else if (hit.collider == null)
        {
            _pushingPlayerIntoWall = false;
        }
    }

    private void OnCollisionExit2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player"))
            _pushingPlayerIntoWall = false;
    }

    private static bool IsInMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;
}
