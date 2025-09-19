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
    [Tooltip("Colapsa perdigones o golpes simultáneos en 1 hit (evita multi-drop).")]
    [SerializeField, Range(0f, 0.3f)] private float hitWindowSeconds = 0.1f;

    [Header("DROP (Modo A: Prefabs)")]
    [Tooltip("Arrastra aquí los prefabs Drop* (cada uno con WeaponPickup).")]
    [SerializeField] private List<GameObject> possiblePickupPrefabs = new();

    //[Header("DROP (Modo B: Prefab genérico + nombres)")]
    //[SerializeField] private GameObject genericPickupPrefab;
    //[SerializeField] private List<string> possibleWeaponNames = new();

    [Header("Opciones de Drop")]
    [SerializeField] private float dropSpawnRadius = 0.25f;
    [SerializeField] private bool dropOnDeath = true;
    [SerializeField, Min(0)] private int extraDeathDrops = 0;

    [Header("Feedback (placeholders)")]
    [SerializeField] private string hitSfxName = "ChickenHit";
    [SerializeField] private string deathSfxName = "ChickenDeath";
    [SerializeField] private bool shakeOnHit = true;

    [Header("Eventos")]
    public UnityEvent onDamaged;
    public UnityEvent onDeath;

    // ================== MOVIMIENTO ==================
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 2.5f;
    [Tooltip("Tiempo medio entre cambios de rumbo (se randomiza ±25%).")]
    [SerializeField] private float directionChangeInterval = 1.5f;

    [Header("Evitación de obstáculos")]
    [Tooltip("Radio corporal para casts de evitación (tamaño del 'cuerpo').")]
    [SerializeField] private float bodyRadius = 0.25f;
    [Tooltip("Distancia de mirada hacia adelante para prever choques.")]
    [SerializeField] private float lookAheadDistance = 0.75f;
    [Tooltip("Direcciones muestreadas al elegir nuevo rumbo (360°/n).")]
    [SerializeField, Range(6, 32)] private int sampleDirections = 16;
    [Tooltip("Hasta dónde mide qué tan despejada está una dirección.")]
    [SerializeField] private float maxProbeDistance = 4f;

    [Header("Capas de colisión")]
    [Tooltip("Capas sólidas a evitar (muros, cajas, tilemap sólido).")]
    [SerializeField] private LayerMask obstacleMask;
    [Tooltip("Hazards letales para jugadores; la gallina los evita, no cruza.")]
    [SerializeField] private LayerMask hazardMask;

    [Header("Anti-atasco")]
    [Tooltip("Cada cuánto se verifica si está atascada.")]
    [SerializeField] private float stuckCheckPeriod = 0.5f;
    [Tooltip("Distancia mínima avanzada para NO considerarse atascada.")]
    [SerializeField] private float minAdvanceDistance = 0.06f;

    [Header("Suavizado y estabilidad")]
    [Tooltip("Suavizado del giro hacia el rumbo objetivo (s).")]
    [SerializeField] private float turnSmoothTime = 0.08f;
    [Tooltip("Cooldown tras cambiar por evitación, evita flip-flop en bordes.")]
    [SerializeField] private float avoidCooldown = 0.08f;
    [Tooltip("Multiplica el radio al castear (<1 reduce roces con esquinas).")]
    [SerializeField, Range(0.7f, 1.2f)] private float castRadiusShrink = 0.9f;

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

    private LayerMask CombinedAvoidMask => obstacleMask | hazardMask;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();
        _shaker = GetComponent<Shaker>();

        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.gravityScale = 0f;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        _col.isTrigger = false; // sólido
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
    }

    // ================== Movimiento FLUIDO ==================
    private void FixedUpdate()
    {
        if (Time.time >= _nextDirTime)
        {
            PickBestDirection();
            _nextDirTime = Time.time + RandomizeInterval(directionChangeInterval);
        }

        if (Time.time >= _avoidUntil && AheadBlocked(_desiredDir, lookAheadDistance))
        {
            PickBestDirection();
            _avoidUntil = Time.time + avoidCooldown;
        }

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

        // Suavizado de giro + desplazamiento a paso de física
        _dir = Vector2.SmoothDamp(_dir, _desiredDir, ref _dirVel, turnSmoothTime, Mathf.Infinity, Time.fixedDeltaTime);
        if (_dir.sqrMagnitude < 0.0001f) _dir = _desiredDir;

        Vector2 next = _rb.position + _dir.normalized * moveSpeed * Time.fixedDeltaTime;
        _rb.MovePosition(next);
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
            float alignment = Mathf.Clamp01(Vector2.Dot(candidate, _dir) * 0.5f + 0.5f); // favorece continuidad
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

    private static float RandomizeInterval(float mean)
    {
        return Random.Range(mean * 0.75f, mean * 1.25f);
    }

    public void TakeDamage(int amount = 1)
    {
        if (Time.time < _nextHitAllowedTime) return;      
        _nextHitAllowedTime = Time.time + hitWindowSeconds;

        if (_hp <= 0) return;

        _hp -= 1; // SIEMPRE 1 por interacción

        if (SoundFXManager.instance != null && !string.IsNullOrEmpty(hitSfxName))
            SoundFXManager.instance.PlaySoundByName(hitSfxName, transform, 0.8f, 1f, false);
        if (shakeOnHit && _shaker != null) _shaker.Shake();

        onDamaged?.Invoke();

        // Drop por golpe
        DropOnePickup();

        if (_hp <= 0) Die();
    }

    private void Die()
    {
        if (dropOnDeath)
        {
            DropOnePickup();
            for (int i = 0; i < extraDeathDrops; i++) DropOnePickup();
        }

        if (SoundFXManager.instance != null && !string.IsNullOrEmpty(deathSfxName))
            SoundFXManager.instance.PlaySoundByName(deathSfxName, transform, 0.9f, 1f, false);

        onDeath?.Invoke();
        Destroy(gameObject); // por ahora solo desaparece
    }

    private void DropOnePickup()
    {
        Vector2 spawnPos = _rb.position + Random.insideUnitCircle * dropSpawnRadius;

        // MODO A: prefabs ya configurados
        if (possiblePickupPrefabs != null && possiblePickupPrefabs.Count > 0)
        {
            var prefab = possiblePickupPrefabs[Random.Range(0, possiblePickupPrefabs.Count)];
            if (prefab != null) Instantiate(prefab, spawnPos, Quaternion.identity);
            return;
        }

        //// MODO B: prefab genérico + nombre
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

    // Hazards/obstáculos: no dañan a la gallina; solo reorientan rumbo.
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
