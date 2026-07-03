using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BlackHoleEntity : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform rangeTransform;

    [Header("Camera Shake")]
    [Tooltip("Fuerza del shake continuo cuando el agujero esta a su tamano MAXIMO; escala linealmente con el tamano actual")]
    [SerializeField] private float lingerShakeAtMaxSize = 0.08f;

    [Header("Attraction")]
    [SerializeField] private float rangeMaxScale = 5f;
    [SerializeField] private float maxAttractionForce = 80f;
    [SerializeField] private float minAttractionForce = 5f;
    [SerializeField] private float forceExponent = 2f;

    [Header("Center Hitbox")]
    [Tooltip("Multiplicador de escala del hitbox central (el que mata) a MAXIMA expansion; vuelve a x1 al encogerse")]
    [SerializeField] private float centerHitboxMaxMultiplier = 3f;

    [Header("Global Pull")]
    [Tooltip("Jalada MINIMA garantizada en cualquier parte de la pantalla. 0 = apagado")]
    [SerializeField] private float globalPullMinForce = 3f;
    [Tooltip("Jalada MAXIMA del global pull, al estar pegado al agujero")]
    [SerializeField] private float globalPullMaxForce = 20f;
    [Tooltip("Distancia (unidades de mundo) a la que el global pull decae hasta el minimo")]
    [SerializeField] private float globalPullRange = 20f;
    [Tooltip("Curva de cercania: 1 = lineal; mas alto = solo crece fuerte cerca del agujero")]
    [SerializeField] private float globalPullExponent = 2f;

    // Para que los efectos hijos (p. ej. BlackHoleVortexPower) se auto-conecten
    // al transform animado y a la escala maxima sin configurarlo en el inspector.
    public Transform RangeTransform => rangeTransform;
    public float RangeMaxScale => rangeMaxScale;

    private readonly List<Rigidbody2D> _playersInRange = new();
    private float _rangeBaseRadius;

    // Todos los jugadores de la escena (para el jalon global). Se cachea en
    // Start: el agujero vive ~7s y no spawnean jugadores a mitad de ronda.
    private PlayerStats[] _allPlayers;
    private Rigidbody2D[] _allPlayerBodies;

    // Vortice de viento hijo: se suelta al empezar la implosion para que se
    // desvanezca en vez de desaparecer de golpe con el Destroy del agujero.
    private BlackHoleVortexPower _vortex;

    // Hitbox central (el trigger que mata): crece con la expansion del agujero.
    private Transform _centerHitbox;
    private Vector3 _centerHitboxBaseScale;

    // Handle del sonido "linger" para poder cortarlo si el agujero muere antes
    // de tiempo (p. ej. limpieza por cambio de ronda). Un PlayOneShot no se puede parar.
    private FMOD.Studio.EventInstance _lingerSound;

    private static readonly WaitForSeconds WaitShort = new(0.3f);
    private static readonly WaitForSeconds WaitHold = new(4f);

    void Start()
    {
        // Asegura que exista la camara de captura (_SceneTex) en esta escena, sin importar
        // cual sea. Si ya hay una, no hace nada. Sin esto, el agujero no deforma al instanciarse.
        BlackHoleSceneCapture.EnsureExists();

        var circle = rangeTransform.GetComponent<CircleCollider2D>();
        _rangeBaseRadius = circle != null ? circle.radius : 0.5f;

        _vortex = GetComponentInChildren<BlackHoleVortexPower>();

        // Localiza el hitbox central (relay con isCenter) para escalarlo con la expansion.
        foreach (var relay in GetComponentsInChildren<BlackHoleTriggerRelay>())
        {
            if (!relay.IsCenter) continue;
            _centerHitbox = relay.transform;
            _centerHitboxBaseScale = relay.transform.localScale;
            break;
        }

        // Cachea a todos los jugadores vivos en escena para el jalon global.
        _allPlayers = FindObjectsByType<PlayerStats>(FindObjectsSortMode.None);
        _allPlayerBodies = new Rigidbody2D[_allPlayers.Length];
        for (int i = 0; i < _allPlayers.Length; i++)
            _allPlayerBodies[i] = _allPlayers[i].GetComponent<Rigidbody2D>();

        //SoundFXManager.instance.PlaySoundByName("BlackHoleSpawn", transform, 1f, 1f, false);
        if (AudioManager.Instance != null)
            _lingerSound = AudioManager.Instance.PlayStoppableSound(FMODEvents.Instance.BHLinger, transform.position);
        rangeTransform.localScale = Vector3.one;
        StartCoroutine(BlackHoleSequence());
    }

    IEnumerator BlackHoleSequence()
    {
        yield return WaitShort;

        yield return rangeTransform.DOScale(Vector3.one * rangeMaxScale, 1f)
            .SetEase(Ease.OutExpo)
            .WaitForCompletion();

        yield return WaitHold;

        // El vortice deja de emitir y se suelta AHORA: sus particulas vivas se
        // terminan de succionar durante la implosion en vez de desaparecer de
        // golpe cuando el agujero se destruya.
        if (_vortex != null) _vortex.ReleaseAndFade();

        // Implosiona casi hasta 0 para que se encoja y se desvanezca (no un "pop" a escala 1).
        yield return rangeTransform.DOScale(Vector3.one * 0.05f, 1f)
            .SetEase(Ease.InExpo)
            .WaitForCompletion();

        yield return WaitShort;
        Destroy(gameObject);
    }

    void Update()
    {
        float t = Mathf.Clamp01(rangeTransform.localScale.x / rangeMaxScale);

        // Shake continuo proporcional al tamano ACTUAL (sigue el tween de DOTween:
        // crece al expandirse, baja al implosionar). La fuente se actualiza cada
        // frame y se quita en OnDestroy.
        if (CameraShakeManager.Instance != null)
            CameraShakeManager.Instance.SetContinuousShake(this, lingerShakeAtMaxSize * t);

        // El hitbox central que mata crece hasta xN a maxima expansion y vuelve
        // a x1 al encogerse, siguiendo el mismo tween.
        if (_centerHitbox != null)
            _centerHitbox.localScale = _centerHitboxBaseScale * Mathf.Lerp(1f, centerHitboxMaxMultiplier, t);
    }

    void FixedUpdate()
    {
        if (GameManager.instance == null || GameManager.instance.destroyProyectiles)
        {
            // Suelta el vortice ANTES del Destroy (en OnDestroy ya no se puede
            // reparentar); sus particulas restantes se desvanecen solas.
            if (_vortex != null) _vortex.ReleaseAndFade();
            Destroy(gameObject);
        }
        for (int i = _playersInRange.Count - 1; i >= 0; i--)
        {
            var rb = _playersInRange[i];
            if (rb == null)
            {
                _playersInRange.RemoveAt(i);
                continue;
            }
            ApplyAttractionForce(rb);
        }

        ApplyGlobalPull();
    }

    // Jalon a todos los jugadores de la pantalla, sin importar la distancia.
    // Crece al acercarse al agujero (curva con globalPullExponent) pero NUNCA
    // baja de globalPullMinForce. Es adicional a la atraccion fuerte por rango:
    // los que ya estan dentro del rango se saltan (ya los jala ApplyAttractionForce).
    // Escala con el tamano actual, asi aparece y muere con el tween del agujero.
    private void ApplyGlobalPull()
    {
        if (globalPullMinForce <= 0f || _allPlayers == null) return;

        float t = Mathf.Clamp01(rangeTransform.localScale.x / rangeMaxScale);
        if (t <= 0f) return;

        for (int i = 0; i < _allPlayers.Length; i++)
        {
            var stats = _allPlayers[i];
            var rb = _allPlayerBodies[i];
            if (stats == null || rb == null || !stats.playerAlive) continue;
            if (_playersInRange.Contains(rb)) continue;

            Vector2 toCenter = (Vector2)transform.position - rb.position;
            float distance = toCenter.magnitude;
            if (distance < 0.01f) continue;

            // 1 pegado al agujero -> 0 a globalPullRange (o mas lejos).
            float closeness = Mathf.Pow(1f - Mathf.Clamp01(distance / globalPullRange), globalPullExponent);
            float force = Mathf.Lerp(globalPullMinForce, globalPullMaxForce, closeness);

            rb.AddForce(toCenter / distance * (force * t));
        }
    }

    void ApplyAttractionForce(Rigidbody2D rb)
    {
        Vector2 toCenter = (Vector2)transform.position - rb.position;
        float distance = toCenter.magnitude;
        if (distance < 0.01f) return;

        float worldRadius = _rangeBaseRadius * rangeTransform.lossyScale.x;
        float t = Mathf.Pow(1f - Mathf.Clamp01(distance / worldRadius), forceExponent);
        float force = Mathf.Lerp(minAttractionForce, maxAttractionForce, t);
        rb.AddForce(toCenter.normalized * force);
    }

    public void RegisterPlayer(Rigidbody2D rb)
    {
        if (!_playersInRange.Contains(rb))
            _playersInRange.Add(rb);
    }

    public void UnregisterPlayer(Rigidbody2D rb)
    {
        _playersInRange.Remove(rb);
    }

    public void KillPlayer(Collider2D col)
    {
        var stats = col.GetComponentInParent<PlayerStats>();
        if (stats != null)
            stats.TakeDamage(9999);
    }

    void OnDestroy()
    {
        // Corta el linger tanto en la muerte natural como en la limpieza por
        // cambio de ronda. Si ya termino solo, isValid() da false y no hace nada.
        if (_lingerSound.isValid())
            _lingerSound.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);

        // Da de baja la fuente de shake continuo (si no, la camara temblaria para siempre).
        if (CameraShakeManager.Instance != null)
            CameraShakeManager.Instance.RemoveContinuousShake(this);

        DOTween.Kill(rangeTransform);
    }
}

