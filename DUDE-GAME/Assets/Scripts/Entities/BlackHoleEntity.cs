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

    private readonly List<Rigidbody2D> _playersInRange = new();
    private float _rangeBaseRadius;

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

        // Implosiona casi hasta 0 para que se encoja y se desvanezca (no un "pop" a escala 1).
        yield return rangeTransform.DOScale(Vector3.one * 0.05f, 1f)
            .SetEase(Ease.InExpo)
            .WaitForCompletion();

        yield return WaitShort;
        Destroy(gameObject);
    }

    void Update()
    {
        // Shake continuo proporcional al tamano ACTUAL (sigue el tween de DOTween:
        // crece al expandirse, baja al implosionar). La fuente se actualiza cada
        // frame y se quita en OnDestroy.
        if (CameraShakeManager.Instance != null)
        {
            float t = Mathf.Clamp01(rangeTransform.localScale.x / rangeMaxScale);
            CameraShakeManager.Instance.SetContinuousShake(this, lingerShakeAtMaxSize * t);
        }
    }

    void FixedUpdate()
    {
        if (GameManager.instance == null || GameManager.instance.destroyProyectiles)
        {
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

