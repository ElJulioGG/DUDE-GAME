using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BlackHoleEntity : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform rangeTransform;

    [Header("Attraction")]
    [SerializeField] private float rangeMaxScale = 5f;
    [SerializeField] private float maxAttractionForce = 80f;
    [SerializeField] private float minAttractionForce = 5f;
    [SerializeField] private float forceExponent = 2f;

    private readonly List<Rigidbody2D> _playersInRange = new();
    private float _rangeBaseRadius;

    private static readonly WaitForSeconds WaitShort = new(0.3f);
    private static readonly WaitForSeconds WaitHold = new(4f);

    void Start()
    {
        var circle = rangeTransform.GetComponent<CircleCollider2D>();
        _rangeBaseRadius = circle != null ? circle.radius : 0.5f;

        SoundFXManager.instance.PlaySoundByName("BlackHoleSpawn", transform, 1f, 1f, false);
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

        yield return rangeTransform.DOScale(Vector3.one, 1f)
            .SetEase(Ease.InExpo)
            .WaitForCompletion();

        yield return WaitShort;
        Destroy(gameObject);
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
        DOTween.Kill(rangeTransform);
    }
}

