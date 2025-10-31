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

    //[Header("DROP (Modo B: Prefab genérico + nombres)")]
    //[SerializeField] private GameObject genericPickupPrefab;
    //[SerializeField] private List<string> possibleWeaponNames = new();

    [Header("Opciones de Drop")]
    [SerializeField] private float dropSpawnRadius = 0.25f;
    [SerializeField] private bool dropOnDeath = true;
    [SerializeField, Min(0)] private int extraDeathDrops = 0;

    // ================== SFX ==================
    [Header("SFX")]
    [SerializeField] private string spawnSfxName = "ChickenSpawn";
    [SerializeField] private string hitSfxName = "ChickenHit";
    //[SerializeField] private string deathSfxName = "ChickenDeath";

    [Header("Death Random Noises")]
    [SerializeField] private List<string> randomDeathSfxNames = new(); // e.g. "ChickenNoise1", "ChickenNoise2"
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
    [SerializeField] private float directionChangeInterval = 1.5f;

    [Header("Evitación de obstáculos")]
    [SerializeField] private float bodyRadius = 0.25f;
    [SerializeField] private float lookAheadDistance = 0.75f;
    [SerializeField, Range(6, 32)] private int sampleDirections = 16;
    [SerializeField] private float maxProbeDistance = 4f;

    [Header("Capas de colisión")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private LayerMask hazardMask;

    [Header("Anti-atasco")]
    [SerializeField] private float stuckCheckPeriod = 0.5f;
    [SerializeField] private float minAdvanceDistance = 0.06f;

    [Header("Suavizado y estabilidad")]
    [SerializeField] private float turnSmoothTime = 0.08f;
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
    [Tooltip("Rota el visual ±90° para simular Up/Down con el sprite de derecha.")]
    [SerializeField] private bool rotateForUpDown = true;


    // ================== Spawn ==================

    [SerializeField] private GameObject spawnFeatherParticles;
    [SerializeField] private GameObject spawnFeatherParticles2;

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

        PlaySfx(spawnSfxName, spawnVolume, RandomPitch(spawnPitchMin, spawnPitchMax));
    }

    private void FixedUpdate()
    {
        // 1) cambio de rumbo por tiempo
        if (Time.time >= _nextDirTime)
        {
            PickBestDirection();
            _nextDirTime = Time.time + RandomizeInterval(directionChangeInterval);
        }

        // 2) evitación
        if (Time.time >= _avoidUntil && AheadBlocked(_desiredDir, lookAheadDistance))
        {
            PickBestDirection();
            _avoidUntil = Time.time + avoidCooldown;
        }

        // 3) anti-atasco
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

        // 4) suavizado + movimiento
        _dir = Vector2.SmoothDamp(_dir, _desiredDir, ref _dirVel, turnSmoothTime, Mathf.Infinity, Time.fixedDeltaTime);
        if (_dir.sqrMagnitude < 0.0001f) _dir = _desiredDir;

        Vector2 next = _rb.position + _dir.normalized * moveSpeed * Time.fixedDeltaTime;
        _rb.MovePosition(next);

        // 5) animación + orientación visual
        UpdateAnimatorAndFacing(next);
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

        // --- Parámetros Animator ---
        animator.SetBool(animParamMoving, moving);
        animator.SetFloat(animParamSpeed, speed * speedToAnimMultiplier);

        // --- Orientación visual con 1 sprite derecha ---
        // Horizontal domina sobre vertical para estabilidad
        if (Mathf.Abs(d.x) >= Mathf.Abs(d.y))
        {
            // Derecha/Izquierda
            visualRoot.localRotation = Quaternion.identity; // sin rotación
            sprite.flipX = d.x < 0f; // izquierda => flipX
        }
        else
        {
            // Arriba/Abajo (rotamos el visual ±90°)
            if (rotateForUpDown)
            {
                sprite.flipX = false; // evita espejo vertical extraño
                float z = (d.y >= 0f) ? 90f : -90f; // Up/Down
                visualRoot.localRotation = Quaternion.Euler(0f, 0f, z);
            }
            else
            {
                // Alternativa: no rotar, siempre derecha (placeholder absoluto)
                visualRoot.localRotation = Quaternion.identity;
                sprite.flipX = false;
            }
        }
    }

    private void PickBestDirection()
    {
        float bestScore = -1f;
        Vector2 best = _desiredDir;

        for (int i = 0; i < sampleDirections; i++)
        {
            float ang = (i / (float)sampleDirections) * Mathf.PI * 2f;
            Vector2 candidate = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            float clearance = MeasureClearance(candidate, maxProbeDistance);
            float alignment = Mathf.Clamp01(Vector2.Dot(candidate, _dir) * 0.5f + 0.5f);
            float score = clearance + alignment * 0.1f;

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
        if (Time.time < _nextHitAllowedTime) return;
        _nextHitAllowedTime = Time.time + hitWindowSeconds;
        if (_hp <= 0) return;

        _hp -= 1;

        PlaySfx(hitSfxName, hitVolume, RandomPitch(hitPitchMin, hitPitchMax));
        if (shakeOnHit) _shaker?.Shake();
        onDamaged?.Invoke();

        DropOnePickup();

        if (_hp <= 0) Die();
    }

    private void Die()
    {
        // Main death sound
        //PlaySfx(deathSfxName, deathVolume, RandomPitch(deathPitchMin, deathPitchMax));

       
        PlayRandomDeathNoises();

        if (dropOnDeath)
        {
            DropOnePickup();
            for (int i = 0; i < extraDeathDrops; i++) DropOnePickup();
        }

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

        //if (genericPickupPrefab != null && possibleWeaponNames != null && possibleWeaponNames.Count > 0)
        //{
        //    string wname = possibleWeaponNames[Random.Range(0, possibleWeaponNames.Count)];
        //    var go = Instantiate(genericPickupPrefab, spawnPos, Quaternion.identity);
        //    var pickup = go.GetComponent<WeaponPickup>();
        //    if (pickup != null)
        //    {
        //        pickup.SetWeapon(wname);
        //        pickup.savedClipAmmo = -1;
        //        pickup.savedReserveAmmo = -1;
        //    }
        //}
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
    private static bool IsInMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;
}
