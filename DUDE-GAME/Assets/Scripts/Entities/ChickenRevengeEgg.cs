using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ChickenRevengeEgg : MonoBehaviour, IDamageable
{
    [SerializeField] private float fuseTime = 1.5f;
    [SerializeField] private float explosionRadius = 2f;
    [SerializeField] private int explosionDamage = 20;
    [SerializeField] private float knockbackForce = 8f;
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private GameObject explosionParticlePrefab;

    private int _hp = 1;
    private float _explodeTime;
    private bool _exploded;

    public void SetPlayerMask(LayerMask mask) => playerMask = mask;
    public void SetExplosionParticle(GameObject prefab) => explosionParticlePrefab = prefab;

    private static readonly Collider2D[] _hits = new Collider2D[8];

    private void OnEnable()
    {
        _hp = 1;
        _exploded = false;
        _explodeTime = Time.time + fuseTime;

        // Pulse animation
        transform.DOScale(Vector3.one * 1.3f, 0.2f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    private void Update()
    {
        if (_exploded) return;

        if (Time.time >= _explodeTime)
            Explode();
    }

    public void TakeDamage(int amount = 1)
    {
        if (_exploded) return;

        _hp -= amount;
        if (_hp <= 0)
        {
            // Destroyed by bullet — no explosion, just die
            DOTween.Kill(transform);
            Destroy(gameObject);
        }
    }

    private void Explode()
    {
        if (_exploded) return;
        _exploded = true;

        // Damage nearby players
        int count = Physics2D.OverlapCircleNonAlloc(
            transform.position, explosionRadius, _hits, playerMask);

        for (int i = 0; i < count; i++)
        {
            var stats = _hits[i].GetComponentInParent<PlayerStats>();
            if (stats != null && stats.playerAlive)
            {
                stats.TakeDamage(explosionDamage);
                stats.ApplyKnockback(transform.position, knockbackForce);
            }
        }

        // Particles
        if (explosionParticlePrefab != null)
            Instantiate(explosionParticlePrefab, transform.position, Quaternion.identity);

        SoundFXManager.instance?.PlaySoundByName("Explosion", transform, 0.8f, 1f);

        DOTween.Kill(transform);
        Destroy(gameObject);
    }
}
