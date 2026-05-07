using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerVisuals : MonoBehaviour
{
    [SerializeField] private GameObject[] bloodSplatterPrefabs;
    public static List<GameObject> allSplatters = new();

    [Header("Visual Damage Shake")]
    [SerializeField] private Transform spriteTransform;
    [SerializeField] private float maxShakeDuration = 0.1f;
    [SerializeField] private float maxShakeStrength = 0.2f;
    [SerializeField] private float shakeRandomness = 90f;
    [SerializeField] private float shakeIntensity = 2f;

    [Header("Shake Easing")]
    [SerializeField] private Ease shakeEase = Ease.OutQuad;
    [SerializeField] private AnimationCurve shakeEaseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool useCurveInsteadOfEase = false;

    [Header("Particles")]
    [SerializeField] private ParticleSystem RippleParticle;
    [SerializeField] private ParticleSystem AuraParticle;
    [SerializeField] private GameObject damageParticlePrefab;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (spriteTransform == null)
        {
            Transform found = transform.Find("PlayerSprite");
            if (found != null)
                spriteTransform = found;
            else
                Debug.LogWarning($"PlayerVisuals on {gameObject.name}: no child named 'PlayerSprite' found.", this);
        }

        if (RippleParticle == null)
        {
            Transform found = transform.Find("RippleParticle1");
            if (found != null)
                RippleParticle = found.GetComponent<ParticleSystem>();
            else
                Debug.LogWarning($"PlayerVisuals on {gameObject.name}: no child named 'RippleParticle' found.", this);
        }

        if (AuraParticle == null)
        {
            Transform found = transform.Find("AuraParticle");
            if (found != null)
                AuraParticle = found.GetComponent<ParticleSystem>();
            else
                Debug.LogWarning($"PlayerVisuals on {gameObject.name}: no child named 'AuraParticle' found.", this);
        }
    }

    private void Update()
    {
        if (rb == null || spriteTransform == null) return;

        float velX = rb.linearVelocity.x;
        if (velX > 0.01f)
            spriteTransform.localScale = new Vector3(1f, spriteTransform.localScale.y, spriteTransform.localScale.z);
        else if (velX < -0.01f)
            spriteTransform.localScale = new Vector3(-1f, spriteTransform.localScale.y, spriteTransform.localScale.z);
    }

    public void PlayDamageShake()
    {
        if (spriteTransform == null) return;

        spriteTransform.DOComplete();

        var shakeTween = spriteTransform.DOShakePosition(
            duration: maxShakeDuration * shakeIntensity,
            strength: maxShakeStrength * shakeIntensity,
            vibrato: (int)(5 + 15 * shakeIntensity),
            randomness: shakeRandomness,
            snapping: false,
            fadeOut: true
        );

        if (useCurveInsteadOfEase)
            shakeTween.SetEase(shakeEaseCurve);
        else
            shakeTween.SetEase(shakeEase);
    }

    public void ResetShake()
    {
        if (spriteTransform == null) return;

        spriteTransform.DOComplete();
        spriteTransform.localPosition = Vector3.zero;
    }

    public void SpawnDamageParticles(int damageAmount, int maxHealth)
    {
        if (damageParticlePrefab == null) return;

        int count = damageAmount >= maxHealth * 0.5f ? 3 : 1;
        for (int i = 0; i < count; i++)
        {
            Quaternion rot = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            Instantiate(damageParticlePrefab, transform.position, rot, transform);
        }
    }

    public void ActivateParticlesPowerUP()
    {
        RippleParticle.Play();
        AuraParticle.Play();
    }

    public void SpawnBloodSplatter(int playerIndex, Vector3 position)
    {
        if (playerIndex >= 0 && playerIndex < bloodSplatterPrefabs.Length)
        {
            Quaternion randomRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            GameObject splatter = Instantiate(bloodSplatterPrefabs[playerIndex], position, randomRotation);
            allSplatters.Add(splatter);
        }
        else
        {
            Debug.LogWarning($"No blood splatter prefab assigned for playerIndex {playerIndex}.");
        }
    }
}
